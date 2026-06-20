# Build Instructions

## Quick Start

After building in Unity, run:
```powershell
.\patch-build.ps1
docker-compose up --build
```

Then open http://localhost:8000 in your browser.

## WebGL Build Process

1. **Build the project in Unity**
   - File > Build Settings
   - Select WebGL platform
   - Click "Build" and choose the `Build` folder

2. **Patch the build** (automatically fixes console warnings)
   ```powershell
   .\patch-build.ps1
   ```

   This script will:
   - Hide yellow warning messages in browser console
   - Keep red error messages visible
   - Fix brotli compression warnings
   - Improve console readability

3. **Deploy with Docker**
   ```powershell
   docker-compose up --build
   ```

   Services will be available at:
   - Landing page: http://localhost:8000
   - Game (WebGL): http://localhost:8080
   - Backend API: http://localhost:8000/api

## New Features

### Avatar Upload
The landing page now supports direct file upload for avatars:
- Accepts: JPEG, PNG, GIF, WebP
- Files stored in `backend/app/uploads/`
- Accessible via `/uploads/` route
- Preview shown before saving
- Unity automatically loads avatar from backend

### Session Management
- Login state persisted in browser `localStorage`
- Auto-restore session on page reload
- "Log Out" button when logged in
- Auth form hidden when logged in
- Shows "Logged in as [username]"

See [AVATAR_UPLOAD_GUIDE.md](AVATAR_UPLOAD_GUIDE.md) for detailed documentation.

## Notes

- The `patch-build.ps1` script is safe to run multiple times
- It will skip patching if already applied
- Always run after each Unity build to maintain console filtering
