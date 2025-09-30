// Mandelbrot Visualization JavaScript

// Global variables
let isGenerating = false;
const MAX_COLOR_VALUE = 255;

// Toast notification system
function showToast(message, type = 'info') {
    // Remove existing toasts
    const existingToasts = document.querySelectorAll('.toast');
    existingToasts.forEach(toast => toast.remove());
    
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.textContent = message;
    document.body.appendChild(toast);
    
    // Show toast
    setTimeout(() => toast.classList.add('show'), 100);
    
    // Auto-hide after 5 seconds
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 5000);
}

// Check CUDA device info
async function checkDeviceInfo() {
    try {
        const response = await fetch('/api/mandelbrot/device');
        const data = await response.json();
        
        if (data.hasCudaDevice && data.currentDevice) {
            document.getElementById('deviceInfo').textContent = 
                `Device: ${data.currentDevice.name} (${data.currentDevice.acceleratorType})`;
        } else {
            document.getElementById('deviceInfo').textContent = 'Device: CUDA Not Available';
            showToast(data.error || 'CUDA device not detected', 'error');
        }
    } catch (error) {
        document.getElementById('deviceInfo').textContent = 'Device: Connection Error';
        showToast('Failed to connect to API', 'error');
    }
}

// Color mapping function
function getColor(iterations, maxIterations) {
    if (iterations === maxIterations) {
        return [0, 0, 0]; // Black for points in the set
    }
    
    const ratio = iterations / MAX_COLOR_VALUE;
    const r = (9 * (1 - ratio) * ratio * ratio * ratio * MAX_COLOR_VALUE);
    const g = (15 * (1 - ratio) * (1 - ratio) * ratio * ratio * MAX_COLOR_VALUE);
    const b = (8.5 * (1 - ratio) * (1 - ratio) * (1 - ratio) * ratio * MAX_COLOR_VALUE);

    return [ r, g, b ];
}

// Generate Mandelbrot set
async function generateMandelbrot() {
    if (isGenerating) return;
    
    isGenerating = true;
    const generateBtn = document.getElementById('generateBtn');
    const canvas = document.getElementById('mandelbrotCanvas');
    const ctx = canvas.getContext('2d');
    
    // Update UI state
    generateBtn.disabled = true;
    generateBtn.textContent = 'Generating...';
    
    // Get parameters from the UI inputs
    const width = parseInt(document.getElementById('width').value);
    const height = parseInt(document.getElementById('height').value);
    const maxIterations = parseInt(document.getElementById('maxIterations').value);
    
    // Validate inputs
    if (!width || !height || !maxIterations || width < 100 || height < 100 || maxIterations < 10) {
        showToast('Please enter valid parameters (min: 100x100, 10 iterations)', 'error');
        // Reset UI state on validation error
        isGenerating = false;
        generateBtn.disabled = false;
        generateBtn.textContent = 'Generate';
        return;
    }
    canvas.width = width;
    canvas.height = height;
    
    try {
        const startTime = performance.now();
        
        // Make API call to the current endpoint (for now ignore center/zoom parameters)
        const response = await fetch(
            `/api/mandelbrot/generate?width=${width}&height=${height}&maxIterations=${maxIterations}`
        );
        const data = await response.json();
        
        const endTime = performance.now();
        const totalTime = Math.round(endTime - startTime);
        
        if (!data.success) {  // camelCase from JSON serialization
            throw new Error(data.error || 'Generation failed');
        }
        
        // Clear canvas
        ctx.clearRect(0, 0, width, height);
        
        // Create ImageData for efficient pixel manipulation
        const imageData = ctx.createImageData(width, height);
        const pixels = imageData.data;
        
        // Map iteration data to colors - check if data property exists
        if (!data.data || data.data.length === 0) {
            throw new Error('No image data received from API');
        }
        
        for (let i = 0; i < data.data.length; i++) {
            const iterations = data.data[i];
            const [r, g, b] = getColor(iterations, maxIterations);
            
            const pixelIndex = i * 4;
            pixels[pixelIndex] = r;     // Red
            pixels[pixelIndex + 1] = g; // Green
            pixels[pixelIndex + 2] = b; // Blue
            pixels[pixelIndex + 3] = 255; // Alpha
        }
        
        // Draw to canvas
        ctx.putImageData(imageData, 0, 0);
        
        // Update render time info
        document.getElementById('renderTime').textContent = 
            `${data.computeTimeMs || totalTime}ms`;
        
        showToast(`Mandelbrot set generated successfully! GPU: ${data.computeTimeMs || totalTime}ms`, 'success');
        
    } catch (error) {
        console.error('Error generating Mandelbrot set:', error);
        showToast(error.message, 'error');
        
        // Clear canvas on error
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        ctx.fillStyle = '#f0f0f0';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        ctx.fillStyle = '#666';
        ctx.font = '16px Arial';
        ctx.textAlign = 'center';
        ctx.fillText('Generation Failed', canvas.width / 2, canvas.height / 2);
    } finally {
        // Reset UI state
        isGenerating = false;
        generateBtn.disabled = false;
        generateBtn.textContent = 'Generate';
    }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', async function() {
    showToast('Initializing Mandelbrot visualization...', 'info');
    
    // Add event listener to generate button
    const generateBtn = document.getElementById('generateBtn');
    generateBtn.addEventListener('click', generateMandelbrot);
    
    // Check device info first
    await checkDeviceInfo();
    
    // Generate initial Mandelbrot set
    await generateMandelbrot();
});