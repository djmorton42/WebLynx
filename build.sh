#!/bin/bash

# WebLynx Build Script
# This script performs a clean build of the application

set -e

echo "Building WebLynx..."

# Clean previous builds
echo "Cleaning previous builds..."
rm -rf bin/ publish/

# Build for macOS (current platform)
echo "Building for macOS..."
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -o publish/osx-x64/

# Build for Windows
echo "Building for Windows..."
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/win-x64/

echo "Build complete!"
echo "Built applications available in:"
echo "  - publish/osx-x64/WebLynx"
echo "  - publish/win-x64/WebLynx.exe"
