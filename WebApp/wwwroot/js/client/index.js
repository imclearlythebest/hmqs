(function () {
    const modules = window.hmqsModules || {};

    if (!modules.db || !modules.createPickerModule || !modules.createMetadataModule || !modules.createPlayerModule || !modules.createPlaylistModule) {
        console.error('hmqsClient modules are not loaded in the expected order.');
        return;
    }

    const state = {
        db: null,
        pickerState: null,
        initializationPromise: null,
        currentAudio: null,
        folderVersion: 0,
        selectedPlaylist: null
    };

    function notifyFolderChanged() {
        state.folderVersion += 1;
        window.dispatchEvent(new CustomEvent('folder-changed', {
            detail: { version: state.folderVersion }
        }));
    }

    const picker = modules.createPickerModule(state, modules.db, notifyFolderChanged);
    const metadata = modules.createMetadataModule(picker.getActiveFolder, notifyFolderChanged);
    const player = modules.createPlayerModule(state, picker.getActiveFolder, metadata.getMetadataFile);
    const playlist = modules.createPlaylistModule(picker.getActiveFolder, notifyFolderChanged);

    function setSelectedPlaylist(name) {
        state.selectedPlaylist = name ? `${name}` : null;
        window.dispatchEvent(new CustomEvent('playlist-selected', {
            detail: {
                playlist: state.selectedPlaylist
            }
        }));
    }

    window.hmqsClient = {
        initializePicker: picker.initializePicker,
        switchFolder: picker.switchFolder,
        addNewFolder: picker.addNewFolder,
        getPickerState: picker.getPickerState,
        getCurrentFolderFiles: picker.getCurrentFolderFiles,
        createMetadataFile: metadata.createMetadataFile,
        getMetadataFile: metadata.getMetadataFile,
        saveMetadataFile: metadata.saveMetadataFile,
        editMetadata: metadata.editMetadata,
        listPlaylists: playlist.listPlaylists,
        getPlaylistTracks: playlist.getPlaylistTracks,
        savePlaylistTracks: playlist.savePlaylistTracks,
        createPlaylist: playlist.createPlaylist,
        deletePlaylist: playlist.deletePlaylist,
        renamePlaylist: playlist.renamePlaylist,
        addTrackToPlaylist: playlist.addTrackToPlaylist,
        removeTrackFromPlaylist: playlist.removeTrackFromPlaylist,
        setSelectedPlaylist,
        getSelectedPlaylist: () => state.selectedPlaylist,
        playMusicFile: player.playMusicFile,
        pauseMusic: player.pauseMusic,
        resumeMusic: player.resumeMusic,
        seekMusic: player.seekMusic,
        setPlaybackQueue: player.setPlaybackQueue,
        nextTrack: player.nextTrack,
        previousTrack: player.previousTrack,
        toggleShuffle: player.toggleShuffle,
        toggleLoop: player.toggleLoop,
        getPlaybackState: player.getPlaybackState,
        getCurrentAudio: player.getCurrentAudio,
        getFolderVersion: () => state.folderVersion
    };
})();