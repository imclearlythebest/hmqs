(function () {
    window.hmqsModules = window.hmqsModules || {};

    function createMetadataModule(getActiveFolder, notifyFolderChanged) {
        function requireActiveFolder() {
            const activeFolder = getActiveFolder();
            if (!activeFolder || !activeFolder.handle) {
                throw new Error('No active folder selected.');
            }

            return activeFolder;
        }

        async function ensureWritePermission(handle) {
            let permission = await handle.queryPermission({ mode: 'readwrite' });
            if (permission !== 'granted') {
                permission = await handle.requestPermission({ mode: 'readwrite' });
            }

            if (permission !== 'granted') {
                throw new Error('Write permission to the selected folder was not granted.');
            }
        }

        async function createMetadataFile(fileName) {
            const activeFolder = requireActiveFolder();
            const metadataFileName = `${fileName}.hmqsmeta`;

            try {
                await ensureWritePermission(activeFolder.handle);
                const metadataHandle = await activeFolder.handle.getFileHandle(metadataFileName, { create: true });
                const writable = await metadataHandle.createWritable();
                await writable.write(JSON.stringify({}, null, 2));
                await writable.close();
                notifyFolderChanged();
            } catch {
                throw new Error(`Could not create metadata for ${fileName}.`);
            }
        }

        async function getMetadataFile(fileName) {
            const activeFolder = requireActiveFolder();
            const metadataFileName = `${fileName}.hmqsmeta`;

            try {
                const metadataHandle = await activeFolder.handle.getFileHandle(metadataFileName);
                const metadataFile = await metadataHandle.getFile();
                const text = await metadataFile.text();
                const metadata = JSON.parse(text);

                return {
                    itunesTrackId: Number.isFinite(metadata.itunesTrackId) && metadata.itunesTrackId > 0 ? metadata.itunesTrackId : null,
                    trackTitle: typeof metadata.trackTitle === 'string' ? metadata.trackTitle : '',
                    itunesArtistId: Number.isFinite(metadata.itunesArtistId) && metadata.itunesArtistId > 0 ? metadata.itunesArtistId : null,
                    artist: typeof metadata.artist === 'string' ? metadata.artist : '',
                    itunesCollectionId: Number.isFinite(metadata.itunesCollectionId) && metadata.itunesCollectionId > 0 ? metadata.itunesCollectionId : null,
                    album: typeof metadata.album === 'string' ? metadata.album : '',
                    genre: typeof metadata.genre === 'string' ? metadata.genre : '',
                    year: Number.isFinite(metadata.year) && metadata.year > 0 ? metadata.year : null,
                    imageUrl: typeof metadata.imageUrl === 'string' ? metadata.imageUrl : ''
                };
            } catch {
                throw new Error(`Could not read metadata for ${fileName}.`);
            }
        }

        async function saveMetadataFile(fileName, metadata) {
            const activeFolder = requireActiveFolder();
            const metadataFileName = `${fileName}.hmqsmeta`;

            const normalized = {};

            if (Number.isFinite(metadata?.itunesTrackId) && metadata.itunesTrackId > 0) {
                normalized.itunesTrackId = metadata.itunesTrackId;
            }

            if (typeof metadata?.trackTitle === 'string' && metadata.trackTitle.trim()) {
                normalized.trackTitle = metadata.trackTitle.trim();
            }

            if (Number.isFinite(metadata?.itunesArtistId) && metadata.itunesArtistId > 0) {
                normalized.itunesArtistId = metadata.itunesArtistId;
            }

            if (typeof metadata?.artist === 'string' && metadata.artist.trim()) {
                normalized.artist = metadata.artist.trim();
            }

            if (Number.isFinite(metadata?.itunesCollectionId) && metadata.itunesCollectionId > 0) {
                normalized.itunesCollectionId = metadata.itunesCollectionId;
            }

            if (typeof metadata?.album === 'string' && metadata.album.trim()) {
                normalized.album = metadata.album.trim();
            }

            if (typeof metadata?.genre === 'string' && metadata.genre.trim()) {
                normalized.genre = metadata.genre.trim();
            }

            if (Number.isFinite(metadata?.year) && metadata.year > 0) {
                normalized.year = metadata.year;
            }

            if (typeof metadata?.imageUrl === 'string' && metadata.imageUrl.trim()) {
                normalized.imageUrl = metadata.imageUrl.trim();
            }

            try {
                await ensureWritePermission(activeFolder.handle);
                const metadataHandle = await activeFolder.handle.getFileHandle(metadataFileName, { create: true });
                const writable = await metadataHandle.createWritable();
                await writable.write(JSON.stringify(normalized, null, 2));
                await writable.close();
                notifyFolderChanged();
            } catch {
                throw new Error(`Could not save metadata for ${fileName}.`);
            }
        }

        async function editMetadata(fileName) {
            const target = `/Catalogue/Edit?fileName=${encodeURIComponent(fileName)}`;
            window.location.assign(target);
        }

        return {
            createMetadataFile,
            getMetadataFile,
            saveMetadataFile,
            editMetadata
        };
    }

    window.hmqsModules.createMetadataModule = createMetadataModule;
})();