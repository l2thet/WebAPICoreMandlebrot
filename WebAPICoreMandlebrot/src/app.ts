import { MandelbrotResponse, ToastType } from './types.js';
import {
    DefaultCenterReal,
    DefaultCenterImaginary,
    DefaultZoom,
    ComplexPlaneMinReal,
    ComplexPlaneMaxReal,
    ComplexPlaneMinImaginary,
    ComplexPlaneMaxImaginary,
    DefaultCanvasWidth,
    DefaultCanvasHeight,
} from './shared-constants.js';

class MandelbrotVisualization {
    private isGenerating: boolean = false;
    private canvas!: HTMLCanvasElement;
    private ctx!: CanvasRenderingContext2D;
    private deviceInfoElement!: HTMLElement;
    private renderTimeElement!: HTMLElement;
    private zoomLevelElement!: HTMLElement;
    private iterationCountElement!: HTMLElement;
    private complexCoordsElement!: HTMLElement;
    private inSetElement!: HTMLElement;
    private pointIterationsElement!: HTMLElement;
    private tooltip!: HTMLDivElement;
    private loadingOverlay!: HTMLDivElement;
    private loadingSpinner!: HTMLDivElement;
    // Store iteration data from original Mandelbrot generation
    private currentIterationData: number[] | null = null;

    // Current view parameters (updated by user interaction and backend)
    private centerReal: number = DefaultCenterReal;
    private centerImaginary: number = DefaultCenterImaginary;
    private zoom: number = DefaultZoom;

    // Fixed canvas dimensions (no user input required)
    private readonly width: number = DefaultCanvasWidth;
    private readonly height: number = DefaultCanvasHeight;

    // Dynamic backend-calculated iteration count
    private currentMaxIterations: number = 5000; // Fallback until first generation

    // Zoom constraints removed - backend handles zoom limits

    constructor() {
        this.initializeElements();
        this.createTooltip();
        this.createLoadingUI();
        this.setupEventListeners();
        // Don't call checkDeviceInfo() and generateMandelbrot() here -
        // they're called in initialize() method
    }

    private initializeElements(): void {
        this.canvas = this.getElement<HTMLCanvasElement>('mandelbrotCanvas');
        const context = this.canvas.getContext('2d');
        if (!context) {
            throw new Error('Could not get 2D context from canvas');
        }
        this.ctx = context;

        this.deviceInfoElement = this.getElement<HTMLElement>('deviceInfo');
        this.renderTimeElement = this.getElement<HTMLElement>('renderTime');
        this.zoomLevelElement = this.getElement<HTMLElement>('zoomLevel');
        this.iterationCountElement = this.getElement<HTMLElement>('iterationCount');
        this.complexCoordsElement = this.getElement<HTMLElement>('complexCoords');
        this.inSetElement = this.getElement<HTMLElement>('inSet');
        this.pointIterationsElement = this.getElement<HTMLElement>('pointIterations');
    }

    private getElement<T extends HTMLElement>(id: string): T {
        const element = document.getElementById(id) as T;
        if (!element) {
            throw new Error(`Element with id '${id}' not found`);
        }
        return element;
    }

    private createTooltip(): void {
        this.tooltip = document.createElement('div');
        this.tooltip.className = 'mandelbrot-tooltip';
        document.body.appendChild(this.tooltip);
    }

    private createLoadingUI(): void {
        // Create loading overlay
        this.loadingOverlay = document.createElement('div');
        this.loadingOverlay.className = 'loading-overlay';
        this.loadingOverlay.style.display = 'none';

        // Create spinner
        this.loadingSpinner = document.createElement('div');
        this.loadingSpinner.className = 'loading-spinner';

        this.loadingOverlay.appendChild(this.loadingSpinner);

        // Create a wrapper div for the canvas and append loading overlay
        const canvasWrapper = document.createElement('div');
        canvasWrapper.style.position = 'relative';
        canvasWrapper.style.display = 'inline-block';

        // Move canvas into wrapper
        const canvas = document.getElementById('mandelbrotCanvas') as HTMLCanvasElement;
        if (canvas && canvas.parentNode) {
            canvas.parentNode.insertBefore(canvasWrapper, canvas);
            canvasWrapper.appendChild(canvas);
            canvasWrapper.appendChild(this.loadingOverlay);
        }
    }

