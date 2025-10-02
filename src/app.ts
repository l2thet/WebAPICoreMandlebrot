import { 
    MandelbrotResponse, 
    DeviceResponse, 
    ToastType,
    PointResponse
} from './types.js';
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
    DefaultViewportWidth,
    DefaultViewportHeight
} from './shared-constants.js';

class MandelbrotVisualization {
    private isGenerating: boolean = false;
    private canvas!: HTMLCanvasElement;
    private ctx!: CanvasRenderingContext2D;
    private deviceInfoElement!: HTMLElement;
    private renderTimeElement!: HTMLElement;
    private zoomLevelElement!: HTMLElement;
    private iterationCountElement!: HTMLElement;
    private tooltip!: HTMLDivElement;
    private loadingOverlay!: HTMLDivElement;
    private loadingSpinner!: HTMLDivElement;
    
    // Current view parameters (updated by user interaction and backend)
    private centerReal: number = DefaultCenterReal;
    private centerImaginary: number = DefaultCenterImaginary;
    private zoom: number = DefaultZoom;
    
    // Fixed UHD dimensions (no user input required)
    private readonly width: number = DefaultCanvasWidth;
    private readonly height: number = DefaultCanvasHeight;
    
    // Dynamic backend-calculated iteration count
    private currentMaxIterations: number = 5000; // Fallback until first generation
    
    // Zoom constraints
    private readonly MAX_ZOOM = 1000000.0;

    constructor() {
        this.initializeElements();
        this.createTooltip();
        this.createLoadingUI();
        this.setupEventListeners();
        this.checkDeviceInfo();
        this.generateMandelbrot(); // Auto-generate on page load
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
        
        // Insert after canvas container
        const canvasContainer = document.querySelector('.canvas-container');
        if (canvasContainer && canvasContainer.parentNode) {
            canvasContainer.appendChild(this.loadingOverlay);
        }
    }

    private setupEventListeners(): void {
        this.canvas.addEventListener('click', (e) => this.handleCanvasClick(e));
        this.canvas.addEventListener('contextmenu', (e) => this.handleRightClick(e));
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
            const data: DeviceResponse = await response.json();
            
            if (data.hasCudaDevice && data.currentDevice) {
                this.deviceInfoElement.textContent = 
                    `Device: ${data.currentDevice.name} (${data.currentDevice.acceleratorType})`;
            } else {
                this.deviceInfoElement.textContent = 'Device: CUDA Not Available';
                this.showToast(data.error || 'CUDA device not detected', 'error');
            }
        } catch (error) {
            this.deviceInfoElement.textContent = 'Device: Connection Error';
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
        const real = this.viewMinReal + canvasX * (this.viewMaxReal - this.viewMinReal) / this.width;
        const imaginary = this.viewMinImaginary + canvasY * (this.viewMaxImaginary - this.viewMinImaginary) / this.height;
        
        return { real, imaginary };
    }



    private handleCanvasClick(event: MouseEvent): void {
        if (this.isGenerating) return; // Prevent clicks during generation

        const rect = this.canvas.getBoundingClientRect();
        const canvasX = ((event.clientX - rect.left) * this.canvas.width) / rect.width;
        const canvasY = ((event.clientY - rect.top) * this.canvas.height) / rect.height;

        const { real, imaginary } = this.canvasToComplex(canvasX, canvasY);

        // Calculate new zoom level (zoom in by 2x on each click)
        const newZoom = this.zoom * 2.0;
        
        if (newZoom > this.MAX_ZOOM) {
            this.showToast(`Maximum zoom level reached (${this.MAX_ZOOM}x)`, 'info');
            return;
        }

        // Update view parameters
        this.centerReal = real;
        this.centerImaginary = imaginary;
        this.zoom = newZoom;

        this.updateZoomDisplay();
        this.generateMandelbrot();
    }

