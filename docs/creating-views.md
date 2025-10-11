# Creating Custom Views

WebLynx uses a flexible view system that allows you to create custom HTML5-based displays for race data. Views are automatically discovered from the `Views` directory and can be accessed via HTTP endpoints.

## View System Overview

Views are HTML5 templates that:
- Are automatically discovered from the `Views` directory
- Have access to live race data via JavaScript API calls
- Can use static configuration properties from `appsettings.json`
- Support CSS styling and static assets (images, fonts, etc.)
- Are processed server-side to inject configuration values

## View Directory Structure

Each view must be in its own subdirectory under `Views/`:

```
Views/
├── my_custom_view/
│   ├── template.html      # Required: Main HTML template
│   ├── styles.css         # Optional: CSS styles
│   ├── description.txt    # Optional: View description
│   └── assets/            # Optional: Images, fonts, etc.
│       ├── logo.png
│       └── background.jpg
└── another_view/
    ├── template.html
    └── styles.css
```

## Required Files

### template.html
The main HTML template file. This is the only required file for a view.

**Minimum template structure:**
```html
<!DOCTYPE html>
<html>
  <head>
    <title>My Custom View</title>
    <link rel="stylesheet" href="/views/my_custom_view/styles.css">
    {{VIEW_PROPERTIES}}
  </head>
  <body>
    <div id="race-content">
      <!-- Your view content here -->
    </div>
    
    <script>
      // Your JavaScript code here
    </script>
  </body>
</html>
```

## Optional Files

### styles.css
CSS styles for your view. Referenced in the template with:
```html
<link rel="stylesheet" href="/views/my_custom_view/styles.css">
```

### description.txt
A brief description of what the view displays. Used in the views index page.

### Static Assets
Any images, fonts, or other static files. Access them via:
```html
<img src="/views/my_custom_view/assets/logo.png" alt="Logo">
```

## View Discovery Process

WebLynx automatically discovers views on startup:

1. **Scan Views Directory**: Looks for subdirectories in the `Views` folder
2. **Validate Structure**: Checks for required `template.html` file
3. **Register Views**: Makes valid views available at `/views/{viewName}`
4. **Log Results**: Reports discovered views in the application log

### View Validation
A view is considered valid if:
- It has a `template.html` file
- The directory name doesn't start with a dot (hidden directories)

## Accessing Views

Once discovered, views are accessible at:
```
http://localhost:5001/views/{viewName}
```

Examples:
- `http://localhost:5001/views/my_custom_view`
- `http://localhost:5001/views/lap_counter`
- `http://localhost:5001/views/broadcast_overlay`

## View Index Page

All available views are listed at:
```
http://localhost:5001/views
```

This page shows:
- View names and descriptions
- Direct links to each view
- Validation status

## Template Processing

Templates are processed server-side before being sent to the browser:

### Configuration Placeholders
These placeholders are replaced with values from `appsettings.json`:

- `{{MEET_TITLE}}` → `BroadcastSettings.MeetTitle`
- `{{EVENT_SUBTITLE}}` → `BroadcastSettings.EventSubtitle`
- `{{UNOFFICIAL_RESULTS_TEXT}}` → `BroadcastSettings.UnofficialResultsText`
- `{{VIEW_PROPERTIES}}` → JavaScript configuration object

### JavaScript Configuration
The `{{VIEW_PROPERTIES}}` placeholder is replaced with:
```javascript
<script>
  const VIEW_CONFIG = {
    LANE_COLORS: { /* lane color mappings */ },
    LANE_STROKE_COLORS: { /* stroke color mappings */ },
    UPDATE_INTERVAL: 250,
    DEFAULT_LANE_COLOR: "#333333",
    DEFAULT_STROKE_COLOR: "#ffffff"
  };
</script>
```

## Live Data Integration

## Shared Helper Functions

WebLynx provides a shared JavaScript library with common utility functions to reduce code duplication and ensure consistent behavior across views.

### Including the Helper Library

Add this script tag to your view's HTML head section:

```html
<script src="/views/shared/weblynx-helpers.js"></script>
```

### Available Helper Functions

#### Time Formatting
```javascript
// Format TimeSpan to "mm:ss.fff" (detailed times)
WebLynx.formatTime(data.currentTime)

// Format TimeSpan to "mm:ss.f" (race clock)
WebLynx.formatRaceTime(data.currentTime)
```

