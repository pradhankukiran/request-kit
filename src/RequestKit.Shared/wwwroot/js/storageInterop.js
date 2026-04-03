const DB_NAME = "RequestKit";
const DB_VERSION = 1;
const STORE_NAME = "workspaces";

let _db = null;

function openDb() {
    if (_db) return Promise.resolve(_db);
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(DB_NAME, DB_VERSION);
        request.onupgradeneeded = (e) => {
            const db = e.target.result;
            if (!db.objectStoreNames.contains(STORE_NAME)) {
                db.createObjectStore(STORE_NAME, { keyPath: "id" });
            }
        };
        request.onsuccess = () => { _db = request.result; resolve(_db); };
        request.onerror = () => reject(request.error);
    });
}

function tx(mode) {
    return openDb().then(db => {
        const transaction = db.transaction(STORE_NAME, mode);
        return transaction.objectStore(STORE_NAME);
    });
}

function promisify(request) {
    return new Promise((resolve, reject) => {
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

export async function listWorkspaces() {
    const store = await tx("readonly");
    const all = await promisify(store.getAll());
    return all.map(w => ({
        id: w.id,
        name: w.name,
        requestCount: Object.keys(w.requests || {}).length,
        collectionCount: (w.collections || []).length,
        modifiedAt: w.modifiedAt
    })).sort((a, b) => new Date(b.modifiedAt) - new Date(a.modifiedAt));
}

export async function loadWorkspace(id) {
    const store = await tx("readonly");
    return await promisify(store.get(id)) || null;
}

export async function saveWorkspace(workspace) {
    const store = await tx("readwrite");
    await promisify(store.put(workspace));
    return true;
}

export async function deleteWorkspace(id) {
    const store = await tx("readwrite");
    await promisify(store.delete(id));
    return true;
}
