using Microsoft.AspNetCore.Mvc;
using ILGPU;
using ILGPU.Runtime;
using WebAPICoreMandelbrot.Services;
using WebAPICoreMandelbrot.Constants;
using WebAPICoreMandelbrot.Contracts.Responses;

namespace WebAPICoreMandelbrot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MandelbrotController : ControllerBase
{
    private readonly IILGPUAcceleratorService _acceleratorService;
    private readonly Accelerator? _accelerator;
    private readonly string? _cudaError;
    private readonly Action<Index1D, ArrayView<int>, int, int, int, double, double, double>? _mandelbrotKernel;

    public MandelbrotController(IILGPUAcceleratorService acceleratorService)
    {
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
        [FromQuery] double centerReal = SharedConstants.DefaultCenterReal,
        [FromQuery] double centerImaginary = SharedConstants.DefaultCenterImaginary,
        [FromQuery] double zoom = SharedConstants.DefaultZoom)
    {
        // Use constants for canvas dimensions
        int width = SharedConstants.DefaultCanvasWidth;
        int height = SharedConstants.DefaultCanvasHeight;
        
        // Validate and clamp zoom level to prevent excessive computation
        zoom = Math.Max(SharedConstants.MinZoom, Math.Min(SharedConstants.MaxZoom, zoom));
        
        // Calculate dynamic iteration count based on zoom level
        int maxIterations = CalculateDynamicIterations(zoom);
        
        // Check if CUDA is available
        if (_accelerator == null)
        {
            return Ok(new MandelbrotResponse
            {
                Success = false,
                Error = _cudaError ?? "NVIDIA CUDA device not available",
                MaxIterations = maxIterations
            });
        }

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await GenerateMandelbrotSet(width, height, centerReal, centerImaginary, zoom);
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
                MaxIterations = maxIterations
            });
        }
    }



    [HttpGet("device")]
    public IActionResult GetDeviceInfo()
    {
        try
        {
            if (!_acceleratorService.IsAvailable)
            {
                return Ok(new DeviceInfoResponse
                {
                    Error = _acceleratorService.ErrorMessage ?? "CUDA accelerator not available",
                    HasCudaDevice = false,
                    StatusMessage = _acceleratorService.GetStatusMessage()
                });
            }
        }
        catch (Exception ex)
        {
            return Ok(new DeviceInfoResponse
            {
                Error = $"Failed to check device availability: {ex.Message}",
                HasCudaDevice = false,
                StatusMessage = "Service error occurred"
            });
        }

        try
        {
            return Ok(new DeviceInfoResponse
            {
                HasCudaDevice = true,
                AcceleratorType = _acceleratorService.DeviceType,
                Name = _acceleratorService.DeviceName,
                MaxNumThreads = _acceleratorService.Accelerator!.MaxNumThreads,
                MaxGroupSize = _acceleratorService.Accelerator.MaxGroupSize.ToString(),
                WarpSize = _acceleratorService.Accelerator.WarpSize,
                NumMultiprocessors = _acceleratorService.Accelerator.NumMultiprocessors
            });
        }
        catch (Exception ex)
        {
            return Ok(new DeviceInfoResponse
            { 
                Error = $"Failed to get device info: {ex.Message}",
                HasCudaDevice = true // We have accelerator but can't get details
            });
        }
    }

    private static int CalculateDynamicIterations(double zoom)
    {
        // Scale iterations based on zoom level to maintain detail
        // Use more reasonable logarithmic scaling for better performance
        int scaledIterations = SharedConstants.BaseIterationCount;
        
        if (zoom > 1.0)
        {
            // Balanced scaling: BaseIterations + (log2(zoom) * ScalingFactor)
            // This provides detail without excessive computation
            double logZoom = Math.Log2(zoom);
            double moderateScale = logZoom * SharedConstants.IterationScalingFactor;
            scaledIterations = (int)Math.Round(SharedConstants.BaseIterationCount + moderateScale);
        }
        
        // Clamp to reasonable bounds
        return Math.Max(SharedConstants.MinIterationCount, Math.Min(SharedConstants.MaxIterationCount, scaledIterations));
    }

    private async Task<int[]> GenerateMandelbrotSet(int width, int height, double centerReal = SharedConstants.DefaultCenterReal, double centerImaginary = SharedConstants.DefaultCenterImaginary, double zoom = SharedConstants.DefaultZoom)
    {
        if (_accelerator == null)
        {
            throw new InvalidOperationException("CUDA accelerator not available");
        }

        // Calculate dynamic iteration count based on zoom level
        int maxIterations = CalculateDynamicIterations(zoom);

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



    /// <summary>
    /// Ultra-optimized ILGPU kernel for computing Mandelbrot set iterations
    /// Advanced GPU optimizations: cardioid/bulb detection, loop unrolling, constant optimization
    /// </summary>
    private static void MandelbrotKernel(Index1D index, ArrayView<int> output, int width, int height, int maxIterations, double centerReal, double centerImaginary, double zoom)
    {
        // Convert linear index to 2D coordinates
        int x = index % width;
        int y = index / width;
        
        // Pre-calculate constants to avoid repeated computation
        const double VIEWPORT_WIDTH = SharedConstants.DefaultViewportWidth;
        const double VIEWPORT_HEIGHT = SharedConstants.DefaultViewportHeight;
        const double ESCAPE_RADIUS_SQ = 4.0; // 2^2 for escape condition
        
        // Use reciprocal multiplication for better GPU performance  
        double invZoom = 1.0 / zoom;
        double viewWidth = VIEWPORT_WIDTH * invZoom;
        double viewHeight = VIEWPORT_HEIGHT * invZoom;
        
        // Pre-calculate view bounds
        double minReal = centerReal - viewWidth * 0.5;
        double minImag = centerImaginary - viewHeight * 0.5;
        
        // Direct coordinate calculation using optimized scaling
        double invWidth = viewWidth / width;
        double invHeight = viewHeight / height;
        
        // Convert pixel coordinates to complex plane (optimized)
        double real = minReal + x * invWidth;
        double imag = minImag + y * invHeight;
        
        // Temporarily removed early bailout optimizations to ensure correct basic Mandelbrot math
        // Will re-add after verifying core mathematics are correct
        
        // Standard Mandelbrot iteration (simplified for debugging)
        double zReal = 0.0, zImag = 0.0;
        int iterations = 0;
        
        // Simple, proven Mandelbrot loop
        while (iterations < maxIterations)
        {
            double zRealSq = zReal * zReal;
            double zImagSq = zImag * zImag;
            
            // Escape condition: |z|^2 > 4
            if (zRealSq + zImagSq > ESCAPE_RADIUS_SQ)
                break;
                
            // Classic Mandelbrot iteration: z = z^2 + c
            double newZReal = zRealSq - zImagSq + real;
            zImag = 2.0 * zReal * zImag + imag;
            zReal = newZReal;
            iterations++;
        }
        
        output[index] = iterations;
    }




}