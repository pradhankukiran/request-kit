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

        var leftLines = (left ?? "").Split('\n');
        var rightLines = (right ?? "").Split('\n');

        var lcs = ComputeLcs(leftLines, rightLines);
        var rawLines = new List<DiffLine>();

        int li = 0, ri = 0, lcsIdx = 0;
        int leftNum = 1, rightNum = 1;

        while (li < leftLines.Length || ri < rightLines.Length)
        {
            if (lcsIdx < lcs.Count && li < leftLines.Length && ri < rightLines.Length
                && leftLines[li] == lcs[lcsIdx] && rightLines[ri] == lcs[lcsIdx])
            {
                rawLines.Add(new DiffLine
                {
                    LeftLineNumber = leftNum++,
                    RightLineNumber = rightNum++,
                    LeftText = leftLines[li],
                    RightText = rightLines[ri],
                    Type = DiffLineType.Unchanged
                });
                li++; ri++; lcsIdx++;
            }
            else if (li < leftLines.Length
                     && (lcsIdx >= lcs.Count || leftLines[li] != lcs[lcsIdx]))
            {
                rawLines.Add(new DiffLine
                {
                    LeftLineNumber = leftNum++,
                    LeftText = leftLines[li],
                    Type = DiffLineType.Removed
                });
                li++;
            }
            else if (ri < rightLines.Length)
            {
                rawLines.Add(new DiffLine
                {
                    RightLineNumber = rightNum++,
                    RightText = rightLines[ri],
                    Type = DiffLineType.Added
                });
                ri++;
            }
        }

        // Detect adjacent Remove+Add pairs and upgrade them to Modified with inline changes
        var result = new List<DiffLine>();
        for (int i = 0; i < rawLines.Count; i++)
        {
            if (i + 1 < rawLines.Count
                && rawLines[i].Type == DiffLineType.Removed
                && rawLines[i + 1].Type == DiffLineType.Added)
            {
                var inlineChanges = ComputeInlineChanges(rawLines[i].LeftText, rawLines[i + 1].RightText);
                result.Add(rawLines[i] with
                {
                    RightLineNumber = rawLines[i + 1].RightLineNumber,
                    RightText = rawLines[i + 1].RightText,
                    Type = DiffLineType.Modified,
                    InlineChanges = inlineChanges
                });
                i++; // skip the Added line, it's merged
            }
            else
            {
                result.Add(rawLines[i]);
            }
        }

        int added = 0, removed = 0, modified = 0, unchanged = 0;
        foreach (var line in result)
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
            Lines = result,
            LinesAdded = added,
            LinesRemoved = removed,
            LinesModified = modified,
            LinesUnchanged = unchanged
        };
    }

    private static List<string> ComputeLcs(string[] a, string[] b)
    {
        int m = a.Length, n = b.Length;
        var dp = new int[m + 1, n + 1];

        for (int i = 1; i <= m; i++)
            for (int j = 1; j <= n; j++)
                dp[i, j] = a[i - 1] == b[j - 1]
                    ? dp[i - 1, j - 1] + 1
                    : Math.Max(dp[i - 1, j], dp[i, j - 1]);

        var result = new List<string>();
        int x = m, y = n;
        while (x > 0 && y > 0)
        {
            if (a[x - 1] == b[y - 1])
            {
                result.Add(a[x - 1]);
                x--; y--;
            }
            else if (dp[x - 1, y] > dp[x, y - 1])
                x--;
            else
                y--;
        }
        result.Reverse();
        return result;
    }

    /// <summary>
    /// Char-by-char comparison for inline (within-line) highlighting.
    /// Returns changes relative to the left and right strings.
    /// </summary>
    private static List<InlineChange> ComputeInlineChanges(string left, string right)
    {
        var changes = new List<InlineChange>();
        int prefixLen = 0;
        int minLen = Math.Min(left.Length, right.Length);

        // Find common prefix
        while (prefixLen < minLen && left[prefixLen] == right[prefixLen])
            prefixLen++;

        // Find common suffix (not overlapping with prefix)
        int suffixLen = 0;
        while (suffixLen < minLen - prefixLen
               && left[left.Length - 1 - suffixLen] == right[right.Length - 1 - suffixLen])
            suffixLen++;

        int leftChangeLen = left.Length - prefixLen - suffixLen;
        int rightChangeLen = right.Length - prefixLen - suffixLen;

        if (leftChangeLen > 0)
        {
            changes.Add(new InlineChange
            {
                StartIndex = prefixLen,
                Length = leftChangeLen,
                Type = DiffLineType.Removed
            });
        }

        if (rightChangeLen > 0)
        {
            changes.Add(new InlineChange
            {
                StartIndex = prefixLen,
                Length = rightChangeLen,
                Type = DiffLineType.Added
            });
        }

        return changes;
    }
}
