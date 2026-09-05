# Analyze-TwilioWebhookLogs.ps1
# Script to find and analyze Twilio webhook requests in IIS logs

param(
	[string]$LogPath,
	[int]$ModuleId = 523,
	[switch]$Live,
	[int]$LastNMinutes = 60
)

# Colors for output
$SuccessColor = 'Green'
$ErrorColor = 'Red'
$WarningColor = 'Yellow'
$InfoColor = 'Cyan'

function Get-StatusCodeDescription {
	param([string]$StatusCode)

	switch ($StatusCode) {
		'200' { return "✅ OK - Success" }
		'400' { return "❌ Bad Request - Invalid form data" }
		'401' { return "❌ Unauthorized - Signature validation failed" }
		'403' { return "❌ Forbidden - Signature validation failed" }
		'500' { return "❌ Internal Server Error - Exception in code" }
		'503' { return "❌ Service Unavailable - App offline" }
		default { return "? Unknown status code" }
	}
}

function Parse-IISLogLine {
	param([string]$Line)

	if ($Line -match '^#' -or [string]::IsNullOrWhiteSpace($Line)) {
		return $null
	}

	$parts = $Line -split '\s+'

	if ($parts.Length -lt 15) {
		return $null
	}

	return [PSCustomObject]@{
		Date = $parts[0]
		Time = $parts[1]
		SiteName = $parts[2]
		Method = $parts[3]
		UriStem = $parts[4]
		UriQuery = $parts[5]
		Port = $parts[6]
		Username = $parts[7]
		ClientIP = $parts[8]
		UserAgent = $parts[9]
		Cookie = $parts[10]
		Referer = $parts[11]
		Host = $parts[12]
		StatusCode = $parts[13]
		SubStatus = $parts[14]
		Win32Status = $parts[15]
		BytesSent = if ($parts.Length -gt 16) { $parts[16] } else { "0" }
		BytesReceived = if ($parts.Length -gt 17) { $parts[17] } else { "0" }
		TimeTaken = if ($parts.Length -gt 18) { $parts[18] } else { "0" }
		FullLine = $Line
	}
}

function Show-WebhookAnalysis {
	param([array]$Entries)

	if ($Entries.Count -eq 0) {
		Write-Host "`n❌ No webhook POST requests found in logs!" -ForegroundColor $ErrorColor
		Write-Host "This means Twilio's requests are NOT reaching your server." -ForegroundColor $WarningColor
		Write-Host "`nPossible reasons:" -ForegroundColor $InfoColor
		Write-Host "  1. Webhook URL not configured in Twilio"
		Write-Host "  2. Firewall blocking Twilio's IPs"
		Write-Host "  3. Twilio having connectivity issues"
		Write-Host "  4. Wrong module ID in URL"
		return
	}

	Write-Host "`n=== Webhook Request Analysis ===" -ForegroundColor $InfoColor
	Write-Host "Found $($Entries.Count) webhook POST request(s)`n"

	$statusGroups = $Entries | Group-Object -Property StatusCode

	foreach ($group in $statusGroups) {
		$status = $group.Name
		$count = $group.Count
		$description = Get-StatusCodeDescription $status
		$color = if ($status -eq '200') { $SuccessColor } else { $ErrorColor }

		Write-Host "Status $status : $count request(s) - $description" -ForegroundColor $color
	}

	Write-Host "`n=== Recent Requests (newest first) ===" -ForegroundColor $InfoColor

	$Entries | Select-Object -First 10 | ForEach-Object {
		$color = if ($_.StatusCode -eq '200') { $SuccessColor } else { $ErrorColor }
		$desc = Get-StatusCodeDescription $_.StatusCode

		Write-Host "`n[$($_.Date) $($_.Time)]" -ForegroundColor $InfoColor -NoNewline
		Write-Host " [$($_.StatusCode)]" -ForegroundColor $color -NoNewline
		Write-Host " $desc"

		Write-Host "  Client IP: $($_.ClientIP)"
		Write-Host "  URI: $($_.UriStem)"
		if ($_.UriQuery -ne '-') {
			Write-Host "  Query: $($_.UriQuery)"
		}
		Write-Host "  Response Time: $($_.TimeTaken)ms"
		Write-Host "  Bytes Sent: $($_.BytesSent), Received: $($_.BytesReceived)"
	}
}

