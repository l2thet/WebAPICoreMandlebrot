# WebAPI Core Mandelbrot

A .NET 8 Web API project with ILGPU CUDA acceleration for real-time Mandelbrot set visualization with interactive web interface.

## Features

- **CUDA GPU Acceleration**: ILGPU-powered computation for high-performance rendering
- **Enhanced Dynamic Iteration Scaling**: Aggressive scaling from 10K to 10M iterations based on zoom level for maximum detail retention
- **Interactive Interface**: Click to zoom, right-click to reset with real-time feedback
- **Backend-Authoritative Math**: All coordinate calculations performed on GPU backend
- **Comprehensive Loading States**: Visual feedback during computation with loading overlays
- **SharedConstants System**: Auto-synced constants between C# and TypeScript
- **TypeScript Frontend**: Modern ES2020 modules with MSBuild integration
- **Streamlined UI**: Auto-generating visualization with minimal controls

## Prerequisites

### Backend Requirements
- .NET 8 SDK
- NVIDIA GPU with CUDA support (required for computation)

### Frontend Development Requirements
- **Node.js** (version 18.0.0 or higher) - Required for TypeScript compilation
- **npm** (version 9.0.0 or higher) - Comes with Node.js

> **Note:** Node.js is required for TypeScript compilation and frontend development. The TypeScript source files in `/src` are compiled to JavaScript in `/wwwroot/js`.

### Installing Node.js

