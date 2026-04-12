(function () {
    window.hmqsModules = window.hmqsModules || {};

    function createPlayerModule(state, getActiveFolder, getMetadataFile) {
        state.playbackQueue = state.playbackQueue || [];
        state.currentIndex = Number.isInteger(state.currentIndex) ? state.currentIndex : -1;
        state.isShuffle = !!state.isShuffle;
        state.isLoop = !!state.isLoop;

        async function readTrackMetadata(fileName) {
            try {
                const metadata = await getMetadataFile(fileName);
                return {
                    trackTitle: typeof metadata?.trackTitle === 'string' ? metadata.trackTitle.trim() : '',
                    artist: typeof metadata?.artist === 'string' ? metadata.artist.trim() : '',
                    album: typeof metadata?.album === 'string' ? metadata.album.trim() : '',
                    imageUrl: typeof metadata?.imageUrl === 'string' ? metadata.imageUrl.trim() : ''
                };
            } catch {
                return {
                    trackTitle: '',
                    artist: '',
                    album: '',
                    imageUrl: ''
                };
            }
        }

        async function submitScrobble(fileName, progress, durationSeconds) {
            try {
                let metadata = {};
                try {
                    metadata = await getMetadataFile(fileName);
                } catch {
                    metadata = {};
                }

                if (!Number.isFinite(metadata.itunesTrackId) || metadata.itunesTrackId <= 0) {
                    return;
                }

                const payload = {
                    itunesTrackId: metadata.itunesTrackId,
                    progress,
                    durationSeconds
                };

                if (Number.isFinite(metadata.itunesArtistId) && metadata.itunesArtistId > 0) {
                    payload.itunesArtistId = metadata.itunesArtistId;
                }

                if (Number.isFinite(metadata.itunesCollectionId) && metadata.itunesCollectionId > 0) {
                    payload.itunesCollectionId = metadata.itunesCollectionId;
                }

                await fetch('/Scrobble/Submit', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    credentials: 'include',
                    body: JSON.stringify(payload)
                });
            } catch {
                // best effort scrobbling
            }
        }

        function setPlaybackQueue(fileNames) {
            state.playbackQueue = (fileNames || []).map(x => `${x}`);
            if (state.currentTrackFileName) {
                const index = state.playbackQueue.indexOf(state.currentTrackFileName);
                state.currentIndex = index;
            }
        }

        function getPlaybackState() {
            return {
                isShuffle: state.isShuffle,
                isLoop: state.isLoop,
                currentIndex: state.currentIndex,
                queueLength: state.playbackQueue.length,
                currentTrackFileName: state.currentTrackFileName || null
            };
        }

        function toggleShuffle() {
            state.isShuffle = !state.isShuffle;
            window.dispatchEvent(new CustomEvent('playback-state-changed', { detail: getPlaybackState() }));
            return state.isShuffle;
        }

        function toggleLoop() {
            state.isLoop = !state.isLoop;
            window.dispatchEvent(new CustomEvent('playback-state-changed', { detail: getPlaybackState() }));
            return state.isLoop;
        }

        function resolveNextIndex() {
            if (!state.playbackQueue.length) {
                return -1;
            }

            if (state.isShuffle) {
                return Math.floor(Math.random() * state.playbackQueue.length);
            }

            const next = state.currentIndex + 1;
            return next >= state.playbackQueue.length ? 0 : next;
        }

        function resolvePreviousIndex() {
            if (!state.playbackQueue.length) {
                return -1;
            }

            const previous = state.currentIndex - 1;
            return previous < 0 ? state.playbackQueue.length - 1 : previous;
        }

        async function playByIndex(index) {
            if (index < 0 || index >= state.playbackQueue.length) {
                return;
            }

            const fileName = state.playbackQueue[index];
            state.currentIndex = index;
            await playMusicFile(fileName);
        }

        async function nextTrack() {
            await submitCurrentTrackScrobble();
            const next = resolveNextIndex();
            await playByIndex(next);
        }

        async function previousTrack() {
            await submitCurrentTrackScrobble();
            const previous = resolvePreviousIndex();
            await playByIndex(previous);
        }

        async function submitCurrentTrackScrobble() {
            if (!state.currentAudio || !state.currentTrackFileName) {
                return;
            }

            const elapsed = state.lastTrackStart
                ? Math.max(0, Math.round((Date.now() - state.lastTrackStart) / 1000))
                : 0;
            const progress = state.currentAudio.duration > 0
                ? Math.min(100, Math.max(0, Math.round((state.currentAudio.currentTime / state.currentAudio.duration) * 100)))
                : 0;

            await submitScrobble(state.currentTrackFileName, progress, elapsed);
        }

        async function playMusicFile(fileName) {
            const activeFolder = getActiveFolder();
            if (!activeFolder || !activeFolder.handle) {
                return;
            }

            try {
                const fileHandle = await activeFolder.handle.getFileHandle(fileName);
                const file = await fileHandle.getFile();
                const fileUrl = URL.createObjectURL(file);

                if (state.currentAudio) {
                    state.currentAudio.pause();
                    URL.revokeObjectURL(state.currentAudio.src);
                }

                state.currentTrackFileName = fileName;
                state.lastTrackStart = Date.now();
                const metadata = await readTrackMetadata(fileName);
                if (state.playbackQueue.length) {
                    const idx = state.playbackQueue.indexOf(fileName);
                    state.currentIndex = idx;
                }

                state.currentAudio = new Audio(fileUrl);

                window.dispatchEvent(new CustomEvent('track-change', {
                    detail: {
                        fileName,
                        trackTitle: metadata.trackTitle,
                        artist: metadata.artist,
                        album: metadata.album,
                        imageUrl: metadata.imageUrl,
                        duration: 0,
                        loading: true
                    }
                }));

                state.currentAudio.addEventListener('loadedmetadata', () => {
                    window.dispatchEvent(new CustomEvent('track-change', {
                        detail: {
                            fileName,
                            trackTitle: metadata.trackTitle,
                            artist: metadata.artist,
                            album: metadata.album,
                            imageUrl: metadata.imageUrl,
                            duration: state.currentAudio.duration,
                            loading: false
                        }
                    }));
                });

                state.currentAudio.play();

                window.dispatchEvent(new CustomEvent('playback-state-changed', { detail: getPlaybackState() }));

                state.currentAudio.addEventListener('ended', () => {
                    URL.revokeObjectURL(fileUrl);

                    if (state.isLoop) {
                        playMusicFile(fileName);
                        return;
                    }

                    nextTrack();
                });
            } catch {
                console.error(`Failed to play ${fileName}.`);
            }
        }

        function pauseMusic() {
            if (state.currentAudio) {
                state.currentAudio.pause();
            }
        }

        function resumeMusic() {
            if (state.currentAudio) {
                state.currentAudio.play();
            }
        }

        function seekMusic(position) {
            if (state.currentAudio) {
                state.currentAudio.currentTime = position;
            }
        }

        function getCurrentAudio() {
            return state.currentAudio;
        }

        return {
            playMusicFile,
            pauseMusic,
            resumeMusic,
            seekMusic,
            getCurrentAudio,
            setPlaybackQueue,
            nextTrack,
            previousTrack,
            toggleShuffle,
            toggleLoop,
            getPlaybackState
        };
    }

    window.hmqsModules.createPlayerModule = createPlayerModule;
})();