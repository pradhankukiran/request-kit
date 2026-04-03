namespace RequestKit.Core.Data;

public static class CommonHeaders
{
    public static readonly string[] Names =
    [
        "Accept", "Accept-Charset", "Accept-Encoding", "Accept-Language",
        "Authorization", "Cache-Control", "Content-Disposition", "Content-Encoding",
        "Content-Length", "Content-Type", "Cookie", "Date", "ETag", "Expires",
        "Host", "If-Match", "If-Modified-Since", "If-None-Match", "Origin",
        "Pragma", "Range", "Referer", "Set-Cookie", "User-Agent",
        "X-Api-Key", "X-Correlation-Id", "X-Forwarded-For", "X-Forwarded-Host",
        "X-Forwarded-Proto", "X-Request-Id", "X-Requested-With"
    ];

    public static readonly string[] ContentTypes =
    [
        "application/json", "application/xml", "application/x-www-form-urlencoded",
        "multipart/form-data", "text/plain", "text/html", "text/csv",
        "application/octet-stream", "application/pdf"
    ];
}
