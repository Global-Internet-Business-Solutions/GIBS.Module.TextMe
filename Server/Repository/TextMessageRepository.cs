using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Oqtane.Modules;

namespace GIBS.Module.TextMe.Repository
{
    public interface ITextMessageRepository
    {
        Models.TextMessage AddTextMessage(Models.TextMessage message, IEnumerable<Models.TextMedia> mediaItems);
        List<Models.TextMessage> GetMessages(int moduleId, string conversationId = null, DateTime? sinceUtc = null);
    }

    public class TextMessageRepository : ITextMessageRepository, ITransientService
    {
        private readonly IDbContextFactory<TextMeContext> _factory;

        public TextMessageRepository(IDbContextFactory<TextMeContext> factory)
        {
            _factory = factory;
        }

        public Models.TextMessage AddTextMessage(Models.TextMessage message, IEnumerable<Models.TextMedia> mediaItems)
        {
            using var db = _factory.CreateDbContext();

            if (mediaItems != null)
            {
                message.MediaItems = mediaItems.ToList();
            }

            db.Messages.Add(message);
            db.SaveChanges();
            return message;
        }

        public List<Models.TextMessage> GetMessages(int moduleId, string conversationId = null, DateTime? sinceUtc = null)
        {
            using var db = _factory.CreateDbContext();
            var query = db.Messages.AsNoTracking().Where(item => item.ModuleId == moduleId);

            if (!string.IsNullOrWhiteSpace(conversationId))
            {
                query = query.Where(item => item.ConversationId == conversationId);
            }

            if (sinceUtc.HasValue)
            {
                query = query.Where(item => item.CreatedOn >= sinceUtc.Value);
            }

            return query.OrderBy(item => item.CreatedOn).ToList();
        }
    }
}