#### Lap Display
```javascript
// Format laps with half-lap logic
WebLynx.formatLapsDisplay(lapsRemaining, raceStatus, halfLapModeEnabled)

// Format laps with HTML for half-lap fractions
WebLynx.formatLapsDisplayHTML(lapsRemaining, raceStatus, halfLapModeEnabled)
```

#### Race Data Management
```javascript
// Fetch race data with error handling
WebLynx.fetchRaceData('place') // or 'lane'

// Update race data with callback
WebLynx.updateRaceData((data, error) => {
  if (error) {
    console.error('Error:', error);
    return;
  }
  // Update your view with data
}, 'place');

// Start automatic updates
WebLynx.startAutoUpdate(updateFunction, 2000, 'place');
```

#### Status Helpers
```javascript
// Get status display info
const statusInfo = WebLynx.getStatusInfo(data.status);
// Returns: { text: 'Running', class: 'running' }

// Check if race is running
if (WebLynx.isRaceRunning(data.status)) {
  // Race is active
}
```

#### Utility Functions
```javascript
// Check if place text is alpha code (DNF, DNS, etc.)
if (WebLynx.isAlphaCode(racer.placeText)) {
  // Handle non-numeric place
}

// Filter active racers
const activeRacers = WebLynx.getActiveRacers(data.racers);
```

### Example: Using Helper Functions

```html
<!DOCTYPE html>
<html>
  <head>
    <script src="/views/shared/weblynx-helpers.js"></script>
    {{VIEW_PROPERTIES}}
  </head>
  <body>
    <div id="race-clock">00:00.0</div>
    <div id="event-name">Loading...</div>
    
    <script>
      function updateView(data, error) {
        if (error) {
          console.error('Error:', error);
          return;
        }
        
        // Use helper functions
        document.getElementById('race-clock').textContent = 
          WebLynx.formatRaceTime(data.currentTime);
        document.getElementById('event-name').textContent = 
          data.event?.eventName || 'No Event';
      }
      
      // Start automatic updates
      WebLynx.startAutoUpdate(updateView, VIEW_CONFIG.UPDATE_INTERVAL);
    </script>
  </body>
</html>
```

Views fetch live race data using JavaScript API calls:

### Basic Data Fetching
```javascript
async function fetchRaceData() {
  try {
    const response = await fetch('/api/race/race-data');
    const data = await response.json();
    
    // Update your view with the data
    updateView(data);
  } catch (error) {
    console.error('Error fetching race data:', error);
  }
}

// Update every 250ms
setInterval(fetchRaceData, VIEW_CONFIG.UPDATE_INTERVAL);
```

### Available Data
The API provides comprehensive race data including:
- Race status and timing
- Event information
- Individual racer data (times, laps, places)
- Configuration settings

See the [API Reference](api-reference.md) for complete data structure details.

## Example View Templates

### Simple Race Clock
```html
<!DOCTYPE html>
<html>
  <head>
    <title>Race Clock</title>
    <style>
      body { font-family: Arial, sans-serif; text-align: center; }
      .clock { font-size: 72px; font-weight: bold; }
    </style>
    {{VIEW_PROPERTIES}}
  </head>
  <body>
    <div class="clock" id="race-clock">00:00.0</div>
    
    <script src="/views/shared/weblynx-helpers.js"></script>
    <script>
      function updateRaceClock(data, error) {
        if (error) {
          console.error('Error:', error);
          return;
        }
        
        document.getElementById('race-clock').textContent = 
          WebLynx.formatRaceTime(data.currentTime);
      }
      
      // Start automatic updates
      WebLynx.startAutoUpdate(updateRaceClock, VIEW_CONFIG.UPDATE_INTERVAL);
    </script>
  </body>
</html>
```

