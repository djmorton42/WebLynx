# Configuration Properties for Views

WebLynx provides dynamic configuration properties that are automatically made available to all views through the template processing system. These properties are defined in the `appsettings.json` file and injected into HTML templates as JavaScript configuration objects. **No code changes or recompilation are required** when adding new properties.

## How Configuration Properties Work

### 1. Configuration Source
Configuration properties are defined in the `ViewProperties` section of `appsettings.json`:

```json
{
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
    "DefaultStrokeColor": "#ffffff"
  }
}
```

### 2. Template Processing
The `TemplateService` automatically processes all HTML templates and injects configuration properties as JavaScript objects. This happens in the `ApplyCommonReplacements` method:

- Configuration placeholders like `{{MEET_TITLE}}`, `{{EVENT_SUBTITLE}}`, and `{{UNOFFICIAL_RESULTS_TEXT}}` are replaced with values from `BroadcastSettings`
- A `{{VIEW_PROPERTIES}}` placeholder is replaced with a JavaScript configuration object
- The JavaScript object is accessible as `VIEW_CONFIG` in your view's JavaScript code

### 3. JavaScript Configuration Object
The injected configuration creates a `VIEW_CONFIG` object that dynamically includes **all** properties from the `ViewProperties` section. Property names are automatically converted to camelCase for JavaScript compatibility:

```javascript
const VIEW_CONFIG = {
  laneColors: {
    "1": "#ffff00",
    "2": "#000000",
    // ... etc
  },
  laneStrokeColors: {
    "1": "#000000",
    "2": "#ffffff",
    // ... etc
  },
  updateInterval: 250,
  defaultLaneColor: "#333333",
  defaultStrokeColor: "#ffffff"
  // Any additional properties you add will automatically appear here
};
```

## Available Configuration Properties

### Lane Colors
- **LaneColors**: Dictionary mapping lane numbers (1-10) to hex color codes
- **LaneStrokeColors**: Dictionary mapping lane numbers (1-10) to stroke/border color codes
- **DefaultLaneColor**: Fallback color for lanes not specified in LaneColors
- **DefaultStrokeColor**: Fallback stroke color for lanes not specified in LaneStrokeColors

### Update Settings
- **UpdateInterval**: Milliseconds between API calls for live data updates (default: 250ms)

## Using Configuration Properties in Views

### Accessing Properties
Use the `VIEW_CONFIG` object in your JavaScript code:

```javascript
// Get lane color for a specific lane
function getLaneColor(lane) {
  return VIEW_CONFIG.laneColors?.[lane] || VIEW_CONFIG.defaultLaneColor;
}

// Get stroke color for a specific lane
function getStrokeColor(lane) {
  return VIEW_CONFIG.laneStrokeColors?.[lane] || VIEW_CONFIG.defaultStrokeColor;
}

// Use update interval for API calls
const UPDATE_INTERVAL = VIEW_CONFIG.updateInterval;
setInterval(updateRaceData, UPDATE_INTERVAL);
```

### Template Placeholders
Use these placeholders in your HTML templates for static configuration values:

- `{{MEET_TITLE}}` - Replaced with `BroadcastSettings.MeetTitle`
- `{{EVENT_SUBTITLE}}` - Replaced with `BroadcastSettings.EventSubtitle`
- `{{UNOFFICIAL_RESULTS_TEXT}}` - Replaced with `BroadcastSettings.UnofficialResultsText`
- `{{VIEW_PROPERTIES}}` - Replaced with the JavaScript configuration object

## Example Usage

Here's how a typical view template uses configuration properties:

```html
<!DOCTYPE html>
<html>
  <head>
    <link rel="stylesheet" href="/views/my_view/styles.css">
    {{VIEW_PROPERTIES}}
  </head>
  <body>
    <div class="meet-title">{{MEET_TITLE}}</div>
    <div class="event-subtitle">{{EVENT_SUBTITLE}}</div>
    
    <script>
      // Use configuration properties
      const UPDATE_INTERVAL = VIEW_CONFIG.updateInterval;
      
      function getLaneColor(lane) {
        return VIEW_CONFIG.laneColors?.[lane] || VIEW_CONFIG.defaultLaneColor;
      }
      
      function updateRaceData() {
        fetch('/api/race/race-data')
          .then(response => response.json())
          .then(data => {
            // Update UI with race data
            data.racers.forEach(racer => {
              const element = document.getElementById(`racer-${racer.lane}`);
              element.style.backgroundColor = getLaneColor(racer.lane);
            });
          });
      }
      
      // Start updating
      updateRaceData();
      setInterval(updateRaceData, UPDATE_INTERVAL);
    </script>
  </body>
</html>
```

## Adding New Configuration Properties

### Steps to Add New Properties

1. **Add to appsettings.json**: Simply add your property to the `ViewProperties` section
2. **Use in views**: Access the property via `VIEW_CONFIG.yourPropertyName` (automatically converted to camelCase)

### Example: Adding New Properties

Add new properties to your `appsettings.json`:

```json
{
  "ViewProperties": {
    "LaneColors": { /* existing */ },
    "UpdateInterval": 250,
    "DefaultLaneColor": "#333333",
    "DefaultStrokeColor": "#ffffff",
    "NewCustomProperty": "Hello World",
    "MaxLanes": 10,
    "EnableAnimations": true,
    "CustomColors": {
      "primary": "#ff0000",
      "secondary": "#00ff00"
    }
  }
}
```

These properties will automatically be available in your views as:

```javascript
// All properties are automatically available
console.log(VIEW_CONFIG.newCustomProperty);     // "Hello World"
console.log(VIEW_CONFIG.maxLanes);              // 10
console.log(VIEW_CONFIG.enableAnimations);      // true
console.log(VIEW_CONFIG.customColors.primary);  // "#ff0000"
```

## Best Practices

1. **Use meaningful names**: Choose descriptive property names that clearly indicate their purpose
2. **Provide defaults**: Always provide sensible default values in your configuration
3. **Type safety**: Be consistent with data types (strings, numbers, booleans, objects)