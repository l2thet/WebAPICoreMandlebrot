namespace WebAPICoreMandelbrot.Contracts.Responses;

public class MandelbrotResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
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