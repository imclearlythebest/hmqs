## Current Stack
- Backend: ASP.NET Core MVC, Identity, EF Core, Pomelo MySQL
- Frontend: Razor views, Alpine.js, Bootstrap 5, Font Awesome
- Browser APIs: File System Access API, IndexedDB (`idb`)
- Optional utility library: HTMX (loaded, lightly used in current flow)

## Project Layout
- Solution root contains one app project: `WebApp`
- Main web shell and partials: `WebApp/Views/Shared`
- Feature pages: `WebApp/Views/Home`, `WebApp/Views/Catalogue`, `WebApp/Views/Auth`
- Controllers: `WebApp/Controllers`
- Domain + DTOs: `WebApp/Models`
- Client modules: `WebApp/wwwroot/js/client`
- Global styles and theme behavior: `WebApp/wwwroot/css/style.css`, `WebApp/wwwroot/js/script.js`

## Runtime Architecture
- Server-side responsibilities:
    - Authentication/authorization and route handling
    - Scrobble ingestion and history queries
    - Page rendering with Razor
- Client-side responsibilities:
    - Folder selection and permission handling
    - Local file scanning (`.mp3`, `.wav`, `.flac`, `.m4a`, `.ogg`, `.aac`, `.wma`)
    - Metadata sidecar file I/O (`*.hmqsmeta`)
    - Playlist file I/O (`*.plist`)
    - Audio playback queue and player state

## Core Conventions
- Metadata is stored as sidecar JSON per track: `<filename>.hmqsmeta`
- Playlist tracks are stored in newline-separated files: `<playlist>.plist`
- Client features are exposed through one facade: `window.hmqsClient`
- Alpine page factories live in `WebApp/wwwroot/js/client/pages.js`
- Shared layout behavior (hide sidebar/player on specific pages) uses:
    - `ViewData["HideSidebar"]`
    - `ViewData["HidePlayer"]`

## Current Feature Scope
- Local library view with sortable track list and metadata preview
- Metadata editor with iTunes IDs, title/artist/album/genre/year/image URL
- Playlist create/rename/delete and add/remove track flow
- Bottom player with seekbar, shuffle/loop, and now-playing metadata
- Scrobble submit endpoint + authenticated history view (ID-focused payload)

## Running the App
1. Configure `DefaultConnection` in `WebApp/appsettings.Development.json`
2. Run from repo root:
     - `dotnet run --project .\WebApp\`

## Important Note (Development)
- `Program.cs` currently calls `EnsureDeleted()` + `EnsureCreated()` on startup.
- This resets the database every run and is intentional for rapid iteration right now.