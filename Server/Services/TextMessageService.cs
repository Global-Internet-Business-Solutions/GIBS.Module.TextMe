using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Oqtane.Modules;
using Oqtane.Repository;
using Oqtane.Shared;
using GIBS.Module.TextMe.Repository;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace GIBS.Module.TextMe.Services
{
    public interface ITextMessageService
    {
        Task<Models.TextMessage> PersistInboundAsync(int moduleId, IFormCollection form);
        Task<Models.TextMessage> SendFromWidgetAsync(int moduleId, Models.ChatSendRequest request);
        Task<List<Models.ChatMessageDto>> GetMessagesAsync(int moduleId, string conversationId, DateTime? sinceUtc);
    }

    public class TextMessageService : ITextMessageService, ITransientService
    {
        private const string AccountSidSettingName = "TwilioAccountSid";
        private const string AuthTokenSettingName = "TwilioAuthToken";
        private const string PhoneNumberSettingName = "TwilioPhoneNumber";
        private const string SendToPhoneNumberSettingName = "SendToPhoneNumber";

        private readonly ITextMessageRepository _textMessageRepository;
        private readonly ISettingRepository _settingRepository;

        public TextMessageService(ITextMessageRepository textMessageRepository, ISettingRepository settingRepository)
        {
            _textMessageRepository = textMessageRepository;
            _settingRepository = settingRepository;
        }

        public Task<Models.TextMessage> PersistInboundAsync(int moduleId, IFormCollection form)
        {
            var now = DateTime.UtcNow;
            var status = GetValue(form, "SmsStatus", "MessageStatus", "MessageStatus");
            if (string.IsNullOrEmpty(status))
            {
                status = "Received";
            }

            var message = new Models.TextMessage
            {
                ModuleId = moduleId,
                TwilioMessageSid = GetValue(form, "MessageSid", "SmsMessageSid"),
                ConversationId = GetValue(form, "ConversationId"),
                Direction = "Inbound",
                SenderNumber = GetValue(form, "From"),
                RecipientNumber = GetValue(form, "To"),
                Body = GetValue(form, "Body"),
                Status = status,
                ErrorCode = GetValue(form, "ErrorCode"),
                CreatedBy = "Webhook",
                CreatedOn = now,
                ModifiedBy = "Webhook",
                ModifiedOn = now
            };

            var mediaItems = new List<Models.TextMedia>();
            if (int.TryParse(GetValue(form, "NumMedia"), out var numMedia) && numMedia > 0)
            {
                for (var i = 0; i < numMedia; i++)
                {
                    var mediaUrl = GetValue(form, $"MediaUrl{i}");
                    if (string.IsNullOrWhiteSpace(mediaUrl))
                    {
                        continue;
                    }

                    mediaItems.Add(new Models.TextMedia
                    {
                        MediaUrl = mediaUrl,
                        ContentType = GetValue(form, $"MediaContentType{i}") ?? "application/octet-stream",
                        CreatedOn = now
                    });
                }
            }

            return Task.FromResult(_textMessageRepository.AddTextMessage(message, mediaItems));
        }

        public async Task<Models.TextMessage> SendFromWidgetAsync(int moduleId, Models.ChatSendRequest request)
        {
            var accountSid = _settingRepository.GetSettingValue(EntityNames.Module, moduleId, AccountSidSettingName, "");
            var authToken = _settingRepository.GetSettingValue(EntityNames.Module, moduleId, AuthTokenSettingName, "");
            var fromPhoneNumber = _settingRepository.GetSettingValue(EntityNames.Module, moduleId, PhoneNumberSettingName, "");
            var sendToPhoneNumber = _settingRepository.GetSettingValue(EntityNames.Module, moduleId, SendToPhoneNumberSettingName, "");
            if (string.IsNullOrWhiteSpace(sendToPhoneNumber))
            {
                sendToPhoneNumber = fromPhoneNumber;
            }

            if (string.IsNullOrWhiteSpace(accountSid))
            {
                throw new InvalidOperationException("Missing TwilioAccountSid setting.");
            }

            if (string.IsNullOrWhiteSpace(authToken))
            {
                throw new InvalidOperationException("Missing TwilioAuthToken setting.");
            }

            if (string.IsNullOrWhiteSpace(fromPhoneNumber))
            {
                throw new InvalidOperationException("Missing TwilioPhoneNumber setting.");
            }

            if (string.IsNullOrWhiteSpace(sendToPhoneNumber))
            {
                throw new InvalidOperationException("Missing TwilioPhoneNumber setting.");
            }

            TwilioClient.Init(accountSid, authToken);

            var body = $"[{request.Name}] {(string.IsNullOrWhiteSpace(request.VisitorPhoneNumber) ? string.Empty : $"({request.VisitorPhoneNumber}) ")}{request.Message}";
            MessageResource sent;
            try
            {
                var options = new CreateMessageOptions(new PhoneNumber(sendToPhoneNumber))
                {
                    From = new PhoneNumber(fromPhoneNumber),
                    Body = body
                };

                sent = await MessageResource.CreateAsync(options);
            }
            catch (ApiException ex)
            {
                throw new InvalidOperationException($"Twilio send failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Twilio request failed: {ex.Message}");
            }

            var now = DateTime.UtcNow;
            var message = new Models.TextMessage
            {
                ModuleId = moduleId,
                TwilioMessageSid = sent.Sid,
                ConversationId = string.IsNullOrWhiteSpace(request.ConversationId) ? Guid.NewGuid().ToString("N") : request.ConversationId,
                Direction = "Outbound",
                SenderNumber = fromPhoneNumber,
                RecipientNumber = sendToPhoneNumber,
                Body = body,
                Status = sent.Status?.ToString() ?? "Queued",
                ErrorCode = sent.ErrorCode?.ToString(),
                CreatedBy = request.Name,
                CreatedOn = now,
                ModifiedBy = request.Name,
                ModifiedOn = now
            };

            try
            {
                return _textMessageRepository.AddTextMessage(message, null);
            }
            catch
            {
                return message;
            }
        }

        public Task<List<Models.ChatMessageDto>> GetMessagesAsync(int moduleId, string conversationId, DateTime? sinceUtc)
        {
            var messages = _textMessageRepository.GetMessages(moduleId, conversationId, sinceUtc)
                .Select(item => new Models.ChatMessageDto
                {
                    MessageId = item.MessageId,
                    Direction = item.Direction,
                    Body = item.Body,
                    SenderNumber = item.SenderNumber,
                    RecipientNumber = item.RecipientNumber,
                    Status = item.Status,
                    ConversationId = item.ConversationId,
                    CreatedOn = item.CreatedOn
                })
                .ToList();

            return Task.FromResult(messages);
        }

        private static string GetValue(IFormCollection form, params string[] keys)
        {
            return keys
                .Select(key => form.TryGetValue(key, out var value) ? value.ToString() : null)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }
    }
}