# Main script
Clear-Host
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor $InfoColor
Write-Host "║   Twilio Webhook Log Analyzer - Module $ModuleId" -ForegroundColor $InfoColor
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor $InfoColor

# Determine log path
if ([string]::IsNullOrWhiteSpace($LogPath)) {
	# Try common locations
	$possiblePaths = @(
		"C:\inetpub\logs\LogFiles\W3SVC*",
		"C:\home\LogFiles\http",  # Azure App Service
		"D:\home\LogFiles\http"   # Azure App Service alternate
	)

	foreach ($path in $possiblePaths) {
		if (Test-Path $path) {
			$LogPath = $path
			Write-Host "✓ Found logs at: $LogPath" -ForegroundColor $SuccessColor
			break
		}
	}

	if ([string]::IsNullOrWhiteSpace($LogPath)) {
		Write-Host "❌ Could not find IIS logs automatically." -ForegroundColor $ErrorColor
		Write-Host "Please specify -LogPath parameter." -ForegroundColor $WarningColor
		exit 1
	}
}

Write-Host "📁 Searching logs in: $LogPath" -ForegroundColor $InfoColor
Write-Host "🔍 Looking for: POST /api/TextMe/webhook/$ModuleId" -ForegroundColor $InfoColor

# Calculate time filter
$cutoffTime = (Get-Date).AddMinutes(-$LastNMinutes)
Write-Host "⏰ Filtering to last $LastNMinutes minutes (since $($cutoffTime.ToString('yyyy-MM-dd HH:mm:ss')))" -ForegroundColor $InfoColor

# Find and parse log files
$logFiles = Get-ChildItem -Path $LogPath -Filter "*.log" -Recurse -ErrorAction SilentlyContinue |
	Where-Object { $_.LastWriteTime -gt $cutoffTime } |
	Sort-Object LastWriteTime -Descending

if ($logFiles.Count -eq 0) {
	Write-Host "❌ No log files found in the specified path." -ForegroundColor $ErrorColor
	exit 1
}

Write-Host "📄 Found $($logFiles.Count) log file(s) to search`n" -ForegroundColor $SuccessColor

# Parse and filter logs
$webhookEntries = @()

foreach ($file in $logFiles) {
	Write-Host "  Scanning: $($file.Name)..." -NoNewline

	$content = Get-Content $file.FullName -ErrorAction SilentlyContinue
	$fileEntries = $content | 
		Where-Object { $_ -match "POST.*webhook/$ModuleId" } |
		ForEach-Object { Parse-IISLogLine $_ } |
		Where-Object { $_ -ne $null }

	if ($fileEntries.Count -gt 0) {
		Write-Host " Found $($fileEntries.Count) request(s)" -ForegroundColor $SuccessColor
		$webhookEntries += $fileEntries
	} else {
		Write-Host " No matches" -ForegroundColor Gray
	}
}

# Sort by datetime descending
$webhookEntries = $webhookEntries | Sort-Object { [DateTime]"$($_.Date) $($_.Time)" } -Descending

# Show analysis
Show-WebhookAnalysis $webhookEntries

# Additional diagnostic info
Write-Host "`n=== Next Steps ===" -ForegroundColor $InfoColor

