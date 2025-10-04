using System;

namespace WebAPICoreMandlebrot.Constants;

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
    
    [ExportToTypeScript(Comment = "Default maximum iterations for computation")]
    public const int DefaultMaxIterations = 100;
    
    [ExportToTypeScript(Comment = "Extended maximum iterations for detailed computation")]
    public const int DefaultMaxIterationsExtended = 1000;
    
    [ExportToTypeScript(Comment = "Base iteration count for zoom scaling calculations")]
    public const int BaseIterationCount = 10000;
    
    [ExportToTypeScript(Comment = "Iteration scaling factor per zoom level (iterations = base * log(zoom) * factor)")]
    public const double IterationScalingFactor = 8000.0;
    
    [ExportToTypeScript(Comment = "Minimum iterations regardless of zoom level")]
    public const int MinIterationCount = 10000;
    
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
    
    [ExportToTypeScript(Comment = "Default canvas width in pixels")]
    public const int DefaultCanvasWidth = 3840;
    
    [ExportToTypeScript(Comment = "Default canvas height in pixels")]
    public const int DefaultCanvasHeight = 2160;
    
    #endregion
}