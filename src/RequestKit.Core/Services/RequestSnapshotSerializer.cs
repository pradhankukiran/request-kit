using System.Text.Json;
using System.Text.Json.Serialization;
using RequestKit.Core.Models;

namespace RequestKit.Core.Services;

public static class RequestSnapshotSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string SerializeRequest(RequestDefinition request) =>
        JsonSerializer.Serialize(request, JsonOptions);

    public static string SerializeResponse(ResponseData response) =>
        JsonSerializer.Serialize(response, JsonOptions);

    public static RequestDefinition? DeserializeRequest(string? snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot))
            return null;

        return JsonSerializer.Deserialize<RequestDefinition>(snapshot, JsonOptions);
    }

    public static ResponseData? DeserializeResponse(string? snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot))
            return null;

        return JsonSerializer.Deserialize<ResponseData>(snapshot, JsonOptions);
    }

    public static HistoryEntry CreateHistoryEntry(RequestDefinition request, ResponseData response)
    {
        return new HistoryEntry
        {
            RequestName = request.Name,
            Method = request.Method,
            Url = request.Url,
            StatusCode = response.StatusCode,
            ResponseTimeMs = response.ResponseTimeMs,
            Timestamp = response.ReceivedAt,
            RequestSnapshot = SerializeRequest(request),
            ResponseSnapshot = SerializeResponse(response)
        };
    }
}