    private setupEventListeners(): void {
        this.canvas.addEventListener('click', e => this.handleCanvasClick(e));
        this.canvas.addEventListener('contextmenu', e => this.handleRightClick(e));
        this.canvas.addEventListener('mousemove', e => this.handleMouseMove(e));
        this.canvas.addEventListener('mouseleave', () => this.handleMouseLeave());
    }

    private showToast(message: string, type: ToastType = 'info'): void {
        // Remove existing toasts
        document.querySelectorAll('.toast').forEach(toast => toast.remove());

        const toast = document.createElement('div');
        toast.className = `toast ${type}`;
        toast.textContent = message;
        document.body.appendChild(toast);

        setTimeout(() => toast.classList.add('show'), 100);
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, 5000);
    }

    private async checkDeviceInfo(): Promise<void> {
        try {
            const response = await fetch('/api/mandelbrot/device');
            const data = (await response.json()) as {
                hasCudaDevice: boolean;
                name?: string;
                acceleratorType?: string;
                error?: string;
            };

            if (data.hasCudaDevice && data.name && data.acceleratorType) {
                this.deviceInfoElement.textContent = `${data.name} (${data.acceleratorType})`;
            } else {
                this.deviceInfoElement.textContent = 'CUDA Not Available';
                // Only show error toast if there's an actual error, not just missing CUDA
                if (data.error) {
                    this.showToast(data.error, 'error');
                }
            }
        } catch (error) {
            this.deviceInfoElement.textContent = 'Connection Error';
            this.showToast('Failed to connect to API', 'error');
        }
    }

    private getColor(iterations: number, maxIterations: number): [number, number, number] {
        if (iterations === maxIterations) {
            return [0, 0, 0]; // Black for points in the set
        }

        const ratio = iterations / 255;
        const r = Math.floor(9 * (1 - ratio) * ratio * ratio * ratio * 255);
        const g = Math.floor(15 * (1 - ratio) * (1 - ratio) * ratio * ratio * 255);
        const b = Math.floor(8.5 * (1 - ratio) * (1 - ratio) * (1 - ratio) * ratio * 255);

        return [r, g, b];
    }

    // Store coordinate bounds from backend response (initialized with defaults)
    private viewMinReal: number = ComplexPlaneMinReal;
    private viewMaxReal: number = ComplexPlaneMaxReal;
    private viewMinImaginary: number = ComplexPlaneMinImaginary;
    private viewMaxImaginary: number = ComplexPlaneMaxImaginary;

    private canvasToComplex(canvasX: number, canvasY: number): { real: number; imaginary: number } {
        // Use the exact coordinate bounds provided by the CUDA backend
        const real =
            this.viewMinReal + (canvasX * (this.viewMaxReal - this.viewMinReal)) / this.width;
        const imaginary =
            this.viewMinImaginary +
            (canvasY * (this.viewMaxImaginary - this.viewMinImaginary)) / this.height;

        return { real, imaginary };
    }

    private handleCanvasClick(event: MouseEvent): void {
        if (this.isGenerating) {
            return;
        } // Prevent clicks during generation

        const rect = this.canvas.getBoundingClientRect();
        const canvasX = ((event.clientX - rect.left) * this.canvas.width) / rect.width;
        const canvasY = ((event.clientY - rect.top) * this.canvas.height) / rect.height;

        const { real, imaginary } = this.canvasToComplex(canvasX, canvasY);

        // For zoom in: send current zoom * 2, let backend validate and return actual zoom used
        const requestedZoom = this.zoom * 2.0;

        // Update view parameters
        this.centerReal = real;
        this.centerImaginary = imaginary;
        this.zoom = requestedZoom; // This will be overridden by backend response

        // Don't update zoom display immediately - let it update with other values when API responds
        this.generateMandelbrot();
    }

    private resetView(): void {
        if (this.isGenerating) {
            return;
        } // Prevent reset during generation

        this.centerReal = DefaultCenterReal;
        this.centerImaginary = DefaultCenterImaginary;
        this.zoom = DefaultZoom; // Send default zoom request to backend

        // Don't update zoom display immediately - let it update with other values when API responds
        this.generateMandelbrot();
    }

    private handleRightClick(event: MouseEvent): void {
        if (this.isGenerating) {
            return;
        } // Prevent right-click during generation

        event.preventDefault(); // Prevent context menu
        this.resetView();
        this.showToast('View reset to default', 'info');
    }

    private handleMouseMove(event: MouseEvent): void {
        const rect = this.canvas.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top;

        // Scale coordinates to actual canvas size (not display size)
        const scaleX = this.canvas.width / rect.width;
        const scaleY = this.canvas.height / rect.height;
        const canvasX = x * scaleX;
        const canvasY = y * scaleY;

        // Convert canvas coordinates to complex plane coordinates
        const complexReal =
            this.centerReal +
            ((canvasX - this.canvas.width / 2) / (this.canvas.width / 2)) * (2.0 / this.zoom);
        const complexImag =
            this.centerImaginary -
            ((canvasY - this.canvas.height / 2) / (this.canvas.height / 2)) * (2.0 / this.zoom);

        // Update the complex coordinates display
        this.complexCoordsElement.textContent = `${complexReal.toFixed(6)} + ${complexImag.toFixed(6)}i`;

        // Get iteration data from stored array
        this.updatePointDetailsFromStoredData(canvasX, canvasY);
    }

    private handleMouseLeave(): void {
        // Clear the point details when mouse leaves canvas
        this.complexCoordsElement.textContent = '--';
        this.inSetElement.textContent = '--';
        this.pointIterationsElement.textContent = '--';
    }

    private updatePointDetailsFromStoredData(canvasX: number, canvasY: number): void {
        // Check if we have iteration data available
        if (!this.currentIterationData || this.currentIterationData.length === 0) {
            this.inSetElement.textContent = '--';
            this.pointIterationsElement.textContent = '--';
            return;
        }

        // Ensure coordinates are within canvas bounds
        const x = Math.max(0, Math.min(Math.floor(canvasX), this.canvas.width - 1));
        const y = Math.max(0, Math.min(Math.floor(canvasY), this.canvas.height - 1));

        // Calculate array index (row-major order: y * width + x)
        const index = y * this.canvas.width + x;

        // Check if index is valid
        if (index < 0 || index >= this.currentIterationData.length) {
            this.inSetElement.textContent = '--';
            this.pointIterationsElement.textContent = '--';
            return;
        }

        // Get iteration count for this pixel
        const iterations = this.currentIterationData[index];
        if (iterations === undefined) {
            this.inSetElement.textContent = '--';
            this.pointIterationsElement.textContent = '--';
            return;
        }

        const inSet = iterations >= this.currentMaxIterations;

        // Update the legend
        this.inSetElement.textContent = inSet ? 'Yes' : 'No';
        this.pointIterationsElement.textContent = inSet
            ? `${this.currentMaxIterations}+`
            : `${iterations}`;
    }

    private updateZoomDisplay(): void {
        const zoomText = this.zoom >= 1 ? `${this.zoom.toFixed(1)}x` : `${this.zoom.toFixed(3)}x`;
        this.zoomLevelElement.textContent = zoomText;
    }

    private showLoadingState(): void {
        // Gray out the canvas
        this.canvas.style.opacity = '0.5';
        this.canvas.style.pointerEvents = 'none';

        // Show loading overlay
        this.loadingOverlay.style.display = 'flex';
    }

    private hideLoadingState(): void {
        // Restore canvas
        this.canvas.style.opacity = '1';
        this.canvas.style.pointerEvents = 'auto';

        // Hide loading overlay
        this.loadingOverlay.style.display = 'none';
    }

    private async generateMandelbrot(): Promise<void> {
        if (this.isGenerating) {
            return;
        }

        this.isGenerating = true;
        this.showLoadingState();

        try {
            const startTime = performance.now();

            // Make API call - let backend calculate iterations based on zoom
            const response = await fetch(
                `/api/mandelbrot/generate?centerReal=${this.centerReal}&centerImaginary=${this.centerImaginary}&zoom=${this.zoom}`
            );

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data: MandelbrotResponse = await response.json();

            if (!data.success) {
                throw new Error(data.error || 'Generation failed');
            }

            if (!data.data || data.data.length === 0) {
                throw new Error('No image data received from API');
            }

            // Update coordinate bounds from CUDA response (authoritative source)
            if (
                data.viewMinReal !== undefined &&
                data.viewMaxReal !== undefined &&
                data.viewMinImaginary !== undefined &&
                data.viewMaxImaginary !== undefined
            ) {
                this.viewMinReal = data.viewMinReal;
                this.viewMaxReal = data.viewMaxReal;
                this.viewMinImaginary = data.viewMinImaginary;
                this.viewMaxImaginary = data.viewMaxImaginary;
            }

            // Update current view parameters from backend response
            if (
                data.centerReal !== undefined &&
                data.centerImaginary !== undefined &&
                data.zoom !== undefined
            ) {
                this.centerReal = data.centerReal;
                this.centerImaginary = data.centerImaginary;
                this.zoom = data.zoom;
            }

            // Update maxIterations with the value calculated by backend
            this.currentMaxIterations = data.maxIterations;

            // Update zoom with the actual value used by backend (removes frontend calculations)
            if (data.zoom !== undefined) {
                this.zoom = data.zoom;
            }

            // Store the iteration data for mouse hover calculations
            this.currentIterationData = data.data;

            // Now that we have data, set canvas size and render
            this.canvas.width = this.width;
            this.canvas.height = this.height;
            this.renderToCanvas(data.data, this.width, this.height, data.maxIterations);

            const endTime = performance.now();
            const totalTime = Math.round(endTime - startTime);

            // Update device info from successful generation response
            if (data.acceleratorName && data.acceleratorType) {
                this.deviceInfoElement.textContent = `${data.acceleratorName} (${data.acceleratorType})`;
            }

            // Update UI with results - convert milliseconds to seconds
            const renderTimeMs = data.computeTimeMs || totalTime;
            const renderTimeSeconds = (renderTimeMs / 1000).toFixed(3);
            this.renderTimeElement.textContent = `${renderTimeSeconds}s`;
            this.iterationCountElement.textContent = `${data.maxIterations.toLocaleString()}`;
            this.updateZoomDisplay(); // Update zoom display with other values after API response
            this.showToast(
                `Mandelbrot set generated successfully! GPU: ${renderTimeSeconds}s (${data.maxIterations.toLocaleString()} iterations)`,
                'success'
            );
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
            this.showToast(`Error generating Mandelbrot set: ${errorMessage}`, 'error');
            this.renderErrorCanvas();
        } finally {
            this.isGenerating = false;
            this.hideLoadingState();
        }
    }

    private renderToCanvas(
        data: number[],
        width: number,
        height: number,
        maxIterations: number
    ): void {
        this.ctx.clearRect(0, 0, width, height);

        const imageData = this.ctx.createImageData(width, height);
        const pixels = imageData.data;

        for (let i = 0; i < data.length; i++) {
            const iterations = data[i];
            if (iterations === undefined) {
                continue;
            }
            const [r, g, b] = this.getColor(iterations, maxIterations);

            const pixelIndex = i * 4;
            pixels[pixelIndex] = r; // Red
            pixels[pixelIndex + 1] = g; // Green
            pixels[pixelIndex + 2] = b; // Blue
            pixels[pixelIndex + 3] = 255; // Alpha
        }

        this.ctx.putImageData(imageData, 0, 0);
    }

    private renderErrorCanvas(): void {
        // Clear the stored iteration data since generation failed
        this.currentIterationData = null;

        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
        this.ctx.fillStyle = '#f0f0f0';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
        this.ctx.fillStyle = '#666';
        this.ctx.font = '16px Arial';
        this.ctx.textAlign = 'center';
        this.ctx.fillText('Generation Failed', this.canvas.width / 2, this.canvas.height / 2);
    }

    public async initialize(): Promise<void> {
        this.showToast('Initializing Mandelbrot visualization...', 'info');
        this.updateZoomDisplay();
        this.iterationCountElement.textContent = '--';
        await this.checkDeviceInfo();
        await this.generateMandelbrot();
    }
}

// Initialize application when DOM is loaded
document.addEventListener('DOMContentLoaded', async () => {
    try {
        const app = new MandelbrotVisualization();
        await app.initialize();

        // Make app globally available for debugging
        (window as unknown as Record<string, unknown>).mandelbrotApp = app;
    } catch (error) {
        const errorMessage =
            error instanceof Error ? error.message : 'Unknown initialization error';
        alert(`Failed to initialize application: ${errorMessage}`);
    }
});
