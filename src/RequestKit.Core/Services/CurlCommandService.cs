using System.Text;
using RequestKit.Core.Models;
using RequestHttpMethod = RequestKit.Core.Models.HttpMethod;

namespace RequestKit.Core.Services;

public static class CurlCommandService
{
    public static string Export(RequestDefinition request)
    {
        var syncedRequest = RequestUrlSynchronizer.SyncUrlFromParams(request);
        var url = syncedRequest.Url;
        var headers = syncedRequest.Headers
            .Where(header => header.Enabled && !string.IsNullOrWhiteSpace(header.Key))
            .Select(header => new KeyValuePair<string, string>(header.Key, header.Value))
            .ToList();

        switch (syncedRequest.Auth.Type)
        {
            case AuthType.BearerToken:
                headers.Add(new KeyValuePair<string, string>("Authorization", $"Bearer {syncedRequest.Auth.Token}"));
                break;
            case AuthType.BasicAuth:
                break;
            case AuthType.ApiKeyHeader:
                if (!string.IsNullOrWhiteSpace(syncedRequest.Auth.ApiKeyName))
                {
                    headers.Add(new KeyValuePair<string, string>(syncedRequest.Auth.ApiKeyName, syncedRequest.Auth.ApiKeyValue));
                }
                break;
            case AuthType.ApiKeyQueryParam:
                url = RequestUrlSynchronizer.AppendQueryParameter(url, syncedRequest.Auth.ApiKeyName, syncedRequest.Auth.ApiKeyValue);
                break;
            case AuthType.CustomHeader:
                if (!string.IsNullOrWhiteSpace(syncedRequest.Auth.CustomHeaderName))
                {
                    headers.Add(new KeyValuePair<string, string>(syncedRequest.Auth.CustomHeaderName, syncedRequest.Auth.CustomHeaderValue));
                }
                break;
        }

        var builder = new StringBuilder();
        builder.Append("curl");

        if (syncedRequest.Method != RequestHttpMethod.GET)
        {
            builder.Append(" -X ");
            builder.Append(syncedRequest.Method);
        }

        foreach (var header in headers)
        {
            builder.Append(" -H ");
            builder.Append(Quote($"{header.Key}: {header.Value}"));
        }

        if (syncedRequest.Auth.Type == AuthType.BasicAuth)
        {
            builder.Append(" -u ");
            builder.Append(Quote($"{syncedRequest.Auth.Username}:{syncedRequest.Auth.Password}"));
        }

        switch (syncedRequest.Body.Type)
        {
            case BodyType.RawJson:
                EnsureContentType(headers, "application/json", builder);
                builder.Append(" --data-raw ");
                builder.Append(Quote(syncedRequest.Body.RawContent));
                break;
            case BodyType.RawXml:
                EnsureContentType(headers, "application/xml", builder);
                builder.Append(" --data-raw ");
                builder.Append(Quote(syncedRequest.Body.RawContent));
                break;
            case BodyType.RawText:
                EnsureContentType(headers, "text/plain", builder);
                builder.Append(" --data-raw ");
                builder.Append(Quote(syncedRequest.Body.RawContent));
                break;
            case BodyType.FormUrlEncoded:
                if (!headers.Any(header => header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)))
                {
                    builder.Append(" -H ");
                    builder.Append(Quote("Content-Type: application/x-www-form-urlencoded"));
                }

                foreach (var field in syncedRequest.Body.FormData.Where(field => field.Enabled && !string.IsNullOrWhiteSpace(field.Key)))
                {
                    builder.Append(" --data-urlencode ");
                    builder.Append(Quote($"{field.Key}={field.Value}"));
                }

                break;
        }

