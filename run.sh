#!/bin/bash

# WebLynx Development Run Script
# This script builds and runs the application in development mode

set -e

echo "🚀 WebLynx Development Runner"
echo "=============================="

# Clean and build
echo "📦 Building application..."
dotnet clean --verbosity quiet
dotnet build --verbosity quiet

if [ $? -eq 0 ]; then
    echo "✅ Build successful!"
    echo ""
    echo "🌐 Starting WebLynx..."
    echo "   - TCP listener will start on port 8080 (configurable in appsettings.json)"
    echo "   - Data will be logged to log/received_data.YYYY-MM-DD.log"
    echo "   - Press Ctrl+C to stop"
    echo ""
    echo "Waiting for connections from FinishLynx..."
    echo "=============================="
    
    # Run the application
    dotnet run
else
    echo "❌ Build failed!"
    exit 1
fi
