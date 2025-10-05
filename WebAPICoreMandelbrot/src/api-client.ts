// Simple API client for Mandelbrot API
// Only includes GET requests as needed by the application

export interface QueryParams {
    [key: string]: string | number | boolean | undefined;
}

/**
 * Simple typed GET request function for API calls
 * @param endpoint - API endpoint (e.g., '/api/mandelbrot/generate')
 * @param params - Query parameters to append to URL
 * @returns Promise with typed response data
 */
export async function apiGet<T>(endpoint: string, params?: QueryParams): Promise<T> {
    // Build URL with query parameters
    const url = new URL(endpoint, window.location.origin);
    if (params) {
        Object.entries(params).forEach(([key, value]) => {
            if (value !== undefined && value !== null) {
                url.searchParams.append(key, String(value));
            }
        });
    }

    const response = await fetch(url.toString());

    if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
    }

    return (await response.json()) as T;
}
