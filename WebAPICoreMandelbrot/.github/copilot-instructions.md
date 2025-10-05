<!-- Project-specific Copilot instructions for WebAPI Core Mandelbrot -->

## Project Context
This is a .NET 8 Web API project with ILGPU CUDA acceleration for Mandelbrot set generation, designed for canvas-based web visualizations.

## Key Technologies
- .NET 8 Web API
- ILGPU 1.5.3 for CUDA GPU acceleration
- NVIDIA CUDA (required for computation)
- Swagger/OpenAPI for documentation

## Architecture Guidelines
- CUDA-only operation (no CPU fallbacks)
- Standardized JSON response format with success/error status
- Error handling for systems without CUDA
- Canvas-optimized data format (ImageData compatible)

## Development Notes
- All GPU kernels should use ILGPU syntax
- Pre-compile kernels at startup for performance
- Use proper GPU memory management (using statements)
- Return standardized MandelbrotResponse objects
- Handle nullable accelerator references safely