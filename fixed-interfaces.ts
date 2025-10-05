// Auto-generated TypeScript interfaces from C# response classes
// DO NOT EDIT MANUALLY - This file is generated during build
// Generated on: 2025-10-05 04:20:38 UTC

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

