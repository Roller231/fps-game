# Avatar Upload Feature Guide

## Overview
Users can now upload avatar images directly from the landing page instead of providing URLs.

## Backend Implementation

### Endpoint: `POST /api/profile/avatar`
- **Parameters**: 
  - `token` (query string): User authentication token
  - `file` (form-data): Image file to upload
- **Accepted formats**: JPEG, PNG, GIF, WebP
- **Storage**: Files saved to `backend/app/uploads/` with UUID filenames
- **Response**: `{"avatar_url": "/uploads/filename.ext"}`

### File Storage
- Directory: `backend/app/uploads/`
- Files persisted via Docker volume `uploads_data`
- Accessible via `/uploads/` static route
- Ignored by git (see `backend/app/uploads/.gitignore`)

## Frontend (Landing Page)

### UI Changes
1. **Avatar Upload**
   - File input instead of text URL field
   - 60x60px circular preview
   - Real-time preview on file selection
   - Automatic upload on profile save

2. **Login State Management**
   - When logged in:
     - Auth form (login/register) is hidden
     - Shows "Logged in as [username]"
     - Displays "Log Out" button
   - When logged out:
     - Shows auth form
     - Hides logout section

3. **Logout Functionality**
   - Clears `localStorage` (token + username)
   - Resets UI to login state
   - Hides profile card
   - Disables PLAY button

### JavaScript Functions
- `updateAuthUI()`: Toggles between login form and logout section
- `logout()`: Handles logout process
- Avatar preview updates on file selection

## Unity Integration

### ProfileService Updates
- `DownloadAvatarCoroutine` now handles relative URLs
- Converts `/uploads/filename.ext` to `http://localhost:8000/uploads/filename.ext`
- Caches avatar sprite to avoid redundant downloads
- Logs avatar loading for debugging

### Usage in Unity
1. Assign `profileAvatarImage` (Image component) in Inspector
2. Avatar automatically loads when profile is fetched
3. Supports both absolute URLs and relative backend paths

## User Flow

1. **First Time User**
   - Register on landing page
   - Upload avatar image
   - Save profile
   - Click PLAY to launch game

2. **Returning User**
   - Landing page auto-restores session from `localStorage`
   - Shows logged-in state immediately
   - Avatar loads from backend
   - PLAY button ready to use

3. **Logout**
   - Click "Log Out" button
   - Session cleared
   - Must log in again to play

## Technical Notes

- Avatar URLs stored as relative paths (`/uploads/...`) in database
- Unity converts to absolute URLs using `backendBaseUrl`
- File size not limited (consider adding validation)
- No image compression (consider adding for production)
- Old avatars not deleted when new ones uploaded (consider cleanup)

## Security Considerations

- File type validation on backend
- Token required for upload
- Files stored outside web root (served via static mount)
- UUID filenames prevent path traversal

## Future Improvements

- [ ] Add file size limit (e.g., 2MB max)
- [ ] Image compression/resizing on upload
- [ ] Delete old avatar when new one uploaded
- [ ] Avatar cropping tool on frontend
- [ ] Default avatar placeholder
- [ ] CDN integration for production
