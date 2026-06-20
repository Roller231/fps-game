# FPS Game - Unity WebGL + FastAPI Backend

Multiplayer FPS game with backend integration for authentication, profiles, weapons, and in-game currency.

## Quick Start (Local)

```bash
# 1. Build Unity WebGL project to /Build folder
# 2. Patch the build
.\patch-build.ps1

# 3. Start services
docker-compose up --build -d

# 4. Open browser
http://localhost:8000
```

## Deploy to Server (IP only, no domain)

See [DEPLOY.md](DEPLOY.md) for full instructions.

**Quick version:**

1. Copy project to server
2. Edit `.env` file:
   ```env
   SERVER_HOST=YOUR_SERVER_IP  # e.g., 123.45.67.89
   ```
3. Run:
   ```bash
   chmod +x deploy.sh
   ./deploy.sh
   ```

**Access:**
- Landing: `http://YOUR_IP:8000`
- Game: `http://YOUR_IP:8080` (auto-opened via PLAY button)

## Features

### Authentication & Profiles
- Registration/Login with token-based auth
- Avatar upload (JPEG, PNG, GIF, WebP)
- Display name & password management
- Session persistence in browser localStorage
- Auto-logout UI

### In-Game Systems
- Weapon inventory with persistence
- Equipped weapon slots (saved to backend)
- In-game currency (balance)
- Stats tracking (kills, waves, playtime)
- Profile auto-save every 30s
- Save throttling to prevent backend overload

### Donate System
- 3 donation packages (Starter, Soldier, Commander)
- Ready for payment integration (Stripe, PayPal, etc.)
- Backend endpoint `/api/donate/grant` prepared
- Frontend hooks documented in code

### Unity Components
- `AuthManager` - Token validation & gameplay gating
- `ProfileService` - Profile sync with backend
- `WeaponInventory` - Weapon management
- `MoneyManager` - Currency system
- `ExternalLinkOpener` - Open URLs from buttons

## Tech Stack

- **Frontend**: Unity WebGL 2022.3.62f3
- **Backend**: FastAPI (Python)
- **Database**: MySQL 8.0
- **Web Server**: Nginx (for WebGL build)
- **Deployment**: Docker Compose

## Project Structure

```
fps-game/
├── Assets/              # Unity project
├── Build/               # Unity WebGL build output
├── backend/
│   ├── app/
│   │   ├── main.py      # FastAPI endpoints
│   │   ├── models.py    # SQLAlchemy models
│   │   ├── schemas.py   # Pydantic schemas
│   │   ├── templates/   # Jinja2 templates (landing page)
│   │   └── uploads/     # User-uploaded avatars
│   ├── Dockerfile
│   └── Dockerfile.webgl # Nginx for WebGL
├── docker-compose.yml
├── .env                 # Configuration (not in git)
├── .env.example         # Template
└── patch-build.ps1      # Suppress Unity console warnings
```

## Configuration

All configuration in `.env` file:

```env
# Server (change for production)
SERVER_HOST=localhost

# Ports
BACKEND_PORT=8000
WEBGL_PORT=8080
MYSQL_PORT=3307

# Database
BACKEND_MYSQL_USER=game_user
BACKEND_MYSQL_PASSWORD=game_pass
BACKEND_MYSQL_DB=game_db
BACKEND_SECRET_KEY=change_me
```

## API Endpoints

- `POST /api/register` - Create account
- `POST /api/login` - Get token
- `GET /api/token/validate` - Validate token
- `GET /api/profile` - Get user profile
- `POST /api/profile` - Update profile
- `POST /api/profile/password` - Change password
- `POST /api/profile/avatar` - Upload avatar
- `POST /api/donate/grant` - Credit currency (payment integration point)

## Development

### Local Testing
```bash
docker-compose up --build
```

### View Logs
```bash
docker-compose logs -f backend
docker-compose logs -f webgl
docker-compose logs -f mysql
```

### Rebuild After Changes
```bash
docker-compose down
docker-compose up --build -d
```

### Clean Database
```bash
docker-compose down -v  # ⚠️ Deletes all data
```

## Payment Integration

See `backend/app/templates/index.html` lines 501-590 for integration points.

**To add payment provider:**
1. Implement `processPayment(pkg, token)` function
2. Call payment SDK (Stripe, PayPal, YooKassa, etc.)
3. On success, call `grantCurrency(pkg, token)`
4. Backend verifies and credits balance

**Security:** Always verify payments server-side via webhooks, never trust client-side confirmation.

## Documentation

- [DEPLOY.md](DEPLOY.md) - Production deployment guide
- [BUILD_INSTRUCTIONS.md](BUILD_INSTRUCTIONS.md) - Build & patch process
- [AVATAR_UPLOAD_GUIDE.md](AVATAR_UPLOAD_GUIDE.md) - Avatar system details
- [TESTING_CHECKLIST.md](TESTING_CHECKLIST.md) - QA checklist
- [BACKEND_INTEGRATION.md](BACKEND_INTEGRATION.md) - Backend API docs

## License

Proprietary - All rights reserved
