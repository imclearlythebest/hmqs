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

            const defaultMetadata = {
                itunesTrackId: 0,
                trackTitle: '',
                itunesArtistId: 0,
                artist: '',
                itunesCollectionId: 0,
                album: '',
                genre: '',
                year: 0,
                imageUrl: ''
            };

            try {
                await ensureWritePermission(activeFolder.handle);
                const metadataHandle = await activeFolder.handle.getFileHandle(metadataFileName, { create: true });
                const writable = await metadataHandle.createWritable();
                await writable.write(JSON.stringify(defaultMetadata, null, 2));
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
                    itunesTrackId: Number.isFinite(metadata.itunesTrackId) ? metadata.itunesTrackId : 0,
                    trackTitle: typeof metadata.trackTitle === 'string' ? metadata.trackTitle : '',
                    itunesArtistId: Number.isFinite(metadata.itunesArtistId) ? metadata.itunesArtistId : 0,
                    artist: typeof metadata.artist === 'string' ? metadata.artist : '',
                    itunesCollectionId: Number.isFinite(metadata.itunesCollectionId) ? metadata.itunesCollectionId : 0,
                    album: typeof metadata.album === 'string' ? metadata.album : '',
                    genre: typeof metadata.genre === 'string' ? metadata.genre : '',
                    year: Number.isFinite(metadata.year) ? metadata.year : 0,
                    imageUrl: typeof metadata.imageUrl === 'string' ? metadata.imageUrl : ''
                };
            } catch {
                throw new Error(`Could not read metadata for ${fileName}.`);
            }
        }

        async function saveMetadataFile(fileName, metadata) {
            const activeFolder = requireActiveFolder();
            const metadataFileName = `${fileName}.hmqsmeta`;

            const normalized = {
                itunesTrackId: Number.isFinite(metadata?.itunesTrackId) && metadata.itunesTrackId > 0 ? metadata.itunesTrackId : 0,
                trackTitle: typeof metadata?.trackTitle === 'string' ? metadata.trackTitle : '',
                itunesArtistId: Number.isFinite(metadata?.itunesArtistId) && metadata.itunesArtistId >= 0 ? metadata.itunesArtistId : 0,
                artist: typeof metadata?.artist === 'string' ? metadata.artist : '',
                itunesCollectionId: Number.isFinite(metadata?.itunesCollectionId) && metadata.itunesCollectionId >= 0 ? metadata.itunesCollectionId : 0,
                album: typeof metadata?.album === 'string' ? metadata.album : '',
                genre: typeof metadata?.genre === 'string' ? metadata.genre : '',
                year: Number.isFinite(metadata?.year) && metadata.year >= 0 ? metadata.year : 0,
                imageUrl: typeof metadata?.imageUrl === 'string' ? metadata.imageUrl : ''
            };

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