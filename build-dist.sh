#!/bin/bash

# WebLynx Windows Distribution Build Script
# This script creates a complete Windows distributable package

set -e

echo "Building WebLynx Windows Distribution..."

# Clean previous builds
echo "Cleaning previous builds..."
rm -rf bin/ publish/ WebLynx-windows-x64.zip

# Build for Windows
echo "Building for Windows (x64)..."
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/win-x64/

# Create distribution directory structure
echo "Creating distribution structure..."
DIST_DIR="WebLynx-windows-dist"
rm -rf "$DIST_DIR"
mkdir -p "$DIST_DIR"

# Copy main executable and configuration
echo "Copying main application files..."
cp publish/win-x64/WebLynx.exe "$DIST_DIR/"
cp appsettings.json "$DIST_DIR/"

# Copy documentation
echo "Copying documentation..."
cp README.md "$DIST_DIR/"

# Copy Views directory (web interface templates and assets)
echo "Copying Views directory..."
cp -r Views "$DIST_DIR/"

# Note: etc directory contains example files and is not needed for distribution
# Note: log directory will be created by the application at runtime if needed

# Create a Windows batch file for easy execution
echo "Creating Windows batch file..."
cat > "$DIST_DIR/run-weblynx.bat" << 'EOF'
@echo off
echo Starting WebLynx...
echo.
echo WebLynx will start the web interface on port 5001
echo Open your browser to http://localhost:5001 to access the interface
echo.
echo Press Ctrl+C to stop the application
echo.
WebLynx.exe
pause
EOF

# Create a Windows PowerShell script for advanced users
echo "Creating PowerShell script..."
cat > "$DIST_DIR/run-weblynx.ps1" << 'EOF'
# WebLynx PowerShell Launcher
Write-Host "Starting WebLynx..." -ForegroundColor Green
Write-Host ""
Write-Host "WebLynx will start the web interface on port 5001" -ForegroundColor Yellow
Write-Host "Open your browser to http://localhost:5001 to access the interface" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press Ctrl+C to stop the application" -ForegroundColor Cyan
Write-Host ""

try {
    & .\WebLynx.exe
} catch {
    Write-Host "Error running WebLynx: $_" -ForegroundColor Red
    Read-Host "Press Enter to exit"
}
EOF

# Create a configuration guide
echo "Creating configuration guide..."
cat > "$DIST_DIR/CONFIGURATION.md" << 'EOF'
# WebLynx Configuration Guide

## Quick Start

1. Double-click `run-weblynx.bat` to start the application
2. Open your browser to http://localhost:5001
3. Configure FinishLynx to send data to port 8080 (timing) and 8081 (results)

## Configuration

Edit `appsettings.json` to customize the application:

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
- Check log files for error messages
- Verify FinishLynx is configured to send data to the correct ports

## Support

For issues or questions, refer to the README.md file or contact support.
EOF

# Create the distribution zip file
echo "Creating distribution package..."
cd "$DIST_DIR"
zip -r ../WebLynx-windows-x64.zip . -x "*.DS_Store" "Thumbs.db"
cd ..

# Clean up temporary directory
rm -rf "$DIST_DIR"

echo ""
echo "Windows distribution build complete!"
echo "Distribution package created: WebLynx-windows-x64.zip"
echo ""
echo "The package includes:"
echo "  - WebLynx.exe (main application)"
echo "  - appsettings.json (configuration)"
echo "  - README.md (documentation)"
echo "  - CONFIGURATION.md (configuration guide)"
echo "  - run-weblynx.bat (easy launcher)"
echo "  - run-weblynx.ps1 (PowerShell launcher)"
echo "  - Views/ (web interface templates and assets)"
echo ""
echo "To distribute:"
echo "  1. Share the WebLynx-windows-x64.zip file"
echo "  2. Recipients can extract and run run-weblynx.bat"
