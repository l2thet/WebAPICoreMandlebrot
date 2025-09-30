# WebAPI Core Mandelbrot

A .NET 8 Web API project with ILGPU CUDA acceleration for Mandelbrot set generation, designed to provide data for canvas-based web visualizations.

## Features

- .NET 8 Web API
- CUDA GPU computing with ILGPU
- Swagger/OpenAPI documentation  
- VS Code debugging support
- Mandelbrot set generation for canvas rendering
- Error handling for systems without CUDA
- Standardized JSON response format

## Prerequisites

- .NET 8 SDK
- NVIDIA GPU with CUDA support (required for computation)
- Visual Studio Code with C# Dev Kit extension

## Getting Started

### 1. Restore Dependencies
```bash
dotnet restore
```

### 2. Build the Project
```bash
dotnet build
```

### 3. Run the Project
```bash
dotnet run
```

The application will be available at:
- Frontend: https://localhost:7000 (Interactive Mandelbrot visualization)
- Swagger UI: https://localhost:7000/swagger (API documentation)

## API Endpoints

### Mandelbrot Controller (`/api/mandelbrot`)

#### `GET /api/mandelbrot/generate`
Generate complete Mandelbrot set data for canvas rendering
- **Query parameters:** `width` (default: 800), `height` (default: 600), `maxIterations` (default: 100)
- **Returns:** Standardized JSON response with success/error status

#### `GET /api/mandelbrot/point`  
Compute single point in Mandelbrot set
- **Query parameters:** `real` (default: -0.5), `imaginary` (default: 0.0), `maxIterations` (default: 100)
- **Returns:** Point computation result with success/error status

#### `GET /api/mandelbrot/device`
Get CUDA device information and availability
- **Returns:** CUDA device details or error message if not available

## Response Format

All endpoints return a standardized response format:

### Successful Generation Response:
```json
{
  "success": true,
  "width": 800,
  "height": 600,
  "maxIterations": 100,
  "data": [0, 1, 2, 5, 100, 15, ...], // One value per pixel (width × height array)
  "computeTimeMs": 5,
  "acceleratorType": "Cuda",
  "acceleratorName": "NVIDIA GeForce RTX 5070"
}
```

### Error Response (No CUDA):
```json
{
  "success": false,
  "error": "NVIDIA CUDA device not available",
  "width": 800,
  "height": 600,
  "maxIterations": 100
}
```

## Frontend Interface

The project includes a web-based frontend at the root URL (`https://localhost:7000`):

### Features
- Interactive canvas visualization of Mandelbrot set
- Adjustable parameters (width, height, max iterations)
- Real-time CUDA device detection and status
- Error handling with toast notifications
- Performance monitoring (GPU vs total time)

### Canvas Integration

For custom implementations, the API is designed for HTML5 canvas integration:

1. Check response `success` field before processing data  
2. Handle error cases gracefully (show message to user)
3. Map iteration values to colors in your JavaScript
4. Draw pixels directly to canvas using ImageData

## Development

### Debugging in VS Code

1. Press `F5` or use the "Run and Debug" panel
2. Select ".NET Core Launch (web)" configuration
3. The project will build automatically and launch with the debugger attached

### Building

- **Build**: `Ctrl+Shift+P` → "Tasks: Run Task" → "build"
- **Watch**: `Ctrl+Shift+P` → "Tasks: Run Task" → "watch" (for hot reload during development)

## CUDA Architecture

This project uses CUDA for GPU computation:

### CUDA Requirements  
- NVIDIA GPU with CUDA support required for computation
- Error handling when CUDA is unavailable

### ILGPU Integration
- MandelbrotKernel: GPU kernel for parallel Mandelbrot computation
- SinglePointKernel: Individual point calculation
- Pre-compilation: Kernels compiled at startup
- Memory Management: GPU buffer allocation and cleanup

### Performance
- GPU computation time: ~5ms for 800x600 images
- Data format compatible with canvas ImageData
- Development server with automatic rebuild

## Project Structure

```
WebAPICoreMandlebrot/
├── Controllers/
│   └── MandelbrotController.cs    # Mandelbrot generation endpoints for canvas rendering
├── Services/
│   ├── ILGPUAcceleratorService.cs # CUDA accelerator service with status information
│   └── ILGPUAcceleratorFactory.cs # Factory for creating ILGPU services
├── Properties/
│   └── launchSettings.json        # Launch profiles
├── wwwroot/
│   └── index.html                 # Frontend interface with canvas visualization
├── Program.cs                    # Application entry point with dependency injection
├── WebAPICoreMandlebrot.csproj   # Project file with ILGPU packages
├── appsettings.json              # Application settings
├── .gitignore                    # Git ignore file for .NET projects
└── README.md                     # This file
```

## Device Compatibility

### Supported NVIDIA GPUs
- RTX series (RTX 40xx, RTX 30xx, RTX 20xx)
- GTX series (GTX 16xx, GTX 10xx and newer)
- Compute Capability 3.5 or higher

### Troubleshooting CUDA Issues
If you encounter CUDA-related errors:
1. **Update** NVIDIA drivers to latest version
2. **Verify** CUDA installation: `nvidia-smi` command
3. **Check** device compatibility at `/api/mandelbrot/device`
4. **Review** PTX compilation errors in console output

## Next Steps

1. **Run the application**: `dotnet run` or press F5 in VS Code
2. **Open the frontend**: Navigate to `https://localhost:7000` 
3. **Adjust parameters**: Use the controls to change width, height, and iterations
4. **Monitor performance**: Check GPU compute times and device status
5. **Test API directly**: Use Swagger UI at `https://localhost:7000/swagger`

## Canvas Integration Tips

- Check the `success` field before processing data
- Handle errors by showing appropriate messages
- Use iteration data directly with `ImageData` for canvas rendering
- Map iteration counts to RGB values for coloring
- Consider the `point` endpoint for mouse hover effects showing coordinates

## Future Features

- Region analysis endpoint with GPU-accelerated sampling for interactive zoom and exploration
- Adaptive quality rendering based on complexity metrics
- Real-time analysis for mouse hover and zoom previews

## Notes

- CUDA-only architecture for GPU computation
- ILGPU Context and CUDA Accelerator are registered as singleton services  
- Error handling when CUDA is unavailable (no CPU fallback)
- All endpoints return standardized JSON responses for web consumption
- Project focused on Mandelbrot visualization