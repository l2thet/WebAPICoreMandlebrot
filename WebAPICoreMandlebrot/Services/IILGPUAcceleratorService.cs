using ILGPU.Runtime;

namespace WebAPICoreMandlebrot.Services;

/// <summary>
/// Interface for ILGPU CUDA accelerator service
/// </summary>
public interface IILGPUAcceleratorService
{
    Accelerator? Accelerator { get; }
    string? ErrorMessage { get; }
    bool IsAvailable { get; }
    string DeviceName { get; }
    string DeviceType { get; }
    
    string GetStatusMessage();
}