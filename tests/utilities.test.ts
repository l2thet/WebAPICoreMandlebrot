import { MandelbrotResponse, DeviceInfo, DeviceResponse } from '../src/types';

describe('Type Definitions', () => {
    describe('MandelbrotResponse', () => {
        test('should handle successful response', () => {
            const successResponse: MandelbrotResponse = {
                success: true,
                width: 800,
                height: 600,
                maxIterations: 1000,
                data: [255, 0, 0, 255], // Red pixel data
                computeTimeMs: 150,
                acceleratorType: 'CUDA',
                acceleratorName: 'NVIDIA GeForce RTX 4090',
                centerReal: -0.5,
                centerImaginary: 0.0,
                zoom: 1.0
            };

            expect(successResponse.success).toBe(true);
            expect(successResponse.data).toEqual([255, 0, 0, 255]);
            expect(successResponse.width).toBe(800);
            expect(successResponse.height).toBe(600);
            expect(successResponse.error).toBeUndefined();
        });

        test('should handle error response', () => {
            const errorResponse: MandelbrotResponse = {
                success: false,
                width: 0,
                height: 0,
                maxIterations: 0,
                error: 'CUDA not available'
            };

            expect(errorResponse.success).toBe(false);
            expect(errorResponse.error).toBe('CUDA not available');
            expect(errorResponse.data).toBeUndefined();
        });

        test('should handle viewport mapping data', () => {
            const responseWithViewport: MandelbrotResponse = {
                success: true,
                width: 800,
                height: 600,
                maxIterations: 1000,
                viewMinReal: -2.5,
                viewMaxReal: 1.0,
                viewMinImaginary: -1.25,
                viewMaxImaginary: 1.25,
                centerReal: -0.75,
                centerImaginary: 0.1,
                zoom: 2.0
            };

            expect(responseWithViewport.viewMinReal).toBe(-2.5);
            expect(responseWithViewport.viewMaxReal).toBe(1.0);
            expect(responseWithViewport.centerReal).toBe(-0.75);
            expect(responseWithViewport.zoom).toBe(2.0);
        });
    });

    describe('DeviceInfo', () => {
        test('should handle device information correctly', () => {
            const deviceInfo: DeviceInfo = {
                acceleratorType: 'CUDA',
                name: 'NVIDIA GeForce RTX 4090',
                maxNumThreads: 2048,
                maxGroupSize: '1024, 1024, 64',
                warpSize: 32,
                numMultiprocessors: 128,
                memorySize: 24575,
                maxConstantMemory: 65536,
                maxSharedMemoryPerGroup: 49152
            };

            expect(deviceInfo.acceleratorType).toBe('CUDA');
            expect(deviceInfo.name).toContain('RTX');
            expect(deviceInfo.maxNumThreads).toBeGreaterThan(0);
            expect(deviceInfo.warpSize).toBe(32);
        });
    });

    describe('DeviceResponse', () => {
        test('should handle successful device response', () => {
            const deviceResponse: DeviceResponse = {
                hasCudaDevice: true,
                kernelPrecompiled: true,
                currentDevice: {
                    acceleratorType: 'CUDA',
                    name: 'NVIDIA GeForce RTX 4090',
                    maxNumThreads: 2048,
                    maxGroupSize: '1024, 1024, 64',
                    warpSize: 32,
                    numMultiprocessors: 128,
                    memorySize: 24575,
                    maxConstantMemory: 65536,
                    maxSharedMemoryPerGroup: 49152
                },
                statusMessage: 'CUDA device ready'
            };

            expect(deviceResponse.hasCudaDevice).toBe(true);
            expect(deviceResponse.kernelPrecompiled).toBe(true);
            expect(deviceResponse.currentDevice).toBeDefined();
            expect(deviceResponse.error).toBeUndefined();
        });

        test('should handle error device response', () => {
            const errorResponse: DeviceResponse = {
                hasCudaDevice: false,
                error: 'No CUDA-capable devices found',
                statusMessage: 'CUDA initialization failed'
            };

            expect(errorResponse.hasCudaDevice).toBe(false);
            expect(errorResponse.error).toBeDefined();
            expect(errorResponse.currentDevice).toBeUndefined();
        });
    });
});

