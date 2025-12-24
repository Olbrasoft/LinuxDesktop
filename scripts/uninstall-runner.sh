#!/bin/bash
set -e

REPO_NAME="LinuxDesktop"
RUNNER_DIR="$HOME/actions-runner-${REPO_NAME}"

echo "🗑️  Uninstalling GitHub Actions Runner..."

if [ ! -d "$RUNNER_DIR" ]; then
    echo "❌ Runner directory not found: $RUNNER_DIR"
    exit 1
fi

cd "$RUNNER_DIR"

# Stop and uninstall service
echo "🛑 Stopping service..."
sudo ./svc.sh stop || true
sudo ./svc.sh uninstall || true

# Remove runner from GitHub
echo "🔑 Getting removal token from GitHub..."
TOKEN=$(gh api -X POST repos/Olbrasoft/${REPO_NAME}/actions/runners/remove-token --jq .token)

if [ -n "$TOKEN" ]; then
    ./config.sh remove --token $TOKEN
else
    echo "⚠️  Could not get removal token, skipping GitHub removal"
fi

# Remove directory
cd ..
rm -rf "$RUNNER_DIR"

echo "✅ Runner uninstalled!"
