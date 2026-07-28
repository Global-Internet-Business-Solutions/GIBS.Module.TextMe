using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using Oqtane.Modules;

namespace GIBS.Module.TextMe.Repository
{
    public interface ITextMeRepository
    {
        IEnumerable<Models.TextMe> GetTextMes(int ModuleId);
        Models.TextMe GetTextMe(int TextMeId);
        Models.TextMe GetTextMe(int TextMeId, bool tracking);
        Models.TextMe AddTextMe(Models.TextMe TextMe);
        Models.TextMe UpdateTextMe(Models.TextMe TextMe);
        void DeleteTextMe(int TextMeId);
    }

    public class TextMeRepository : ITextMeRepository, ITransientService
    {
        private readonly IDbContextFactory<TextMeContext> _factory;

        public TextMeRepository(IDbContextFactory<TextMeContext> factory)
        {
            _factory = factory;
        }

        public IEnumerable<Models.TextMe> GetTextMes(int ModuleId)
        {
            using var db = _factory.CreateDbContext();
            return db.TextMe.Where(item => item.ModuleId == ModuleId).ToList();
        }

        public Models.TextMe GetTextMe(int TextMeId)
        {
            return GetTextMe(TextMeId, true);
        }

        public Models.TextMe GetTextMe(int TextMeId, bool tracking)
        {
            using var db = _factory.CreateDbContext();
            if (tracking)
            {
                return db.TextMe.Find(TextMeId);
            }
            else
            {
                return db.TextMe.AsNoTracking().FirstOrDefault(item => item.TextMeId == TextMeId);
            }
        }

        public Models.TextMe AddTextMe(Models.TextMe TextMe)
        {
            using var db = _factory.CreateDbContext();
            db.TextMe.Add(TextMe);
            db.SaveChanges();
            return TextMe;
        }

        public Models.TextMe UpdateTextMe(Models.TextMe TextMe)
        {
            using var db = _factory.CreateDbContext();
            db.Entry(TextMe).State = EntityState.Modified;
            db.SaveChanges();
            return TextMe;
        }

        public void DeleteTextMe(int TextMeId)
        {
            using var db = _factory.CreateDbContext();
            Models.TextMe TextMe = db.TextMe.Find(TextMeId);
            db.TextMe.Remove(TextMe);
            db.SaveChanges();
        }
    }
}
