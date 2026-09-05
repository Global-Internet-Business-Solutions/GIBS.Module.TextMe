# Troubleshooting Twilio Error 11200

## Quick Reference
- **Your Webhook URL**: `https://playground.gibs.net/api/TextMe/webhook/523`
- **Expected Method**: POST
- **Expected Source**: Twilio IP ranges (see below)
- **Expected Status**: 200

## Finding Webhook Requests in IIS Logs

### 1. Filter IIS Logs for Webhook Requests

Look for POST requests to the webhook endpoint:

```powershell
# PowerShell command to filter IIS logs
Get-Content "C:\inetpub\logs\LogFiles\W3SVC*\u_ex*.log" | 
	Select-String "POST /api/TextMe/webhook/523" | 
	Select-Object -Last 10
```

### 2. What to Look For

**Successful webhook request should look like:**
```
2026-08-21 15:XX:XX ... POST /api/TextMe/webhook/523 - 443 - <TWILIO_IP> ... 200 0 0 ...
```

**Status codes and their meaning:**
- `200 0 0` = ✅ Success (but Twilio still reports 11200? See Azure issue below)
- `401 0 0` or `403 0 0` = ❌ Signature validation failed
- `400 0 0` = ❌ Bad request (form data issue)
- `500 0 0` = ❌ Server error (exception in code)
- `503 0 0` = ❌ Service unavailable (app offline/recycling)

### 3. Twilio IP Addresses

