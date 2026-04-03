let dotNetRef = null;

export function initialize(ref) {
    dotNetRef = ref;
    document.addEventListener("keydown", handleKeyDown);
}

function handleKeyDown(e) {
    if (!dotNetRef) return;
    const isMonaco = e.target.closest(".monaco-editor");
    const isInput = e.target.tagName === "INPUT" || e.target.tagName === "TEXTAREA" || e.target.tagName === "SELECT";

    const isGlobal = (
        (e.ctrlKey && e.key === "1") ||
        (e.ctrlKey && e.key === "2") ||
        (e.ctrlKey && e.key === "3") ||
        (e.ctrlKey && e.key === "s") ||
        (e.ctrlKey && e.key === "n" && !isInput && !isMonaco) ||
        (e.ctrlKey && e.key === "Enter" && !isMonaco)
    );

    if (!isGlobal) return;
    e.preventDefault();

    const shortcut = [
        e.ctrlKey ? "Ctrl" : "",
        e.shiftKey ? "Shift" : "",
        e.key.toUpperCase()
    ].filter(Boolean).join("+");

    dotNetRef.invokeMethodAsync("OnShortcut_Internal", shortcut);
}

export function dispose() {
    document.removeEventListener("keydown", handleKeyDown);
    dotNetRef = null;
}
