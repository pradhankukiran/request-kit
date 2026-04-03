using System.Text.RegularExpressions;

namespace RequestKit.Core.Services;

public static partial class EnvironmentVariableResolver
{
    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex VariablePattern();

    public static string Resolve(string input, IReadOnlyDictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(input)) return input;

        return VariablePattern().Replace(input, match =>
        {
            var key = match.Groups[1].Value;
            return variables.TryGetValue(key, out var value) ? value : match.Value;
        });
    }
}
