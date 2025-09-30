using ILGPU.Runtime;

namespace WebAPICoreMandlebrot.Services;

/// <summary>
/// Service that provides access to ILGPU CUDA accelerator and error information
/// </summary>
public class ILGPUAcceleratorService
{
    public Accelerator? Accelerator { get; }
    public string? ErrorMessage { get; }
    public bool IsAvailable => Accelerator != null;
    public string DeviceName => Accelerator?.Name ?? "No Device";
    public string DeviceType => Accelerator?.AcceleratorType.ToString() ?? "None";

    public ILGPUAcceleratorService(Accelerator? accelerator, string? errorMessage = null)
    {
        Accelerator = accelerator;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets a user-friendly status message about CUDA availability
    /// </summary>
    public string GetStatusMessage()
    {
        if (IsAvailable)
        {
            return $"CUDA device available: {DeviceName}";
        }
        
        return ErrorMessage ?? "CUDA device not available";
    }
}