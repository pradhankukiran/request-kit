namespace RequestKit.Core.Models;

public record RegexMatch
{
    public int Index { get; init; }
    public int Length { get; init; }
    public string Value { get; init; } = "";
    public List<RegexGroup> Groups { get; init; } = [];
}

public record RegexGroup
{
    public int GroupIndex { get; init; }
    public string? Name { get; init; }
    public string Value { get; init; } = "";
    public int Index { get; init; }
    public int Length { get; init; }
}

public record RegexExplanationPart
{
    public string Token { get; init; } = "";
    public string Description { get; init; } = "";
}

public record CommonPattern
{
    public string Name { get; init; } = "";
    public string Pattern { get; init; } = "";
    public string Description { get; init; } = "";
    public string SampleMatch { get; init; } = "";
    public string Category { get; init; } = "";
}
