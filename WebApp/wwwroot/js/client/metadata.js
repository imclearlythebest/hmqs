(function () {
    window.hmqsModules = window.hmqsModules || {};

    function createMetadataModule(getActiveFolder, notifyFolderChanged) {
        const ITUNES_SEARCH_LIMIT = 5;

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

        function buildSearchTermFromFileName(fileName) {
            const raw = normalizeText(fileName)
                .replace(/\.[^.]+$/, '')
                .replace(/[._-]+/g, ' ')
                .replace(/\s+/g, ' ')
                .trim();

            return raw;
        }

        function buildSearchTerm(fileName, metadata) {
            const normalized = normalizeMetadataRecord(metadata || {});
            const metadataTerm = [
                normalized.trackTitle,
                normalized.artist,
                normalized.album,
                normalized.genre
            ]
                .filter(Boolean)
                .join(' ')
                .replace(/\s+/g, ' ')
                .trim();

            if (metadataTerm) {
                return metadataTerm;
            }

            return buildSearchTermFromFileName(fileName);
        }

        function mapItunesResultToMetadata(result) {
            const yearValue = Number.parseInt(`${result?.releaseDate || ''}`.slice(0, 4), 10);

            return normalizeMetadataRecord({
                itunesTrackId: result?.trackId,
                trackTitle: result?.trackName,
                itunesArtistId: result?.artistId,
                artist: result?.artistName,
                itunesCollectionId: result?.collectionId,
                album: result?.collectionName,
                genre: result?.primaryGenreName,
                year: Number.isFinite(yearValue) ? yearValue : null,
                imageUrl: result?.artworkUrl100 || ''
            });
        }

        function mapItunesResultToCandidate(result) {
            return {
                trackTitle: normalizeText(result?.trackName),
                artist: normalizeText(result?.artistName),
                album: normalizeText(result?.collectionName),
                year: normalizeOptionalId(Number.parseInt(`${result?.releaseDate || ''}`.slice(0, 4), 10)),
                imageUrl: normalizeText(result?.artworkUrl100),
                metadata: mapItunesResultToMetadata(result)
            };
        }

        function buildItunesSearchEndpoint(term) {
            return `https://itunes.apple.com/search?media=music&entity=song&limit=${ITUNES_SEARCH_LIMIT}&term=${encodeURIComponent(term)}`;
        }

        async function fetchJson(endpoint, networkErrorMessage, invalidPayloadErrorMessage) {
            let response;
            try {
                response = await fetch(endpoint, { method: 'GET' });
            } catch {
                throw new Error(networkErrorMessage);
            }

            if (!response.ok) {
                throw new Error('iTunes lookup failed.');
            }

            try {
                return await response.json();
            } catch {
                throw new Error(invalidPayloadErrorMessage);
            }
        }

        async function lookupMetadataCandidatesFromItunes(fileName, metadata) {
            const term = buildSearchTerm(fileName, metadata);
            if (!term) {
                throw new Error('Could not build a lookup term from metadata or file name.');
            }

            const endpoint = buildItunesSearchEndpoint(term);
            const body = await fetchJson(
                endpoint,
                'Could not reach iTunes lookup service.',
                'Received invalid response from iTunes lookup service.'
            );

            const results = Array.isArray(body?.results) ? body.results : [];
            if (!results.length) {
                throw new Error('No iTunes match found for this file.');
            }

            return results
                .slice(0, ITUNES_SEARCH_LIMIT)
                .map(mapItunesResultToCandidate);
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
            const current = `${window.location.pathname || '/'}${window.location.search || ''}${window.location.hash || ''}`;
            const target = `/Catalogue/Edit?fileName=${encodeURIComponent(fileName)}&returnUrl=${encodeURIComponent(current)}`;
            window.location.assign(target);
        }

        return {
            createMetadataFile,
            getMetadataFile,
            saveMetadataFile,
            editMetadata,
            buildScrobblePayload,
            normalizeMetadataRecord,
            toSparseMetadataRecord,
            lookupMetadataCandidatesFromItunes
        };
    }

    window.hmqsModules.createMetadataModule = createMetadataModule;
})();