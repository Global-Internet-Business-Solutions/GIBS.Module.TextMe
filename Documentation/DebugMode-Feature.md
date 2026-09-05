# Debug Mode Feature

## Overview

Debug Mode is a configurable setting that controls verbose logging and diagnostic endpoint availability in the TextMe module. This feature helps developers troubleshoot webhook issues without flooding production logs with diagnostic information.

## Configuration

### Enabling Debug Mode

1. Navigate to your Oqtane site
2. Go to the TextMe module settings page
3. Scroll to the **Debug Mode** checkbox
4. Check the box to enable debug mode
5. Click **Update Settings** to save

### Setting Details

- **Setting Key**: `TwilioDebugMode`
- **Type**: Boolean (true/false)
- **Default**: `false` (disabled)
- **Scope**: Module-level setting

## Features Controlled by Debug Mode

### 1. Verbose Webhook Logging

**When Debug Mode is ENABLED:**
```
[abc12345] Webhook POST received for module 523, ContentType: application/x-www-form-urlencoded, URL: https://...
[abc12345] Webhook form keys: From, To, Body, MessageSid, Has Signature: True
[abc12345] SUCCESS - Inbound Twilio Message Persisted 8 523
```

**When Debug Mode is DISABLED:**
- Only errors are logged
- Successful webhook processing is silent
- Reduces log noise in production

### 2. Debug Endpoint Access

**Endpoint**: `POST /api/TextMe/webhook/{moduleid}/debug`

**When Debug Mode is ENABLED:**
- Returns detailed diagnostic information
- Includes request headers, form data, validation details
- Useful for troubleshooting signature validation issues

**When Debug Mode is DISABLED:**
- Returns `404 Not Found`
- Prevents unauthorized diagnostic data exposure
- Security warning logged: `"Debug endpoint access denied - debug mode disabled for module {ModuleId}"`

### 3. Log Content

Debug Mode affects logging in the main webhook endpoint (`/api/TextMe/webhook/{moduleid}`):

| Event | Debug Mode ON | Debug Mode OFF |
|-------|---------------|----------------|
| Incoming Request | ✅ Logged with details | ⚠️ Silent |
| Form Data Received | ✅ Keys and signature status | ⚠️ Silent |
| Signature Validation Failed | ✅ Logged with details | ✅ Logged (always) |
| Message Persisted | ✅ Logged with message ID | ⚠️ Silent |
| General Errors | ✅ Logged | ✅ Logged (always) |

## Production Recommendations

### Development/Testing
✅ **Enable Debug Mode** when:
- Setting up Twilio webhooks for the first time
- Troubleshooting error 11200 or signature validation failures
- Verifying webhook configuration after deployment
- Investigating message delivery issues

### Production
⚠️ **Disable Debug Mode** because:
- Reduces log volume and storage costs
- Protects sensitive request data from being logged
- Improves performance (fewer log writes)
- Errors are still logged for critical issues

## Technical Implementation

### Server-Side Code

**Controller Method** (`TextMeController.cs`):
```csharp
private bool IsDebugModeEnabled(int moduleid)
{
	var settings = _settingRepository.GetSettings(EntityNames.Module, moduleid).ToList();
	var debugModeSetting = settings.FirstOrDefault(s => s.SettingName == "TwilioDebugMode");
	return debugModeSetting != null && bool.TryParse(debugModeSetting.SettingValue, out var enabled) && enabled;
}
```

**Debug Endpoint Guard**:
```csharp
[HttpPost("webhook/{moduleid:int}/debug")]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
public async Task<IActionResult> WebhookDebug(int moduleid)
{
	if (!IsDebugModeEnabled(moduleid))
	{
		_logger.Log(LogLevel.Warning, this, LogFunction.Security, 
			"Debug endpoint access denied - debug mode disabled for module {ModuleId}", moduleid);
		return NotFound();
	}
	// ... debug logic ...
}
```

**Conditional Logging**:
```csharp
if (isDebugMode)
{
	_logger.Log(LogLevel.Information, this, LogFunction.Read, 
		"[{RequestId}] Webhook POST received for module {ModuleId}, ContentType: {ContentType}", 
		requestId, moduleid, Request.ContentType);
}
```

### Client-Side Code

**Settings UI** (`Settings.razor`):
```razor
<div class="row mb-1 align-items-center">
	<Label Class="col-sm-3" For="debugmode" 
		   HelpText="Enable debug endpoint and verbose logging for troubleshooting" 
		   ResourceType="@resourceType">Debug Mode:</Label>
	<div class="col-sm-9">
		<input id="debugmode" type="checkbox" class="form-check-input" @bind="@_debugMode" />
		<small class="form-text text-muted d-block mt-1">
			When enabled: Debug endpoint available at /api/TextMe/webhook/{moduleId}/debug and verbose logging active
		</small>
	</div>
</div>
```

**Setting Persistence**:
```csharp
// Load
if (!bool.TryParse(SettingService.GetSetting(settings, DebugModeSettingName, bool.FalseString), out _debugMode))
{
	_debugMode = false;
}

// Save
settings = SettingService.SetSetting(settings, DebugModeSettingName, _debugMode.ToString());
```

## Security Considerations

### Why Debug Endpoint is Gated

The debug endpoint exposes:
- Request headers (including Twilio signature)
- Form data from Twilio
- Validation attempt details
- Internal URL patterns

**Protection Mechanisms:**
1. ✅ Module-level setting check (user must have module management access)
2. ✅ Returns 404 when disabled (no information disclosure)
3. ✅ Security log warning on unauthorized access attempts
4. ✅ Still requires network-level access to the endpoint

### Why Verbose Logging is Conditional

Logging webhook details in production:
- ❌ Increases log storage costs
- ❌ May expose phone numbers and message content
- ❌ Creates noise that obscures real errors
- ✅ Critical errors are **always** logged regardless of debug mode

## Troubleshooting

### Debug Mode Not Working

**Check the Setting:**
```sql
-- Query Oqtane database
SELECT * FROM Setting 
WHERE EntityName = 'Module' 
  AND EntityId = {YourModuleId}
  AND SettingName = 'TwilioDebugMode';
```

**Expected Result:**
- `SettingValue` should be `"True"` or `"False"`
- If missing, the default is `false`

### Debug Endpoint Returns 404

**Possible Causes:**
1. Debug Mode is disabled ✅ Enable in settings
2. Wrong URL format ✅ Use `POST /api/TextMe/webhook/{moduleid}/debug`
3. Module ID is incorrect ✅ Verify from URL when viewing the module

### Logs Not Appearing

**Check:**
1. Debug Mode is enabled in settings ✅
2. Oqtane Event Log is configured ✅ Admin → Event Log
3. Log level allows `Information` entries ✅ Check site configuration
4. Cache might need clearing ✅ Restart app pool or site

## Related Documentation

- [SOLUTION-IgnoreAntiforgeryToken.md](./SOLUTION-IgnoreAntiforgeryToken.md) - Root cause and fix for Twilio 11200 error
- [QuickStart-Troubleshooting.md](./QuickStart-Troubleshooting.md) - General troubleshooting guide
- [TroubleshootingTwilio11200.md](./TroubleshootingTwilio11200.md) - Comprehensive diagnostic guide

## Version History

- **v1.0** - Initial Debug Mode implementation
  - Added `TwilioDebugMode` module setting
  - Gated debug endpoint behind setting check
  - Conditional verbose logging for webhook events
  - Documentation created

---

**Recommendation**: Always test with Debug Mode enabled, then disable for production use. 🎯
