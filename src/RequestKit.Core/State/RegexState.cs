using RequestKit.Core.Models;

namespace RequestKit.Core.State;

public class RegexState
{
    public event Action? OnChange;

    public string Pattern { get; private set; } = "";
    public string TestString { get; private set; } = "";
    public HashSet<RegexFlag> Flags { get; private set; } = [RegexFlag.Global];
    public List<RegexMatch> Matches { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public List<RegexExplanationPart> Explanation { get; private set; } = [];

    public void SetPattern(string pattern) { Pattern = pattern; OnChange?.Invoke(); }
    public void SetTestString(string text) { TestString = text; OnChange?.Invoke(); }
    public void ToggleFlag(RegexFlag flag)
    {
        if (Flags.Contains(flag)) Flags.Remove(flag); else Flags.Add(flag);
        OnChange?.Invoke();
    }
    public void SetResults(List<RegexMatch> matches, string? error, List<RegexExplanationPart> explanation)
    {
        Matches = matches; ErrorMessage = error; Explanation = explanation;
        OnChange?.Invoke();
    }
}
