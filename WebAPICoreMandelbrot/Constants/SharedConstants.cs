using System;

namespace WebAPICoreMandelbrot.Constants;

/// <summary>
/// Attribute to mark constants that should be exported to TypeScript
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ExportToTypeScriptAttribute : Attribute
{
    public string? TypeScriptName { get; set; }
    public string? Comment { get; set; }
}

/// <summary>
/// Shared constants between C# backend and TypeScript frontend
/// Constants marked with [ExportToTypeScript] will be auto-generated in TypeScript
/// </summary>
public static class SharedConstants
{
    #region Mandelbrot Default Values
    
    [ExportToTypeScript(Comment = "Default center point for Mandelbrot set (real part)")]
    public const double DefaultCenterReal = -0.5;
    
    [ExportToTypeScript(Comment = "Default center point for Mandelbrot set (imaginary part)")]
    public const double DefaultCenterImaginary = 0.0;
    
    [ExportToTypeScript(Comment = "Default zoom level")]
    public const double DefaultZoom = 1.0;
    
    [ExportToTypeScript(Comment = "Minimum zoom level")]
    public const double MinZoom = 0.1;
    
    [ExportToTypeScript(Comment = "Maximum zoom level to prevent excessive computation")]
    public const double MaxZoom = 1000000.0;
    
    [ExportToTypeScript(Comment = "Default maximum iterations for computation")]
    public const int DefaultMaxIterations = 100;
    
    [ExportToTypeScript(Comment = "Extended maximum iterations for detailed computation")]
    public const int DefaultMaxIterationsExtended = 1000;
    
    [ExportToTypeScript(Comment = "Base iteration count for zoom scaling calculations - Ultra detail quality")]
    public const int BaseIterationCount = 100000;
    
    [ExportToTypeScript(Comment = "Iteration scaling factor per zoom level for ultra-detail (iterations = base + log2(zoom) * factor)")]
    public const double IterationScalingFactor = 50000.0;
    
    [ExportToTypeScript(Comment = "Minimum iterations regardless of zoom level - Ultra detail quality")]
    public const int MinIterationCount = 100000;
    
    [ExportToTypeScript(Comment = "Maximum iterations to prevent excessive computation")]
    public const int MaxIterationCount = 10000000;
    
    #endregion
    
    #region Viewport Configuration
    
    [ExportToTypeScript(Comment = "Default viewport width in complex plane units")]
    public const double DefaultViewportWidth = 3.5;
    
    [ExportToTypeScript(Comment = "Default viewport height in complex plane units")]
    public const double DefaultViewportHeight = 2.5;
    
    #endregion
    
    #region Complex Plane Bounds
    
    [ExportToTypeScript(Comment = "Minimum real value of the complex plane")]
    public const double ComplexPlaneMinReal = -2.5;
    
    [ExportToTypeScript(Comment = "Maximum real value of the complex plane")]
    public const double ComplexPlaneMaxReal = 1.0;
    
    [ExportToTypeScript(Comment = "Minimum imaginary value of the complex plane")]
    public const double ComplexPlaneMinImaginary = -1.25;
    
    [ExportToTypeScript(Comment = "Maximum imaginary value of the complex plane")]
    public const double ComplexPlaneMaxImaginary = 1.25;
    
    #endregion
    
    #region Rendering Defaults
    
    [ExportToTypeScript(Comment = "Optimal canvas width for consistent Mandelbrot visualization (4:3 aspect ratio)")]
    public const int DefaultCanvasWidth = 1024;
    
    [ExportToTypeScript(Comment = "Optimal canvas height for consistent Mandelbrot visualization (4:3 aspect ratio)")]
    public const int DefaultCanvasHeight = 768;
    
    #endregion
}