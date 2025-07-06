using AiStudio4.Core.Models;

namespace AiStudio4.Core.Interfaces
{
    public interface ITipOfTheDayService
    {
        TipOfTheDaySettings GetSettings();
        void UpdateSettings(TipOfTheDaySettings settings);
        void LoadSettings();
        void SaveSettings();
    }
}