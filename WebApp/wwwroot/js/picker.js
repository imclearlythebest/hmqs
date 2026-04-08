function openDB() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open("file-db", 1);
        request.onupgradeneeded = () => {
            const db = request.result;
            db.createObjectStore("handles");
        };

        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

async function saveHandle(handle) {
    const db = await openDB();
    const tx = db.transaction("handles", "readwrite");
    tx.objectStore("handles").put(handle, "musicFolder");
    return new Promise((resolve, reject) => {
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}

async function loadHandle() {
    try {
        const db = await openDB();
        return new Promise((resolve, reject) => {
            const tx = db.transaction("handles", "readonly");
            const store = tx.objectStore("handles");
            const req = store.get("musicFolder");
            req.onsuccess = () => resolve(req.result);
            req.onerror = () => reject(req.error);
        });
    } catch {
        return null; // database does not exist
    }
}

async function ensurePermission(handle) {
    const perm = await handle.queryPermission({ mode: "readwrite" });
    if (perm === "granted") return true;
    if (perm === "prompt") {
        return "prompt";
    }
    return false;
}

// ---------- Main logic ----------
(async function () {
    const statusEl = document.getElementById("status");
    const btn = document.getElementById("pick-btn");

    const handle = await loadHandle();

    if (handle) {
        const ok = await ensurePermission(handle);
        if (ok === true) {
            // Permission exists → store in a variable we can use
            window.dirHandle = handle;
            btn.style.display = "none";
            statusEl.textContent = "Directory ready to use!";
        } else if (ok === "prompt") {
            // Need to reprompt the user
            statusEl.textContent = "Need folder permission again :(";
            btn.style.display = "inline";
            btn.onclick = async () => {
                const newHandle = await window.showDirectoryPicker();
                await saveHandle(newHandle);
                window.dirHandle = newHandle;
                statusEl.textContent = "Directory permission granted!";
                btn.style.display = "none";
            };
        }
    } else {
        // No handle / maybe no DB → show button
        statusEl.textContent = "No folder selected yet.";
        btn.style.display = "inline";
        btn.onclick = async () => {
            const newHandle = await window.showDirectoryPicker();
            await saveHandle(newHandle);
            window.dirHandle = newHandle;
            statusEl.textContent = "Directory selected and stored!";
            btn.style.display = "none";
        };
    }
})();