using Microsoft.AspNetCore.Mvc;
using ILGPU;
using ILGPU.Runtime;
using WebAPICoreMandlebrot.Services;

namespace WebAPICoreMandlebrot.Controllers;

public class MandelbrotResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int MaxIterations { get; set; }
    public int[]? Data { get; set; }
    public long? ComputeTimeMs { get; set; }
    public string? AcceleratorType { get; set; }
    public string? AcceleratorName { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class MandelbrotController : ControllerBase
{
    private readonly Context _context;
    private readonly ILGPUAcceleratorService _acceleratorService;
    private readonly Accelerator? _accelerator;
    private readonly string? _cudaError;
    private readonly Action<Index1D, ArrayView<int>, int, int, int>? _mandelbrotKernel;

    public MandelbrotController(Context context, ILGPUAcceleratorService acceleratorService)
    {
        _context = context;
        _acceleratorService = acceleratorService;
        _accelerator = acceleratorService.Accelerator;
        _cudaError = acceleratorService.ErrorMessage;
        
        // Pre-compile the kernel if accelerator is available
        if (_accelerator != null)
        {
            try
            {
                _mandelbrotKernel = _accelerator.LoadAutoGroupedStreamKernel<
                    Index1D, ArrayView<int>, int, int, int>(MandelbrotKernel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to pre-compile Mandelbrot kernel: {ex.Message}");
                _mandelbrotKernel = null; // Will compile on-demand
            }
        }
    }

    [HttpGet("generate")]
    public async Task<IActionResult> GenerateMandelbrot(
        [FromQuery] int width = 1280, 
        [FromQuery] int height = 1024,
        [FromQuery] int maxIterations = 1000)
    {
        // Check if CUDA is available
        if (_accelerator == null)
        {
            return Ok(new MandelbrotResponse
            {
                Success = false,
                Error = _cudaError ?? "NVIDIA CUDA device not available",
                Width = width,
                Height = height,
                MaxIterations = maxIterations
            });
        }

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await GenerateMandelbrotSet(width, height, maxIterations);
            stopwatch.Stop();

            return Ok(new MandelbrotResponse
            {
                Success = true,
                Width = width,
                Height = height,
                MaxIterations = maxIterations,
                Data = result,
                ComputeTimeMs = stopwatch.ElapsedMilliseconds,
                AcceleratorType = _accelerator.AcceleratorType.ToString(),
                AcceleratorName = _accelerator.Name
            });
        }
        catch (Exception ex)
        {
            return Ok(new MandelbrotResponse
            {
                Success = false,
                Error = $"GPU computation failed: {ex.Message}",
                Width = width,
                Height = height,
                MaxIterations = maxIterations
            });
        }
    }

    [HttpGet("device")]
    public IActionResult GetDeviceInfo()
    {
        if (!_acceleratorService.IsAvailable)
        {
            return Ok(new {
                Error = _acceleratorService.ErrorMessage ?? "CUDA accelerator not available",
                HasCudaDevice = false,
                StatusMessage = _acceleratorService.GetStatusMessage()
            });
        }

        try
        {
            return Ok(new {
                CurrentDevice = new {
                    AcceleratorType = _acceleratorService.DeviceType,
                    Name = _acceleratorService.DeviceName,
                    MaxNumThreads = _acceleratorService.Accelerator!.MaxNumThreads,
                    MaxGroupSize = _acceleratorService.Accelerator.MaxGroupSize.ToString(),
                    WarpSize = _acceleratorService.Accelerator.WarpSize,
                    NumMultiprocessors = _acceleratorService.Accelerator.NumMultiprocessors,
                    MemorySize = _acceleratorService.Accelerator.MemorySize,
                    MaxConstantMemory = _acceleratorService.Accelerator.MaxConstantMemory,
                    MaxSharedMemoryPerGroup = _acceleratorService.Accelerator.MaxSharedMemoryPerGroup
                },
                HasCudaDevice = true,
                KernelPrecompiled = _mandelbrotKernel != null
            });
        }
        catch (Exception ex)
        {
            return Ok(new { 
                Error = $"Failed to get device info: {ex.Message}",
                HasCudaDevice = true // We have accelerator but can't get details
            });
        }
    }

    [HttpGet("point")]
    public async Task<IActionResult> GetMandelbrotPoint(
        [FromQuery] double real = -0.5, 
        [FromQuery] double imaginary = 0.0, 
        [FromQuery] int maxIterations = 100)
    {
        if (_accelerator == null)
        {
            return Ok(new { 
                Error = _cudaError ?? "CUDA accelerator not available",
                Success = false 
            });
        }

        try
        {
            var result = await ComputeMandelbrotPoint(real, imaginary, maxIterations);
            return Ok(new { 
                Real = real, 
                Imaginary = imaginary, 
                Iterations = result,
                MaxIterations = maxIterations,
                Success = true
            });
        }
        catch (Exception ex)
        {
            return Ok(new { 
                Error = $"Failed to compute point: {ex.Message}",
                Success = false 
            });
        }
    }

    private async Task<int[]> GenerateMandelbrotSet(int width, int height, int maxIterations)
    {
        if (_accelerator == null)
        {
            throw new InvalidOperationException("CUDA accelerator not available");
        }

        return await Task.Run(() =>
        {
            // Create GPU buffer for results
            using var buffer = _accelerator.Allocate1D<int>(width * height);
            
            // Try to use pre-compiled kernel, or compile on-demand
            Action<Index1D, ArrayView<int>, int, int, int> kernel;
            if (_mandelbrotKernel != null)
            {
                kernel = _mandelbrotKernel;
            }
            else
            {
                // Compile kernel on-demand if pre-compilation failed
                kernel = _accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<int>, int, int, int>(MandelbrotKernel);
            }
            
            // Launch the kernel
            kernel((Index1D)(width * height), buffer.View, width, height, maxIterations);
            
            // Wait for GPU to complete
            _accelerator.Synchronize();
            
            // Get results
            var result = buffer.GetAsArray1D();
            return result;
        });
    }

    private async Task<int> ComputeMandelbrotPoint(double real, double imaginary, int maxIterations)
    {
        if (_accelerator == null)
        {
            throw new InvalidOperationException("CUDA accelerator not available");
        }

        return await Task.Run(() =>
        {
            // For single point computation, use GPU with 1x1 buffer
            using var buffer = _accelerator.Allocate1D<int>(1);
            using var realBuffer = _accelerator.Allocate1D<double>(1);
            using var imagBuffer = _accelerator.Allocate1D<double>(1);
            
            // Set input values
            realBuffer.CopyFromCPU(new double[] { real });
            imagBuffer.CopyFromCPU(new double[] { imaginary });
            
            // Load single point kernel
            var kernel = _accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<int>, ArrayView<double>, ArrayView<double>, int>(SinglePointKernel);
            
            // Launch kernel
            kernel((Index1D)1, buffer.View, realBuffer.View, imagBuffer.View, maxIterations);
            
            _accelerator.Synchronize();
            return buffer.GetAsArray1D()[0];
        });
    }

    /// <summary>
    /// ILGPU kernel for computing Mandelbrot set iterations
    /// Each thread computes one pixel of the output
    /// </summary>
    private static void MandelbrotKernel(Index1D index, ArrayView<int> output, int width, int height, int maxIterations)
    {
        // Convert linear index to 2D coordinates
        int x = index % width;
        int y = index / width;
        
        // Define the complex plane bounds
        double minReal = -2.5, maxReal = 1.0;
        double minImag = -1.25, maxImag = 1.25;
        
        // Convert pixel coordinates to complex plane
        double real = minReal + (double)x * (maxReal - minReal) / width;
        double imag = minImag + (double)y * (maxImag - minImag) / height;
        
        // Compute Mandelbrot iterations for this point
        double zReal = 0.0, zImag = 0.0;
        int iterations = 0;
        
        while (iterations < maxIterations)
        {
            double magnitude = zReal * zReal + zImag * zImag;
            if (magnitude > 4.0)
                break;
                
            double tempReal = zReal * zReal - zImag * zImag + real;
            zImag = 2.0 * zReal * zImag + imag;
            zReal = tempReal;
            iterations++;
        }
        
        output[index] = iterations;
    }

    /// <summary>
    /// ILGPU kernel for computing a single Mandelbrot point
    /// </summary>
    private static void SinglePointKernel(Index1D index, ArrayView<int> output, ArrayView<double> realInput, ArrayView<double> imagInput, int maxIterations)
    {
        double cReal = realInput[index];
        double cImag = imagInput[index];
        
        double zReal = 0.0, zImag = 0.0;
        int iterations = 0;
        
        while (iterations < maxIterations)
        {
            double magnitude = zReal * zReal + zImag * zImag;
            if (magnitude > 4.0)
                break;
                
            double tempReal = zReal * zReal - zImag * zImag + cReal;
            zImag = 2.0 * zReal * zImag + cImag;
            zReal = tempReal;
            iterations++;
        }
        
        output[index] = iterations;
    }
}