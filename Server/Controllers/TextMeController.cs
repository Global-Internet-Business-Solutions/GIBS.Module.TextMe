using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Oqtane.Controllers;
using Oqtane.Enums;
using Oqtane.Infrastructure;
using Oqtane.Repository;
using Oqtane.Shared;
using GIBS.Module.TextMe.Services;
using GIBS.Module.TextMe.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace GIBS.Module.TextMe.Controllers
{
    [Route(ControllerRoutes.ApiRoute)]
    public class TextMeController : ModuleControllerBase
    {
        private readonly ITextMessageService _textMessageService;
        private readonly ITwilioRequestValidationService _twilioRequestValidationService;
        private readonly ISettingRepository _settingRepository;

        public TextMeController(ITextMessageService textMessageService, ITwilioRequestValidationService twilioRequestValidationService, ISettingRepository settingRepository, ILogManager logger, IHttpContextAccessor accessor) : base(logger, accessor)
        {
            _textMessageService = textMessageService;
            _twilioRequestValidationService = twilioRequestValidationService;
            _settingRepository = settingRepository;
        }

        private bool IsDebugModeEnabled(int moduleid)
        {
            var settings = _settingRepository.GetSettings(EntityNames.Module, moduleid).ToList();
            var debugModeSetting = settings.FirstOrDefault(s => s.SettingName == "TwilioDebugMode");
            return debugModeSetting != null && bool.TryParse(debugModeSetting.SettingValue, out var enabled) && enabled;
        }

        [HttpPost("send/{moduleid:int}")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Send(int moduleid, [FromBody] ChatSendRequest request)
        {
            if (moduleid <= 0 || request == null || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Name and message are required.");
            }

            try
            {
                await _textMessageService.SendFromWidgetAsync(moduleid, request);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Create, "Validation error sending widget message for module {ModuleId} {Error}", moduleid, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Create, "Error sending outbound widget message for module {ModuleId} {Error}", moduleid, ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, $"Unexpected server error while sending message. {ex.Message}");
            }
        }

        [HttpGet("messages/{moduleid:int}")]
        [AllowAnonymous]
        public async Task<IEnumerable<ChatMessageDto>> Messages(int moduleid, string conversationId, DateTime? sinceUtc)
        {
            if (moduleid <= 0)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return null;
            }

            try
            {
                return await _textMessageService.GetMessagesAsync(moduleid, conversationId, sinceUtc);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Read, "Error loading messages for module {ModuleId} {Error}", moduleid, ex.Message);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return null;
            }
        }

        [HttpGet("webhook/{moduleid:int}/ping")]
        [AllowAnonymous]
        public IActionResult WebhookPing(int moduleid)
        {
            // Diagnostic endpoint to verify webhook is reachable
            _logger.Log(LogLevel.Information, this, LogFunction.Read, "Webhook ping received for module {ModuleId}", moduleid);
            return Ok(new { 
                status = "online", 
                moduleId = moduleid,
                timestamp = DateTime.UtcNow,
                message = "Webhook endpoint is reachable. Configure this URL in Twilio without /ping suffix."
            });
        }

        [HttpPost("webhook/{moduleid:int}/debug")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> WebhookDebug(int moduleid)
        {
            // Check if debug mode is enabled
            if (!IsDebugModeEnabled(moduleid))
            {
                _logger.Log(LogLevel.Warning, this, LogFunction.Security, "Debug endpoint access denied - debug mode disabled for module {ModuleId}", moduleid);
                return NotFound();
            }

            // Enhanced diagnostic endpoint for troubleshooting
            _logger.Log(LogLevel.Information, this, LogFunction.Read, "Webhook DEBUG POST received for module {ModuleId}", moduleid);

            var debugInfo = new System.Text.StringBuilder();
            debugInfo.AppendLine("=== Webhook Debug Information ===");
            debugInfo.AppendLine($"Module ID: {moduleid}");
            debugInfo.AppendLine($"Method: {Request.Method}");
            debugInfo.AppendLine($"Content-Type: {Request.ContentType}");
            debugInfo.AppendLine($"Has Form Content: {Request.HasFormContentType}");
            debugInfo.AppendLine($"Request URL: {Request.GetDisplayUrl()}");
            debugInfo.AppendLine();

            debugInfo.AppendLine("=== Headers ===");
            foreach (var header in Request.Headers)
            {
                // Mask sensitive data
                var value = header.Key.Contains("Signature", StringComparison.OrdinalIgnoreCase) 
                    ? "[PRESENT]" 
                    : header.Value.ToString();
                debugInfo.AppendLine($"{header.Key}: {value}");
            }
            debugInfo.AppendLine();

            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync();
                debugInfo.AppendLine("=== Form Data ===");
                foreach (var item in form)
                {
                    debugInfo.AppendLine($"{item.Key}: {item.Value}");
                }
                debugInfo.AppendLine();

                debugInfo.AppendLine("=== Validation Debug ===");
                var validationDebug = _twilioRequestValidationService.GetValidationDebugInfo(moduleid, Request, form);
                debugInfo.AppendLine(validationDebug);
            }

            _logger.Log(LogLevel.Information, this, LogFunction.Read, "Debug info: {DebugInfo}", debugInfo.ToString());

            return Ok(new { 
                timestamp = DateTime.UtcNow,
                debugInfo = debugInfo.ToString()
            });
        }

        [HttpPost("webhook/{moduleid:int}")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook(int moduleid)
        {
            var isDebugMode = IsDebugModeEnabled(moduleid);
            var requestId = Guid.NewGuid().ToString("N").Substring(0, 8);

            // Log at appropriate level based on debug mode
            if (isDebugMode)
            {
                _logger.Log(LogLevel.Information, this, LogFunction.Read, "[{RequestId}] Webhook POST received for module {ModuleId}, ContentType: {ContentType}, URL: {Url}", 
                    requestId, moduleid, Request.ContentType, Request.GetDisplayUrl());
            }

            if (moduleid <= 0)
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Read, "[{RequestId}] Invalid moduleId: {ModuleId}", requestId, moduleid);
                Response.Headers["Content-Type"] = "text/plain";
                return BadRequest("Invalid module ID");
            }

            if (!Request.HasFormContentType)
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Read, "[{RequestId}] Invalid ContentType for module {ModuleId}. Expected form data, got: {ContentType}", 
                    requestId, moduleid, Request.ContentType);
                Response.Headers["Content-Type"] = "text/plain";
                return BadRequest("Expected application/x-www-form-urlencoded content type");
            }

            try
            {
                var form = await Request.ReadFormAsync();

                // Verbose logging only in debug mode
                if (isDebugMode)
                {
                    _logger.Log(LogLevel.Information, this, LogFunction.Read, "[{RequestId}] Webhook form keys: {Keys}, Has Signature: {HasSignature}", 
                        requestId,
                        string.Join(", ", form.Keys), 
                        Request.Headers.ContainsKey("X-Twilio-Signature"));
                }

                if (!_twilioRequestValidationService.IsValid(moduleid, Request, form))
                {
                    var debugInfo = _twilioRequestValidationService.GetValidationDebugInfo(moduleid, Request, form);
                    _logger.Log(LogLevel.Error, this, LogFunction.Security, "[{RequestId}] Invalid Twilio Signature for module {ModuleId}. Debug: {Debug}", 
                        requestId, moduleid, debugInfo);

                    // Return 403 Forbidden instead of 401 to prevent Twilio from treating this as a temporary error
                    Response.Headers["Content-Type"] = "text/plain";
                    return StatusCode((int)HttpStatusCode.Forbidden, "Invalid Twilio signature");
                }

                var message = await _textMessageService.PersistInboundAsync(moduleid, form);
                if (message == null)
                {
                    _logger.Log(LogLevel.Error, this, LogFunction.Create, "[{RequestId}] Failed to persist message for module {ModuleId}", requestId, moduleid);
                    Response.Headers["Content-Type"] = "text/plain";
                    return BadRequest("Failed to process message");
                }

                if (isDebugMode)
                {
                    _logger.Log(LogLevel.Information, this, LogFunction.Create, "[{RequestId}] SUCCESS - Inbound Twilio Message Persisted {MessageId} {ModuleId}", 
                        requestId, message.MessageId, moduleid);
                }

                // Return 200 OK with empty body as Twilio expects
                Response.Headers["Content-Type"] = "text/plain";
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Read, "[{RequestId}] EXCEPTION processing Twilio Webhook for module {ModuleId}: {Error}\nStackTrace: {StackTrace}", 
                    requestId, moduleid, ex.Message, ex.StackTrace);

                // Return 500 to signal Twilio to retry
                Response.Headers["Content-Type"] = "text/plain";
                return StatusCode((int)HttpStatusCode.InternalServerError, "Internal server error processing webhook");
            }
        }
    }
}
