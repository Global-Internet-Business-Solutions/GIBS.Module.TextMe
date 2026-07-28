using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Oqtane.Shared;
using Oqtane.Enums;
using Oqtane.Infrastructure;
using GIBS.Module.TextMe.Services;
using Oqtane.Controllers;
using System.Net;
using System.Threading.Tasks;

namespace GIBS.Module.TextMe.Controllers
{
    [Route(ControllerRoutes.ApiRoute)]
    public class TextMeController : ModuleControllerBase
    {
        private readonly ITextMeService _TextMeService;

        public TextMeController(ITextMeService TextMeService, ILogManager logger, IHttpContextAccessor accessor) : base(logger, accessor)
        {
            _TextMeService = TextMeService;
        }

        // GET: api/<controller>?moduleid=x
        [HttpGet]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public async Task<IEnumerable<Models.TextMe>> Get(string moduleid)
        {
            int ModuleId;
            if (int.TryParse(moduleid, out ModuleId) && IsAuthorizedEntityId(EntityNames.Module, ModuleId))
            {
                return await _TextMeService.GetTextMesAsync(ModuleId);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized TextMe Get Attempt {ModuleId}", moduleid);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }
        }

        // GET api/<controller>/5
        [HttpGet("{id}/{moduleid}")]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public async Task<Models.TextMe> Get(int id, int moduleid)
        {
            Models.TextMe TextMe = await _TextMeService.GetTextMeAsync(id, moduleid);
            if (TextMe != null && IsAuthorizedEntityId(EntityNames.Module, TextMe.ModuleId))
            {
                return TextMe;
            }
            else
            { 
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized TextMe Get Attempt {TextMeId} {ModuleId}", id, moduleid);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }
        }

        // POST api/<controller>
        [HttpPost]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task<Models.TextMe> Post([FromBody] Models.TextMe TextMe)
        {
            if (ModelState.IsValid && IsAuthorizedEntityId(EntityNames.Module, TextMe.ModuleId))
            {
                TextMe = await _TextMeService.AddTextMeAsync(TextMe);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized TextMe Post Attempt {TextMe}", TextMe);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                TextMe = null;
            }
            return TextMe;
        }

        // PUT api/<controller>/5
        [HttpPut("{id}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task<Models.TextMe> Put(int id, [FromBody] Models.TextMe TextMe)
        {
            if (ModelState.IsValid && TextMe.TextMeId == id && IsAuthorizedEntityId(EntityNames.Module, TextMe.ModuleId))
            {
                TextMe = await _TextMeService.UpdateTextMeAsync(TextMe);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized TextMe Put Attempt {TextMe}", TextMe);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                TextMe = null;
            }
            return TextMe;
        }

        // DELETE api/<controller>/5
        [HttpDelete("{id}/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task Delete(int id, int moduleid)
        {
            Models.TextMe TextMe = await _TextMeService.GetTextMeAsync(id, moduleid);
            if (TextMe != null && IsAuthorizedEntityId(EntityNames.Module, TextMe.ModuleId))
            {
                await _TextMeService.DeleteTextMeAsync(id, TextMe.ModuleId);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized TextMe Delete Attempt {TextMeId} {ModuleId}", id, moduleid);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            }
        }
    }
}
