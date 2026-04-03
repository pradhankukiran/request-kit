using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using RequestKit.Core.Models;

namespace RequestKit.Core.Services;

public static class DiffEngine
{
    private const int MaxInputLength = 100_000;

    public static DiffResult ComputeDiff(string left, string right)
    {
        if ((left?.Length ?? 0) > MaxInputLength || (right?.Length ?? 0) > MaxInputLength)
        {
            return new DiffResult
            {
                Lines = [new DiffLine
                {
                    LeftText = "Input too large to diff",
                    RightText = "Input too large to diff",
                    Type = DiffLineType.Modified
                }]
            };
        }

        var diffBuilder = new SideBySideDiffBuilder(new Differ());
        var diff = diffBuilder.BuildDiffModel(left ?? "", right ?? "");

        var lines = new List<DiffLine>();
        int maxLines = Math.Max(diff.OldText.Lines.Count, diff.NewText.Lines.Count);

        for (int i = 0; i < maxLines; i++)
        {
            var oldLine = i < diff.OldText.Lines.Count ? diff.OldText.Lines[i] : null;
            var newLine = i < diff.NewText.Lines.Count ? diff.NewText.Lines[i] : null;

            var type = ClassifyLine(oldLine, newLine);

            var diffLine = new DiffLine
            {
                LeftLineNumber = oldLine?.Position,
                RightLineNumber = newLine?.Position,
                LeftText = oldLine?.Text ?? "",
                RightText = newLine?.Text ?? "",
                Type = type,
                InlineChanges = type == DiffLineType.Modified
                    ? BuildInlineChanges(oldLine, newLine)
                    : null
            };

            lines.Add(diffLine);
        }

        int added = 0, removed = 0, modified = 0, unchanged = 0;
        foreach (var line in lines)
        {
            switch (line.Type)
            {
                case DiffLineType.Added: added++; break;
                case DiffLineType.Removed: removed++; break;
                case DiffLineType.Modified: modified++; break;
                case DiffLineType.Unchanged: unchanged++; break;
            }
        }

        return new DiffResult
        {
            Lines = lines,
            LinesAdded = added,
            LinesRemoved = removed,
            LinesModified = modified,
            LinesUnchanged = unchanged
        };
    }

    private static DiffLineType ClassifyLine(DiffPiece? oldLine, DiffPiece? newLine)
    {
        var oldType = oldLine?.Type ?? ChangeType.Imaginary;
        var newType = newLine?.Type ?? ChangeType.Imaginary;

        if (oldType == ChangeType.Unchanged && newType == ChangeType.Unchanged)
            return DiffLineType.Unchanged;
        if (oldType == ChangeType.Modified || newType == ChangeType.Modified)
            return DiffLineType.Modified;
        if (oldType == ChangeType.Deleted && newType == ChangeType.Imaginary)
            return DiffLineType.Removed;
        if (oldType == ChangeType.Imaginary && newType == ChangeType.Inserted)
            return DiffLineType.Added;

        // Fallback: if one side is deleted and the other inserted on the same row
        if (oldType == ChangeType.Deleted && newType == ChangeType.Inserted)
            return DiffLineType.Modified;

        return DiffLineType.Unchanged;
    }

    private static List<InlineChange>? BuildInlineChanges(DiffPiece? oldLine, DiffPiece? newLine)
    {
        if (oldLine?.SubPieces == null && newLine?.SubPieces == null)
            return null;

        var changes = new List<InlineChange>();

        // Build removed spans from old line sub-pieces
        if (oldLine?.SubPieces != null)
        {
            int pos = 0;
            foreach (var piece in oldLine.SubPieces)
            {
                if (piece.Type is ChangeType.Deleted or ChangeType.Modified)
                {
                    changes.Add(new InlineChange
                    {
                        StartIndex = pos,
                        Length = piece.Text?.Length ?? 0,
                        Type = DiffLineType.Removed
                    });
                }
                pos += piece.Text?.Length ?? 0;
            }
        }

        // Build added spans from new line sub-pieces
        if (newLine?.SubPieces != null)
        {
            int pos = 0;
            foreach (var piece in newLine.SubPieces)
            {
                if (piece.Type is ChangeType.Inserted or ChangeType.Modified)
                {
                    changes.Add(new InlineChange
                    {
                        StartIndex = pos,
                        Length = piece.Text?.Length ?? 0,
                        Type = DiffLineType.Added
                    });
                }
                pos += piece.Text?.Length ?? 0;
            }
        }

        return changes.Count > 0 ? changes : null;
    }
}