    private resetView(): void {
        if (this.isGenerating) return; // Prevent reset during generation

        this.centerReal = DefaultCenterReal;
        this.centerImaginary = DefaultCenterImaginary;
        this.zoom = DefaultZoom;

        this.updateZoomDisplay();
        this.generateMandelbrot();
    }

    private handleRightClick(event: MouseEvent): void {
        if (this.isGenerating) return; // Prevent right-click during generation
        
        event.preventDefault(); // Prevent context menu
        this.resetView();
        this.showToast('View reset to default', 'info');
    }

    private updateZoomDisplay(): void {
        const zoomText = this.zoom >= 1 
            ? `${this.zoom.toFixed(1)}x` 
            : `${this.zoom.toFixed(3)}x`;
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
        if (this.isGenerating) return;
        
        this.isGenerating = true;
        this.showLoadingState();
        
        try {
            const startTime = performance.now();
            
            // Make API call - let backend calculate iterations based on zoom
            const response = await fetch(
                `/api/mandelbrot/generate?width=${this.width}&height=${this.height}&centerReal=${this.centerReal}&centerImaginary=${this.centerImaginary}&zoom=${this.zoom}`
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
            if (data.viewMinReal !== undefined && data.viewMaxReal !== undefined &&
                data.viewMinImaginary !== undefined && data.viewMaxImaginary !== undefined) {
                this.viewMinReal = data.viewMinReal;
                this.viewMaxReal = data.viewMaxReal;
                this.viewMinImaginary = data.viewMinImaginary;
                this.viewMaxImaginary = data.viewMaxImaginary;
            }
            
            // Update current view parameters from backend response
            if (data.centerReal !== undefined && data.centerImaginary !== undefined && data.zoom !== undefined) {
                this.centerReal = data.centerReal;
                this.centerImaginary = data.centerImaginary;
                this.zoom = data.zoom;
            }
            
            // Update maxIterations with the value calculated by backend
            this.currentMaxIterations = data.maxIterations;
            
            // Now that we have data, set canvas size and render
            this.canvas.width = this.width;
            this.canvas.height = this.height;
            this.renderToCanvas(data.data, this.width, this.height, data.maxIterations);
            
            const endTime = performance.now();
            const totalTime = Math.round(endTime - startTime);
            
            // Update UI with results
            this.renderTimeElement.textContent = `${data.computeTimeMs || totalTime}ms`;
            this.iterationCountElement.textContent = `${data.maxIterations}`;
            this.showToast(`Mandelbrot set generated successfully! GPU: ${data.computeTimeMs || totalTime}ms (${data.maxIterations.toLocaleString()} iterations)`, 'success');
            
        } catch (error) {
            console.error('Error generating Mandelbrot set:', error);
            const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
            this.showToast(errorMessage, 'error');
            this.renderErrorCanvas();
        } finally {
            this.isGenerating = false;
            this.hideLoadingState();
        }
    }

    private renderToCanvas(data: number[], width: number, height: number, maxIterations: number): void {
        this.ctx.clearRect(0, 0, width, height);
        
        const imageData = this.ctx.createImageData(width, height);
        const pixels = imageData.data;
        
        for (let i = 0; i < data.length; i++) {
            const iterations = data[i];
            if (iterations === undefined) continue;
            const [r, g, b] = this.getColor(iterations, maxIterations);
            
            const pixelIndex = i * 4;
            pixels[pixelIndex] = r;         // Red
            pixels[pixelIndex + 1] = g;     // Green
            pixels[pixelIndex + 2] = b;     // Blue
            pixels[pixelIndex + 3] = 255;   // Alpha
        }
        
        this.ctx.putImageData(imageData, 0, 0);
    }

    private renderErrorCanvas(): void {
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
        (window as any).mandelbrotApp = app;
    } catch (error) {
        console.error('Failed to initialize Mandelbrot application:', error);
        alert('Failed to initialize application. Please check the console for details.');
    }
});