# Quick Start: Diagnosing Twilio Error 11200

## ✅ SOLUTION CONFIRMED - PRODUCTION WORKING!

**The root cause was Oqtane's anti-forgery (CSRF) protection blocking Twilio's webhooks.**

### The Fix (Applied & Verified)
Added `[IgnoreAntiforgeryToken]` attribute to the webhook endpoints. See `Documentation/SOLUTION-IgnoreAntiforgeryToken.md` for full details.

**Production Verification:**
```
[c54cd69d] SUCCESS - Inbound Twilio Message Persisted 8 523
```

- ✅ Twilio webhook POST successfully reached server from IP `35.174.13.123`
- ✅ `X-Twilio-Signature` header validated
- ✅ Message persisted successfully (MessageId: 8, ModuleId: 523)

### Debug Mode Feature

A **Debug Mode** toggle has been added to module settings:

**When Enabled:**
- Verbose webhook logging (request IDs, form keys, signatures)  
- Debug endpoint accessible at `/api/TextMe/webhook/{moduleid}/debug`
- Detailed success/failure logs

**Production Recommendation:**  
Keep Debug Mode **OFF** in production; enable only for troubleshooting.

**Why this was the issue:**
- Oqtane requires anti-forgery tokens on all POST requests
- Twilio doesn't send these tokens
- Requests were being rejected at middleware level (before your code)
- Your signature validation provides security, so bypassing anti-forgery is safe

---

## Your Situation
- ✅ Ping test successful (endpoint is reachable)
- ❌ Twilio reports error 11200 (HTTP retrieval failure)
- 🔍 Need to find out what's happening when Twilio POSTs to your webhook

## Step 1: Run the Log Analysis Script

Open PowerShell and navigate to your project directory:

```powershell
cd C:\GitHub\GIBS.Module.TextMe
```

### Basic Usage - Analyze Last Hour
```powershell
.\Scripts\Analyze-TwilioWebhookLogs.ps1
```

### Specify Log Path (if needed)
```powershell
.\Scripts\Analyze-TwilioWebhookLogs.ps1 -LogPath "C:\inetpub\logs\LogFiles\W3SVC1"
```

### Live Monitoring Mode
```powershell
.\Scripts\Analyze-TwilioWebhookLogs.ps1 -Live
```
This will continuously watch for new webhook requests. Send a test SMS and see the result in real-time!

### Different Module ID
```powershell
.\Scripts\Analyze-TwilioWebhookLogs.ps1 -ModuleId 123
```

## Step 2: Interpret the Results

### Scenario A: No Webhook Requests Found
```
❌ No webhook POST requests found in logs!
```

**This means:** Twilio's requests are NOT reaching your IIS server at all.

**Causes:**
1. Webhook URL not configured in Twilio Console
2. Firewall blocking Twilio's IP ranges
3. DNS not resolving correctly
4. Wrong module ID in Twilio configuration

**Fix:**
1. Check Twilio Console → Phone Numbers → Your Number → Messaging Configuration
2. Verify URL is exactly: `https://playground.gibs.net/api/TextMe/webhook/523`
3. Verify HTTP method is **POST**
4. Check firewall rules allow Twilio IPs

### Scenario B: Requests Found with Status 200
```
✓ All requests returned 200 OK
```

**This means:** Your application says it's successful, but Twilio still sees an error.

**Causes:**
1. Response taking too long (>15 seconds)
2. Azure gateway/WAF modifying response
3. Response content-type mismatch
4. Connection dropped before response fully sent

**Fix:**
1. Check Oqtane logs:
   ```sql
   SELECT * FROM Log 
   WHERE Category LIKE '%TextMe%' 
   ORDER BY LogDate DESC
   ```
2. Look for timing issues (time between POST received and SUCCESS message)
3. Check Azure Application Gateway timeout settings
4. Review Twilio Debugger for their error details

### Scenario C: Requests Found with Status 401/403
```
Status 403 : 5 request(s) - ❌ Forbidden - Signature validation failed
```

**This means:** Signature validation is failing.

**Fix:**
1. Use the debug endpoint:
   ```
   Twilio Console → Change webhook to:
   https://playground.gibs.net/api/TextMe/webhook/523/debug
   ```
2. Send test SMS
3. Check Twilio's request debugger for the response body
4. The response will show you exactly why validation is failing

### Scenario D: Requests Found with Status 500
```
Status 500 : 2 request(s) - ❌ Internal Server Error - Exception in code
```

**This means:** Your application is crashing.

**Fix:**
1. Check Oqtane logs immediately:
   ```sql
   SELECT * FROM Log 
   WHERE LogLevel = 'Error' 
   ORDER BY LogDate DESC
   ```
2. Look for exception stack traces
3. Common issues:
   - Database connection failure
   - Missing Twilio auth token in settings
   - Null reference exceptions

## Step 3: Database Log Queries

### Recent TextMe Activity
```sql
SELECT TOP 50
	LogDate,
	LogLevel,
	Category,
	Feature,
	Function,
	Message
FROM Log
WHERE Category LIKE '%TextMe%'
   OR Message LIKE '%webhook%'
   OR Message LIKE '%Twilio%'
ORDER BY LogDate DESC
```

### Find Specific Request (if you know the time)
```sql
SELECT * FROM Log
WHERE LogDate >= '2026-08-21 15:47:00'
  AND LogDate <= '2026-08-21 15:48:00'
  AND Category LIKE '%TextMe%'
ORDER BY LogDate DESC
```