Twilio webhooks come from these IP ranges (as of 2024):
- `54.172.60.0/23`
- `54.244.51.0/24`
- `52.1.157.0/24`
- And others - see [Twilio's official list](https://www.twilio.com/docs/glossary/what-are-the-twilio-ip-addresses)

**Check if requests from these IPs are even reaching your server.**

## Common Azure-Specific Issues

### Azure App Service + IIS Logs Issue

⚠️ **IMPORTANT**: Azure App Service might show 200 OK in IIS logs even when the application returns an error!

This happens because:
1. IIS logs the response from the Azure load balancer, not your app
2. ARR (Application Request Routing) might mask the real status code

### How to See Real Application Logs

Use Azure Application Insights or Oqtane's built-in logging:

#### Option 1: Azure Portal Log Stream
```
Azure Portal → Your Web App → Monitoring → Log stream
```

#### Option 2: Oqtane Database Logs
```sql
SELECT TOP 50 *
FROM Log
WHERE Category LIKE '%TextMe%'
  OR Message LIKE '%webhook%'
ORDER BY LogDate DESC
```

#### Option 3: Download Application Logs
```
Azure Portal → Your Web App → Advanced Tools (Kudu) 
→ Debug Console → LogFiles → Application
```

## Step-by-Step Diagnostic Process

### Phase 1: Confirm Twilio Can Reach Your Server

1. **Check IIS logs for ANY POST to `/api/TextMe/webhook/523`**:
   ```powershell
   Get-Content "C:\inetpub\logs\LogFiles\W3SVC*\u_ex*.log" | 
	   Select-String "POST.*webhook/523"
   ```

2. **If NO entries found**:
   - Twilio cannot reach your server at all
   - Check firewall rules
   - Verify Twilio webhook URL is configured correctly
   - Test with the debug endpoint: `/api/TextMe/webhook/523/debug`

3. **If entries found**:
   - Note the IP address - is it from Twilio?
   - Note the status code (sc-status column)
   - Proceed to Phase 2

### Phase 2: Check Application Response

1. **Query Oqtane logs** for the exact time of the webhook request:
   ```sql
   SELECT * FROM Log
   WHERE LogDate >= '2026-08-21 15:47:00'
	 AND LogDate <= '2026-08-21 15:48:00'
	 AND (Category LIKE '%TextMe%' OR Message LIKE '%webhook%')
   ORDER BY LogDate DESC
   ```

2. **Look for these log entries** (added by enhanced logging):
   ```
   [RequestId] Webhook POST received for module 523
   [RequestId] Webhook form keys: ...
   [RequestId] SUCCESS - Inbound Twilio Message Persisted
   ```

3. **If you see "Invalid Twilio Signature"**:
   - Use the debug endpoint (see below)
   - Problem is with signature validation

4. **If you see "Failed to persist message"**:
   - Database connection issue
   - Check the full error message

### Phase 3: Use Debug Endpoint

Temporarily configure Twilio to use the debug endpoint:

```
https://playground.gibs.net/api/TextMe/webhook/523/debug
```

This endpoint:
- ✅ Accepts requests without validation
- ✅ Returns detailed debug information
- ✅ Logs everything

**After configuring, send a test SMS and check Twilio's Debugger for the response body.**

## Azure-Specific Configuration Checklist

- [ ] **HTTPS Only**: Enabled ✅ (confirmed)
- [ ] **Always On**: Should be enabled for webhooks
- [ ] **ARR Affinity**: Disable if causing issues
- [ ] **Application Insights**: Enable for better logging
- [ ] **Minimum TLS Version**: 1.2 or higher
- [ ] **Firewall Rules**: Allow Twilio IPs
- [ ] **Kudu Console**: Accessible at `https://your-app.scm.azurewebsites.net`

## PowerShell Script to Monitor Logs

Save this as `Monitor-WebhookLogs.ps1`:

```powershell
param(
	[string]$LogPath = "C:\inetpub\logs\LogFiles\W3SVC*",
	[int]$TailLines = 20
)

Write-Host "Monitoring webhook requests (Ctrl+C to stop)..." -ForegroundColor Green

$lastCount = 0
while ($true) {
	$entries = Get-Content "$LogPath\u_ex*.log" -ErrorAction SilentlyContinue | 
		Select-String "POST.*webhook/523" |
		Select-Object -Last $TailLines

	if ($entries.Count -gt $lastCount) {
		Clear-Host
		Write-Host "=== Latest Webhook Requests ===" -ForegroundColor Cyan
		$entries | ForEach-Object {
			$line = $_ -split '\s+'
			$date = $line[0]
			$time = $line[1]
			$method = $line[3]
			$uri = $line[4]
			$ip = $line[8]
			$status = $line[11]

			Write-Host "$date $time " -NoNewline
			Write-Host "[$status] " -NoNewline -ForegroundColor $(if ($status -eq '200') { 'Green' } else { 'Red' })
			Write-Host "$method $uri from $ip"
		}
		$lastCount = $entries.Count
	}

	Start-Sleep -Seconds 2
}
```

Run it while testing:
```powershell
.\Monitor-WebhookLogs.ps1
```

## Signature Validation Issues

If logs show "Invalid Twilio Signature", the issue is URL mismatch. Common causes:

### 1. Azure ARR/Load Balancer Modifications
Azure might be changing:
- `http://` to `https://` (or vice versa)
- Adding/removing trailing slashes
- Modifying query strings

**Solution**: The `Public Webhook Base URL` setting (`https://playground.gibs.net`) should handle this via fallback validation.

### 2. Verify Public Base URL Setting
```sql
SELECT * FROM Setting
WHERE EntityName = 'Module'
  AND EntityId = 523
  AND SettingName = 'TwilioWebhookPublicBaseUrl'
```

Should return: `https://playground.gibs.net`

### 3. Test Signature Validation
The debug endpoint will show:
- Request URL that Twilio is using
- Request URL your app sees
- All fallback URLs being tried
- Whether signature validation passes

## Quick Test Commands

### Test from your machine (will log but fail validation)
```powershell
curl -X POST https://playground.gibs.net/api/TextMe/webhook/523 `
  -H "Content-Type: application/x-www-form-urlencoded" `
  -d "MessageSid=TEST&From=%2B15555551234&To=%2B15089284600&Body=TestMessage"
```

### Test debug endpoint
```powershell
curl -X POST https://playground.gibs.net/api/TextMe/webhook/523/debug `
  -H "Content-Type: application/x-www-form-urlencoded" `
  -d "MessageSid=TEST&From=%2B15555551234&To=%2B15089284600&Body=TestMessage"
```

### Test ping endpoint
```powershell
curl https://playground.gibs.net/api/TextMe/webhook/523/ping
```

## Next Steps

1. **Find the actual webhook POST requests** in IIS logs
2. **Note the status code** returned
3. **Check Oqtane database logs** for detailed error messages
4. **If signature validation fails**, use the debug endpoint
5. **Share the log entries** for further analysis

## Contact Information

If you need help interpreting the logs, provide:
- [ ] IIS log entries for POST to webhook/523
- [ ] Oqtane database log entries around the same time
- [ ] Status code from IIS logs
- [ ] Any error messages from application logs
