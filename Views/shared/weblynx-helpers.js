/**
 * WebLynx Shared Helper Functions
 * 
 * This library provides common utility functions for WebLynx views.
 * All functions are namespaced under the WebLynx object to avoid global namespace pollution.
 * 
 * Usage:
 *   <script src="/views/shared/weblynx-helpers.js"></script>
 *   WebLynx.formatTime(data.currentTime);
 */

window.WebLynx = window.WebLynx || {};

/**
 * Time Formatting Functions
 */

/**
 * Format TimeSpan string to "mm:ss.fff" format (for detailed times)
 * @param {string} timeSpanString - TimeSpan in "HH:mm:ss.fffffff" format
 * @returns {string} Formatted time as "mm:ss.fff"
 */
WebLynx.formatTime = function(timeSpanString) {
  if (!timeSpanString) return '00:00.000';
  
  // TimeSpan is serialized as "HH:mm:ss.fffffff" format
  // We need to parse it and format as "mm:ss.fff"
  const parts = timeSpanString.split(':');
  if (parts.length !== 3) return '00:00.000';
  
  const hours = parseInt(parts[0]) || 0;
  const minutes = parseInt(parts[1]) || 0;
  const secondsParts = parts[2].split('.');
  const seconds = parseInt(secondsParts[0]) || 0;
  
  // Handle the fractional seconds part (7 digits)
  let milliseconds = 0;
  if (secondsParts[1]) {
    const fractionalPart = secondsParts[1].padEnd(7, '0');
    milliseconds = Math.floor(parseInt(fractionalPart.substring(0, 3)) || 0);
  }
  
  const totalMinutes = hours * 60 + minutes;
  const formattedMinutes = totalMinutes.toString().padStart(2, '0');
  const formattedSeconds = seconds.toString().padStart(2, '0');
  const formattedMilliseconds = milliseconds.toString().padStart(3, '0');
  
  return `${formattedMinutes}:${formattedSeconds}.${formattedMilliseconds}`;
};

/**
 * Format TimeSpan string to "mm:ss.f" format (for race clock)
 * @param {string} timeSpanString - TimeSpan in "HH:mm:ss.fffffff" format
 * @returns {string} Formatted time as "mm:ss.f"
 */
WebLynx.formatRaceTime = function(timeSpanString) {
  if (!timeSpanString) return '00:00.0';
  
  // TimeSpan is serialized as "HH:mm:ss.fffffff" format
  // We need to parse it and format as "mm:ss.f"
  const parts = timeSpanString.split(':');
  if (parts.length !== 3) return '00:00.0';
  
  const hours = parseInt(parts[0]) || 0;
  const minutes = parseInt(parts[1]) || 0;
  const secondsParts = parts[2].split('.');
  const seconds = parseInt(secondsParts[0]) || 0;
  
  // Handle the fractional seconds part (7 digits) - take first digit for tenths
  let tenths = 0;
  if (secondsParts[1]) {
    const fractionalPart = secondsParts[1].padEnd(7, '0');
    tenths = Math.floor(parseInt(fractionalPart.substring(0, 1)) || 0);
  }
  
  const totalMinutes = hours * 60 + minutes;
  const formattedMinutes = totalMinutes.toString().padStart(2, '0');
  const formattedSeconds = seconds.toString().padStart(2, '0');
  
  return `${formattedMinutes}:${formattedSeconds}.${tenths}`;
};

/**
 * Lap Display Functions
 */

/**
 * Format laps remaining with half-lap logic
 * @param {number} lapsRemaining - Number of laps remaining
 * @param {string|number} raceStatus - Current race status
 * @param {boolean} halfLapModeEnabled - Whether half-lap mode is enabled
 * @param {boolean} hasFirstCrossing - Whether racer has completed first crossing
 * @returns {string} Formatted laps display
 */
WebLynx.formatLapsDisplay = function(lapsRemaining, raceStatus, halfLapModeEnabled, hasFirstCrossing) {
  if (lapsRemaining === null || lapsRemaining === undefined) {
    return '-';
  }
  
  let displayValue = lapsRemaining;
  
  // Apply half-lap mode display logic
  if (halfLapModeEnabled) {
    if (raceStatus === 'NotStarted' || raceStatus === 0) {
      // Before race starts, show the actual value (including half laps)
      displayValue = lapsRemaining;
    } else {
      // After race starts, check if this is a half-lap race
      if (lapsRemaining % 1 === 0.5) {
        // Half-lap race: show the whole number part (visually remove the 1/2)
        displayValue = Math.floor(lapsRemaining);
      } else {
        // Whole lap race: always subtract 1 after race starts
        displayValue = lapsRemaining - 1;
        
        // If the value would be negative, show finished text
        if (displayValue < 0) {
          return VIEW_CONFIG.finishedText || '-';
        }
      }
    }
  } else {
    // Half-lap mode disabled: always subtract 1 from display value
    displayValue = lapsRemaining - 1;
    
    // If the value would be negative, show finished text
    if (displayValue < 0) {
      return VIEW_CONFIG.finishedText || '-';
    }
  }
  
  // If race has started (Running, Paused, or Finished), only show whole numbers
  if (raceStatus === 'Running' || raceStatus === 'Paused' || raceStatus === 'Finished' ||
      raceStatus === 1 || raceStatus === 2 || raceStatus === 3) {
    return Math.floor(displayValue).toString();
  }
  
  // Before race starts, show decimal values including half laps
  if (displayValue % 1 === 0.5) {
    // Show as "13 1/2" format for half laps
    const wholePart = Math.floor(displayValue);
    return wholePart + ' 1/2';
  }
  
  // Show as decimal for other cases
  return displayValue.toString();
};

