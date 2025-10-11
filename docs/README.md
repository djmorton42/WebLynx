# WebLynx Documentation

Welcome to the WebLynx documentation. WebLynx is a real-time race data display system designed to work with FinishLynx timing systems for speed skating competitions.

## Overview

WebLynx receives live timing data from FinishLynx via TCP connections and provides:
- Real-time race data via REST API
- Customizable HTML5-based views for displays
- Static configuration properties for views
- Automatic view discovery and management

## Quick Start

1. **Configure FinishLynx**: Set up FinishLynx to send data to WebLynx on ports 8080 (timing) and 8081 (results)
2. **Configure WebLynx**: Edit `appsettings.json` to match your setup
3. **Start WebLynx**: Run the application executable
4. **Access Views**: Open `http://localhost:5001/views` to see available displays

## Documentation Sections

### [Configuration Properties](configuration-properties.md)
Learn how static configuration properties from `appsettings.json` are made available to views, including:
- How the template processing system works
- Available configuration properties
- Using configuration in JavaScript
- Adding new configuration properties

### [API Reference](api-reference.md)
Complete reference for the live race data API, including:
- Available endpoints and their parameters
- Data models and response formats
- Time format specifications
- Error handling and status codes
- Example usage with JavaScript and cURL

### [Creating Views](creating-views.md)
Guide to creating custom HTML5-based views, including:
- View directory structure and required files
- Template processing and placeholders
- Live data integration with JavaScript
- Example templates and best practices
- Debugging and testing tips

## Configuration

WebLynx is configured via `appsettings.json`. Key sections include:

- **TcpSettings**: Ports for receiving FinishLynx data
- **HttpSettings**: Web interface port
- **BroadcastSettings**: Static text for displays
- **LapCounterSettings**: Lap counting behavior
- **ViewProperties**: Dynamic configuration for views

See the [Configuration Properties](configuration-properties.md) documentation for details on view-specific configuration.

## Architecture

WebLynx consists of several key components:

- **MultiPortTcpService**: Receives data from FinishLynx
- **MessageParser**: Processes incoming timing messages
- **RaceStateManager**: Maintains current race state
- **TemplateService**: Processes HTML templates
- **ViewDiscoveryService**: Discovers and validates views
- **Controllers**: Provide REST API endpoints

## Views System

Views are HTML5 templates located in the `Views` directory. Each view:
- Is automatically discovered on startup
- Has access to live race data via JavaScript API
- Can use static configuration properties
- Supports CSS styling and static assets
- Is accessible via HTTP endpoints

## API Endpoints

WebLynx provides several REST API endpoints:

- `GET /api/race/race-data` - Complete race data
- `GET /api/race/current` - Current race state
- `GET /api/race/event` - Event information
- `GET /api/race/racers` - All racers
- `GET /api/race/racers/{lane}` - Specific racer
- `GET /api/race/status` - Race status summary
- `GET /views` - View index page
- `GET /views/{viewName}` - Specific view

## Development

### Prerequisites
- .NET 9.0 SDK
- FinishLynx timing system (for live data)

### Building
```bash
# Build for current platform
dotnet build

# Build for distribution
./build-dist.sh
```

### Running
```bash
# Run directly
dotnet run

# Or use the executable
./WebLynx
```

## Troubleshooting

### Common Issues

**Views not appearing:**
- Check that `template.html` exists in the view directory
- Verify the view directory name doesn't start with a dot
- Check application logs for validation errors

**API calls failing:**
- Ensure WebLynx is running on the correct port
- Check that FinishLynx is sending data to the configured ports
- Verify firewall settings allow connections

**Configuration not working:**
- Ensure `{{VIEW_PROPERTIES}}` placeholder is in your template
- Check that properties are defined in `appsettings.json`
- Restart WebLynx after changing configuration

### Logs

WebLynx creates log files in the `log/` directory:
- `received_data.YYYY-MM-DD.log` - Raw data from FinishLynx
- `live_race_info.YYYY-MM-DD.txt` - Race information and results

## Support

For issues and questions:
1. Check the documentation sections above
2. Review application logs for error messages
3. Verify configuration settings
4. Test with the provided example views

## License

See the LICENSE file for licensing information.
