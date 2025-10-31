using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebLynx.Services;

namespace WebLynx.Controllers;

[Controller]
public class KeyValueController : Controller
{
    private readonly ILogger<KeyValueController> _logger;
    private readonly KeyValueStoreService _keyValueStore;

    public KeyValueController(ILogger<KeyValueController> logger, KeyValueStoreService keyValueStore)
    {
        _logger = logger;
        _keyValueStore = keyValueStore;
    }

    [HttpGet("key-values")]
    public IActionResult GetKeyValueForm()
    {
        try
        {
            var allValues = _keyValueStore.GetAllValues();
            
            var valuesListHtml = string.Join("\n", allValues.OrderBy(kvp => kvp.Key).Select(kvp =>
            {
                var encodedKey = System.Net.WebUtility.HtmlEncode(kvp.Key);
                var encodedValue = System.Net.WebUtility.HtmlEncode(kvp.Value);
                // Use raw values with proper HTML attribute encoding (HTML-encode is sufficient for attributes)
                var attrKey = encodedKey.Replace("\"", "&quot;").Replace("'", "&#39;");
                var attrValue = encodedValue.Replace("\"", "&quot;").Replace("'", "&#39;");
                return $@"            <tr class=""key-value-row"" data-key=""{attrKey}"" data-value=""{attrValue}"">
                <td><strong>{encodedKey}</strong></td>
                <td>{encodedValue}</td>
            </tr>";
            }));

            var html = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Key-Value Store</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            max-width: 900px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f5f5f5;
        }}
        .container {{
            background-color: white;
            padding: 30px;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        h1 {{
            color: #333;
            text-align: center;
            margin-bottom: 30px;
        }}
        .form-section {{
            margin-bottom: 40px;
            padding: 20px;
            background-color: #f9f9f9;
            border-radius: 5px;
        }}
        .form-group {{
            margin-bottom: 15px;
        }}
        label {{
            display: block;
            margin-bottom: 5px;
            font-weight: bold;
            color: #555;
        }}
        input[type=""text""] {{
            width: 100%;
            padding: 10px;
            border: 1px solid #ddd;
            border-radius: 4px;
            font-size: 14px;
            box-sizing: border-box;
        }}
        button {{
            background-color: #007bff;
            color: white;
            padding: 10px 20px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-size: 14px;
        }}
        button:hover {{
            background-color: #0056b3;
        }}
        .values-section {{
            margin-top: 30px;
        }}
        .values-section h2 {{
            color: #333;
            margin-bottom: 15px;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin-top: 10px;
        }}
        th {{
            background-color: #007bff;
            color: white;
            padding: 12px;
            text-align: left;
            border: 1px solid #ddd;
        }}
        td {{
            padding: 10px;
            border: 1px solid #ddd;
            background-color: white;
        }}
        tr:nth-child(even) td {{
            background-color: #f9f9f9;
        }}
        .key-value-row {{
            cursor: pointer;
            transition: background-color 0.2s ease;
        }}
        .key-value-row:hover {{
            background-color: #e3f2fd !important;
        }}
        .key-value-row:hover td {{
            background-color: #e3f2fd !important;
        }}
        .no-values {{
            text-align: center;
            color: #666;
            font-style: italic;
            padding: 20px;
        }}
        .info-text {{
            color: #666;
            font-size: 13px;
            margin-top: 5px;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <h1>Key-Value Store</h1>
        
        <div class=""form-section"">
            <h2>Set Key-Value Pair</h2>
            <form method=""POST"" action=""/key-values"">
                <div class=""form-group"">
                    <label for=""key"">Key:</label>
                    <input type=""text"" id=""key"" name=""key"" required placeholder=""Enter key name"">
                </div>
                <div class=""form-group"">
                    <label for=""value"">Value:</label>
                    <input type=""text"" id=""value"" name=""value"" placeholder=""Enter value (leave empty to remove key)"">
                    <div class=""info-text"">Leave value empty to remove the key</div>
                </div>
                <button type=""submit"">Set Value</button>
            </form>
        </div>

        <div class=""values-section"">
            <h2>Current Stored Values</h2>
            <div class=""info-text"" style=""margin-bottom: 10px;"">Click on a row to edit it</div>
            {(allValues.Any() 
                ? $@"<table>
                <thead>
                    <tr>
                        <th>Key</th>
                        <th>Value</th>
                    </tr>
                </thead>
                <tbody>
                    {valuesListHtml}
                </tbody>
            </table>" 
                : @"<div class=""no-values"">No values stored yet. Use the form above to add key-value pairs.</div>")}
        </div>
    </div>
    
    <script>
        document.addEventListener('DOMContentLoaded', function() {{
            const rows = document.querySelectorAll('.key-value-row');
            const keyInput = document.getElementById('key');
            const valueInput = document.getElementById('value');
            
            rows.forEach(row => {{
                row.addEventListener('click', function() {{
                    // Get attribute values directly (they're already HTML-decoded by the browser)
                    const key = this.getAttribute('data-key');
                    const value = this.getAttribute('data-value');
                    
                    if (key !== null && value !== null) {{
                        keyInput.value = key;
                        valueInput.value = value;
                        
                        // Scroll to form and focus on value input
                        document.querySelector('.form-section').scrollIntoView({{ behavior: 'smooth', block: 'nearest' }});
                        valueInput.focus();
                        valueInput.select();
                    }}
                }});
            }});
        }});
    </script>
</body>
</html>";

            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering key-value form");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("key-values")]
    public IActionResult PostKeyValue([FromForm] KeyValueRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Key))
            {
                return BadRequest("Key is required");
            }

            _keyValueStore.SetValue(request.Key, request.Value);
            
            _logger.LogInformation("Key-value pair {Action}: {Key} = {Value}", 
                string.IsNullOrWhiteSpace(request.Value) ? "removed" : "set", 
                request.Key, 
                request.Value ?? "(empty)");

            // Redirect back to the form after POST
            return RedirectToAction("GetKeyValueForm");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting key-value pair");
            return StatusCode(500, "Internal server error");
        }
    }

    public class KeyValueRequest
    {
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}

