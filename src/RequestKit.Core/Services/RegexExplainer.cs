using RequestKit.Core.Models;

namespace RequestKit.Core.Services;

public static class RegexExplainer
{
    public static List<RegexExplanationPart> Explain(string pattern)
    {
        var parts = new List<RegexExplanationPart>();
        if (string.IsNullOrEmpty(pattern)) return parts;

        int i = 0;
        while (i < pattern.Length)
        {
            var (token, desc, consumed) = ParseToken(pattern, i);
            parts.Add(new RegexExplanationPart { Token = token, Description = desc });
            i += consumed;
        }

        return parts;
    }

    private static (string Token, string Description, int Consumed) ParseToken(string pattern, int pos)
    {
        char c = pattern[pos];

        // Escape sequences
        if (c == '\\' && pos + 1 < pattern.Length)
        {
            char next = pattern[pos + 1];
            return next switch
            {
                'd' => ("\\d", "Digit (0-9)", 2),
                'D' => ("\\D", "Non-digit", 2),
                'w' => ("\\w", "Word character (a-z, A-Z, 0-9, _)", 2),
                'W' => ("\\W", "Non-word character", 2),
                's' => ("\\s", "Whitespace (space, tab, newline)", 2),
                'S' => ("\\S", "Non-whitespace", 2),
                'b' => ("\\b", "Word boundary", 2),
                'B' => ("\\B", "Non-word boundary", 2),
                'n' => ("\\n", "Newline", 2),
                'r' => ("\\r", "Carriage return", 2),
                't' => ("\\t", "Tab", 2),
                '0' => ("\\0", "Null character", 2),
                _ => ($"\\{next}", $"Escaped '{next}' (literal)", 2)
            };
        }

        // Character classes [...]
        if (c == '[')
        {
            int end = FindClosingBracket(pattern, pos);
            string token = pattern[pos..(end + 1)];
            bool negated = token.Length > 1 && token[1] == '^';
            string desc = negated ? "Negated character class: none of " + token[2..^1] : "Character class: one of " + token[1..^1];
            return (token, desc, token.Length);
        }

        // Groups (...) with lookahead/lookbehind/non-capturing
        if (c == '(')
        {
            if (pos + 1 < pattern.Length && pattern[pos + 1] == '?')
            {
                if (pos + 2 < pattern.Length)
                {
                    char modifier = pattern[pos + 2];
                    if (modifier == ':')
                    {
                        int end = FindClosingParen(pattern, pos);
                        string token = pattern[pos..(end + 1)];
                        return (token, "Non-capturing group", token.Length);
                    }
                    if (modifier == '=')
                    {
                        int end = FindClosingParen(pattern, pos);
                        string token = pattern[pos..(end + 1)];
                        return (token, "Positive lookahead", token.Length);
                    }
                    if (modifier == '!')
                    {
                        int end = FindClosingParen(pattern, pos);
                        string token = pattern[pos..(end + 1)];
                        return (token, "Negative lookahead", token.Length);
                    }
                    if (modifier == '<' && pos + 3 < pattern.Length)
                    {
                        if (pattern[pos + 3] == '=')
                        {
                            int end = FindClosingParen(pattern, pos);
                            string token = pattern[pos..(end + 1)];
                            return (token, "Positive lookbehind", token.Length);
                        }
                        if (pattern[pos + 3] == '!')
                        {
                            int end = FindClosingParen(pattern, pos);
                            string token = pattern[pos..(end + 1)];
                            return (token, "Negative lookbehind", token.Length);
                        }
                        // Named group (?<name>...)
                        int nameEnd = pattern.IndexOf('>', pos + 3);
                        if (nameEnd > 0)
                        {
                            int end = FindClosingParen(pattern, pos);
                            string token = pattern[pos..(end + 1)];
                            string name = pattern[(pos + 3)..nameEnd];
                            return (token, $"Named capturing group '{name}'", token.Length);
                        }
                    }
                }
            }

            // Regular capturing group
            int groupEnd = FindClosingParen(pattern, pos);
            string groupToken = pattern[pos..(groupEnd + 1)];
            return (groupToken, "Capturing group", groupToken.Length);
        }

        // Quantifiers
        if (c == '{')
        {
            int end = pattern.IndexOf('}', pos);
            if (end > pos)
            {
                string token = pattern[pos..(end + 1)];
                bool lazy = end + 1 < pattern.Length && pattern[end + 1] == '?';
                if (lazy) token += "?";
                string inner = pattern[(pos + 1)..end];
                string desc;
                if (inner.Contains(','))
                {
                    var split = inner.Split(',');
                    if (split.Length >= 2)
                    {
                        desc = split[1].Length == 0
                            ? $"{split[0]} or more times"
                            : $"Between {split[0]} and {split[1]} times";
                    }
                    else
                    {
                        desc = $"{inner} times";
                    }
                }
                else
                {
                    desc = $"Exactly {inner} times";
                }
                if (lazy) desc += " (lazy)";
                return (token, desc, token.Length);
            }
        }

        if (c == '*')
        {
            bool lazy = pos + 1 < pattern.Length && pattern[pos + 1] == '?';
            return lazy ? ("*?", "Zero or more (lazy)", 2) : ("*", "Zero or more", 1);
        }
        if (c == '+')
        {
            bool lazy = pos + 1 < pattern.Length && pattern[pos + 1] == '?';
            return lazy ? ("+?", "One or more (lazy)", 2) : ("+", "One or more", 1);
        }
        if (c == '?')
        {
            bool lazy = pos + 1 < pattern.Length && pattern[pos + 1] == '?';
            return lazy ? ("??", "Optional (lazy)", 2) : ("?", "Optional (zero or one)", 1);
        }

        // Anchors and specials
        return c switch
        {
            '.' => (".", "Any character (except newline)", 1),
            '^' => ("^", "Start of string/line", 1),
            '$' => ("$", "End of string/line", 1),
            '|' => ("|", "Alternation (OR)", 1),
            _ => (c.ToString(), $"Literal '{c}'", 1)
        };
    }

    private static int FindClosingBracket(string pattern, int openPos)
    {
        int i = openPos + 1;
        // Allow ] as first char in class
        if (i < pattern.Length && pattern[i] == '^') i++;
        if (i < pattern.Length && pattern[i] == ']') i++;
        while (i < pattern.Length)
        {
            if (pattern[i] == '\\' && i + 1 < pattern.Length)
            {
                i += 2;
                continue;
            }
            if (pattern[i] == ']') return i;
            i++;
        }
        return pattern.Length - 1;
    }

    private static int FindClosingParen(string pattern, int openPos)
    {
        int depth = 1;
        int i = openPos + 1;
        while (i < pattern.Length && depth > 0)
        {
            if (pattern[i] == '\\' && i + 1 < pattern.Length)
            {
                i += 2;
                continue;
            }
            if (pattern[i] == '(') depth++;
            else if (pattern[i] == ')') depth--;
            if (depth > 0) i++;
        }
        return i < pattern.Length ? i : pattern.Length - 1;
    }
}
