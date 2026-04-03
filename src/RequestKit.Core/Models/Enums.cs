namespace RequestKit.Core.Models;

public enum HttpMethod
{
    GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS
}

public enum AuthType
{
    None, BearerToken, BasicAuth, ApiKeyHeader, ApiKeyQueryParam, CustomHeader
}

public enum BodyType
{
    None, RawJson, RawXml, RawText, FormData
}

public enum ContentType
{
    Json, Xml, Html, PlainText, Unknown
}

public enum ToolMode
{
    ApiClient, RegexBuilder, DiffViewer
}

public enum DiffViewMode
{
    SideBySide, Unified
}

public enum RegexFlag
{
    Global, CaseInsensitive, Multiline, DotAll, Unicode
}