        builder.Append(' ');
        builder.Append(Quote(url));
        return builder.ToString();
    }

    public static bool TryImport(string rawCommand, out RequestDefinition request, out string? error)
    {
        request = new();
        error = null;

        var normalized = Normalize(rawCommand);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            error = "Paste a cURL command to import.";
            return false;
        }

        var tokens = Tokenize(normalized);
        if (tokens.Count == 0)
        {
            error = "Paste a valid cURL command to import.";
            return false;
        }

        var startIndex = string.Equals(tokens[0], "curl", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        var url = string.Empty;
        RequestHttpMethod? method = null;
        var headers = new List<KeyValueEntry>();
        var formFields = new List<KeyValueEntry>();
        var rawBodySegments = new List<string>();
        var useQueryStringForData = false;
        var sawBody = false;
        var auth = new AuthConfig();

        for (var index = startIndex; index < tokens.Count; index++)
        {
            var token = tokens[index];
            switch (token)
            {
                case "-X":
                case "--request":
                    if (!TryReadValue(tokens, ref index, out var methodToken) || !Enum.TryParse<RequestHttpMethod>(methodToken, true, out var parsedMethod))
                    {
                        error = "The cURL command uses an unsupported HTTP method.";
                        return false;
                    }

                    method = parsedMethod;
                    break;
                case "-H":
                case "--header":
                    if (!TryReadValue(tokens, ref index, out var headerToken))
                    {
                        error = "A header flag is missing its value.";
                        return false;
                    }

                    if (!TryParseHeader(headerToken, headers, ref auth))
                    {
                        error = $"Could not parse header: {headerToken}";
                        return false;
                    }

                    break;
                case "-d":
                case "--data":
                case "--data-raw":
                case "--data-binary":
                case "--data-ascii":
                    if (!TryReadValue(tokens, ref index, out var dataToken))
                    {
                        error = "A data flag is missing its value.";
                        return false;
                    }

                    rawBodySegments.Add(dataToken);
                    sawBody = true;
                    break;
                case "--data-urlencode":
                    if (!TryReadValue(tokens, ref index, out var encodedToken))
                    {
                        error = "A data-urlencode flag is missing its value.";
                        return false;
                    }

                    formFields.Add(ParseDataField(encodedToken));
                    sawBody = true;
                    break;
                case "-F":
                case "--form":
                    error = "Multipart cURL imports are not supported yet.";
                    return false;
                case "-u":
                case "--user":
                    if (!TryReadValue(tokens, ref index, out var credentialToken))
                    {
                        error = "A user flag is missing its value.";
                        return false;
                    }

                    ParseBasicAuth(credentialToken, ref auth);
                    break;
                case "-I":
                case "--head":
                    method = RequestHttpMethod.HEAD;
                    break;
                case "-G":
                case "--get":
                    useQueryStringForData = true;
                    method = RequestHttpMethod.GET;
                    break;
                case "--url":
                    if (!TryReadValue(tokens, ref index, out var urlToken))
                    {
                        error = "The --url flag is missing its value.";
                        return false;
                    }

                    url = urlToken;
                    break;
                default:
                    if (LooksLikeUrl(token))
                    {
                        url = token;
                    }

                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            error = "The cURL command does not include a URL.";
            return false;
        }

        var body = new RequestBody();
        if (useQueryStringForData && formFields.Count > 0)
        {
            foreach (var field in formFields.Where(field => !string.IsNullOrWhiteSpace(field.Key)))
            {
                url = RequestUrlSynchronizer.AppendQueryParameter(url, field.Key, field.Value);
            }

            formFields.Clear();
            sawBody = false;
        }

        if (formFields.Count > 0)
        {
            body = new RequestBody
            {
                Type = BodyType.FormUrlEncoded,
                FormData = formFields
            };
        }
        else if (rawBodySegments.Count > 0)
        {
            var rawBody = string.Join("&", rawBodySegments);
            body = new RequestBody
            {
                Type = InferRawBodyType(rawBody, headers),
                RawContent = rawBody
            };
        }

        request = RequestUrlSynchronizer.SyncParamsFromUrl(new RequestDefinition
        {
            Name = BuildRequestName(url),
            Method = method ?? (sawBody ? RequestHttpMethod.POST : RequestHttpMethod.GET),
            Url = url,
            Headers = headers,
            Auth = auth,
            Body = body
        });

        return true;
    }

    private static void EnsureContentType(List<KeyValuePair<string, string>> headers, string contentType, StringBuilder builder)
    {
        if (headers.Any(header => header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        builder.Append(" -H ");
        builder.Append(Quote($"Content-Type: {contentType}"));
    }

    private static string BuildRequestName(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return "Imported cURL Request";
        }

        var segment = uri.Segments.LastOrDefault()?.Trim('/');
        return string.IsNullOrWhiteSpace(segment)
            ? $"{uri.Host} Request"
            : $"{segment} Request";
    }

    private static BodyType InferRawBodyType(string body, IEnumerable<KeyValueEntry> headers)
    {
        var contentType = headers
            .FirstOrDefault(header => header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            ?.Value;

        return ContentTypeDetector.Detect(body, contentType) switch
        {
            ContentType.Json => BodyType.RawJson,
            ContentType.Xml => BodyType.RawXml,
            _ => BodyType.RawText
        };
    }

    private static bool TryParseHeader(string token, List<KeyValueEntry> headers, ref AuthConfig auth)
    {
        var separatorIndex = token.IndexOf(':');
        if (separatorIndex <= 0)
        {
            return false;
        }

        var key = token[..separatorIndex].Trim();
        var value = token[(separatorIndex + 1)..].Trim();

        if (key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
        {
            if (TryParseAuthorizationHeader(value, ref auth))
            {
                return true;
            }
        }

        headers.Add(new KeyValueEntry
        {
            Key = key,
            Value = value
        });

        return true;
    }

    private static bool TryParseAuthorizationHeader(string value, ref AuthConfig auth)
    {
        if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            auth = auth with
            {
                Type = AuthType.BearerToken,
                Token = value["Bearer ".Length..]
            };
            return true;
        }

        if (value.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(value["Basic ".Length..]));
                var separator = decoded.IndexOf(':');
                if (separator > -1)
                {
                    auth = auth with
                    {
                        Type = AuthType.BasicAuth,
                        Username = decoded[..separator],
                        Password = decoded[(separator + 1)..]
                    };
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    private static void ParseBasicAuth(string token, ref AuthConfig auth)
    {
        var separator = token.IndexOf(':');
        auth = auth with
        {
            Type = AuthType.BasicAuth,
            Username = separator >= 0 ? token[..separator] : token,
            Password = separator >= 0 ? token[(separator + 1)..] : string.Empty
        };
    }

    private static KeyValueEntry ParseDataField(string token)
    {
        var separator = token.IndexOf('=');
        return separator > -1
            ? new KeyValueEntry
            {
                Key = token[..separator],
                Value = token[(separator + 1)..]
            }
            : new KeyValueEntry
            {
                Key = token,
                Value = string.Empty
            };
    }

    private static bool LooksLikeUrl(string token) =>
        token.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || token.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static bool TryReadValue(IReadOnlyList<string> tokens, ref int index, out string value)
    {
        if (index + 1 >= tokens.Count)
        {
            value = string.Empty;
            return false;
        }

        index++;
        value = tokens[index];
        return true;
    }

    private static List<string> Tokenize(string command)
    {
        var tokens = new List<string>();
        var buffer = new StringBuilder();
        var quote = '\0';
        var escaping = false;

        foreach (var character in command)
        {
            if (escaping)
            {
                buffer.Append(character);
                escaping = false;
                continue;
            }

            if (character == '\\' && quote != '\'')
            {
                escaping = true;
                continue;
            }

            if (quote != '\0')
            {
                if (character == quote)
                {
                    quote = '\0';
                }
                else
                {
                    buffer.Append(character);
                }

                continue;
            }

            if (character is '"' or '\'')
            {
                quote = character;
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (buffer.Length > 0)
                {
                    tokens.Add(buffer.ToString());
                    buffer.Clear();
                }

                continue;
            }

            buffer.Append(character);
        }

        if (buffer.Length > 0)
        {
            tokens.Add(buffer.ToString());
        }

        return tokens;
    }

    private static string Normalize(string command)
    {
        return command
            .Replace("\\\r\n", " ", StringComparison.Ordinal)
            .Replace("\\\n", " ", StringComparison.Ordinal)
            .Trim();
    }

    private static string Quote(string value)
    {
        var normalized = value ?? string.Empty;
        return $"'{normalized.Replace("'", "'\"'\"'", StringComparison.Ordinal)}'";
    }
}
