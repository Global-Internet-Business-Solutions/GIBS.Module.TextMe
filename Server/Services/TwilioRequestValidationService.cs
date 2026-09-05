using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Oqtane.Enums;
using Oqtane.Infrastructure;
using Oqtane.Modules;
using Oqtane.Repository;
using Oqtane.Shared;
using Twilio.Security;

namespace GIBS.Module.TextMe.Services
{
    public interface ITwilioRequestValidationService
    {
        bool IsValid(int moduleId, HttpRequest request, IFormCollection form);
        string GetValidationDebugInfo(int moduleId, HttpRequest request, IFormCollection form);
    }

    public class TwilioRequestValidationService : ITwilioRequestValidationService, ITransientService
    {
        private const string AuthTokenSettingName = "TwilioAuthToken";
        private const string PublicWebhookBaseUrlSettingName = "TwilioWebhookPublicBaseUrl";
        private readonly ISettingRepository _settingRepository;
        private readonly ILogManager _logger;

        public TwilioRequestValidationService(ISettingRepository settingRepository, ILogManager logger)
        {
            _settingRepository = settingRepository;
            _logger = logger;
        }

        public string GetValidationDebugInfo(int moduleId, HttpRequest request, IFormCollection form)
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine($"Request URL: {request.GetDisplayUrl()}");
            info.AppendLine($"Method: {request.Method}");
            info.AppendLine($"ContentType: {request.ContentType}");
            info.AppendLine($"Has Signature Header: {request.Headers.ContainsKey("X-Twilio-Signature")}");

            if (request.Headers.TryGetValue("X-Twilio-Signature", out var sig))
            {
                info.AppendLine($"Signature: {sig}");
            }

            info.AppendLine($"Form Keys: {string.Join(", ", form.Keys)}");

            var publicBaseUrl = _settingRepository.GetSettingValue(EntityNames.Module, moduleId, PublicWebhookBaseUrlSettingName, "");
            info.AppendLine($"Public Base URL Setting: {publicBaseUrl}");

            var fallbackUrls = GetFallbackUrls(moduleId, request);
            info.AppendLine($"Fallback URLs: {string.Join(", ", fallbackUrls)}");

            return info.ToString();
        }

        public bool IsValid(int moduleId, HttpRequest request, IFormCollection form)
        {
            var authToken = _settingRepository.GetSettingValue(EntityNames.Module, moduleId, AuthTokenSettingName, "");
            if (string.IsNullOrWhiteSpace(authToken))
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "No auth token configured for module {ModuleId}", moduleId);
                return false;
            }

            if (!request.Headers.TryGetValue("X-Twilio-Signature", out var signature) || string.IsNullOrWhiteSpace(signature))
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "No X-Twilio-Signature header found for module {ModuleId}", moduleId);
                return false;
            }

            var parameters = form.ToDictionary(item => item.Key, item => item.Value.ToString());
            var requestValidator = new RequestValidator(authToken);
            var twilioSignature = signature.ToString();
            var displayUrl = request.GetDisplayUrl();

            _logger.Log(LogLevel.Information, this, LogFunction.Security, "Validating Twilio signature for module {ModuleId}, URL: {Url}", moduleId, displayUrl);

            if (requestValidator.Validate(displayUrl, parameters, twilioSignature))
            {
                _logger.Log(LogLevel.Information, this, LogFunction.Security, "Signature validated successfully with display URL for module {ModuleId}", moduleId);
                return true;
            }

            _logger.Log(LogLevel.Warning, this, LogFunction.Security, "Primary URL validation failed for module {ModuleId}, trying fallback URLs", moduleId);

            var fallbackUrls = GetFallbackUrls(moduleId, request).ToList();
            for (int i = 0; i < fallbackUrls.Count; i++)
            {
                var fallbackUrl = fallbackUrls[i];
                _logger.Log(LogLevel.Information, this, LogFunction.Security, "Trying fallback URL {Index}: {Url}", i + 1, fallbackUrl);

                if (requestValidator.Validate(fallbackUrl, parameters, twilioSignature))
                {
                    _logger.Log(LogLevel.Information, this, LogFunction.Security, "Signature validated successfully with fallback URL {Index} for module {ModuleId}", i + 1, moduleId);
                    return true;
                }
            }

            _logger.Log(LogLevel.Error, this, LogFunction.Security, "All signature validation attempts failed for module {ModuleId}. Display URL: {DisplayUrl}, Fallback count: {FallbackCount}", moduleId, displayUrl, fallbackUrls.Count);
            return false;
        }

        private IEnumerable<string> GetFallbackUrls(int moduleId, HttpRequest request)
        {
            var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pathAndQuery = $"{request.PathBase}{request.Path}{request.QueryString}";

            var forwardedProto = GetFirstForwardedValue(request, "X-Forwarded-Proto");
            var forwardedHost = GetFirstForwardedValue(request, "X-Forwarded-Host");
            var forwardedPrefix = GetFirstForwardedValue(request, "X-Forwarded-Prefix");

            if (!string.IsNullOrWhiteSpace(forwardedHost))
            {
                var scheme = string.IsNullOrWhiteSpace(forwardedProto) ? request.Scheme : forwardedProto;
                var prefix = string.IsNullOrWhiteSpace(forwardedPrefix) ? string.Empty : EnsureStartsWithSlash(forwardedPrefix).TrimEnd('/');
                urls.Add($"{scheme}://{forwardedHost}{prefix}{pathAndQuery}");
            }

            var publicBaseUrl = _settingRepository.GetSettingValue(EntityNames.Module, moduleId, PublicWebhookBaseUrlSettingName, "");
            if (!string.IsNullOrWhiteSpace(publicBaseUrl))
            {
                urls.Add($"{publicBaseUrl.TrimEnd('/')}{EnsureStartsWithSlash(pathAndQuery)}");
            }

            return urls;
        }

        private static string GetFirstForwardedValue(HttpRequest request, string headerName)
        {
            if (!request.Headers.TryGetValue(headerName, out var value) || string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value
                .ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));
        }

        private static string EnsureStartsWithSlash(string value)
        {
            return value.StartsWith("/", StringComparison.Ordinal) ? value : $"/{value}";
        }
    }
}
