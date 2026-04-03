const editors = new Map();
let monacoReady = false;
let dotNetRef = null;

export async function initialize(ref) {
    dotNetRef = ref;
    if (monacoReady) return true;
    await loadMonaco();
    monacoReady = true;
    return true;
}

function loadMonaco() {
    return new Promise((resolve, reject) => {
        if (window.monaco) { resolve(); return; }

        const LOAD_TIMEOUT_MS = 10000;
        const CDN_URL = "https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.52.2/min/vs";

        const timer = setTimeout(() => {
            reject(new Error("Monaco editor failed to load within " + LOAD_TIMEOUT_MS / 1000 + " seconds"));
        }, LOAD_TIMEOUT_MS);

        const script = document.createElement("script");
        // TODO: Add SRI hash (integrity attribute) once a stable hash is available
        // for this CDN version. The hash must be recomputed on each version bump.
        script.src = CDN_URL + "/loader.min.js";
        script.crossOrigin = "anonymous";
        script.onload = () => {
            window.require.config({ paths: { vs: CDN_URL } });
            window.require(["vs/editor/editor.main"], () => {
                clearTimeout(timer);
                resolve();
            });
        };
        script.onerror = () => {
            clearTimeout(timer);
            reject(new Error("Failed to load Monaco editor from CDN: " + script.src));
        };
        document.head.appendChild(script);
    });
}

export function createEditor(elementId, initialValue, language, readOnly) {
    if (!monacoReady) throw new Error("Monaco not initialized");
    const container = document.getElementById(elementId);
    if (!container) throw new Error(`Element ${elementId} not found`);

    // Dispose existing editor if any
    if (editors.has(elementId)) {
        editors.get(elementId).dispose();
    }

    const editor = monaco.editor.create(container, {
        value: initialValue || "",
        language: language || "plaintext",
        theme: "vs",
        minimap: { enabled: false },
        fontSize: 13,
        fontFamily: "'JetBrains Mono', 'Cascadia Code', 'Fira Code', monospace",
        lineNumbers: "on",
        scrollBeyondLastLine: false,
        automaticLayout: true,
        tabSize: 2,
        wordWrap: "on",
        padding: { top: 8 },
        renderLineHighlight: "line",
        readOnly: readOnly || false,
        scrollbar: { verticalScrollbarSize: 10, horizontalScrollbarSize: 10 }
    });

    if (!readOnly) {
        editor.addAction({
            id: "execute",
            label: "Execute",
            keybindings: [monaco.KeyMod.CtrlCmd | monaco.KeyCode.Enter],
            run: () => {
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync("OnExecuteRequested_Internal", elementId, editor.getValue());
                }
            }
        });

        editor.onDidChangeModelContent(() => {
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync("OnContentChanged_Internal", elementId, editor.getValue());
            }
        });
    }

    editors.set(elementId, editor);
    return true;
}

export function getValue(elementId) {
    const editor = editors.get(elementId);
    return editor ? editor.getValue() : "";
}

export function setValue(elementId, value) {
    const editor = editors.get(elementId);
    if (editor) editor.setValue(value || "");
}

export function setLanguage(elementId, language) {
    const editor = editors.get(elementId);
    if (editor) monaco.editor.setModelLanguage(editor.getModel(), language);
}

export function setReadOnly(elementId, readOnly) {
    const editor = editors.get(elementId);
    if (editor) editor.updateOptions({ readOnly });
}

export function setDecorations(elementId, ranges) {
    const editor = editors.get(elementId);
    if (!editor) return;

    const decorations = ranges.map(r => ({
        range: new monaco.Range(r.startLine, r.startCol, r.endLine, r.endCol),
        options: {
            inlineClassName: r.className || "rk-match-highlight",
            stickiness: monaco.editor.TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges
        }
    }));

    if (!editor._rkDecorations) editor._rkDecorations = [];
    editor._rkDecorations = editor.deltaDecorations(editor._rkDecorations, decorations);
}

export function hasEditor(elementId) {
    return editors.has(elementId);
}

export function focus(elementId) {
    const editor = editors.get(elementId);
    if (editor) editor.focus();
}

export function dispose(elementId) {
    const editor = editors.get(elementId);
    if (editor) { editor.dispose(); editors.delete(elementId); }
}

export function disposeAll() {
    for (const [, editor] of editors) editor.dispose();
    editors.clear();
}
