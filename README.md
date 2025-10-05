# WebAPI Core Mandelbrot

A .NET 8 Web API solution with ILGPU CUDA acceleration for real-time Mandelbrot set visualization with interactive web interface. This multi-project solution includes automatic TypeScript interface generation, shared constants synchronization, and clean architectural separation.

## Features

- **Ultra-Optimized CUDA GPU Acceleration**: Advanced ILGPU kernel with mathematical optimizations for maximum performance
- **Ultra-Detail Quality**: 100K+ base iterations scaling to 10M+ for high-quality visualization  
- **Optimized Canvas Resolution**: Fixed 1024×768 dimensions for consistent visualization quality and ~10x performance boost
- **Interactive Interface**: Click to zoom, right-click to reset with synchronized legend updates
- **Backend-Controlled Zoom Logic**: All zoom calculations and validation performed server-side
- **Multi-Project Architecture**: Clean separation with Contracts project for shared models and TypeScript generator project
- **Automatic TypeScript Generation**: C# response classes automatically converted to TypeScript interfaces using reflection
- **Solution-Wide Configuration**: Unified formatting, linting, and editor settings across all projects
- **VS Code Integration**: Comprehensive build tasks and workspace configuration
- **Automated Build Workflows**: npm scripts for complete clean-build automation
- **SharedConstants System**: Auto-synced constants between C# and TypeScript with MSBuild integration
- **TypeScript Frontend**: Modern ES2020 modules with optimized coordinate synchronization

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

> **Important**: This solution is designed to work from the **solution root** directory. All commands should be run from `WebAPICoreMandelbrotSolution/` unless otherwise specified.

### 1. Setup HTTPS Certificate (First Time Only)
```bash
# Navigate to the main project directory
cd WebAPICoreMandlebrot

# Trust the development certificate for HTTPS
dotnet dev-certs https --trust
```

### 2. Install Frontend Dependencies
```bash
# From the main project directory
cd WebAPICoreMandlebrot
npm install
```

### 3. Restore .NET Dependencies (All Projects)
```bash
# From solution root - restores all projects
dotnet restore
```

### 4. Automated Workflow (Recommended)
```bash
# From the main project directory
cd WebAPICoreMandlebrot

# Complete clean-build workflow
npm run build:full

# Then start the server (from solution root or project directory)
dotnet run --project WebAPICoreMandlebrot
```

### 5. Manual Steps (Alternative)
```bash
# From the main project directory
cd WebAPICoreMandlebrot

# Build TypeScript
npm run build

# From solution root - build all projects
cd ..
dotnet build

# Run the main project
dotnet run --project WebAPICoreMandlebrot
```

### 6. VS Code Development
```bash
# Open the solution in VS Code from the root
code .

# All build tasks are available via Ctrl+Shift+P → "Tasks: Run Task"
# Use the integrated terminal to run dotnet commands
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

## Automatic TypeScript Interface Generation

This project features **automatic TypeScript interface generation** from C# response classes using reflection and MSBuild integration. This ensures type safety between backend and frontend without manual synchronization.

### How It Works:
1. **C# Response Classes**: Define API response models in `WebAPICoreMandelbrot.Contracts/Responses/`
2. **Reflection-Based Generator**: The `WebAPICoreMandelbrot.TypeScriptGenerator` project uses `System.Reflection` to analyze C# classes
3. **MSBuild Integration**: TypeScript interfaces are automatically generated during every build
4. **Prettier-Compliant Output**: Generator produces properly formatted code that passes ESLint and Prettier checks automatically
5. **Type-Safe Frontend**: Import and use strongly-typed interfaces in your TypeScript code

### Generated Interfaces:
```typescript
// auto-generated src/response-interfaces.ts
export interface MandelbrotResponse {
    acceleratorName?: string;
    acceleratorType?: string;
    centerImaginary: number;
    centerReal: number;
    computeTimeMs?: number;
    data?: number[];
    error?: string;
    maxIterations: number;
    success: boolean;
    viewMaxImaginary: number;
    viewMaxReal: number;
    viewMinImaginary: number;
    viewMinReal: number;
    zoom: number;
}

export interface DeviceInfoResponse {
    acceleratorType?: string;
    error?: string;
    hasCudaDevice: boolean;
    maxGroupSize?: string;
    maxNumThreads: number;
    name?: string;
    numMultiprocessors: number;
    statusMessage?: string;
    warpSize: number;
}
```

### Usage in TypeScript:
```typescript
import { MandelbrotResponse, DeviceInfoResponse } from './response-interfaces.js';
import { apiGet } from './api-client.js';

