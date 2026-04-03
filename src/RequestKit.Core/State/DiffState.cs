using RequestKit.Core.Models;

namespace RequestKit.Core.State;

public class DiffState
{
    public event Action? OnChange;

    public string LeftText { get; private set; } = "";
    public string RightText { get; private set; } = "";
    public DiffViewMode ViewMode { get; private set; } = DiffViewMode.SideBySide;
    public DiffResult? Result { get; private set; }

    public void SetLeftText(string text) { LeftText = text; OnChange?.Invoke(); }
    public void SetRightText(string text) { RightText = text; OnChange?.Invoke(); }
    public void SetViewMode(DiffViewMode mode) { ViewMode = mode; OnChange?.Invoke(); }
    public void SetResult(DiffResult result) { Result = result; OnChange?.Invoke(); }
}
