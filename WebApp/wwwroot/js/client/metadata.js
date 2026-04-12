(function () {
    window.hmqsModules = window.hmqsModules || {};

    function createMetadataModule(getActiveFolder, notifyFolderChanged) {
        function normalizeText(value) {
            return typeof value === 'string' ? value.trim() : '';
        }

        function normalizeOptionalId(value) {
            return Number.isFinite(value) && value > 0 ? value : null;
        }

        function normalizeMetadataRecord(metadata) {
            return {
                itunesTrackId: normalizeOptionalId(metadata?.itunesTrackId),
                trackTitle: normalizeText(metadata?.trackTitle),
                itunesArtistId: normalizeOptionalId(metadata?.itunesArtistId),
                artist: normalizeText(metadata?.artist),
                itunesCollectionId: normalizeOptionalId(metadata?.itunesCollectionId),
                album: normalizeText(metadata?.album),
                genre: normalizeText(metadata?.genre),
                year: normalizeOptionalId(metadata?.year),
                imageUrl: normalizeText(metadata?.imageUrl)
            };
        }

        function toSparseMetadataRecord(metadata) {
            const normalized = normalizeMetadataRecord(metadata);
            const sparse = {};

            if (normalized.itunesTrackId) {
                sparse.itunesTrackId = normalized.itunesTrackId;
            }

            if (normalized.trackTitle) {
                sparse.trackTitle = normalized.trackTitle;
            }

            if (normalized.itunesArtistId) {
                sparse.itunesArtistId = normalized.itunesArtistId;
            }

            if (normalized.artist) {
                sparse.artist = normalized.artist;
            }

            if (normalized.itunesCollectionId) {
                sparse.itunesCollectionId = normalized.itunesCollectionId;
            }

            if (normalized.album) {
                sparse.album = normalized.album;
            }

            if (normalized.genre) {
                sparse.genre = normalized.genre;
            }

            if (normalized.year) {
                sparse.year = normalized.year;
            }

            if (normalized.imageUrl) {
                sparse.imageUrl = normalized.imageUrl;
            }

            return sparse;
        }

        function buildScrobblePayload(metadata, progress, durationSeconds) {
            const normalized = normalizeMetadataRecord(metadata);
            if (!normalized.itunesTrackId) {
                return null;
            }

            const payload = {
                itunesTrackId: normalized.itunesTrackId,
                progress,
                durationSeconds
            };

            if (normalized.itunesArtistId) {
                payload.itunesArtistId = normalized.itunesArtistId;
            }

            if (normalized.itunesCollectionId) {
                payload.itunesCollectionId = normalized.itunesCollectionId;
            }

            return payload;
        }

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

                return normalizeMetadataRecord(metadata);
            } catch {
                throw new Error(`Could not read metadata for ${fileName}.`);
            }
        }

        async function saveMetadataFile(fileName, metadata) {
            const activeFolder = requireActiveFolder();
            const metadataFileName = `${fileName}.hmqsmeta`;

            const normalized = toSparseMetadataRecord(metadata);

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
            editMetadata,
            buildScrobblePayload,
            normalizeMetadataRecord,
            toSparseMetadataRecord
        };
    }

    window.hmqsModules.createMetadataModule = createMetadataModule;
})();