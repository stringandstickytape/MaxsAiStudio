// InjectedDependencies/AppearanceSettingsDictionary.cs
using System.Collections.Generic;

namespace AiStudio4.InjectedDependencies
{
    public class AppearanceSettingsDictionary
    {
        public Dictionary<string, AppearanceSettings> UserAppearanceSettings { get; set; } = new();
    }
}