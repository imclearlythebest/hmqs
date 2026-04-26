(function () {
    window.hmqsModules = window.hmqsModules || {};

    function createDownloaderModule(getActiveFolder, saveMetadataFile, lookupMetadataCandidatesFromItunes) {

        async function resolveMetadata(metadata) {
            const normalized = window.hmqsClient.normalizeMetadataRecord(metadata || {});
            if (normalized.itunesTrackId && normalized.itunesCollectionId && normalized.trackTitle && normalized.artist) {
                return normalized;
            }

            if (typeof lookupMetadataCandidatesFromItunes !== 'function') {
                return normalized;
            }

            try {
                const candidates = await lookupMetadataCandidatesFromItunes(normalized.trackTitle || normalized.artist || 'download', normalized);
                const best = Array.isArray(candidates) && candidates.length > 0 ? candidates[0] : null;
                const candidateMetadata = window.hmqsClient.normalizeMetadataRecord(best?.metadata || {});
                return {
                    ...candidateMetadata,
                    ...normalized,
                    imageUrl: normalized.imageUrl || candidateMetadata.imageUrl || ''
                };
            } catch {
                return normalized;
            }
        }
        
        async function downloadAndSave(itunesId, metadata) {
            const activeFolder = getActiveFolder();
            if (!activeFolder || !activeFolder.handle) {
                alert('Please select a local folder first.');
                return;
            }

            try {
                const resolvedMetadata = await resolveMetadata(metadata);
                const trackTitle = resolvedMetadata.trackTitle || '';
                const artistName = resolvedMetadata.artistName || resolvedMetadata.artist || '';
                const response = await fetch(`/Download/Trigger?trackTitle=${encodeURIComponent(trackTitle)}&artistName=${encodeURIComponent(artistName)}`);
                if (!response.ok) {
                    const serverMessage = (await response.text()).trim();
                    throw new Error(serverMessage || `Download failed on server (${response.status}).`);
                }

                const blob = await response.blob();
                const fileName = `${resolvedMetadata.trackTitle || itunesId}.mp3`.replace(/[\/\\?%*:|"<>]/g, '-');
                
                // Save MP3
                const fileHandle = await activeFolder.handle.getFileHandle(fileName, { create: true });
                const writable = await fileHandle.createWritable();
                await writable.write(blob);
                await writable.close();

                // Save Metadata
                await saveMetadataFile(fileName, resolvedMetadata);

                alert(`Downloaded and saved: ${fileName}`);
            } catch (err) {
                console.error(err);
                alert(`Error: ${err.message}`);
            }
        }

        return {
            downloadAndSave
        };
    }

    window.hmqsModules.createDownloaderModule = createDownloaderModule;
})();
