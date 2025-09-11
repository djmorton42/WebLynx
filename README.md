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
    "ListenPort": 8080,
    "BufferSize": 4096
  }
}
```

- `ListenPort`: Port to listen for TCP connections from FinishLynx
- `BufferSize`: Buffer size for reading TCP data

## Building

Use the provided build script to create distribution packages:

```bash
./build.sh
```

This will create:
- `WebLynx-macos-x64.zip` - macOS executable
- `WebLynx-windows-x64.zip` - Windows executable

## Running

1. Extract the appropriate package for your platform
2. Ensure `appsettings.json` is in the same directory as the executable
3. Run the executable:
   - macOS: `./WebLynx`
   - Windows: `WebLynx.exe`

## Data Logging

Received data is logged to `data/received_data.log` with:
- Timestamp and client information
- Raw data in hex format
- ASCII interpretation (when applicable)
- Binary-safe handling of control characters

## Development

To run in development mode:

```bash
dotnet run
```

## Requirements

- .NET 8.0 SDK (for development)
- No additional dependencies required for running (self-contained)