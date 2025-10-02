// Type definitions for Mandelbrot API responses and frontend interfaces

export interface MandelbrotResponse {
    success: boolean;
    error?: string;
    width: number;
    height: number;
    maxIterations: number;
    data?: number[];
    computeTimeMs?: number;
    acceleratorType?: string;
    acceleratorName?: string;
    
    // Coordinate mapping data from CUDA calculations
    viewMinReal?: number;
    viewMaxReal?: number;
    viewMinImaginary?: number;
    viewMaxImaginary?: number;
    centerReal?: number;
    centerImaginary?: number;
    zoom?: number;
}

export interface DeviceInfo {
    acceleratorType: string;
    name: string;
    maxNumThreads: number;
    maxGroupSize: string;
    warpSize: number;
    numMultiprocessors: number;
    memorySize: number;
    maxConstantMemory: number;
    maxSharedMemoryPerGroup: number;
}

export interface DeviceResponse {
    currentDevice?: DeviceInfo;
    hasCudaDevice: boolean;
    kernelPrecompiled?: boolean;
    error?: string;
    statusMessage?: string;
}



export type ToastType = 'info' | 'success' | 'error';