### Check Module Settings
```sql
SELECT 
	SettingName,
	SettingValue
FROM Setting
WHERE EntityName = 'Module'
  AND EntityId = 523
  AND SettingName LIKE 'Twilio%'
```

Should show:
- `TwilioAccountSid`
- `TwilioAuthToken`
- `TwilioPhoneNumber`
- `TwilioWebhookPublicBaseUrl` = `https://playground.gibs.net`

## Step 4: Use Debug Endpoint (If Needed)

The debug endpoint bypasses signature validation and provides detailed diagnostic info.

### Temporarily Configure Twilio
1. Twilio Console → Phone Numbers → Your Number
2. "A Message Comes In" → Webhook:
   ```
   https://playground.gibs.net/api/TextMe/webhook/523/debug
   ```
3. Send test SMS
4. Check Twilio's request debugger for the response

**Response will include:**
- All headers (including signature)
- Form data keys and values
- Request URL from Twilio's perspective
- Request URL your app sees
- All fallback URLs being tried
- Validation debug info

**⚠️ IMPORTANT:** Change back to regular webhook URL after debugging!

## Step 5: Test Manually

### Test Connectivity
```powershell
curl https://playground.gibs.net/api/TextMe/webhook/523/ping
```

Expected: 
```json
{"status":"online","moduleId":523,"timestamp":"..."}
```

### Test POST (Will fail validation but will log)
```powershell
Invoke-WebRequest `
  -Uri "https://playground.gibs.net/api/TextMe/webhook/523" `
  -Method POST `
  -ContentType "application/x-www-form-urlencoded" `
  -Body "MessageSid=TEST123&From=%2B15555551234&To=%2B15089284600&Body=TestMessage"
```

Check logs for the request - should see "Invalid Twilio signature" which proves the endpoint is working.

### Test Debug Endpoint
```powershell
Invoke-WebRequest `
  -Uri "https://playground.gibs.net/api/TextMe/webhook/523/debug" `
  -Method POST `
  -ContentType "application/x-www-form-urlencoded" `
  -Body "MessageSid=TEST123&From=%2B15555551234&To=%2B15089284600&Body=TestMessage"
```

Should return detailed debug information.

## Common Azure-Specific Issues

### Issue: ARR Affinity Interfering
**Symptom:** Random failures, works sometimes

**Fix:**
```
Azure Portal → Your Web App → Configuration → General settings
→ ARR affinity: OFF
```

### Issue: Always On Disabled
**Symptom:** First request fails or is very slow

**Fix:**
```
Azure Portal → Your Web App → Configuration → General settings
→ Always On: ON
```

### Issue: Application Insights Not Capturing Logs
**Fix:**
```
Azure Portal → Your Web App → Application Insights
→ Enable and configure
```

### Issue: IIS Recycling During Request
**Symptom:** Random 503 errors

**Fix:**
```
Azure Portal → Your Web App → Configuration → General settings
→ Increase WEBSITE_TIME_ZONE
→ Check recycling schedule
```

## Decision Tree

```
Can you see ANY POST to /api/TextMe/webhook/523 in IIS logs?
├─ NO → Twilio can't reach your server
│   ├─ Check Twilio webhook configuration
│   ├─ Check firewall rules
│   └─ Use /ping endpoint to verify connectivity
│
└─ YES → What status code?
	├─ 200 → Success in IIS but Twilio reports error
	│   ├─ Check response time (< 15 seconds)
	│   ├─ Check Azure gateway/WAF logs
	│   ├─ Check Oqtane logs for actual errors
	│   └─ Review Twilio debugger
	│
	├─ 401/403 → Signature validation failed
	│   ├─ Use debug endpoint to see validation details
	│   ├─ Verify Public Webhook Base URL setting
	│   └─ Check auth token matches Twilio
	│
	├─ 500 → Application error
	│   ├─ Check Oqtane logs for exception
	│   ├─ Check database connectivity
	│   └─ Verify all settings are configured
	│
	└─ Other → Configuration issue
		└─ Check IIS/Azure configuration
```

## What Information to Provide for Help

If you need further assistance, please provide:

1. **Output from the PowerShell script**
2. **IIS log entries** for the webhook requests (last 5)
3. **Oqtane database log entries** (last 10 related to TextMe)
4. **Twilio debugger screenshot** showing the error
5. **Module settings** from database (`SELECT * FROM Setting WHERE EntityId = 523`)
6. **Status codes** you're seeing (200, 401, 403, 500, etc.)

## Quick Reference

| Endpoint | Purpose | Authentication |
|----------|---------|----------------|
| `/api/TextMe/webhook/523` | Production webhook | Signature validation required |
| `/api/TextMe/webhook/523/ping` | Test connectivity (GET) | None required |
| `/api/TextMe/webhook/523/debug` | Debug signature validation (POST) | None required |

| Status Code | Meaning | Action |
|-------------|---------|--------|
| 200 | Success | Check if Twilio still reports error |
| 400 | Bad Request | Check form data format |
| 401/403 | Auth Failed | Use debug endpoint |
| 500 | Server Error | Check Oqtane logs |
| 503 | Unavailable | App recycling or offline |

---

## Support

- 📚 Full documentation: `Documentation\TroubleshootingTwilio11200.md`
- 🔧 PowerShell script: `Scripts\Analyze-TwilioWebhookLogs.ps1`
- 🐛 GitHub Issues: [GIBS.Module.TextMe Issues](https://github.com/Global-Internet-Business-Solutions/GIBS.Module.TextMe/issues)
