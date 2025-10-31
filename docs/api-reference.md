# Live Race Data API

WebLynx provides a comprehensive REST API for fetching live race data from FinishLynx timing systems. This API is designed to provide real-time race information to web-based views and external applications.

## Base URL

All API endpoints are available at:
```
http://localhost:5001/api/race/
```

The default port (5001) can be configured in `appsettings.json` under `HttpSettings.Port`.

## Authentication

Currently, no authentication is required for API access. All endpoints are publicly accessible.

## Endpoints

### Get Current Race Data (Complete)

**Endpoint:** `GET /api/race/race-data`

**Description:** Returns complete race data including all racers, event information, and race status.

**Query Parameters:**
- `sortBy` (optional): Sort racers by `"place"` or `"lane"` (default: `"place"`)

**Response:**
```json
{
  "currentTime": "00:01:23.4567890",
  "event": {
    "eventName": "Men's 1000m",
    "eventNumber": "Event 1"
  },
  "status": "Running",
  "lastUpdated": "2024-01-15T10:30:45.123Z",
  "announcementMessage": "Race in progress",
  "halfLapModeEnabled": true,
  "keyValues": {
    "customKey1": "customValue1",
    "customKey2": "customValue2"
  },
  "racers": [
    {
      "lane": 1,
      "id": "RACER001",
      "name": "John Smith",
      "affiliation": "Team Alpha",
      "placeText": "1st",
      "hasPlaceData": true,
      "reactionTime": "00:00:00.1234567",
      "cumulativeSplitTime": "00:01:20.1234567",
      "lastSplitTime": "00:00:15.1234567",
      "bestSplitTime": "00:00:14.9876543",
      "lapsRemaining": 8.5,
      "delayedLapsRemaining": 8.0,
      "lapCountLastChanged": "2024-01-15T10:30:30.123Z",
      "speed": 45.2,
      "pace": "00:01:20.1234567",
      "finalTime": null,
      "deltaTime": "00:00:02.1234567",
      "hasFinished": false,
      "hasFirstCrossing": true
    }
  ]
}
```

### Get Current Race (Basic)

**Endpoint:** `GET /api/race/current`

**Description:** Returns the complete `RaceData` object with all internal properties.

**Response:** Full `RaceData` object (see Models section for structure)

### Test Announcement

**Endpoint:** `POST /api/race/test-announcement`

**Description:** Sets a test announcement message for testing purposes.

**Request Body:**
```json
{
  "message": "Test announcement message"
}
```

**Response:** Success confirmation

## Key-Value Store API

WebLynx provides a simple key-value store for storing arbitrary values that can be accessed by views via the JSON API. This is useful for custom data that needs to be displayed alongside race information.

### Key-Value Store Management

**Endpoint:** `GET /key-values`

**Description:** Displays a web form for managing key-value pairs. Shows all currently stored values in a table and provides a form to add, update, or remove entries.

**Response:** HTML page with:
- Form to set key-value pairs
- Table listing all current stored values
- Empty value removes the key

### Set Key-Value Pair

**Endpoint:** `POST /key-values`

**Description:** Sets or removes a key-value pair. If the value is empty, the key is removed.

**Request Body:** (Form-encoded)
```
key=exampleKey
value=exampleValue
```

**Parameters:**
- `key` (required): The key name
- `value` (optional): The value to store. If empty or omitted, the key is removed.

**Response:** Redirects to `/key-values` (GET) after processing

### Accessing Key-Values in Views

Key-value pairs are automatically included in the race data API response under the `keyValues` field:

```javascript
fetch('/api/race/race-data')
  .then(response => response.json())
  .then(data => {
    // Access stored key-values
    const customValue = data.keyValues['exampleKey'];
    console.log('Custom value:', customValue);
  });
```

## Data Models

### RaceDataApiResponse
```typescript
interface RaceDataApiResponse {
  currentTime: string;           // TimeSpan format: "HH:mm:ss.fffffff"
  event: RaceEvent | null;
  status: string;               // "NotStarted", "Running", "Paused", "Finished"
  lastUpdated: string;         // ISO 8601 timestamp
  announcementMessage: string | null;
  halfLapModeEnabled: boolean;
  keyValues: { [key: string]: string };  // Arbitrary key-value pairs
  racers: RacerApiResponse[];
}
```