describe('Coordinate Conversion Utilities', () => {
    describe('Complex Plane Conversions', () => {
        test('should convert canvas coordinates to complex plane', () => {
            const canvasToComplex = (
                canvasX: number,
                canvasY: number,
                canvasWidth: number,
                canvasHeight: number,
                centerReal: number = -0.5,
                centerImaginary: number = 0.0,
                zoom: number = 1.0,
                viewportWidth: number = 3.5
            ) => {
                const viewportHeight = viewportWidth * (canvasHeight / canvasWidth);
                const pixelWidth = viewportWidth / zoom / canvasWidth;
                const pixelHeight = viewportHeight / zoom / canvasHeight;

                const real = centerReal + (canvasX - canvasWidth / 2) * pixelWidth;
                const imaginary = centerImaginary + (canvasY - canvasHeight / 2) * pixelHeight;

                return { real, imaginary };
            };

            const result = canvasToComplex(400, 300, 800, 600);
            expect(typeof result.real).toBe('number');
            expect(typeof result.imaginary).toBe('number');
            expect(isFinite(result.real)).toBe(true);
            expect(isFinite(result.imaginary)).toBe(true);
        });

        test('should handle edge cases in conversion', () => {
            const canvasToComplex = (x: number, y: number, w: number, h: number) => {
                const viewportWidth = 3.5;
                const viewportHeight = viewportWidth * (h / w);
                const centerReal = -0.5;
                const centerImaginary = 0.0;
                const zoom = 1.0;

                const pixelWidth = viewportWidth / zoom / w;
                const pixelHeight = viewportHeight / zoom / h;

                return {
                    real: centerReal + (x - w / 2) * pixelWidth,
                    imaginary: centerImaginary + (y - h / 2) * pixelHeight
                };
            };

            // Test corners
            const topLeft = canvasToComplex(0, 0, 800, 600);
            const bottomRight = canvasToComplex(800, 600, 800, 600);
            const center = canvasToComplex(400, 300, 800, 600);

            expect(topLeft.real).toBeLessThan(center.real);
            expect(topLeft.imaginary).toBeLessThan(center.imaginary);
            expect(bottomRight.real).toBeGreaterThan(center.real);
            expect(bottomRight.imaginary).toBeGreaterThan(center.imaginary);
        });
    });

    describe('Zoom and Iteration Calculations', () => {
        test('should calculate iterations based on zoom', () => {
            const calculateIterations = (zoom: number) => {
                const baseIterations = 10000;
                const scalingFactor = 8000;
                const minIterations = 10000;
                const maxIterations = 10000000;

                const calculated = Math.floor(baseIterations + Math.log(zoom) * scalingFactor);
                return Math.max(minIterations, Math.min(maxIterations, calculated));
            };

            expect(calculateIterations(1)).toBe(10000);
            expect(calculateIterations(2)).toBeGreaterThan(10000);
            expect(calculateIterations(100)).toBeGreaterThan(calculateIterations(10));
            expect(calculateIterations(1000000)).toBeLessThanOrEqual(10000000);
        });

        test('should handle extreme zoom values', () => {
            const calculateIterations = (zoom: number) => {
                const baseIterations = 10000;
                const scalingFactor = 8000;
                const minIterations = 10000;
                const maxIterations = 10000000;

                if (zoom <= 0) return minIterations;
                
                const calculated = Math.floor(baseIterations + Math.log(zoom) * scalingFactor);
                return Math.max(minIterations, Math.min(maxIterations, calculated));
            };

            expect(calculateIterations(0)).toBe(10000);
            expect(calculateIterations(0.1)).toBe(10000); // Should clamp to minimum
            expect(calculateIterations(Infinity)).toBe(10000000); // Should clamp to maximum
        });
    });
});