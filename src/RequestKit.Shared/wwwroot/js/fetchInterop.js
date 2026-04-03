export async function sendRequest(method, url, headers, body, timeoutMs) {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), timeoutMs || 30000);

    const start = performance.now();
    try {
        const opts = {
            method: method,
            headers: headers || {},
            signal: controller.signal,
            mode: "cors",
            credentials: "omit"
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
            errorMessage: null
        };
    } catch (e) {
        const elapsed = performance.now() - start;
        return {
            success: false,
            statusCode: 0,
            statusText: "",
            headers: {},
            body: "",
            responseTimeMs: elapsed,
            responseSizeBytes: 0,
            errorMessage: e.name === "AbortError" ? `Request timed out after ${timeoutMs || 30000}ms` : e.message
        };
    } finally {
        clearTimeout(timeoutId);
    }
}