#### Windows:
1. **Download from official site**: Go to [nodejs.org](https://nodejs.org/)
2. **Choose LTS version** (recommended): Download the "LTS" version (currently 20.x)
3. **Run installer**: Download and run the `.msi` installer
4. **Verify installation**: Open PowerShell and run:
   ```powershell
   node --version
   npm --version
   ```

#### Alternative - Using Chocolatey (if available):
```powershell
choco install nodejs
```

#### Alternative - Using winget:
```powershell
winget install OpenJS.NodeJS
```

#### Verification:
After installation, restart your terminal and verify:
```powershell
node --version    # Should show v18.x.x or higher
npm --version     # Should show 9.x.x or higher
```

## HTTPS Certificate Setup

The application uses HTTPS on `localhost:7000` and requires a development certificate to work properly. Follow these steps to set up the self-signed certificate:

### Option 1: Automatic Setup (Recommended)
```bash
# Trust the ASP.NET Core development certificate
dotnet dev-certs https --trust
```

### Option 2: Manual Certificate Management
If you encounter certificate issues or need to regenerate:

```bash
# Check existing certificates
dotnet dev-certs https --check

# Clean existing certificates (if needed)
dotnet dev-certs https --clean

# Generate new development certificate
dotnet dev-certs https

# Trust the certificate (Windows/macOS)
dotnet dev-certs https --trust

# For Linux - export certificate for manual installation
dotnet dev-certs https -ep ${HOME}/.aspnet/https/aspnetapp.pfx -p <password>
```

### Troubleshooting Certificate Issues

**Browser Shows "Not Secure" or Certificate Errors:**
1. Run `dotnet dev-certs https --trust` and restart your browser
2. For Chrome: Go to `chrome://settings/certificates` → Manage certificates → Import the localhost certificate
3. For Firefox: Accept the security exception when prompted

**PowerShell/API Testing Certificate Errors:**
```powershell
# Skip certificate validation for API testing only
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
```

**Production Deployment:**
For production deployment, replace the development certificate with a proper SSL certificate from a Certificate Authority.

## Getting Started

### 1. Setup HTTPS Certificate (First Time Only)
```bash
# Trust the development certificate for HTTPS
dotnet dev-certs https --trust
```

### 2. Install Frontend Dependencies
```bash
npm install
```

### 3. Restore .NET Dependencies
```bash
dotnet restore
```

### 4. Build TypeScript
```bash
npm run build
```

### 5. Build the .NET Project
```bash
dotnet build
```

### 6. Run the Project
```bash
dotnet run
```

The application will be available at:
- Frontend: https://localhost:7000 (Interactive Mandelbrot visualization)
- Swagger UI: https://localhost:7000/swagger (API documentation)

## Shared Constants System

This project uses an **MSBuild-integrated constants sharing system** that ensures consistency between C# backend and TypeScript frontend without requiring external tools.

### How It Works:
1. **C# Constants**: Define constants in `Constants/SharedConstants.cs` with `[ExportToTypeScript]` attribute
2. **Auto-Generation**: MSBuild runs a TypeScript script during compilation to extract constants  
3. **TypeScript Output**: Generates `src/shared-constants.ts` with matching TypeScript constants
4. **Import & Use**: Both C# and TypeScript use the same values automatically

### Example:
```csharp
// C# - Constants/SharedConstants.cs
[ExportToTypeScript(Comment = "Default center point")]
public const double DefaultCenterReal = -0.5;
```

```typescript
// TypeScript - auto-generated src/shared-constants.ts  
export const DefaultCenterReal = -0.5;
```

### Updating Constants:
1. Edit values in `Constants/SharedConstants.cs`
2. Add `[ExportToTypeScript]` attribute to new constants  
3. Run `npm run generate-constants` or `dotnet build` - TypeScript constants update automatically
4. Both frontend and backend use the new values

### Manual Generation:
```bash
# Generate constants manually
npm run generate-constants

# Or build TypeScript (includes constants generation)
npm run build
```

> **Note**: The `src/shared-constants.ts` file is auto-generated. Never edit it manually.

## TypeScript Development Setup

### 1. Install Node.js Dependencies
```bash
# Install TypeScript and development dependencies
npm install
```

### 2. TypeScript Compilation Options

#### Build once:
```bash
npm run build
```

#### Watch mode (recommended during development):
```bash
npm run watch
```

#### Clean compiled files:
```bash
npm run clean
```

### 3. Project Structure
```
src/                    # TypeScript source files
├── app.ts             # Main application logic
├── types.ts           # Type definitions
wwwroot/js/            # Compiled JavaScript output (generated)
├── app.js             # Compiled from src/app.ts
├── app.d.ts           # Type declarations
└── app.js.map         # Source maps
```

### 4. Development Workflow
1. Make changes to TypeScript files in `/src`
2. Run `npm run watch` to automatically compile on save
3. Refresh the browser to see changes
4. Use browser dev tools with source maps for debugging

> **Important:** Never edit files in `/wwwroot/js` directly - they are generated from TypeScript sources!

## API Endpoints

### Mandelbrot Controller (`/api/mandelbrot`)

#### `GET /api/mandelbrot/generate`
Generate complete Mandelbrot set data for canvas rendering
- **Query parameters:** `width` (default: 3840), `height` (default: 2160), `centerReal` (default: -0.5), `centerImaginary` (default: 0.0), `zoom` (default: 1.0)
- **Note:** `maxIterations` is automatically calculated based on zoom level (10K to 10M range)
- **Returns:** Standardized JSON response with success/error status and coordinate mapping data

#### `GET /api/mandelbrot/device`
Get CUDA device information and availability
- **Returns:** CUDA device details or error message if not available

## Response Format

All endpoints return a standardized response format:

### Successful Generation Response:
```json
{
  "success": true,
  "width": 3840,
  "height": 2160,
  "maxIterations": 15000,
  "data": [0, 1, 2, 5, 100, 15, ...], // One value per pixel (width × height array)
  "computeTimeMs": 5,
  "acceleratorType": "Cuda",
  "acceleratorName": "NVIDIA GeForce RTX 5070",
  "viewMinReal": -2.25,
  "viewMaxReal": 1.25,
  "viewMinImaginary": -1.25,
  "viewMaxImaginary": 1.25,
  "centerReal": -0.5,
  "centerImaginary": 0.0,
  "zoom": 1.0
}
```

### Error Response (No CUDA):
```json
{
  "success": false,
  "error": "NVIDIA CUDA device not available",
  "width": 3840,
  "height": 2160,
  "maxIterations": 10000
}
```

## Frontend Interface

The project includes a web-based frontend at the root URL (`https://localhost:7000`):

### Features
- **Streamlined Auto-Generation**: Click anywhere to zoom in, right-click to reset to default view
- **Enhanced Iteration Scaling**: Dynamic scaling from 10K to 10M iterations based on zoom depth
- **Comprehensive Loading States**: Visual feedback with loading overlays during computation
- **Real-time CUDA Detection**: GPU device status and performance monitoring
- **Backend-Authoritative Coordinates**: All mathematical calculations performed on GPU backend
- **Responsive Design**: Centered canvas display with proper scaling and viewport management

### Canvas Integration

For custom implementations, the API is designed for HTML5 canvas integration:

1. Check response `success` field before processing data  
2. Handle error cases gracefully (show message to user)
3. Map iteration values to colors in your JavaScript
4. Draw pixels directly to canvas using ImageData

## Development

### Building

```bash
# Build .NET project
dotnet build

# Build TypeScript
npm run build

# Watch mode for development
npm run watch
```

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
- GPU computation time: ~5ms for standard zoom levels, scales with iteration count
- Enhanced iteration scaling: 10K base iterations, up to 10M for deep zoom levels
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

### Troubleshooting HTTPS Issues

**Application won't start or shows HTTPS errors:**
```bash
# Reset and recreate development certificates
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

**Browser security warnings:**
- Click "Advanced" → "Proceed to localhost" for testing
- Add certificate exception in browser settings

**API calls fail with certificate errors:**
```powershell
# For PowerShell testing, bypass certificate validation
$PSDefaultParameterValues['Invoke-RestMethod:SkipCertificateCheck'] = $true
$PSDefaultParameterValues['Invoke-WebRequest:SkipCertificateCheck'] = $true
```

## Next Steps

1. **Run the application**: `dotnet run`
2. **Open the frontend**: Navigate to `https://localhost:7000`
3. **Interact with visualization**: Click to zoom, right-click to reset
4. **Monitor performance**: Check GPU compute times and device status
5. **Test API directly**: Use Swagger UI at `https://localhost:7000/swagger`

## Canvas Integration Tips

- Check the `success` field before processing data
- Handle errors by showing appropriate messages
- Use iteration data directly with `ImageData` for canvas rendering
- Map iteration counts to RGB values for coloring
- Focus on the main `generate` endpoint for full Mandelbrot set visualization
- Implement loading states for better user experience during computation

## Future Features

- Region analysis endpoint with GPU-accelerated sampling for interactive zoom and exploration
- Adaptive quality rendering based on complexity metrics
- Additional color schemes and visualization modes
- Export functionality for high-resolution images

## Notes

- CUDA-only architecture for GPU computation
- ILGPU Context and CUDA Accelerator are registered as singleton services  
- Error handling when CUDA is unavailable (no CPU fallback)
- All endpoints return standardized JSON responses for web consumption
- Project focused on Mandelbrot visualization