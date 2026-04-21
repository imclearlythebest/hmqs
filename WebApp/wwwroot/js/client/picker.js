(function () {
    window.hmqsModules = window.hmqsModules || {};

    function createPickerModule(state, dbApi, notifyFolderChanged) {
        async function initializePicker() {
            if (state.pickerState) {
                return state.pickerState;
            }

            if (state.initializationPromise) {
                return state.initializationPromise;
            }

            state.initializationPromise = (async () => {
                state.db = await dbApi.openDatabase();
                state.pickerState = await initializePickerState();
                return state.pickerState;
            })();

            return state.initializationPromise;
        }

        async function initializePickerState() {
            const folders = await dbApi.getStoredFolders(state.db);
            const lastId = await dbApi.getLastPickedFolderId(state.db);
            const lastFolder = lastId ? await dbApi.getStoredFolder(state.db, lastId) : null;

            let lastHasPermission = false;
            let lastHandle = null;

            if (lastFolder && lastFolder.handle) {
                lastHandle = lastFolder.handle;
                let permission = await dbApi.checkFolderPermission(lastHandle);
                if (permission !== 'granted') {
                    permission = await dbApi.requestFolderPermission(lastHandle);
                }
                lastHasPermission = permission === 'granted';
            }

            const activeId = (lastFolder && lastHasPermission) ? lastId : null;

            return {
                folders,
                activeId,
                lastHasPermission,
                lastFolder: lastHasPermission ? lastHandle : null
            };
        }

        async function addNewFolder() {
            const handle = await window.showDirectoryPicker();

            const permission = await dbApi.requestFolderPermission(handle);
            if (permission !== 'granted') {
                return state.pickerState;
            }

            const id = await dbApi.storeFolder(state.db, {
                name: handle.name,
                handle
            });

            await dbApi.setLastPickedFolderId(state.db, id);

            state.pickerState = {
                folders: await dbApi.getStoredFolders(state.db),
                activeId: id,
                lastHasPermission: true,
                lastFolder: handle
            };

            notifyFolderChanged();
            return state.pickerState;
        }

        function getActiveFolder() {
            if (!state.pickerState || !state.pickerState.activeId) {
                return null;
            }

            return state.pickerState.folders.find(f => f.id === state.pickerState.activeId) || null;
        }

        async function getCurrentFolderFiles() {
            if (!state.pickerState) {
                await initializePicker();
            }

            const activeFolder = getActiveFolder();
            if (!activeFolder || !activeFolder.handle) {
                return [];
            }

            const musicExtensions = ['.mp3', '.wav', '.flac', '.m4a', '.ogg', '.aac', '.wma'];
            const files = [];

            for await (const entry of activeFolder.handle.values()) {
                if (entry.kind !== 'file') {
                    continue;
                }

                const ext = entry.name.substring(entry.name.lastIndexOf('.')).toLowerCase();
                if (!musicExtensions.includes(ext)) {
                    continue;
                }

                const metadataFileName = `${entry.name}.hmqsmeta`;
                let hasMetadata = false;
                let canScrobble = false;
                let trackTitle = '';
                let artist = '';
                let album = '';
                let imageUrl = '';
                let itunesTrackId = 0;

                try {
                    const metadataEntry = await activeFolder.handle.getFileHandle(metadataFileName);
                    if (metadataEntry) {
                        hasMetadata = true;
                        const metadataFile = await metadataEntry.getFile();
                        const text = await metadataFile.text();
                        const metadata = JSON.parse(text);
                        canScrobble = Number.isFinite(metadata.itunesTrackId) && metadata.itunesTrackId > 0;
                        itunesTrackId = canScrobble ? metadata.itunesTrackId : 0;
                        trackTitle = typeof metadata.trackTitle === 'string' ? metadata.trackTitle.trim() : '';
                        artist = typeof metadata.artist === 'string' ? metadata.artist.trim() : '';
                        album = typeof metadata.album === 'string' ? metadata.album.trim() : '';
                        imageUrl = typeof metadata.imageUrl === 'string' ? metadata.imageUrl.trim() : '';
                    }
                } catch {
                    hasMetadata = false;
                    canScrobble = false;
                    trackTitle = '';
                    artist = '';
                    album = '';
                    imageUrl = '';
                }

                files.push({
                    name: entry.name,
                    hasMetadata,
                    canScrobble,
                    itunesTrackId,
                    trackTitle,
                    artist,
                    album,
                    imageUrl
                });
            }

            files.sort((a, b) => {
                const left = (a.trackTitle || a.name || '').toLowerCase();
                const right = (b.trackTitle || b.name || '').toLowerCase();
                return left.localeCompare(right, undefined, { numeric: true, sensitivity: 'base' });
            });

            return files;
        }

        async function switchFolder(id) {
            const folder = await dbApi.getStoredFolder(state.db, id);
            if (!folder || !folder.handle) {
                return state.pickerState;
            }

            const permission = await dbApi.checkFolderPermission(folder.handle);
            if (permission !== 'granted') {
                const requested = await dbApi.requestFolderPermission(folder.handle);
                if (requested !== 'granted') {
                    return state.pickerState;
                }
            }

            await dbApi.setLastPickedFolderId(state.db, id);

            state.pickerState = {
                folders: await dbApi.getStoredFolders(state.db),
                activeId: id,
                lastHasPermission: true,
                lastFolder: folder.handle
            };

            notifyFolderChanged();
            return state.pickerState;
        }

        function getPickerState() {
            return state.pickerState;
        }

        return {
            initializePicker,
            addNewFolder,
            getActiveFolder,
            getCurrentFolderFiles,
            switchFolder,
            getPickerState
        };
    }

    window.hmqsModules.createPickerModule = createPickerModule;
})();
