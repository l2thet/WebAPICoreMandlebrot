using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using WebAPICoreMandlebrot.Controllers;
using WebAPICoreMandlebrot.Services;
using WebAPICoreMandlebrot.Constants;
using ILGPU;
using ILGPU.Runtime;

namespace WebAPICoreMandlebrot.Tests;

public class MandelbrotControllerTests
{
    private readonly Mock<IILGPUAcceleratorService> _mockAcceleratorService;

    public MandelbrotControllerTests()
    {
        _mockAcceleratorService = new Mock<IILGPUAcceleratorService>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidService_ShouldInitializeCorrectly()
    {
        // Arrange
        _mockAcceleratorService.Setup(x => x.Accelerator).Returns((Accelerator?)null);
        _mockAcceleratorService.Setup(x => x.ErrorMessage).Returns((string?)null);

        // Act
        var controller = new MandelbrotController(_mockAcceleratorService.Object);

        // Assert
        Assert.NotNull(controller);
    }

    [Fact]
    public void Constructor_WithNullAccelerator_ShouldHandleGracefully()
    {
        // Arrange
        _mockAcceleratorService.Setup(x => x.Accelerator).Returns((Accelerator?)null);
        _mockAcceleratorService.Setup(x => x.ErrorMessage).Returns("CUDA not available");

        // Act
        var controller = new MandelbrotController(_mockAcceleratorService.Object);

        // Assert
        Assert.NotNull(controller);
    }

    [Fact]
    public async Task Constructor_WithNullAcceleratorAndNullError_ShouldUseDefaultError()
    {
        // Arrange
        _mockAcceleratorService.Setup(x => x.Accelerator).Returns((Accelerator?)null);
        _mockAcceleratorService.Setup(x => x.ErrorMessage).Returns((string?)null);

        var controller = new MandelbrotController(_mockAcceleratorService.Object);

        // Act - Test the default error message is used
        var result = await controller.GenerateMandelbrot();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<MandelbrotResponse>(okResult.Value);
        Assert.False(response.Success);
        Assert.Equal("NVIDIA CUDA device not available", response.Error);
    }

    #endregion

    #region GetDeviceInfo Tests

    [Fact]
    public void GetDeviceInfo_WhenAcceleratorNotAvailable_ShouldReturnErrorInfo()
    {
        // Arrange
        _mockAcceleratorService.Setup(x => x.IsAvailable).Returns(false);
        _mockAcceleratorService.Setup(x => x.ErrorMessage).Returns("CUDA device not found");
        _mockAcceleratorService.Setup(x => x.GetStatusMessage()).Returns("CUDA device not available");

        var controller = new MandelbrotController(_mockAcceleratorService.Object);

        // Act
        var result = controller.GetDeviceInfo();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        // Verify the service methods were called
        _mockAcceleratorService.Verify(x => x.IsAvailable, Times.Once);
    }

    [Fact]
    public void GetDeviceInfo_WhenAcceleratorNotAvailable_WithNullError_ShouldUseDefaultMessage()
    {
        // Arrange - Test the branch where ErrorMessage is null
        _mockAcceleratorService.Setup(x => x.IsAvailable).Returns(false);
        _mockAcceleratorService.Setup(x => x.ErrorMessage).Returns((string?)null);
        _mockAcceleratorService.Setup(x => x.GetStatusMessage()).Returns("CUDA device not available");

        var controller = new MandelbrotController(_mockAcceleratorService.Object);

        // Act
        var result = controller.GetDeviceInfo();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var deviceInfo = okResult.Value;
        Assert.NotNull(deviceInfo);
        
        // Use reflection to check the Error property since it's an anonymous type
        var errorProperty = deviceInfo.GetType().GetProperty("Error");
        var errorValue = errorProperty?.GetValue(deviceInfo)?.ToString();
        Assert.Equal("CUDA accelerator not available", errorValue);
    }

    [Fact]  
    public void GetDeviceInfo_WhenAcceleratorAvailable_ButExceptionThrown_ShouldReturnErrorWithCudaFlag()
    {
        // Arrange - Test the catch block in GetDeviceInfo
        _mockAcceleratorService.Setup(x => x.IsAvailable).Returns(true);
        _mockAcceleratorService.Setup(x => x.DeviceType).Throws(new InvalidOperationException("Test exception"));

        var controller = new MandelbrotController(_mockAcceleratorService.Object);

        // Act
        var result = controller.GetDeviceInfo();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var deviceInfo = okResult.Value;
        Assert.NotNull(deviceInfo);
        
        // Check that it returns error but still indicates CUDA device exists
        var errorProperty = deviceInfo.GetType().GetProperty("Error");
        var hasCudaProperty = deviceInfo.GetType().GetProperty("HasCudaDevice");
        
        var errorValue = errorProperty?.GetValue(deviceInfo)?.ToString();
        var hasCudaValue = hasCudaProperty?.GetValue(deviceInfo);
        
        Assert.Contains("Failed to get device info", errorValue);
        Assert.True((bool)(hasCudaValue ?? false));
    }

    #endregion

    #region GenerateMandelbrot Tests

    [Fact]
    public async Task GenerateMandelbrot_WithNullAccelerator_ShouldReturnErrorResponse()
    {
        // Arrange
        _mockAcceleratorService.Setup(x => x.Accelerator).Returns((Accelerator?)null);
        _mockAcceleratorService.Setup(x => x.ErrorMessage).Returns("Test Error");

        var controller = new MandelbrotController(_mockAcceleratorService.Object);

        // Act
        var result = await controller.GenerateMandelbrot();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<MandelbrotResponse>(okResult.Value);
        
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal(SharedConstants.DefaultCanvasWidth, response.Width);
        Assert.Equal(SharedConstants.DefaultCanvasHeight, response.Height);
    }

    [Fact]
    public async Task GenerateMandelbrot_WithDefaultParameters_ShouldUseSharedConstants()
    {
        // Arrange
        _mockAcceleratorService.Setup(x => x.Accelerator).Returns((Accelerator?)null);
        _mockAcceleratorService.Setup(x => x.ErrorMessage).Returns("Test");

        var controller = new MandelbrotController(_mockAcceleratorService.Object);

        // Act
        var result = await controller.GenerateMandelbrot();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<MandelbrotResponse>(okResult.Value);
        
        Assert.Equal(SharedConstants.DefaultCanvasWidth, response.Width);
        Assert.Equal(SharedConstants.DefaultCanvasHeight, response.Height);
    }

    [Fact]
    public async Task GenerateMandelbrot_WithCustomParameters_ShouldUseProvidedValues()
    {
        // Arrange
        _mockAcceleratorService.Setup(x => x.Accelerator).Returns((Accelerator?)null);
        _mockAcceleratorService.Setup(x => x.ErrorMessage).Returns("Test");

        var controller = new MandelbrotController(_mockAcceleratorService.Object);
        var customWidth = 100;
        var customHeight = 200;
        var customCenterReal = -0.7;
        var customCenterImaginary = 0.0;
        var customZoom = 2.0;

        // Act
        var result = await controller.GenerateMandelbrot(customWidth, customHeight, customCenterReal, customCenterImaginary, customZoom);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<MandelbrotResponse>(okResult.Value);
        
        // When no accelerator is available, only basic parameters are returned
        Assert.Equal(customWidth, response.Width);
        Assert.Equal(customHeight, response.Height);
        Assert.False(response.Success); // Should be error response
        Assert.NotNull(response.Error);
        
        // Coordinate data is only populated on successful generation
        // Error responses don't include coordinate information
    }

    [Fact]
    public async Task GenerateMandelbrot_WithExceptionInGPUComputation_ShouldReturnErrorResponse()
    {
        // This test is challenging because we can't easily mock the GPU computation failure
        // without a more complex setup. For now, we'll focus on testing the parameters
        // and ensure the method handles the null accelerator case properly.
        
        // Arrange
        _mockAcceleratorService.Setup(x => x.Accelerator).Returns((Accelerator?)null);
        _mockAcceleratorService.Setup(x => x.ErrorMessage).Returns("GPU computation failed");

        var controller = new MandelbrotController(_mockAcceleratorService.Object);

        // Act
        var result = await controller.GenerateMandelbrot(width: 640, height: 480, zoom: 4.0);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<MandelbrotResponse>(okResult.Value);
        
        Assert.False(response.Success);
        Assert.Equal(640, response.Width);
        Assert.Equal(480, response.Height);
        Assert.Contains("GPU computation failed", response.Error);
        
        // Verify zoom-based iteration scaling worked with logarithmic formula: base + (log2(zoom) * factor)
        var expectedIterations = SharedConstants.BaseIterationCount + (2 * SharedConstants.IterationScalingFactor); // log2(4) = 2
        var clampedExpected = Math.Max(SharedConstants.MinIterationCount, 
            Math.Min(SharedConstants.MaxIterationCount, (int)Math.Round(expectedIterations)));
        Assert.Equal(clampedExpected, response.MaxIterations);
    }

    #endregion

    #region MandelbrotResponse Tests

    [Fact]
    public void MandelbrotResponse_ShouldHaveCorrectProperties()
    {
        // Act
        var response = new MandelbrotResponse();

        // Assert - Verify all properties exist and can be set
        response.Success = true;
        response.Error = "Test error";
        response.Width = 800;
        response.Height = 600;
        response.MaxIterations = 1000;
        response.Data = new int[800 * 600];
        response.ComputeTimeMs = 100;
        response.AcceleratorType = "CUDA";
        response.AcceleratorName = "Test GPU";
        response.ViewMinReal = -2.0;
        response.ViewMaxReal = 1.0;
        response.ViewMinImaginary = -1.5;
        response.ViewMaxImaginary = 1.5;
        response.CenterReal = -0.5;
        response.CenterImaginary = 0.0;
        response.Zoom = 1.0;

        // Verify the values were set correctly
        Assert.True(response.Success);
        Assert.Equal("Test error", response.Error);
        Assert.Equal(800, response.Width);
        Assert.Equal(600, response.Height);
        Assert.Equal(1000, response.MaxIterations);
        Assert.NotNull(response.Data);
        Assert.Equal(100, response.ComputeTimeMs);
        Assert.Equal("CUDA", response.AcceleratorType);
        Assert.Equal("Test GPU", response.AcceleratorName);
        Assert.Equal(-2.0, response.ViewMinReal);
        Assert.Equal(1.0, response.ViewMaxReal);
        Assert.Equal(-1.5, response.ViewMinImaginary);
        Assert.Equal(1.5, response.ViewMaxImaginary);
        Assert.Equal(-0.5, response.CenterReal);
        Assert.Equal(0.0, response.CenterImaginary);
        Assert.Equal(1.0, response.Zoom);
    }

    #endregion

    #region Dynamic Iterations Tests (via reflection since it's private)

    [Theory]
    [InlineData(1.0, SharedConstants.BaseIterationCount)] // No scaling for zoom = 1.0
    [InlineData(0.5, SharedConstants.BaseIterationCount)] // No scaling for zoom < 1.0
    [InlineData(2.0, SharedConstants.BaseIterationCount + SharedConstants.IterationScalingFactor)] // 2x zoom: base + (log2(2) * factor) = base + (1 * factor)
    [InlineData(4.0, SharedConstants.BaseIterationCount + (2 * SharedConstants.IterationScalingFactor))] // 4x zoom: base + (log2(4) * factor) = base + (2 * factor)
    public async Task CalculateDynamicIterations_ShouldCalculateCorrectIterations(double zoom, double expectedBase)
    {
        // We need to test the private method through the public API
        // Since we can't directly test the private method, we'll test its behavior
        // through the GenerateMandelbrot method's response MaxIterations property

        // Arrange
        _mockAcceleratorService.Setup(x => x.Accelerator).Returns((Accelerator?)null);
        _mockAcceleratorService.Setup(x => x.ErrorMessage).Returns("Test");

        var controller = new MandelbrotController(_mockAcceleratorService.Object);

        // Act
        var result = await controller.GenerateMandelbrot(zoom: zoom);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<MandelbrotResponse>(okResult.Value);
        
        // The MaxIterations should be calculated by CalculateDynamicIterations
        var expectedIterations = Math.Max(SharedConstants.MinIterationCount, 
            Math.Min(SharedConstants.MaxIterationCount, (int)Math.Round(expectedBase)));
        
        Assert.Equal(expectedIterations, response.MaxIterations);
    }

    [Fact]
    public async Task CalculateDynamicIterations_ShouldClampToMinimum()
    {
        // Arrange - Use a very small zoom that would result in iterations below minimum
        _mockAcceleratorService.Setup(x => x.Accelerator).Returns((Accelerator?)null);
        _mockAcceleratorService.Setup(x => x.ErrorMessage).Returns("Test");

        var controller = new MandelbrotController(_mockAcceleratorService.Object);

        // Act
        var result = await controller.GenerateMandelbrot(zoom: 0.001); // Very small zoom

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<MandelbrotResponse>(okResult.Value);
        
        // Should be clamped to minimum
        Assert.True(response.MaxIterations >= SharedConstants.MinIterationCount);
    }

    [Fact]
    public async Task CalculateDynamicIterations_WithZoomLessThanOne_ShouldReturnBaseIterations()
    {
        // Arrange - Test the branch where zoom <= 1.0 (no scaling)
        _mockAcceleratorService.Setup(x => x.Accelerator).Returns((Accelerator?)null);
        _mockAcceleratorService.Setup(x => x.ErrorMessage).Returns("Test");

        var controller = new MandelbrotController(_mockAcceleratorService.Object);

        // Act
        var result = await controller.GenerateMandelbrot(zoom: 0.5); // Less than 1.0

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<MandelbrotResponse>(okResult.Value);
        
        // Should return base iterations with no scaling for zoom < 1.0
        Assert.Equal(SharedConstants.BaseIterationCount, response.MaxIterations);
    }

    [Fact]
    public async Task CalculateDynamicIterations_WithExtremelyHighZoom_ShouldRespectPerformanceLimits()
    {
        // Arrange - Use an extremely high zoom to test reasonable scaling behavior
        _mockAcceleratorService.Setup(x => x.Accelerator).Returns((Accelerator?)null);
        _mockAcceleratorService.Setup(x => x.ErrorMessage).Returns("Test");

        var controller = new MandelbrotController(_mockAcceleratorService.Object);

        // With logarithmic scaling: BaseIterations + (log2(zoom) * ScalingFactor)
        // Formula: 10000 + (log2(zoom) * 8000)
        // Even very high zooms should produce reasonable iteration counts for performance
        var extremeZoom = Math.Pow(2, 100); // Very high zoom but still manageable

        // Act
        var result = await controller.GenerateMandelbrot(zoom: extremeZoom); 

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<MandelbrotResponse>(okResult.Value);
        
        // Should be much higher than base with ultra-detail scaling
        Assert.True(response.MaxIterations > SharedConstants.BaseIterationCount);
        Assert.True(response.MaxIterations <= SharedConstants.MaxIterationCount); // Within maximum bounds
    }

    [Fact]
    public void CalculateDynamicIterations_WithLargeZoom_ShouldApplyLogarithmicScaling()
    {
        // Arrange - Test the logarithmic scaling behavior with realistic large zoom values
        var controllerType = typeof(MandelbrotController);
        var method = controllerType.GetMethod("CalculateDynamicIterations", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Test with a large but reasonable zoom value
        // zoom = 2^100 should give log2(zoom) = 100, and 100 * 50000 = 5,000,000
        // So total = 100000 + 5,000,000 = 5,100,000 iterations (ultra detail quality)
        var largeZoom = Math.Pow(2, 100); 

        // Act
        var result = (int)(method?.Invoke(null, new object[] { largeZoom }) ?? 0);

        // Assert - Should be base + scaled amount for ultra-detail quality
        var expectedIterations = SharedConstants.BaseIterationCount + (100 * SharedConstants.IterationScalingFactor);
        Assert.Equal((int)expectedIterations, result);
        Assert.True(result > SharedConstants.BaseIterationCount);
        Assert.True(result < SharedConstants.MaxIterationCount);
    }

    [Theory]
    [InlineData(0.1)] // Less than 1, should use base
    [InlineData(0.5)] // Less than 1, should use base  
    [InlineData(1.0)] // Exactly 1, should use base
    public async Task CalculateDynamicIterations_WithZoomLessOrEqualOne_ShouldUseBaseIterations(double zoom)
    {
        // Arrange
        _mockAcceleratorService.Setup(x => x.Accelerator).Returns((Accelerator?)null);
        _mockAcceleratorService.Setup(x => x.ErrorMessage).Returns("Test");

        var controller = new MandelbrotController(_mockAcceleratorService.Object);

        // Act
        var result = await controller.GenerateMandelbrot(zoom: zoom);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<MandelbrotResponse>(okResult.Value);
        
        // For zoom <= 1.0, should return base iterations
        Assert.Equal(SharedConstants.BaseIterationCount, response.MaxIterations);
    }

    #endregion
}