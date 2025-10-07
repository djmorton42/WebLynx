# WebLynx

A C# .NET application for receiving live race data from FinishLynx photofinish software and serving it via HTTP routes.

## Features

- **TCP Data Reception**: Listens on a configurable port for live race data from FinishLynx
- **Data Logging**: Safely logs received data (including binary data) to files
- **Cross-Platform**: Runs on both macOS and Windows

## Configuration

The application uses `appsettings.json` for configuration:

```json
{
  "TcpSettings": {
    "TimingPort": 8080,
    "ResultsPort": 8081,
    "BufferSize": 4096
  },
  "HttpSettings": {
    "Port": 5001
  },
  "BroadcastSettings": {
    "MeetTitle": "Speed Skating Meet",
    "EventSubtitle": "Race Event",
    "UnofficialResultsText": "Unofficial Results"
  },
  "LapCounterSettings": {
    "DelayedDisplaySeconds": 3,
    "HalfLapModeEnabled": true
  },
  "LoggingSettings": {
    "EnableDataLogging": true,
    "EnableLiveRaceInfoLogging": true
  }
}
```

- `TcpSettings.TimingPort`: Port to listen for timing data from FinishLynx
- `TcpSettings.ResultsPort`: Port to listen for results data from FinishLynx
- `TcpSettings.BufferSize`: Buffer size for reading TCP data
- `HttpSettings.Port`: Port for the HTTP web interface
- `BroadcastSettings`: Configuration for broadcast overlays and displays
- `LapCounterSettings`: Configuration for lap counter display behavior
- `LoggingSettings.EnableDataLogging`: Enable/disable writing to `log/received_data.YYYY-MM-DD.log`
- `LoggingSettings.EnableLiveRaceInfoLogging`: Enable/disable writing to `log/live_race_info.YYYY-MM-DD.txt`

## Building

### Development Build

For development builds, use the clean build script:

```bash
./build.sh
```

This will build the application for both platforms without creating distributables.

### Distribution Build

To create distributable packages:

```bash
./build-dist.sh
```

This will create:
- `WebLynx-macos-x64.zip` - macOS distributable package
- `WebLynx-win-x64.zip` - Windows distributable package

## Running

1. Extract the appropriate package for your platform
2. Ensure `appsettings.json` is in the same directory as the executable
3. Run the executable:
   - macOS: `./WebLynx`
   - Windows: `WebLynx.exe`

## Data Logging

Received data is logged to `log/received_data.YYYY-MM-DD.log` with:
- Timestamp and client information
- Raw data in hex format
- ASCII interpretation (when applicable)
- Binary-safe handling of control characters

## Development

To run in development mode:

```bash
./run.sh
```

## Requirements

- .NET 8.0 SDK (for development)
- No additional dependencies required for running (self-contained)