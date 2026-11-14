#!/usr/bin/env bash
set -e

echo "=== [VoxieQuest] Starting Vercel Build for Blazor WebAssembly ==="

# 1. Download official Microsoft dotnet-install script if dotnet is not available
if ! command -v dotnet &> /dev/null; then
    echo "=== [VoxieQuest] Installing .NET 8 SDK via dotnet-install.sh ==="
    curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
    chmod +x dotnet-install.sh
    ./dotnet-install.sh --channel 8.0 --install-dir "$HOME/.dotnet"
    export DOTNET_ROOT="$HOME/.dotnet"
    export PATH="$DOTNET_ROOT:$PATH"
fi

echo "=== [VoxieQuest] Active .NET SDK Version ==="
dotnet --version

# 2. Publish Blazor WebAssembly in Release configuration
echo "=== [VoxieQuest] Publishing VoxieQuest.Web ==="
dotnet publish src/VoxieQuest.Web/VoxieQuest.Web.csproj -c Release -o publish

# 3. Ensure vercel.json is copied to the final static directory
if [ -f "src/VoxieQuest.Web/wwwroot/vercel.json" ]; then
    cp -f "src/VoxieQuest.Web/wwwroot/vercel.json" "publish/wwwroot/vercel.json"
fi

echo "=== [VoxieQuest] Build Finished! Static output located at publish/wwwroot ==="
