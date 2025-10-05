using ILGPU;
using ILGPU.Runtime;

namespace WebAPICoreMandelbrot.Services;

/// <summary>
/// Factory interface for creating ILGPU accelerator services
/// </summary>
public interface IILGPUAcceleratorFactory
{
    /// <summary>
    /// Creates an ILGPU accelerator service with CUDA detection
    /// </summary>
    ILGPUAcceleratorService CreateAcceleratorService();
}

/// <summary>
/// Factory implementation for creating ILGPU CUDA accelerator services
/// </summary>
public class ILGPUAcceleratorFactory : IILGPUAcceleratorFactory
{
    private readonly Context _context;

    public ILGPUAcceleratorFactory(Context context)
    {
        _context = context;
    }

    public ILGPUAcceleratorService CreateAcceleratorService()
    {
        Accelerator? accelerator = null;
        string? errorMessage = null;

        try
        {
            // Search for CUDA devices only
            foreach (var device in _context)
            {
                if (device.AcceleratorType == AcceleratorType.Cuda)
                {
                    try
                    {
                        Console.WriteLine($"Found NVIDIA CUDA device: {device.Name}");
                        accelerator = device.CreateAccelerator(_context);
                        Console.WriteLine($"Successfully initialized CUDA accelerator: {device.Name}");
                        break; // Success - exit loop
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"CUDA device initialization failed: {ex.Message}");
                        errorMessage = $"NVIDIA CUDA device found but failed to initialize: {ex.Message}";
                        break; // Failed - don't try more devices
                    }
                }
            }

            // If no CUDA device found
            if (accelerator == null && string.IsNullOrEmpty(errorMessage))
            {
                Console.WriteLine("No NVIDIA CUDA device detected");
                errorMessage = "No NVIDIA CUDA device detected. GPU acceleration is required for Mandelbrot computation.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during CUDA device detection: {ex.Message}");
            errorMessage = $"Error during CUDA device detection: {ex.Message}";
        }

        return new ILGPUAcceleratorService(accelerator, errorMessage);
    }
}