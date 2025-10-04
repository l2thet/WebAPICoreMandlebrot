import { SHARED_CONSTANTS as sharedConstants } from '../src/shared-constants';

describe('Shared Constants', () => {
    describe('Default Values', () => {
        test('should have correct Mandelbrot defaults', () => {
            expect(sharedConstants.DefaultCenterReal).toBe(-0.5);
            expect(sharedConstants.DefaultCenterImaginary).toBe(0.0);
            expect(sharedConstants.DefaultZoom).toBe(1.0);
            expect(sharedConstants.DefaultMaxIterations).toBe(100);
            expect(sharedConstants.DefaultMaxIterationsExtended).toBe(1000);
        });

        test('should have correct canvas defaults', () => {
            expect(sharedConstants.DefaultCanvasWidth).toBe(3840);
            expect(sharedConstants.DefaultCanvasHeight).toBe(2160);
        });

        test('should have correct viewport defaults', () => {
            expect(sharedConstants.DefaultViewportWidth).toBe(3.5);
            expect(sharedConstants.DefaultViewportHeight).toBe(2.5);
        });
    });

    describe('Complex Plane Bounds', () => {
        test('should have correct bounds', () => {
            expect(sharedConstants.ComplexPlaneMinReal).toBe(-2.5);
            expect(sharedConstants.ComplexPlaneMaxReal).toBe(1.0);
            expect(sharedConstants.ComplexPlaneMinImaginary).toBe(-1.25);
            expect(sharedConstants.ComplexPlaneMaxImaginary).toBe(1.25);
        });

        test('bounds should be mathematically consistent', () => {
            const realRange = sharedConstants.ComplexPlaneMaxReal - sharedConstants.ComplexPlaneMinReal;
            const imaginaryRange = sharedConstants.ComplexPlaneMaxImaginary - sharedConstants.ComplexPlaneMinImaginary;
            
            expect(realRange).toBeGreaterThan(0);
            expect(imaginaryRange).toBeGreaterThan(0);
            expect(realRange).toBeCloseTo(3.5, 3);
            expect(imaginaryRange).toBeCloseTo(2.5, 3);
        });
    });

    describe('Iteration Configuration', () => {
        test('should have reasonable iteration limits', () => {
            expect(sharedConstants.BaseIterationCount).toBe(10000);
            expect(sharedConstants.MinIterationCount).toBe(10000);
            expect(sharedConstants.MaxIterationCount).toBe(10000000);
            expect(sharedConstants.IterationScalingFactor).toBe(8000.0);
        });

        test('iteration limits should be logically ordered', () => {
            expect(sharedConstants.MinIterationCount).toBeGreaterThan(0);
            expect(sharedConstants.MaxIterationCount).toBeGreaterThan(sharedConstants.MinIterationCount);
            expect(sharedConstants.BaseIterationCount).toBeGreaterThan(0);
            expect(sharedConstants.IterationScalingFactor).toBeGreaterThan(0);
        });
    });

    describe('Aspect Ratios', () => {
        test('canvas and viewport should have positive aspect ratios', () => {
            const canvasAspectRatio = sharedConstants.DefaultCanvasWidth / sharedConstants.DefaultCanvasHeight;
            const viewportAspectRatio = sharedConstants.DefaultViewportWidth / sharedConstants.DefaultViewportHeight;
            
            expect(canvasAspectRatio).toBeGreaterThan(0);
            expect(viewportAspectRatio).toBeGreaterThan(0);
            
            // Canvas is 16:9 (1.778), viewport is 3.5:2.5 (1.4) - both valid ratios
            expect(canvasAspectRatio).toBeCloseTo(1.778, 2); // 3840/2160
            expect(viewportAspectRatio).toBeCloseTo(1.4, 2); // 3.5/2.5
        });

        test('should work with various aspect ratios', () => {
            const testDimensions = [
                { width: 800, height: 600 },
                { width: 1920, height: 1080 },
                { width: 3840, height: 2160 }
            ];

            testDimensions.forEach(({ width, height }) => {
                const aspectRatio = width / height;
                expect(aspectRatio).toBeGreaterThan(0);
                
                // Verify we can calculate viewport dimensions for any aspect ratio
                const viewportHeight = sharedConstants.DefaultViewportHeight;
                const viewportWidth = viewportHeight * aspectRatio;
                expect(viewportWidth).toBeGreaterThan(0);
            });
        });
    });
});

describe('Coordinate System Utilities', () => {
    describe('Complex Plane Calculations', () => {
        test('should calculate complex coordinates correctly', () => {
            // Test converting canvas coordinates to complex plane
            const canvasX = 100;
            const canvasY = 100;
            const canvasWidth = 800;
            const canvasHeight = 600;
            
            // Calculate complex coordinates (simplified version of what the app does)
            const real = sharedConstants.ComplexPlaneMinReal + 
                        (canvasX / canvasWidth) * (sharedConstants.ComplexPlaneMaxReal - sharedConstants.ComplexPlaneMinReal);
            const imaginary = sharedConstants.ComplexPlaneMinImaginary + 
                             (canvasY / canvasHeight) * (sharedConstants.ComplexPlaneMaxImaginary - sharedConstants.ComplexPlaneMinImaginary);
            
            expect(real).toBeGreaterThanOrEqual(sharedConstants.ComplexPlaneMinReal);
            expect(real).toBeLessThanOrEqual(sharedConstants.ComplexPlaneMaxReal);
            expect(imaginary).toBeGreaterThanOrEqual(sharedConstants.ComplexPlaneMinImaginary);
            expect(imaginary).toBeLessThanOrEqual(sharedConstants.ComplexPlaneMaxImaginary);
        });

        test('should handle edge cases in coordinate conversion', () => {
            // Test corners of canvas
            const testCases = [
                { x: 0, y: 0 }, // Top-left
                { x: 800, y: 0 }, // Top-right
                { x: 0, y: 600 }, // Bottom-left
                { x: 800, y: 600 } // Bottom-right
            ];

            testCases.forEach(({ x, y }) => {
                const real = sharedConstants.ComplexPlaneMinReal + 
                            (x / 800) * (sharedConstants.ComplexPlaneMaxReal - sharedConstants.ComplexPlaneMinReal);
                const imaginary = sharedConstants.ComplexPlaneMinImaginary + 
                                 (y / 600) * (sharedConstants.ComplexPlaneMaxImaginary - sharedConstants.ComplexPlaneMinImaginary);
                
                expect(typeof real).toBe('number');
                expect(typeof imaginary).toBe('number');
                expect(isFinite(real)).toBe(true);
                expect(isFinite(imaginary)).toBe(true);
            });
        });
    });

    describe('Zoom and Iteration Scaling', () => {
        test('should calculate iterations based on zoom level', () => {
            const testZoomLevels = [1, 2, 5, 10, 100];
            
            testZoomLevels.forEach(zoom => {
                const iterations = Math.floor(sharedConstants.BaseIterationCount + 
                                            Math.log(zoom) * sharedConstants.IterationScalingFactor);
                const clampedIterations = Math.max(sharedConstants.MinIterationCount, 
                                                 Math.min(sharedConstants.MaxIterationCount, iterations));
                
                expect(clampedIterations).toBeGreaterThanOrEqual(sharedConstants.MinIterationCount);
                expect(clampedIterations).toBeLessThanOrEqual(sharedConstants.MaxIterationCount);
                expect(Number.isInteger(clampedIterations)).toBe(true);
            });
        });
    });
});