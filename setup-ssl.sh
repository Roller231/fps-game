#!/bin/bash
# SSL Setup Script for fpsmilitarygame.online

set -e

DOMAIN="fpsmilitarygame.online"
EMAIL="admin@fpsmilitarygame.online"  # Change this to your email

echo "=== SSL Certificate Setup for $DOMAIN ==="
echo ""

# Check if domain is set in .env
if ! grep -q "SERVER_HOST=$DOMAIN" .env 2>/dev/null; then
    echo "⚠️  Warning: .env file should have SERVER_HOST=$DOMAIN"
    echo "Update .env file and restart after getting certificate"
fi

echo "1. Starting nginx without SSL first..."
docker-compose up -d webgl

echo ""
echo "2. Obtaining SSL certificate from Let's Encrypt..."
docker-compose run --rm certbot certonly \
    --webroot \
    --webroot-path=/var/www/certbot \
    --email $EMAIL \
    --agree-tos \
    --no-eff-email \
    -d $DOMAIN \
    -d www.$DOMAIN

echo ""
echo "3. Restarting nginx with SSL configuration..."
docker-compose down
docker-compose up -d

echo ""
echo "✅ SSL Setup Complete!"
echo ""
echo "🌐 Your game is now available at:"
echo "   https://$DOMAIN"
echo ""
echo "📝 Certificate will auto-renew every 12 hours via certbot container"
echo ""
