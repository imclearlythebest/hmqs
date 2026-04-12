(function () {
    function pickerDropdown() {
        return {
            state: null,
            async init() {
                this.state = await window.hmqsClient.initializePicker();
            },
            async switchFolder(id) {
                this.state = await window.hmqsClient.switchFolder(id);
                this.closeMenu();
            },
            async addNewFolder() {
                this.state = await window.hmqsClient.addNewFolder();
                this.closeMenu();
            },
            closeMenu() {
                const toggle = this.$root.querySelector("[data-bs-toggle='dropdown']");
                if (toggle) {
                    const dropdown = bootstrap.Dropdown.getOrCreateInstance(toggle);
                    dropdown.hide();
                }
            },
            getActiveFolderName() {
                const activeFolder = this.state?.folders?.find(f => f.id === this.state?.activeId);
                return activeFolder ? activeFolder.name : 'Select Folder';
            }
        };
    }

    function homePage() {
        return {
            files: [],
            loading: false,
            selectedPlaylist: null,
            availablePlaylists: [],
            playlistTarget: '',
            newPlaylistName: '',
            pendingPlaylistFile: null,
            pendingPlaylistLabel: '',
            addingToPlaylist: false,
            playlistModal: null,
            dragIndex: null,
            dragOverIndex: null,
            reorderMode: false,
            suppressNextLoading: false,

            async init() {
                await this.loadFiles();
                this.selectedPlaylist = window.hmqsClient.getSelectedPlaylist();
                if (this.$refs.playlistModal) {
                    this.playlistModal = bootstrap.Modal.getOrCreateInstance(this.$refs.playlistModal);
                }
                window.addEventListener('folder-changed', async () => {
                    await this.loadFiles({ silent: this.suppressNextLoading });
                    this.suppressNextLoading = false;
                });
                window.addEventListener('playlist-selected', async e => {
                    this.selectedPlaylist = e.detail?.playlist || null;
                    this.reorderMode = false;
                    await this.loadFiles();
                });
            },

            async loadFiles(options = {}) {
                const silent = !!options.silent;
                if (!silent) {
                    this.loading = true;
                }
                const allFiles = await window.hmqsClient.getCurrentFolderFiles();

                if (!this.selectedPlaylist) {
                    this.files = allFiles;
                    window.hmqsClient.setPlaybackQueue(this.files.map(x => x.name));
                    this.loading = false;
                    return;
                }

                try {
                    const tracks = await window.hmqsClient.getPlaylistTracks(this.selectedPlaylist);
                    const byName = new Map(allFiles.map(f => [f.name, f]));
                    this.files = tracks
                        .map(trackName => byName.get(trackName))
                        .filter(Boolean)
                        .map(file => ({ ...file }));
                } catch {
                    this.files = allFiles;
                }

                window.hmqsClient.setPlaybackQueue(this.files.map(x => x.name));
                this.loading = false;
            },

            async createMetadataFile(fileName) {
                await window.hmqsClient.createMetadataFile(fileName);
                await this.loadFiles();
            },

            async editMetadata(fileName) {
                await window.hmqsClient.editMetadata(fileName);
            },

            async openMetadataEditor(file) {
                if (!file?.name) {
                    return;
                }

                if (!file.hasMetadata) {
                    await this.createMetadataFile(file.name);
                }

                await this.editMetadata(file.name);
            },

            displayTrackName(file) {
                const title = typeof file?.trackTitle === 'string' ? file.trackTitle.trim() : '';
                return title || file?.name || '';
            },

            displayArtistAlbum(file) {
                const artist = typeof file?.artist === 'string' ? file.artist.trim() : '';
                const album = typeof file?.album === 'string' ? file.album.trim() : '';
                if (artist && album) {
                    return `${artist} - ${album}`;
                }

                return artist || album || '';
            },

            async playFile(fileName) {
                await window.hmqsClient.playMusicFile(fileName);
            },

            async addToPlaylist(file) {
                await this.openAddToPlaylistModal(file);
            },

            async openAddToPlaylistModal(file) {
                this.pendingPlaylistFile = file?.name || '';
                this.pendingPlaylistLabel = this.displayTrackName(file) || file?.name || '';
                this.newPlaylistName = '';
                this.playlistTarget = '';

                try {
                    this.availablePlaylists = await window.hmqsClient.listPlaylists();
                    if (this.selectedPlaylist && this.availablePlaylists.includes(this.selectedPlaylist)) {
                        this.playlistTarget = this.selectedPlaylist;
                    } else if (this.availablePlaylists.length) {
                        this.playlistTarget = this.availablePlaylists[0];
                    }
                } catch {
                    this.availablePlaylists = [];
                }

                if (this.playlistModal) {
                    this.playlistModal.show();
                }
            },

            closeAddToPlaylistModal() {
                if (this.playlistModal) {
                    this.playlistModal.hide();
                }
            },

            async confirmAddToPlaylist() {
                if (!this.pendingPlaylistFile) {
                    return;
                }

                const createdName = this.newPlaylistName.trim();
                let targetPlaylist = this.playlistTarget;

                this.addingToPlaylist = true;
                try {
                    if (createdName) {
                        await window.hmqsClient.createPlaylist(createdName);
                        targetPlaylist = createdName;
                    }

                    if (!targetPlaylist) {
                        return;
                    }

                    await window.hmqsClient.addTrackToPlaylist(targetPlaylist, this.pendingPlaylistFile);
                    this.closeAddToPlaylistModal();

                    if (this.selectedPlaylist) {
                        await this.loadFiles();
                    }
                } finally {
                    this.addingToPlaylist = false;
                }
            },

            async removeFromPlaylist(fileName, index = null) {
                if (!this.selectedPlaylist) {
                    return;
                }

                await window.hmqsClient.removeTrackFromPlaylist(this.selectedPlaylist, fileName, index);
                await this.loadFiles();
            },

            startDrag(index) {
                if (!this.selectedPlaylist || !this.reorderMode) {
                    return;
                }

                this.dragIndex = index;
                this.dragOverIndex = index;
            },

            setDragOver(index) {
                if (!this.selectedPlaylist || !this.reorderMode || this.dragIndex === null) {
                    return;
                }

                this.dragOverIndex = index;
            },

            async dropDrag(index) {
                if (!this.selectedPlaylist || !this.reorderMode || this.dragIndex === null) {
                    return;
                }

                const from = this.dragIndex;
                const to = index;
                this.dragIndex = null;
                this.dragOverIndex = null;

                if (from === to || from < 0 || to < 0 || from >= this.files.length || to >= this.files.length) {
                    return;
                }

                const reordered = [...this.files];
                const [moved] = reordered.splice(from, 1);
                reordered.splice(to, 0, moved);
                this.files = reordered;
                this.suppressNextLoading = true;

                await window.hmqsClient.savePlaylistTracks(
                    this.selectedPlaylist,
                    reordered.map(x => x.name)
                );
            },

            endDrag() {
                this.dragIndex = null;
                this.dragOverIndex = null;
            },

            rowDragClass(index) {
                if (!this.selectedPlaylist || !this.reorderMode || this.dragIndex === null) {
                    return '';
                }

                if (this.dragIndex === index) {
                    return 'hmqs-dragging-row';
                }

                return this.dragOverIndex === index ? 'hmqs-drag-over-row' : '';
            },

            toggleReorderMode() {
                this.reorderMode = !this.reorderMode;
                this.endDrag();
            }
        };
    }

    function sidebarPlaylists() {
        return {
            playlists: [],
            selectedPlaylist: null,
            createPlaylistName: '',
            renameSourceName: '',
            renamePlaylistName: '',
            deletePlaylistName: '',
            createModal: null,
            renameModal: null,
            deleteModal: null,

            async init() {
                await window.hmqsClient.initializePicker();
                await this.loadPlaylists();
                this.selectedPlaylist = window.hmqsClient.getSelectedPlaylist();
                if (this.$refs.createPlaylistModal) {
                    this.createModal = bootstrap.Modal.getOrCreateInstance(this.$refs.createPlaylistModal);
                }
                if (this.$refs.renamePlaylistModal) {
                    this.renameModal = bootstrap.Modal.getOrCreateInstance(this.$refs.renamePlaylistModal);
                }
                if (this.$refs.deletePlaylistModal) {
                    this.deleteModal = bootstrap.Modal.getOrCreateInstance(this.$refs.deletePlaylistModal);
                }

                window.addEventListener('folder-changed', async () => {
                    await this.loadPlaylists();
                });

                window.addEventListener('playlist-selected', e => {
                    this.selectedPlaylist = e.detail?.playlist || null;
                });
            },

            async loadPlaylists() {
                try {
                    this.playlists = await window.hmqsClient.listPlaylists();
                } catch {
                    this.playlists = [];
                }
            },

            showAllSongs() {
                window.hmqsClient.setSelectedPlaylist(null);
            },

            selectPlaylist(name) {
                window.hmqsClient.setSelectedPlaylist(name);
            },

            openCreatePlaylistModal() {
                this.createPlaylistName = '';
                if (this.createModal) {
                    this.createModal.show();
                }
            },

            closeCreatePlaylistModal() {
                if (this.createModal) {
                    this.createModal.hide();
                }
            },

            async confirmCreatePlaylist() {
                const name = this.createPlaylistName.trim();
                if (!name) {
                    return;
                }

                try {
                    await window.hmqsClient.createPlaylist(name);
                    await this.loadPlaylists();
                    window.hmqsClient.setSelectedPlaylist(name);
                    this.closeCreatePlaylistModal();
                } catch (err) {
                    window.alert(err?.message || 'Could not create playlist.');
                }
            },

            openRenamePlaylistModal(oldName) {
                this.renameSourceName = oldName;
                this.renamePlaylistName = oldName;
                if (this.renameModal) {
                    this.renameModal.show();
                }
            },

            closeRenamePlaylistModal() {
                if (this.renameModal) {
                    this.renameModal.hide();
                }
            },

            async confirmRenamePlaylist() {
                const oldName = this.renameSourceName;
                const next = this.renamePlaylistName.trim();
                if (!next || next === oldName) {
                    return;
                }

                try {
                    await window.hmqsClient.renamePlaylist(oldName, next);
                    await this.loadPlaylists();
                    if (this.selectedPlaylist === oldName) {
                        window.hmqsClient.setSelectedPlaylist(next);
                    }
                    this.closeRenamePlaylistModal();
                } catch (err) {
                    window.alert(err?.message || 'Could not rename playlist.');
                }
            },

            openDeletePlaylistModal(name) {
                this.deletePlaylistName = name;
                if (this.deleteModal) {
                    this.deleteModal.show();
                }
            },

            closeDeletePlaylistModal() {
                if (this.deleteModal) {
                    this.deleteModal.hide();
                }
            },

            async confirmDeletePlaylist() {
                const name = this.deletePlaylistName;
                if (!name) {
                    return;
                }

                try {
                    await window.hmqsClient.deletePlaylist(name);
                    await this.loadPlaylists();
                    if (this.selectedPlaylist === name) {
                        window.hmqsClient.setSelectedPlaylist(null);
                    }
                    this.closeDeletePlaylistModal();
                } catch (err) {
                    window.alert(err?.message || 'Could not delete playlist.');
                }
            }
        };
    }

    function metadataEditor(fileName) {
        return {
            saving: false,
            error: '',
            success: '',
            form: {
                itunesTrackId: null,
                trackTitle: '',
                itunesArtistId: 0,
                artist: '',
                itunesCollectionId: 0,
                album: '',
                genre: '',
                year: 0,
                imageUrl: ''
            },

            async init() {
                try {
                    await window.hmqsClient.initializePicker();
                    const metadata = await window.hmqsClient.getMetadataFile(fileName);

                    this.form.itunesTrackId = metadata.itunesTrackId ?? null;
                    this.form.trackTitle = metadata.trackTitle ?? '';
                    this.form.itunesArtistId = Number.isFinite(metadata.itunesArtistId) ? metadata.itunesArtistId : 0;
                    this.form.artist = metadata.artist ?? '';
                    this.form.itunesCollectionId = Number.isFinite(metadata.itunesCollectionId) ? metadata.itunesCollectionId : 0;
                    this.form.album = metadata.album ?? '';
                    this.form.genre = metadata.genre ?? '';
                    this.form.year = Number.isFinite(metadata.year) ? metadata.year : 0;
                    this.form.imageUrl = metadata.imageUrl ?? '';
                } catch (err) {
                    this.error = err?.message || 'Failed to load metadata.';
                }
            },

            async save() {
                this.error = '';
                this.success = '';

                const payload = {
                    itunesTrackId: Number.isFinite(this.form.itunesTrackId) ? this.form.itunesTrackId : 0,
                    trackTitle: this.form.trackTitle?.trim() ?? '',
                    itunesArtistId: Number.isFinite(this.form.itunesArtistId) ? this.form.itunesArtistId : 0,
                    artist: this.form.artist?.trim() ?? '',
                    itunesCollectionId: Number.isFinite(this.form.itunesCollectionId) ? this.form.itunesCollectionId : 0,
                    album: this.form.album?.trim() ?? '',
                    genre: this.form.genre?.trim() ?? '',
                    year: Number.isFinite(this.form.year) ? this.form.year : 0,
                    imageUrl: this.form.imageUrl?.trim() ?? ''
                };

                this.saving = true;
                try {
                    await window.hmqsClient.saveMetadataFile(fileName, payload);
                    this.success = 'Metadata saved.';
                } catch (err) {
                    this.error = err?.message || 'Failed to save metadata.';
                } finally {
                    this.saving = false;
                }
            }
        };
    }

    function metadataEditorFromElement(el) {
        const fileName = el?.dataset?.fileName || '';
        return metadataEditor(fileName);
    }

    function playerWidget() {
        return {
            currentTrack: null,
            currentTrackDisplay: '',
            currentArtist: '',
            currentAlbum: '',
            currentCoverUrl: '',
            isPlaying: false,
            currentTime: 0,
            duration: 0,
            seekTooltip: '00:00',
            interval: null,
            isShuffle: false,
            isLoop: false,
            isDraggingSeek: false,

            init() {
                window.addEventListener('track-change', e => {
                    this.currentTrack = e.detail.fileName;
                    this.currentTrackDisplay = e.detail.trackTitle || e.detail.fileName;
                    this.currentArtist = e.detail.artist || '';
                    this.currentAlbum = e.detail.album || '';
                    this.currentCoverUrl = e.detail.imageUrl || '';
                    this.duration = e.detail.duration;
                    this.currentTime = 0;
                    this.isPlaying = true;
                });

                window.addEventListener('playback-state-changed', e => {
                    this.isShuffle = !!e.detail?.isShuffle;
                    this.isLoop = !!e.detail?.isLoop;
                });

                const state = window.hmqsClient.getPlaybackState();
                this.isShuffle = state.isShuffle;
                this.isLoop = state.isLoop;

                this.interval = setInterval(() => {
                    const audio = window.hmqsClient?.getCurrentAudio();
                    if (audio) {
                        if (!this.isDraggingSeek) {
                            this.currentTime = audio.currentTime || 0;
                        }
                        this.isPlaying = !audio.paused;
                        if (audio.duration && audio.duration !== this.duration) {
                            this.duration = audio.duration;
                        }
                    }
                }, 40);
            },

            play() {
                window.hmqsClient.resumeMusic();
            },

            pause() {
                window.hmqsClient.pauseMusic();
            },

            seek(e) {
                window.hmqsClient.seekMusic(parseFloat(e.target.value));
            },

            seekInput(e) {
                this.isDraggingSeek = true;
                this.currentTime = parseFloat(e.target.value);
                this.seek(e);
            },

            updateSeekHover(e) {
                const slider = e.target;
                const rect = slider.getBoundingClientRect();
                if (rect.width <= 0 || !this.duration) {
                    this.seekTooltip = this.formatTime(0);
                    return;
                }

                const relative = Math.max(0, Math.min(rect.width, e.clientX - rect.left));
                const hovered = (relative / rect.width) * this.duration;
                this.seekTooltip = this.formatTime(hovered);
            },

            formatTime(value) {
                if (!Number.isFinite(value) || value < 0) {
                    return '00:00';
                }

                const seconds = Math.floor(value);
                const minutes = Math.floor(seconds / 60);
                const remainder = seconds % 60;
                return `${String(minutes).padStart(2, '0')}:${String(remainder).padStart(2, '0')}`;
            },

            displayNowPlayingMeta() {
                if (this.currentArtist && this.currentAlbum) {
                    return `${this.currentArtist} - ${this.currentAlbum}`;
                }

                return this.currentArtist || this.currentAlbum || '';
            },

            seekCommit() {
                this.isDraggingSeek = false;
            },

            seekStyle() {
                if (!this.duration || this.duration <= 0) {
                    return '--seek-percent: 0%;';
                }

                const pct = Math.max(0, Math.min(100, (this.currentTime / this.duration) * 100));
                return `--seek-percent: ${pct}%;`;
            },

            async next() {
                await window.hmqsClient.nextTrack();
            },

            async previous() {
                await window.hmqsClient.previousTrack();
            },

            toggleShuffle() {
                this.isShuffle = window.hmqsClient.toggleShuffle();
            },

            toggleLoop() {
                this.isLoop = window.hmqsClient.toggleLoop();
            },

        };
    }

    window.hmqsPages = {
        pickerDropdown,
        sidebarPlaylists,
        homePage,
        metadataEditorFromElement,
        metadataEditor,
        playerWidget
    };
})();