#!/bin/bash

# WebLynx Build Script
# This script builds the application for both macOS and Windows

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

# Create distribution packages
echo "Creating distribution packages..."

# macOS package
cd publish/osx-x64/
zip -r ../../WebLynx-macos-x64.zip WebLynx appsettings.json
cd ../..

# Windows package
cd publish/win-x64/
zip -r ../../WebLynx-windows-x64.zip WebLynx.exe appsettings.json
cd ../..

echo "Build complete!"
echo "Distribution packages created:"
echo "  - WebLynx-macos-x64.zip"
echo "  - WebLynx-windows-x64.zip"
