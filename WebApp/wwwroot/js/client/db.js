(function () {
    window.hmqsModules = window.hmqsModules || {};

    async function openDatabase() {
        return idb.openDB('music-app', 1, {
            upgrade(db) {
                if (!db.objectStoreNames.contains('folders')) {
                    db.createObjectStore('folders', { keyPath: 'id', autoIncrement: true });
                }

                if (!db.objectStoreNames.contains('meta')) {
                    db.createObjectStore('meta');
                }
            }
        });
    }

    async function getStoredFolders(db) {
        return db.getAll('folders');
    }

    async function getStoredFolder(db, id) {
        return db.get('folders', id);
    }

    async function storeFolder(db, folder) {
        return db.put('folders', folder);
    }

    async function getLastPickedFolderId(db) {
        return db.get('meta', 'lastPickedFolder');
    }

    async function setLastPickedFolderId(db, id) {
        return db.put('meta', id, 'lastPickedFolder');
    }

    async function checkFolderPermission(handle) {
        return handle.queryPermission({ mode: 'readwrite' });
    }

    async function requestFolderPermission(handle) {
        return handle.requestPermission({ mode: 'readwrite' });
    }

    window.hmqsModules.db = {
        openDatabase,
        getStoredFolders,
        getStoredFolder,
        storeFolder,
        getLastPickedFolderId,
        setLastPickedFolderId,
        checkFolderPermission,
        requestFolderPermission
    };
})();