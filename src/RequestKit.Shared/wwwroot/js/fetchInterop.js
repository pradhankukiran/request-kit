const activeRequests = new Map();

export async function sendRequest(requestId, method, url, headers, body, timeoutMs, followRedirects) {
    const controller = new AbortController();
    const request = { controller, cancelled: false, timedOut: false };
    activeRequests.set(requestId, request);

    const timeoutId = setTimeout(() => {
        request.timedOut = true;
        controller.abort();
    }, timeoutMs || 30000);

    const start = performance.now();
    try {
        const opts = {
            method: method,
            headers: headers || {},
            signal: controller.signal,
            mode: "cors",
            credentials: "omit",
            redirect: followRedirects === false ? "manual" : "follow"
        };

        if (body && method !== "GET" && method !== "HEAD") {
            opts.body = body;
        }

        const response = await fetch(url, opts);
        const elapsed = performance.now() - start;

        const responseBody = await response.text();

        const responseHeaders = {};
        response.headers.forEach((value, key) => {
            responseHeaders[key] = value;
        });

        return {
            success: true,
            statusCode: response.status,
            statusText: response.statusText,
            headers: responseHeaders,
            body: responseBody,
            responseTimeMs: elapsed,
            responseSizeBytes: new Blob([responseBody]).size,
            errorMessage: null,
            wasCancelled: false
        };
    } catch (e) {
        const elapsed = performance.now() - start;
        const cancelled = request.cancelled === true;
        const timedOut = request.timedOut === true;

        return {
            success: false,
            statusCode: 0,
            statusText: "",
            headers: {},
            body: "",
            responseTimeMs: elapsed,
            responseSizeBytes: 0,
            errorMessage: cancelled
                ? "Request cancelled."
                : e.name === "AbortError" && timedOut
                    ? `Request timed out after ${timeoutMs || 30000}ms`
                    : e.message,
            wasCancelled: cancelled
        };
    } finally {
        clearTimeout(timeoutId);
        activeRequests.delete(requestId);
    }
}

export function cancelRequest(requestId) {
    const request = activeRequests.get(requestId);
    if (!request) {
        return false;
    }

    request.cancelled = true;
    request.controller.abort();
    return true;
}
