#!/bin/bash
# Quick deploy script for production server

set -e

echo "=== FPS Game Deploy Script ==="
echo ""

# Check if .env exists
if [ ! -f .env ]; then
    echo "❌ .env file not found!"
    echo "Copy .env.example to .env and configure SERVER_HOST"
    exit 1
fi

# Load .env
export $(cat .env | grep -v '^#' | xargs)

echo "📋 Configuration:"
echo "   SERVER_HOST: $SERVER_HOST"
echo "   BACKEND_PORT: $BACKEND_PORT"
echo "   WEBGL_PORT: $WEBGL_PORT"
echo ""

# Check if Build folder exists
if [ ! -d "Build" ]; then
    echo "⚠️  Warning: Build folder not found!"
    echo "   Make sure to build Unity WebGL project first."
    read -p "Continue anyway? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

echo "🔨 Building Docker images..."
docker-compose build

echo "🚀 Starting services..."
docker-compose up -d

echo ""
echo "⏳ Waiting for services to start..."
sleep 5

echo ""
echo "📊 Service status:"
docker-compose ps

echo ""
echo "✅ Deploy complete!"
echo ""
echo "🌐 Access your game:"
echo "   Landing page: http://$SERVER_HOST:$BACKEND_PORT"
echo "   Game (WebGL):  http://$SERVER_HOST:$WEBGL_PORT"
echo ""
echo "📝 View logs:"
echo "   docker-compose logs -f"
echo ""