### Racer List with Lane Colors
```html
<!DOCTYPE html>
<html>
  <head>
    <title>Racer List</title>
    <link rel="stylesheet" href="/views/racer_list/styles.css">
    {{VIEW_PROPERTIES}}
  </head>
  <body>
    <div class="racers-container" id="racers-container">
      <!-- Racers will be populated by JavaScript -->
    </div>
    
    <template id="racer-template">
      <div class="racer-card">
        <div class="lane-indicator"></div>
        <div class="racer-info">
          <div class="racer-name"></div>
          <div class="racer-affiliation"></div>
        </div>
        <div class="racer-time"></div>
        <div class="racer-laps"></div>
      </div>
    </template>
    
    <script>
      function getLaneColor(lane) {
        return VIEW_CONFIG.LANE_COLORS[lane] || VIEW_CONFIG.DEFAULT_LANE_COLOR;
      }
      
      function createRacerCard(racer) {
        const template = document.getElementById('racer-template');
        const clone = template.content.cloneNode(true);
        
        clone.querySelector('.lane-indicator').style.backgroundColor = 
          getLaneColor(racer.lane);
        clone.querySelector('.racer-name').textContent = racer.name || '-';
        clone.querySelector('.racer-affiliation').textContent = racer.affiliation || '-';
        clone.querySelector('.racer-time').textContent = 
          formatTime(racer.cumulativeSplitTime);
        clone.querySelector('.racer-laps').textContent = 
          racer.delayedLapsRemaining || '-';
        
        return clone;
      }
      
      function updateRaceData() {
        fetch('/api/race/race-data?sortBy=place')
          .then(response => response.json())
          .then(data => {
            const container = document.getElementById('racers-container');
            container.innerHTML = '';
            
            data.racers.forEach(racer => {
              if (racer.lane > 0) { // Only show active racers
                container.appendChild(createRacerCard(racer));
              }
            });
          })
          .catch(error => console.error('Error:', error));
      }
      
      updateRaceData();
      setInterval(updateRaceData, VIEW_CONFIG.UPDATE_INTERVAL);
    </script>
  </body>
</html>
```

## Best Practices

### Performance
- Use reasonable update intervals (250ms-2000ms)
- Implement error handling for API calls
- Use CSS transforms for animations instead of changing layout properties
- Minimize DOM manipulations

### Responsive Design
- Use CSS media queries for different screen sizes
- Design for common broadcast resolutions (1920x1080, 1280x720)
- Test on different devices and browsers

### Error Handling
```javascript
function updateRaceData() {
  fetch('/api/race/race-data')
    .then(response => {
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      return response.json();
    })
    .then(data => {
      // Update view with data
      updateView(data);
    })
    .catch(error => {
      console.error('Error fetching race data:', error);
      // Show fallback content or error message
      showErrorState();
    });
}
```

### Configuration Usage
- Use `VIEW_CONFIG` for dynamic configuration values
- Provide fallback values for missing configuration
- Document any custom configuration properties you add

### Asset Management
- Use relative paths for assets: `/views/my_view/assets/image.png`
- Optimize images for web (WebP, AVIF when possible)
- Include alt text for accessibility

## Debugging Views

### Common Issues
1. **View not appearing**: Check that `template.html` exists and is valid HTML
2. **Styling not applied**: Verify CSS file path and syntax
3. **API calls failing**: Check browser console for errors
4. **Configuration not working**: Ensure `{{VIEW_PROPERTIES}}` placeholder is present

### Development Tips
- Use browser developer tools to debug JavaScript
- Check the views index page (`/views`) to see validation status
- Monitor application logs for server-side errors
- Test with different race data scenarios

### Testing
- Test with different numbers of racers (1-10)
- Test with different race statuses (NotStarted, Running, Paused, Finished)
- Test with half-lap mode enabled/disabled
- Test error conditions (no data, API failures)

## Advanced Features

### HTML5 Templates
Use `<template>` elements for dynamic content:
```html
<template id="racer-template">
  <div class="racer-card">
    <div class="racer-name"></div>
    <div class="racer-time"></div>
  </div>
</template>

<script>
  function createRacerCard(racer) {
    const template = document.getElementById('racer-template');
    const clone = template.content.cloneNode(true);
    
    clone.querySelector('.racer-name').textContent = racer.name;
    clone.querySelector('.racer-time').textContent = racer.time;
    
    return clone;
  }
</script>
```

### CSS Grid/Flexbox Layouts
Use modern CSS for responsive layouts:
```css
.racers-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 20px;
  padding: 20px;
}

@media (max-width: 768px) {
  .racers-grid {
    grid-template-columns: 1fr;
  }
}
```

### Animation and Transitions
Add smooth animations for better user experience:
```css
.racer-card {
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.racer-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(0,0,0,0.1);
}
```

## Distribution

When creating views for distribution:

1. **Include all necessary files** in the view directory
2. **Document the view** with a clear description
3. **Test thoroughly** with different race scenarios
4. **Provide examples** of how to use the view
5. **Follow naming conventions** (use underscores, not spaces)

Views are automatically included in WebLynx distribution packages and will be available to users immediately after installation.
