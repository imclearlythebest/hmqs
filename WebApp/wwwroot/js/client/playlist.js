(function () {
    window.hmqsModules = window.hmqsModules || {};

    function createPlaylistModule(getActiveFolder, notifyFolderChanged) {
        function requireActiveFolder() {
            const activeFolder = getActiveFolder();
            if (!activeFolder || !activeFolder.handle) {
                throw new Error('No active folder selected.');
            }

            return activeFolder;
        }

        function normalizeName(name) {
            return (name || '').trim();
        }

        function toPlaylistFileName(name) {
            return `${name}.plist`;
        }

        async function listPlaylists() {
            const activeFolder = requireActiveFolder();
            const playlists = [];

            for await (const entry of activeFolder.handle.values()) {
                if (entry.kind !== 'file' || !entry.name.toLowerCase().endsWith('.plist')) {
                    continue;
                }

                playlists.push(entry.name.slice(0, -6));
            }

            playlists.sort((a, b) => a.localeCompare(b));
            return playlists;
        }

        async function getPlaylistTracks(name) {
            const playlistName = normalizeName(name);
            if (!playlistName) {
                return [];
            }

            const activeFolder = requireActiveFolder();
            const handle = await activeFolder.handle.getFileHandle(toPlaylistFileName(playlistName));
            const file = await handle.getFile();
            const text = await file.text();

            return text
                .split(/\r?\n/)
                .map(x => x.trim())
                .filter(Boolean);
        }

        async function savePlaylistTracks(name, tracks) {
            const playlistName = normalizeName(name);
            if (!playlistName) {
                throw new Error('Playlist name is required.');
            }

            const activeFolder = requireActiveFolder();
            const handle = await activeFolder.handle.getFileHandle(toPlaylistFileName(playlistName), { create: true });
            const writable = await handle.createWritable();
            const content = (tracks || []).map(x => `${x}`.trim()).filter(Boolean).join('\n');
            await writable.write(content);
            await writable.close();
            notifyFolderChanged();
        }

        async function createPlaylist(name) {
            const playlistName = normalizeName(name);
            if (!playlistName) {
                throw new Error('Playlist name is required.');
            }

            await savePlaylistTracks(playlistName, []);
        }

        async function deletePlaylist(name) {
            const playlistName = normalizeName(name);
            if (!playlistName) {
                return;
            }

            const activeFolder = requireActiveFolder();
            await activeFolder.handle.removeEntry(toPlaylistFileName(playlistName));
            notifyFolderChanged();
        }

        async function renamePlaylist(oldName, newName) {
            const source = normalizeName(oldName);
            const target = normalizeName(newName);

            if (!source || !target) {
                throw new Error('Both old and new playlist names are required.');
            }

            const tracks = await getPlaylistTracks(source);
            await savePlaylistTracks(target, tracks);
            await deletePlaylist(source);
        }

        async function addTrackToPlaylist(name, fileName) {
            const playlistName = normalizeName(name);
            const track = normalizeName(fileName);
            if (!playlistName || !track) {
                throw new Error('Playlist name and file name are required.');
            }

            const existing = await getPlaylistTracks(playlistName);
            if (!existing.includes(track)) {
                existing.push(track);
            }

            await savePlaylistTracks(playlistName, existing);
        }

        async function removeTrackFromPlaylist(name, fileName) {
            const playlistName = normalizeName(name);
            const track = normalizeName(fileName);
            if (!playlistName || !track) {
                throw new Error('Playlist name and file name are required.');
            }

            const existing = await getPlaylistTracks(playlistName);
            const updated = existing.filter(x => x !== track);
            await savePlaylistTracks(playlistName, updated);
        }

        return {
            listPlaylists,
            getPlaylistTracks,
            savePlaylistTracks,
            createPlaylist,
            deletePlaylist,
            renamePlaylist,
            addTrackToPlaylist,
            removeTrackFromPlaylist
        };
    }

    window.hmqsModules.createPlaylistModule = createPlaylistModule;
})();
