# Testing Checklist

## Landing Page

### Registration & Login
- [ ] Register new user with username and password
- [ ] Display name auto-fills from username if empty
- [ ] Login with existing credentials
- [ ] Error shown for wrong password
- [ ] Error shown for duplicate username on registration

### Avatar Upload
- [ ] File input accepts images (JPEG, PNG, GIF, WebP)
- [ ] Preview shows selected image immediately
- [ ] Avatar uploads successfully on "Save Profile"
- [ ] Avatar persists after page reload
- [ ] Avatar URL starts with `/uploads/`

### Session Management
- [ ] Token saved to `localStorage` after login/register
- [ ] Page reload restores logged-in state
- [ ] Auth form hidden when logged in
- [ ] Shows "Logged in as [username]"
- [ ] "Log Out" button visible when logged in
- [ ] Clicking "Log Out" clears session
- [ ] After logout, auth form reappears
- [ ] After logout, PLAY button disabled

### PLAY Button
- [ ] Disabled when not logged in
- [ ] Enabled after successful login/register
- [ ] Shows token in chip below button
- [ ] Clicking redirects to game with token in URL
- [ ] Token persists in URL parameter

### Profile Settings
- [ ] Profile card hidden until logged in
- [ ] Display name can be updated
- [ ] Avatar can be changed
- [ ] Password can be changed
- [ ] Current password required for password change
- [ ] Toast notifications show success/error

## Unity Game

### Profile Loading
- [ ] Game extracts token from URL parameter
- [ ] Profile loads from backend on game start
- [ ] Avatar downloads and displays in UI
- [ ] Relative avatar URLs (`/uploads/...`) work correctly
- [ ] Display name shows in UI
- [ ] Balance shows in UI with `$` prefix

### Avatar Display
- [ ] Avatar image component assigned in Inspector
- [ ] Avatar loads automatically after profile fetch
- [ ] Avatar caches to avoid redundant downloads
- [ ] Console logs avatar loading URL
- [ ] No errors if avatar URL is empty/null

### Weapon Persistence
- [ ] Equipped weapons load correctly
- [ ] Weapon slots preserved after scene reload
- [ ] Ammo counts restored
- [ ] No weapon slot shifting

### Profile Saving
- [ ] Profile saves throttled (no concurrent saves)
- [ ] Save queue works correctly
- [ ] Balance updates persist
- [ ] Weapon changes persist
- [ ] Stats updates persist

## WebGL Build

### Console Warnings
- [ ] Run `patch-build.ps1` after Unity build
- [ ] Yellow warnings hidden in browser console
- [ ] Red errors still visible
- [ ] No brotli compression errors shown
- [ ] Game loads without console spam

### File Loading
- [ ] `.br` files load correctly (if present)
- [ ] `.gz` files load correctly (if present)
- [ ] Fallback to uncompressed files works
- [ ] No 404 errors for missing compressed files

## Docker Deployment

### Services
- [ ] MySQL starts and is healthy
- [ ] Backend starts after MySQL ready
- [ ] WebGL service serves Build folder
- [ ] All ports accessible (3307, 8000, 8080)

### Volumes
- [ ] `mysql_data` persists database
- [ ] `uploads_data` persists avatar files
- [ ] Avatars survive container restart
- [ ] Database survives container restart

### CORS
- [ ] Unity game can fetch from backend
- [ ] No CORS errors in browser console
- [ ] `/uploads/` files accessible from game

## Edge Cases

- [ ] Empty display name defaults to username
- [ ] No avatar uploaded (should not error)
- [ ] Very long username/display name
- [ ] Special characters in username
- [ ] Large avatar file (test size limits)
- [ ] Invalid file type upload attempt
- [ ] Network error during avatar upload
- [ ] Token expired/invalid
- [ ] Multiple tabs with same user
- [ ] Logout in one tab affects others (refresh needed)

## Performance

- [ ] Avatar loads within 2 seconds
- [ ] Profile loads within 1 second
- [ ] No lag when switching scenes
- [ ] Save throttling prevents backend overload
- [ ] No memory leaks from avatar sprites

## Security

- [ ] Token not visible in plain text (except debug)
- [ ] Password not logged anywhere
- [ ] File upload validates type
- [ ] Token required for all protected endpoints
- [ ] SQL injection not possible (parameterized queries)
