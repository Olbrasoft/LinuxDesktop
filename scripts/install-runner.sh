#!/bin/bash
set -e

REPO_OWNER="Olbrasoft"
REPO_NAME="LinuxDesktop"
RUNNER_DIR="$HOME/actions-runner-${REPO_NAME}"
RUNNER_VERSION="2.321.0"

echo "🚀 Installing GitHub Actions Runner for ${REPO_OWNER}/${REPO_NAME}..."

# Check if runner already exists
if [ -d "$RUNNER_DIR" ]; then
    echo "❌ Runner directory already exists: $RUNNER_DIR"
    echo "   Run ./scripts/uninstall-runner.sh first"
    exit 1
fi

# Download runner
mkdir -p "$RUNNER_DIR"
cd "$RUNNER_DIR"
curl -o actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz -L \
    https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz
tar xzf ./actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz
rm actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz

# Get registration token
echo "🔑 Getting registration token from GitHub..."
TOKEN=$(gh api -X POST repos/${REPO_OWNER}/${REPO_NAME}/actions/runners/registration-token --jq .token)

if [ -z "$TOKEN" ]; then
    echo "❌ Failed to get registration token. Make sure you have 'gh' CLI installed and authenticated."
    exit 1
fi

# Configure runner
echo "⚙️  Configuring runner..."
./config.sh --url https://github.com/${REPO_OWNER}/${REPO_NAME} --token $TOKEN --unattended

# Install as service
echo "🔧 Installing runner as systemd service..."
sudo ./svc.sh install
sudo ./svc.sh start

echo "✅ Runner installed and started!"
echo "   Directory: $RUNNER_DIR"
echo "   Status: sudo ./svc.sh status"
