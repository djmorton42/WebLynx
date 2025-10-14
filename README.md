# WebLynx

A FinishLynx Virtual Scoreboard, written in C# .NET, for receiving live race data from FinishLynx photofinish software and displaying the race data on customizable and extensible HTML views over HTTP.

## Features

- **TCP Data Reception**: Receives Running Times and Results from FinishLynx on configurable ports (8080 and 8081 by default).
- **HTTP Race Data API**: A JSON Api to allow fetching of the state of the current race, including the race clock, racer list, split times and placement info.
- **Customizable and Extensible HTML View Engine**: Users can create arbitrary HTML views that use the race data. These will be loaded when the application starts and served to any client that requests them. In this way, users can create customized views of the race data for multiple purposes: Overlays for Broadcasting or Livestreaming, Lap Counters, Results displays, Start List displays, etc. Because each view is completely customizable, you can add white-glove branding, logs, etc. appropriate for your organization.

## FinishLynx Configuration

- Copy the 'WebLynx.lss' file from the ./etc directory to the FinishLynx directory. There should already be a large number of *.lss files in that directory.
- To integrate FinishLynx and WebLynx, you must create TWO scoreboard devices (Scoreboard->Options->New). FinishLynx can not send BOTH race clock information and live results information to the same scoreboard, so we'll configure one for clock data and one for results data.

### Running Time Scoreboard Configuration

- Name this scoreboard "WebLynx Clock"
- See the following screenshot for configuration:

![FinishLynx Scoreboard Running Time Configuration](./etc/WebLynx%20Clock%20Options.png)

- Ensure "Script" is set to "WebLynx.lss". FinishLynx loads the list of LSS files on startup, so if you have copied it to the FinishLynx directory and don't see it, try restarting FinishLynx.
- Set "Code Set" to "Unicode"
- Set "Serial Port" to "Network (connect)" and set the "Port" to "8080".
- If you are running WebLynx on the same computer as FinishLynx, set the "Ip Address" to "127.0.0.1". If you are not, set it to the IP address of the computer that is running WebLynx.
- Ensure "Running Time" is set to "Normal"
- Click "Options" and ensure that "Send results if armed" is checked. All other options should be unchecked.
- Set Offset to "0.000" (although you can change this if you like)
- Uncheck all the "Auto Break" settings.
- Ensure "Results" is set to 'Off'.

### Live Results Scoreboard Configuration

- Name this scoreboard "WebLynx Results"
- See the following two screenshots for configuration:

![FinishLynx Scoreboard Results Configuration 1](./etc/WebLynx%20Results%20Options.png)

![FinishLynx Scoreboard Results Configuration 2](./etc/WebLynx%20Results%20Options%202.png)

- Ensure "Script" is set to "WebLynx.lss"
- Set "Code Set" to "Unicode"
- Set "Serial Port" to "Network (connect)" and set the "Port" to "8081" (NOTE this is different from the port for race clock data configured above)
- If you are running WebLynx on the same computer as FinishLynx, set the "Ip Address" to "127.0.0.1". If you are not, set it to the IP address of the computer that is running WebLynx.
- Ensure "Running Time" is set to "Off"
- Ensure "Results" is set to "Auto"
- Ensure "Paging" is unchecked.
- Ensure "Time Precision" is set to "Thousandths"
- Select "Options" and ensure "Always send place", "Include first name" and "Track live results" are selected.

Press 'Ok' to save your changes.

Now, FinishLynx will connecto to WebLynx when it is running and send race clock data to it on Port 8081 and race results data to it on port 8081. WebLynx aggregates this data and makes it available in a consistent way via JSON api.

## WebLynx Configuration

WebLynx uses `appsettings.json` for configuration. There are several notable properties.

- The TcpSettings section allows the race clock (TimingPort) and race results (ResultsPort) to be changed. They are 8080 and 8081 by default. BufferSize should be left unchanged at 4096.
- The HttpSettings section controls what port WebLynx listens on to serve its customizable HTML views
- BroadcastSettings contains several properties that are made available to all views around the meet.
- LapCounterSettings contains two properties "HalfLapModeEnabled" and "DelayedDisplaySeconds".
  - When using a lap counting and timing device with FinishLynx, WebLynx will know how many finish line crossings (laps) each racer has left. When a racer crosses the finish line, the lap count display may be in their field of vision for a period of time and the laps remaining count changing right away could be confusing. As such, the JSON API contains two related properties, one to display the number of lap crossings remaining, and one that is delayed by this number of seconds to allow the racer to pass by any lap displays before it updates.
  - HalfLapModeEnabled should be left enabled. This expects the actual number of laps to be configured for each race. Some races will be a whole number of laps (4, 9, etc.) and some races will also have a half number of laps (4.5, 13.5, etc.). This allows FinishLynx to correctly calculate split times for half laps.
- LoggingSettings allows enabling or disabling of logging of the incoming data from FinishLynx, which will be stored in the ./log directory.
- Logging settings can be changed to control the detail of the logging. This should remain unchanged.
- ViewProperties is a special section. These can contain arbitrary structured JSON and all the keys will be provided to all HTML views and can be used for providing configuration properties to those views. 

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
    "MeetTitle": "Meet Title",
    "EventSubtitle": "Location - Date",
    "UnofficialResultsText": "Unofficial Results"
  },
  "LapCounterSettings": {
    "DelayedDisplaySeconds": 4,
    "HalfLapModeEnabled": true
  },
  "LoggingSettings": {
    "EnableDataLogging": true,
    "EnableLiveRaceInfoLogging": true
  },
  "ViewProperties": {
    "LaneColors": {
      "1": "#ffff00",
      "2": "#000000",
      "3": "#00ffff",
      "4": "#ff0000",
      "5": "#ffffff",
      "6": "#000080",
      "7": "#808080",
      "8": "#ffc0cb",
      "9": "#ffa500",
      "10": "#008000"
    },
    "LaneStrokeColors": {
      "1": "#000000",
      "2": "#ffffff",
      "3": "#000000",
      "4": "#ffffff",
      "5": "#000000",
      "6": "#ffffff",
      "7": "#ffffff",
      "8": "#000000",
      "9": "#000000",
      "10": "#ffffff"
    },
    "UpdateInterval": 250,
    "DefaultLaneColor": "#333333",
    "DefaultStrokeColor": "#ffffff",
    "FinishedText": "-"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}

```

## Creating Custom Views

See the `docs` directory for information on constructing customized views.

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