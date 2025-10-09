#!/bin/bash

# WebLynx Distribution Build Script
# This script creates complete distributable packages for Windows and macOS

set -e

echo "Building WebLynx Distributions..."

# Clean previous builds
echo "Cleaning previous builds..."
rm -rf bin/ publish/ WebLynx-*-x64.zip

# Build for Windows
echo "Building for Windows (x64)..."
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/win-x64/

# Build for macOS
echo "Building for macOS (x64)..."
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -o publish/osx-x64/

# Function to create distribution package
create_distribution() {
    local platform=$1
    local executable_name=$2
    local dist_name=$3
    
    echo "Creating $platform distribution structure..."
    DIST_DIR="WebLynx-$platform-dist"
    rm -rf "$DIST_DIR"
    mkdir -p "$DIST_DIR"

    # Copy main executable and configuration
    echo "Copying main application files..."
    cp "publish/$platform/$executable_name" "$DIST_DIR/"
    cp appsettings.json "$DIST_DIR/"
    
    # Copy VERSION.txt if it exists
    if [ -f "VERSION.txt" ]; then
        echo "Copying VERSION.txt..."
        cp VERSION.txt "$DIST_DIR/"
    fi

    # Copy documentation
    echo "Copying documentation..."
    cp README.md "$DIST_DIR/"

    # Copy Views directory (web interface templates and assets)
    echo "Copying Views directory..."
    cp -r Views "$DIST_DIR/"

    # Note: etc directory contains example files and is not needed for distribution
    # Note: log directory will be created by the application at runtime if needed

    # Create a configuration guide
    echo "Creating configuration guide..."
    cat > "$DIST_DIR/CONFIGURATION.md" << 'EOF'
# WebLynx Configuration Guide

## Quick Start

appsettings.json is used to configure the application.

### TCP Settings
- `TimingPort`: Port for timing data from FinishLynx (default: 8080)
- `ResultsPort`: Port for results data from FinishLynx (default: 8081)
- `BufferSize`: TCP buffer size (default: 4096)

### HTTP Settings
- `Port`: Web interface port (default: 5001)

### Broadcast Settings
- `MeetTitle`: Title displayed on broadcast overlays
- `EventSubtitle`: Subtitle for the event
- `UnofficialResultsText`: Text for unofficial results

### Lap Counter Settings
- `DelayedDisplaySeconds`: Delay before showing lap times (default: 3)
- `HalfLapModeEnabled`: Enable half-lap counting (default: true)

### Logging Settings
- `EnableDataLogging`: Log received data to files (default: true)
- `EnableLiveRaceInfoLogging`: Log race information (default: true)

## Log Files

Log files are created in the `log/` directory:
- `received_data.YYYY-MM-DD.log`: Raw data received from FinishLynx
- `live_race_info.YYYY-MM-DD.txt`: Race information and results

## Troubleshooting

- Ensure ports 8080, 8081, and 5001 are not blocked by firewall
- Verify FinishLynx is configured to send data to the correct ports

EOF


    # Create the distribution zip file
    echo "Creating $platform distribution package..."
    cd "$DIST_DIR"
    zip -r "../$dist_name" . -x "*.DS_Store" "Thumbs.db"
    cd ..

    # Clean up temporary directory
    rm -rf "$DIST_DIR"
}

# Create Windows distribution
create_distribution "win-x64" "WebLynx.exe" "WebLynx-win-x64.zip"

# Create macOS distribution
create_distribution "osx-x64" "WebLynx" "WebLynx-macos-x64.zip"

echo ""
echo "Distribution build complete!"
echo "Distribution packages created:"
echo "  - WebLynx-win-x64.zip (Windows)"
echo "  - WebLynx-macos-x64.zip (macOS)"
echo ""
echo "Each package includes:"
echo "  - Main application executable"
echo "  - appsettings.json (configuration)"
echo "  - README.md (documentation)"
echo "  - CONFIGURATION.md (configuration guide)"
echo "  - Views/ (web interface templates and assets)"
echo ""
echo "To distribute:"
echo "  1. Share the appropriate zip file for the target platform"
echo "  2. Recipients can extract and run the executable directly"
