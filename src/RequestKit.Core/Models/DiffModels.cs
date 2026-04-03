namespace RequestKit.Core.Models;

public enum DiffLineType
{
    Unchanged, Added, Removed, Modified
}

public record DiffLine
{
    public int? LeftLineNumber { get; init; }
    public int? RightLineNumber { get; init; }
    public string LeftText { get; init; } = "";
    public string RightText { get; init; } = "";
    public DiffLineType Type { get; init; }
    public List<InlineChange>? InlineChanges { get; init; }
}

public record InlineChange
{
    public int StartIndex { get; init; }
    public int Length { get; init; }
    public DiffLineType Type { get; init; }
}

public record DiffResult
{
    public List<DiffLine> Lines { get; init; } = [];
    public int LinesAdded { get; init; }
    public int LinesRemoved { get; init; }
    public int LinesModified { get; init; }
    public int LinesUnchanged { get; init; }
}

public record JsonDiffNode
{
    public string Path { get; init; } = "";
    public string Key { get; init; } = "";
    public DiffLineType Type { get; init; }
    public string? LeftValue { get; init; }
    public string? RightValue { get; init; }
    public List<JsonDiffNode> Children { get; init; } = [];
}