/**
 * Format laps remaining with HTML for half-lap fractions
 * @param {number} lapsRemaining - Number of laps remaining
 * @param {string|number} raceStatus - Current race status
 * @param {boolean} halfLapModeEnabled - Whether half-lap mode is enabled
 * @param {boolean} hasFirstCrossing - Whether racer has completed first crossing
 * @returns {string} Formatted laps display with HTML
 */
WebLynx.formatLapsDisplayHTML = function(lapsRemaining, raceStatus, halfLapModeEnabled, hasFirstCrossing) {
  if (lapsRemaining === null || lapsRemaining === undefined) {
    return '-';
  }
  
  
  // Apply half-lap mode display logic
  let displayValue = lapsRemaining;
  
  if (halfLapModeEnabled) {
    if (raceStatus === 'NotStarted' || raceStatus === 0) {
      // Before race starts, show the actual value (including half laps)
      displayValue = lapsRemaining;
    } else {
      // After race starts, check if this is a half-lap race
      if (lapsRemaining % 1 === 0.5) {
        // Half-lap race: show the whole number part (visually remove the 1/2)
        displayValue = Math.floor(lapsRemaining);
      } else {
        // Whole lap race: always subtract 1 after race starts
        displayValue = lapsRemaining - 1;
        
        // If the value would be negative, show finished text
        if (displayValue < 0) {
          return VIEW_CONFIG.finishedText || '-';
        }
      }
    }
  } else {
    // Half-lap mode disabled: always subtract 1 from display value
    displayValue = lapsRemaining - 1;
    
    // If the value would be negative, show finished text
    if (displayValue < 0) {
      return VIEW_CONFIG.finishedText || '-';
    }
  }
  
  // If race has started (Running, Paused, or Finished), only show whole numbers
  if (raceStatus === 'Running' || raceStatus === 'Paused' || raceStatus === 'Finished' ||
      raceStatus === 1 || raceStatus === 2 || raceStatus === 3) {
    return Math.floor(displayValue).toString();
  }
  
  // Before race starts, show decimal values including half laps
  if (displayValue % 1 === 0.5) {
    // Show as "13" with small "1/2" for half laps
    const wholePart = Math.floor(displayValue);
    return `${wholePart}<span class="half-lap-fraction">1/2</span>`;
  }
  
  // Show as decimal for other cases
  return displayValue.toString();
};

/**
 * Race Data Functions
 */

/**
 * Fetch race data with error handling
 * @param {string} sortBy - Sort order ('place' or 'lane')
 * @returns {Promise} Promise that resolves to race data
 */
WebLynx.fetchRaceData = function(sortBy = 'place') {
  return fetch(`/api/race/race-data?sortBy=${sortBy}`)
    .then(response => {
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      return response.json();
    });
};

/**
 * Update race data with automatic error handling and callback
 * @param {Function} callback - Function to call with race data
 * @param {string} sortBy - Sort order ('place' or 'lane')
 */
WebLynx.updateRaceData = function(callback, sortBy = 'place') {
  WebLynx.fetchRaceData(sortBy)
    .then(data => {
      callback(data);
    })
    .catch(error => {
      console.error('Error fetching race data:', error);
      // Call callback with null to indicate error
      callback(null, error);
    });
};

/**
 * Status Helper Functions
 */

/**
 * Get status display information (text and CSS class)
 * @param {string|number} raceStatus - Current race status
 * @returns {Object} Object with text and class properties
 */
WebLynx.getStatusInfo = function(raceStatus) {
  const statusMap = {
    'NotStarted': { text: 'Ready', class: 'notstarted' },
    'Running': { text: 'Running', class: 'running' },
    'Paused': { text: 'Paused', class: 'paused' },
    'Finished': { text: 'Finished', class: 'finished' },
    '0': { text: 'Ready', class: 'notstarted' },
    '1': { text: 'Running', class: 'running' },
    '2': { text: 'Paused', class: 'paused' },
    '3': { text: 'Finished', class: 'finished' }
  };
  
  return statusMap[raceStatus] || { text: raceStatus || 'Unknown', class: 'notstarted' };
};

/**
 * Check if race status indicates running state
 * @param {string|number} raceStatus - Current race status
 * @returns {boolean} True if race is running
 */
WebLynx.isRaceRunning = function(raceStatus) {
  return raceStatus === 'Running' || raceStatus === 1;
};

/**
 * Utility Functions
 */

/**
 * Check if place text is an alpha code (DNF, DNS, DSQ, etc.)
 * @param {string} placeText - Place text to check
 * @returns {boolean} True if it's an alpha code
 */
WebLynx.isAlphaCode = function(placeText) {
  // Check if the place text is an alpha code (DNF, DNS, DSQ, etc.)
  // rather than a numeric position
  if (!placeText || placeText.trim() === '') {
    return false;
  }
  
  // Try to parse as integer - if it fails, it's likely an alpha code
  return isNaN(parseInt(placeText.trim()));
};

/**
 * Filter active racers (lane > 0)
 * @param {Array} racers - Array of racer objects
 * @returns {Array} Array of active racers
 */
WebLynx.getActiveRacers = function(racers) {
  return racers.filter(racer => racer.lane > 0);
};

/**
 * Initialize automatic race data updates
 * @param {Function} updateFunction - Function to call for updates
 * @param {number} interval - Update interval in milliseconds
 * @param {string} sortBy - Sort order ('place' or 'lane')
 */
WebLynx.startAutoUpdate = function(updateFunction, interval = 2000, sortBy = 'place') {
  // Initial load
  WebLynx.updateRaceData(updateFunction, sortBy);
  
  // Set up interval
  return setInterval(() => {
    WebLynx.updateRaceData(updateFunction, sortBy);
  }, interval);
};
