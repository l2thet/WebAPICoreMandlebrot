using Microsoft.AspNetCore.Mvc;
using ILGPU;
using ILGPU.Runtime;
using WebAPICoreMandlebrot.Services;
using WebAPICoreMandlebrot.Constants;

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
    
    // Coordinate mapping data from CUDA calculations
    public double ViewMinReal { get; set; }
    public double ViewMaxReal { get; set; }
    public double ViewMinImaginary { get; set; }
    public double ViewMaxImaginary { get; set; }
    public double CenterReal { get; set; }
    public double CenterImaginary { get; set; }
    public double Zoom { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class MandelbrotController : ControllerBase
{
    private readonly Context _context;
    private readonly ILGPUAcceleratorService _acceleratorService;
    private readonly Accelerator? _accelerator;
    private readonly string? _cudaError;
    private readonly Action<Index1D, ArrayView<int>, int, int, int, double, double, double>? _mandelbrotKernel;

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
                    Index1D, ArrayView<int>, int, int, int, double, double, double>(MandelbrotKernel);
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
        [FromQuery] int width = SharedConstants.DefaultCanvasWidth, 
        [FromQuery] int height = SharedConstants.DefaultCanvasHeight,
        [FromQuery] double centerReal = SharedConstants.DefaultCenterReal,
        [FromQuery] double centerImaginary = SharedConstants.DefaultCenterImaginary,
        [FromQuery] double zoom = SharedConstants.DefaultZoom)
    {
        // Calculate dynamic iteration count based on zoom level
        int maxIterations = CalculateDynamicIterations(zoom);
        
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
            var result = await GenerateMandelbrotSet(width, height, maxIterations, centerReal, centerImaginary, zoom);
            stopwatch.Stop();

            // Calculate the coordinate bounds (same as CUDA kernel)
            double viewWidth = SharedConstants.DefaultViewportWidth / zoom;
            double viewHeight = SharedConstants.DefaultViewportHeight / zoom;
            double viewMinReal = centerReal - viewWidth / 2.0;
            double viewMaxReal = centerReal + viewWidth / 2.0;
            double viewMinImag = centerImaginary - viewHeight / 2.0;
            double viewMaxImag = centerImaginary + viewHeight / 2.0;

            return Ok(new MandelbrotResponse
            {
                Success = true,
                Width = width,
                Height = height,
                MaxIterations = maxIterations,
                Data = result,
                ComputeTimeMs = stopwatch.ElapsedMilliseconds,
                AcceleratorType = _accelerator.AcceleratorType.ToString(),
                AcceleratorName = _accelerator.Name,
                ViewMinReal = viewMinReal,
                ViewMaxReal = viewMaxReal,
                ViewMinImaginary = viewMinImag,
                ViewMaxImaginary = viewMaxImag,
                CenterReal = centerReal,
                CenterImaginary = centerImaginary,
                Zoom = zoom
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

    private static int CalculateDynamicIterations(double zoom)
    {
        // Scale iterations based on zoom level to maintain detail
        // Formula: iterations = BaseIterationCount + (log2(zoom) * IterationScalingFactor)
        int scaledIterations = SharedConstants.BaseIterationCount;
        
        if (zoom > 1.0)
        {
            // Only scale up for zoom > 1x
            double logZoom = Math.Log2(zoom);
            scaledIterations = (int)Math.Round(SharedConstants.BaseIterationCount + (logZoom * SharedConstants.IterationScalingFactor));
        }
        
        // Clamp to reasonable bounds
        return Math.Max(SharedConstants.MinIterationCount, Math.Min(SharedConstants.MaxIterationCount, scaledIterations));
    }

    private async Task<int[]> GenerateMandelbrotSet(int width, int height, int maxIterations, double centerReal = SharedConstants.DefaultCenterReal, double centerImaginary = SharedConstants.DefaultCenterImaginary, double zoom = SharedConstants.DefaultZoom)
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
            Action<Index1D, ArrayView<int>, int, int, int, double, double, double> kernel;
            if (_mandelbrotKernel != null)
            {
                kernel = _mandelbrotKernel;
            }
            else
            {
                // Compile kernel on-demand if pre-compilation failed
                kernel = _accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<int>, int, int, int, double, double, double>(MandelbrotKernel);
            }
            
            // Launch the kernel
            kernel((Index1D)(width * height), buffer.View, width, height, maxIterations, centerReal, centerImaginary, zoom);
            
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
    private static void MandelbrotKernel(Index1D index, ArrayView<int> output, int width, int height, int maxIterations, double centerReal, double centerImaginary, double zoom)
    {
        // Convert linear index to 2D coordinates
        int x = index % width;
        int y = index / width;
        
        // Calculate view bounds based on center and zoom
        // Default view dimensions from shared constants, scaled by zoom
        double viewWidth = SharedConstants.DefaultViewportWidth / zoom;
        double viewHeight = SharedConstants.DefaultViewportHeight / zoom;
        
        double minReal = centerReal - viewWidth / 2.0;
        double maxReal = centerReal + viewWidth / 2.0;
        double minImag = centerImaginary - viewHeight / 2.0;
        double maxImag = centerImaginary + viewHeight / 2.0;
        
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