if ($webhookEntries.Count -eq 0) {
	Write-Host "1. Verify Twilio webhook URL is correctly configured:"
	Write-Host "   https://playground.gibs.net/api/TextMe/webhook/$ModuleId"
	Write-Host "2. Send a test SMS to your Twilio number"
	Write-Host "3. Run this script again immediately after"
	Write-Host "4. If still no entries, check Twilio Debugger for connection errors"
} else {
	$hasErrors = $webhookEntries | Where-Object { $_.StatusCode -ne '200' }

	if ($hasErrors) {
		Write-Host "Errors detected! Recommended actions:" -ForegroundColor $WarningColor
		Write-Host "1. Check Oqtane database logs:"
		Write-Host "   SELECT * FROM Log WHERE Category LIKE '%TextMe%' ORDER BY LogDate DESC"
		Write-Host "2. Use the debug endpoint to see validation details:"
		Write-Host "   https://playground.gibs.net/api/TextMe/webhook/$ModuleId/debug"
		Write-Host "3. Check Application Insights or Azure Log Stream"
	} else {
		Write-Host "✓ All requests returned 200 OK" -ForegroundColor $SuccessColor
		Write-Host "If Twilio still reports error 11200:" -ForegroundColor $WarningColor
		Write-Host "1. Check if response is being sent within 15 seconds"
		Write-Host "2. Verify Content-Type header is set correctly"
		Write-Host "3. Check Azure Application Gateway or WAF settings"
		Write-Host "4. Review Twilio's debugger for their perspective"
	}
}

Write-Host "`n=== Useful Commands ===" -ForegroundColor $InfoColor
Write-Host "Test webhook locally:"
Write-Host "  curl -X POST https://playground.gibs.net/api/TextMe/webhook/$ModuleId -H 'Content-Type: application/x-www-form-urlencoded' -d 'MessageSid=TEST&From=%2B1555&To=%2B1555&Body=Test'"
Write-Host "`nUse debug endpoint:"
Write-Host "  curl -X POST https://playground.gibs.net/api/TextMe/webhook/$ModuleId/debug -H 'Content-Type: application/x-www-form-urlencoded' -d 'MessageSid=TEST&From=%2B1555&To=%2B1555&Body=Test'"
Write-Host "`nTest connectivity:"
Write-Host "  curl https://playground.gibs.net/api/TextMe/webhook/$ModuleId/ping"

if ($Live) {
	Write-Host "`n=== Live Monitoring Mode ===" -ForegroundColor $InfoColor
	Write-Host "Press Ctrl+C to stop..." -ForegroundColor $WarningColor
	Write-Host ""

	$lastEntryTime = if ($webhookEntries.Count -gt 0) { 
		[DateTime]"$($webhookEntries[0].Date) $($webhookEntries[0].Time)" 
	} else { 
		(Get-Date).AddHours(-1) 
	}

	while ($true) {
		Start-Sleep -Seconds 5

		# Check for new entries
		$newEntries = @()
		foreach ($file in $logFiles) {
			if ($file.LastWriteTime -gt $lastEntryTime.AddSeconds(-10)) {
				$content = Get-Content $file.FullName -ErrorAction SilentlyContinue | Select-Object -Last 100
				$fileEntries = $content | 
					Where-Object { $_ -match "POST.*webhook/$ModuleId" } |
					ForEach-Object { Parse-IISLogLine $_ } |
					Where-Object { 
						$_ -ne $null -and 
						[DateTime]"$($_.Date) $($_.Time)" -gt $lastEntryTime 
					}

				if ($fileEntries.Count -gt 0) {
					$newEntries += $fileEntries
				}
			}
		}

		if ($newEntries.Count -gt 0) {
			foreach ($entry in $newEntries) {
				$color = if ($entry.StatusCode -eq '200') { $SuccessColor } else { $ErrorColor }
				$desc = Get-StatusCodeDescription $entry.StatusCode

				Write-Host "[$($entry.Date) $($entry.Time)] " -NoNewline -ForegroundColor $InfoColor
				Write-Host "[$($entry.StatusCode)] " -NoNewline -ForegroundColor $color
				Write-Host "$desc - IP: $($entry.ClientIP) - Time: $($entry.TimeTaken)ms"

				$lastEntryTime = [DateTime]"$($entry.Date) $($entry.Time)"
			}
		} else {
			Write-Host "." -NoNewline -ForegroundColor Gray
		}
	}
}

Write-Host ""
