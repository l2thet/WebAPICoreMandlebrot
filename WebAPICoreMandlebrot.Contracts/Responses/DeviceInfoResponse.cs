namespace WebAPICoreMandlebrot.Contracts.Responses;

public class DeviceInfoResponse
{
    public string? Error { get; set; }
    public bool HasCudaDevice { get; set; }
    public string? StatusMessage { get; set; }
    
    // Device information (when available)
    public string? AcceleratorType { get; set; }
    public string? Name { get; set; }
    public int MaxNumThreads { get; set; }
    public string? MaxGroupSize { get; set; }
    public int WarpSize { get; set; }
    public int NumMultiprocessors { get; set; }
}