### RacerApiResponse
```typescript
interface RacerApiResponse {
  lane: number;                 // Lane number (1-10, 0 = not racing)
  id: string;                   // Unique racer identifier
  name: string;                 // Racer name
  affiliation: string;           // Team/club affiliation
  placeText: string;            // Current place ("1st", "2nd", etc.)
  hasPlaceData: boolean;        // Whether place data is available
  reactionTime: string | null;  // Reaction time (TimeSpan format)
  cumulativeSplitTime: string | null;  // Total time elapsed
  lastSplitTime: string | null; // Time for most recent lap
  bestSplitTime: string | null; // Best lap time
  lapsRemaining: number | null; // Laps remaining (can be decimal for half-laps)
  delayedLapsRemaining: number | null; // Laps remaining with delay applied
  lapCountLastChanged: string | null;   // When lap count last changed
  speed: number | null;         // Current speed (km/h)
  pace: string | null;         // Current pace (TimeSpan format)
  finalTime: string | null;    // Final race time (if finished)
  deltaTime: string | null;    // Time difference from leader
  hasFinished: boolean;         // Whether racer has finished
  hasFirstCrossing: boolean;    // Whether racer has crossed finish line
}
```

### RaceEvent
```typescript
interface RaceEvent {
  eventName: string;            // Name of the event
  eventNumber: string;          // Event number/identifier
}
```

## Time Format

All time values are returned in .NET TimeSpan format: `"HH:mm:ss.fffffff"`

Examples:
- `"00:01:23.4567890"` - 1 minute, 23 seconds, 456.7890 milliseconds
- `"00:00:15.1234567"` - 15.1234567 seconds
- `"01:30:45.0000000"` - 1 hour, 30 minutes, 45 seconds

## Race Status Values

Race status can be returned as either string or numeric values:

**String Values:**
- `"NotStarted"` - Race has not begun
- `"Running"` - Race is currently in progress
- `"Paused"` - Race is paused
- `"Finished"` - Race has completed

**Numeric Values:**
- `0` - Not Started
- `1` - Running
- `2` - Paused
- `3` - Finished

## Half-Lap Mode

When `halfLapModeEnabled` is `true`:
- `lapsRemaining` can include decimal values (e.g., `8.5` for 8½ laps)
- `delayedLapsRemaining` applies a configurable delay before showing lap count changes
- Views should handle half-lap display formatting appropriately

## Error Handling

All endpoints return appropriate HTTP status codes:

- `200 OK` - Success
- `404 Not Found` - Resource not found (e.g., invalid lane number)
- `500 Internal Server Error` - Server error

Error responses include descriptive messages:
```json
{
  "error": "Racer not found for lane 5"
}
```

## Rate Limiting

No rate limiting is currently implemented. However, views should use reasonable update intervals (typically 250ms-2000ms) to avoid overwhelming the system.

## CORS

CORS is not currently configured. For cross-origin requests, you may need to configure CORS headers in your WebLynx deployment.

## Example Usage

### JavaScript Fetch Example
```javascript
async function fetchRaceData() {
  try {
    const response = await fetch('/api/race/race-data?sortBy=place');
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    const data = await response.json();
    
    // Update UI with race data
    updateRaceDisplay(data);
  } catch (error) {
    console.error('Error fetching race data:', error);
  }
}

// Update every 250ms
setInterval(fetchRaceData, 250);
```

### cURL Example
```bash
# Get complete race data
curl http://localhost:5001/api/race/race-data

# Get race data sorted by lane
curl http://localhost:5001/api/race/race-data?sortBy=lane

# Get raw race data
curl http://localhost:5001/api/race/current
```

## Integration Notes

- The API provides real-time data from FinishLynx timing systems
- Data is updated as soon as new timing information is received
- Views should implement proper error handling and fallback displays