// Type-safe API calls
const mandelbrot = await apiGet<MandelbrotResponse>('/api/mandelbrot/generate');
const deviceInfo = await apiGet<DeviceInfoResponse>('/api/mandelbrot/device-info');
```

### Manual Generation:
```bash
# Generate interfaces manually (happens automatically during build)
dotnet build

# Or build the TypeScript generator project specifically
dotnet build WebAPICoreMandelbrot.TypeScriptGenerator
```

### Adding New Response Classes:
1. Create a new response class in `WebAPICoreMandelbrot.Contracts/Responses/`
2. Add it to the generator in `TypeScriptGenerator.cs` (in `GenerateTypeScriptInterfaces()` method)
3. Run `dotnet build` - TypeScript interface will be generated automatically
4. Import and use the new interface in your TypeScript code

> **Note**: The `src/response-interfaces.ts` file is auto-generated. Never edit it manually.

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

## Build Automation

### Automated Build Workflows
```bash
# Complete workflows
npm run build:full     # Clean → Restore → Build TypeScript + .NET

# Individual steps
npm run dotnet:clean   # Clean .NET solution
npm run dotnet:build   # Build .NET project
npm run dotnet:test    # Run .NET unit tests
npm run build          # Build TypeScript with linting
npm run lint:fix       # Fix TypeScript formatting issues
```

### MSBuild Integration
```bash
# MSBuild automatically generates TypeScript interfaces during build
dotnet build  # Triggers TypeScript generation + constants + compilation
```

## API Endpoints

### Mandelbrot Controller (`/api/mandelbrot`)

#### `GET /api/mandelbrot/generate`
Generate complete Mandelbrot set data for canvas rendering
- **Query parameters:** `centerReal` (default: -0.5), `centerImaginary` (default: 0.0), `zoom` (default: 1.0)
- **Note:** Canvas dimensions are fixed at 1024×768 pixels, `maxIterations` is automatically calculated based on zoom level (100K to 10M range)
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
  "maxIterations": 15000,
  "data": [0, 1, 2, 5, 100, 15, ...], // One value per pixel (1024 × 768 array)
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
- **Fixed Canvas Dimensions**: Optimized 1024×768 (4:3 aspect ratio) for consistent quality across all devices

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

### Ultra-Optimized GPU Performance
- **Advanced Mathematical Optimizations**: Simplified kernel with verified Mandelbrot mathematics
- **Ultra-Detail Quality**: 100,000 base iterations scaling to 5M+ for deep zooms
- **Logarithmic Scaling**: `iterations = 100K + (log2(zoom) * 50K)` for balanced performance
- **Backend Zoom Control**: All zoom validation and calculation server-side (0.1x to 1M zoom range)
- **GPU Computation Time**: ~2-10ms for ultra-detail quality depending on zoom level
- **Memory Efficiency**: Optimized coordinate calculations and GPU buffer management
- **Synchronized Updates**: All legend values (zoom, iterations, time, coordinates) update together

## Project Structure

> **Repository Structure**: Git repository tracks from the solution root level. All projects are organized as subdirectories under the main solution.

```
WebAPICoreMandelbrotSolution/          # 🏠 Solution Root & Git Repository
├── .git/                              # Git repository (tracks entire solution)
├── .gitignore                         # Solution-level ignore patterns
├── .editorconfig                      # 🔧 Editor configuration (applies to all projects)
├── .eslintrc.json                     # 🔧 ESLint configuration (solution-wide)
├── .prettierrc                        # 🔧 Prettier formatting rules (solution-wide)
├── .prettierignore                    # 🔧 Prettier ignore patterns
├── .github/                           # 🔄 GitHub workflows and configuration
├── WebApiCoreMandelbrot.sln           # 📁 Main solution file (all projects)
├── global.json                        # .NET SDK version configuration
├── TestResults/                       # Solution-level test results
├── .vscode/                           # 🔧 VS Code configuration (root level)
│   ├── launch.json                    # Debug configuration for VS Code
│   └── tasks.json                     # Build tasks for all projects
│
├── WebAPICoreMandlebrot/              # 🚀 Main API Project
│   ├── Controllers/
│   │   └── MandelbrotController.cs    # Mandelbrot generation endpoints
│   ├── Services/
│   │   ├── IILGPUAcceleratorService.cs        # CUDA service interface
│   │   ├── ILGPUAcceleratorService.cs         # CUDA service implementation
│   │   └── ILGPUAcceleratorFactory.cs         # ILGPU factory
│   ├── Constants/
│   │   └── SharedConstants.cs         # Shared constants with TypeScript export
│   ├── Properties/
│   │   └── launchSettings.json        # Launch profiles (HTTPS configuration)
│   ├── Build/                         # 🛠️ Build automation
│   │   ├── generate-constants.ts      # Constants generation script
│   │   └── tsconfig.json              # Build TypeScript config
│   ├── src/                           # 📝 TypeScript source files
│   │   ├── app.ts                     # Main visualization application
│   │   ├── api-client.ts              # Type-safe API client
│   │   ├── types.ts                   # TypeScript type definitions
│   │   ├── shared-constants.ts        # Auto-generated from C# constants
│   │   └── response-interfaces.ts     # Auto-generated from C# response classes
│   ├── wwwroot/                       # 🌐 Static web assets
│   │   ├── index.html                 # Interactive frontend
│   │   ├── styles.css                 # Application styling
│   │   └── js/                        # Compiled TypeScript output (generated)
│   │       ├── app.js                 # Compiled main application
│   │       ├── app.d.ts               # Type declarations
│   │       └── *.js.map               # Source maps for debugging
│   ├── TestResults/                   # Project-level test results
│   ├── Program.cs                     # Application entry point
│   ├── WebAPICoreMandelbrot.csproj    # Project file with ILGPU references
│   ├── package.json                   # 📦 NPM dependencies and scripts
│   ├── tsconfig.json                  # TypeScript compilation config
│   ├── .eslintrc.json                 # ESLint config (copy for node_modules resolution)
│   ├── .gitignore                     # Project-specific ignore patterns
│   ├── appsettings.json               # Application configuration
│   ├── appsettings.Development.json   # Development environment settings
│   └── README.md                      # 📖 This comprehensive documentation
├── WebAPICoreMandlebrot.Contracts/    # 📄 Shared response contracts
│   ├── Responses/
│   │   ├── MandelbrotResponse.cs      # API response models
│   │   └── DeviceInfoResponse.cs      # Device info response
│   └── WebAPICoreMandlebrot.Contracts.csproj
├── WebAPICoreMandlebrot.TypeScriptGenerator/  # 🔧 TypeScript interface generator
│   ├── TypeScriptGenerator.cs        # C# reflection-based generator
│   ├── Program.cs                     # Console application entry point
│   └── WebAPICoreMandlebrot.TypeScriptGenerator.csproj
└── WebAPICoreMandlebrot.Tests/        # 🧪 .NET unit tests
    ├── MandelbrotControllerTests.cs   # Controller test suite
    └── WebAPICoreMandlebrot.Tests.csproj
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

