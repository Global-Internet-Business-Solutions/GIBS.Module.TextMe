using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Oqtane.Enums;
using Oqtane.Infrastructure;
using Oqtane.Models;
using Oqtane.Security;
using Oqtane.Shared;
using GIBS.Module.TextMe.Repository;

namespace GIBS.Module.TextMe.Services
{
    public class ServerTextMeService : ITextMeService
    {
        private readonly ITextMeRepository _TextMeRepository;
        private readonly IUserPermissions _userPermissions;
        private readonly ILogManager _logger;
        private readonly IHttpContextAccessor _accessor;
        private readonly Alias _alias;

        public ServerTextMeService(ITextMeRepository TextMeRepository, IUserPermissions userPermissions, ITenantManager tenantManager, ILogManager logger, IHttpContextAccessor accessor)
        {
            _TextMeRepository = TextMeRepository;
            _userPermissions = userPermissions;
            _logger = logger;
            _accessor = accessor;
            _alias = tenantManager.GetAlias();
        }

        public Task<List<Models.TextMe>> GetTextMesAsync(int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.View))
            {
                return Task.FromResult(_TextMeRepository.GetTextMes(ModuleId).ToList());
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized TextMe Get Attempt {ModuleId}", ModuleId);
                return null;
            }
        }

        public Task<Models.TextMe> GetTextMeAsync(int TextMeId, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.View))
            {
                return Task.FromResult(_TextMeRepository.GetTextMe(TextMeId));
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized TextMe Get Attempt {TextMeId} {ModuleId}", TextMeId, ModuleId);
                return null;
            }
        }

        public Task<Models.TextMe> AddTextMeAsync(Models.TextMe TextMe)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, TextMe.ModuleId, PermissionNames.Edit))
            {
                TextMe = _TextMeRepository.AddTextMe(TextMe);
                _logger.Log(LogLevel.Information, this, LogFunction.Create, "TextMe Added {TextMe}", TextMe);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized TextMe Add Attempt {TextMe}", TextMe);
                TextMe = null;
            }
            return Task.FromResult(TextMe);
        }

        public Task<Models.TextMe> UpdateTextMeAsync(Models.TextMe TextMe)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, TextMe.ModuleId, PermissionNames.Edit))
            {
                TextMe = _TextMeRepository.UpdateTextMe(TextMe);
                _logger.Log(LogLevel.Information, this, LogFunction.Update, "TextMe Updated {TextMe}", TextMe);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized TextMe Update Attempt {TextMe}", TextMe);
                TextMe = null;
            }
            return Task.FromResult(TextMe);
        }

        public Task DeleteTextMeAsync(int TextMeId, int ModuleId)
        {
            if (_userPermissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, ModuleId, PermissionNames.Edit))
            {
                _TextMeRepository.DeleteTextMe(TextMeId);
                _logger.Log(LogLevel.Information, this, LogFunction.Delete, "TextMe Deleted {TextMeId}", TextMeId);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized TextMe Delete Attempt {TextMeId} {ModuleId}", TextMeId, ModuleId);
            }
            return Task.CompletedTask;
        }
    }
}
