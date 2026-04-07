const API_BASE = "http://localhost:5196/api";

// ─── TOKEN HELPERS ────────────────────────────────────────────────────────────

function saveTokens(accessToken, refreshToken) {
    localStorage.setItem("accessToken", accessToken);
    localStorage.setItem("refreshToken", refreshToken);
}

function getAccessToken() {
    return localStorage.getItem("accessToken");
}

function clearTokens() {
    localStorage.removeItem("accessToken");
    localStorage.removeItem("refreshToken");
    localStorage.removeItem("username");
    localStorage.removeItem("email");
}

function isLoggedIn() {
    return getAccessToken() !== null;
}

// ─── AUTH GUARD ───────────────────────────────────────────────────────────────

function requireAuth() {
    if (!isLoggedIn()) {
        window.location.href = "login.html";
    }
}

function redirectIfLoggedIn() {
    if (isLoggedIn()) {
        window.location.href = "dashboard.html";
    }
}

// ─── AUTH API CALLS ───────────────────────────────────────────────────────────

async function registerUser(dto) {
    const response = await fetch(`${API_BASE}/auth/register`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dto)
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Registration failed.");
    return data;
}

async function loginUser(dto) {
    const response = await fetch(`${API_BASE}/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dto)
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Login failed.");
    return data;
}

// ─── SHARED HEADER HELPER ─────────────────────────────────────────────────────

function authHeaders() {
    return {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${getAccessToken()}`
    };
}

// ─── SONG API CALLS ───────────────────────────────────────────────────────────

async function getSongs() {
    const response = await fetch(`${API_BASE}/songs`, {
        method: "GET",
        headers: authHeaders()
    });
    if (response.status === 401) {
        clearTokens();
        window.location.href = "login.html";
        return;
    }
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Failed to load songs.");
    return data;
}

async function addSong(dto) {
    const response = await fetch(`${API_BASE}/songs`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify(dto)
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Failed to add song.");
    return data;
}

async function deleteSong(id) {
    const response = await fetch(`${API_BASE}/songs/${id}`, {
        method: "DELETE",
        headers: authHeaders()
    });
    if (!response.ok) throw new Error("Failed to delete song.");
}

async function getSongCount() {
    const response = await fetch(`${API_BASE}/songs/count`, {
        headers: authHeaders()
    });
    const data = await response.json();
    return data.count;
}

// ─── STREAM HELPER ────────────────────────────────────────────────────────────

// Builds the streaming URL for a song
// The <audio> element uses this URL as its src
// The JWT token is passed as a query param because <audio> cannot set headers
function getStreamUrl(songId) {
    return `${API_BASE}/stream/${songId}?token=${getAccessToken()}`;
}

// ─── METADATA API CALLS ───────────────────────────────────────────────────────

async function searchMetadata(query) {
    const response = await fetch(
        `${API_BASE}/metadata/search?q=${encodeURIComponent(query)}`,
        { headers: authHeaders() }
    );
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Search failed.");
    return data;
}

async function applyMetadata(dto) {
    const response = await fetch(`${API_BASE}/metadata/apply`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify(dto)
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Failed to apply metadata.");
    return data;
}

// ─── SPOTIFY IMPORT API CALLS ─────────────────────────────────────────────────

async function importSpotifyTracks(trackNames) {
    const response = await fetch(`${API_BASE}/spotify/import`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({ trackNames })
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Import failed.");
    return data;
}

async function getSpotifyImports() {
    const response = await fetch(`${API_BASE}/spotify/imports`, {
        headers: authHeaders()
    });
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || "Failed to load imports.");
    return data;
}

async function getImportCount() {
    const response = await fetch(`${API_BASE}/spotify/count`, {
        headers: authHeaders()
    });
    const data = await response.json();
    return data.count;
}