1. **Open in VS Code**: From solution root, run `code .` for workspace development
2. **Run the application**: `dotnet run --project WebAPICoreMandlebrot` (from solution root)
3. **Open the frontend**: Navigate to `https://localhost:7000`
4. **Interact with visualization**: Click to zoom, right-click to reset
5. **Monitor performance**: Check GPU compute times and device status
6. **Test API directly**: Use Swagger UI at `https://localhost:7000/swagger`
7. **Build frontend**: Use `npm run build` from WebAPICoreMandlebrot directory

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

## Development Workflow Summary

### 🏠 Working from Solution Root
This solution is designed to work from the **solution root level** (`WebAPICoreMandelbrotSolution/`):

1. **VS Code Setup**: Open VS Code from the root directory (`code .`)
2. **Debugging**: Use VS Code's integrated terminal and debugging features
3. **Git Repository**: All projects tracked from solution root level
4. **Build Tasks**: Available via VS Code Command Palette (`Ctrl+Shift+P` → "Tasks: Run Task")

### 🔧 Key Commands Reference
```bash
# From solution root
dotnet restore                         # Restore all projects
dotnet build                          # Build entire solution  
dotnet run --project WebAPICoreMandlebrot  # Run main project
code .                                # Open in VS Code workspace

# From WebAPICoreMandlebrot/ directory  
npm install                           # Install frontend dependencies
npm run build:full                    # Build TypeScript + .NET
npm run build                         # Build TypeScript with linting
```

### 📁 Multi-Project Benefits
- **Clean Architecture**: Shared contracts in separate project
- **Automatic Generation**: TypeScript interfaces and constants auto-generated from C#
- **Type Safety**: Full type safety between frontend and backend with zero manual sync
- **VS Code Integration**: Root-level workspace and build tasks
- **Git Tracking**: Entire solution managed as single repository

## Notes

- **CUDA-only architecture** for GPU computation with ILGPU
- **ILGPU Context and CUDA Accelerator** registered as singleton services  
- **Error handling** when CUDA is unavailable (no CPU fallback)
- **Standardized JSON responses** for all endpoints and web consumption
- **Multi-project solution** with clean separation and shared contracts
- **Root-level development** with VS Code workspace configuration