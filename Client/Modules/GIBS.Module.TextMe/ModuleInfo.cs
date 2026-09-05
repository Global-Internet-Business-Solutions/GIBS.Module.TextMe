using Oqtane.Models;
using Oqtane.Modules;

namespace GIBS.Module.TextMe
{
    public class ModuleInfo : IModule
    {
        public ModuleDefinition ModuleDefinition => new ModuleDefinition
        {
            Name = "TextMe",
            Description = "Twilio TextMe Module for Oqtane",
            Version = "1.0.2",
            ServerManagerType = "GIBS.Module.TextMe.Manager.TextMeManager, GIBS.Module.TextMe.Server.Oqtane",
            ReleaseVersions = "1.0.0,1.0.1,1.0.2",
            Dependencies = "GIBS.Module.TextMe.Shared.Oqtane",
            PackageName = "GIBS.Module.TextMe" 
        };
    }
}
