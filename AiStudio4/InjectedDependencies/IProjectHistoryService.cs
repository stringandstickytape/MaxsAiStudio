// InjectedDependencies/IProjectHistoryService.cs


namespace AiStudio4.InjectedDependencies
{
    public interface IProjectHistoryService
    {
        List<string> GetProjectPathHistory();
        void AddProjectPathToHistory(string path);
        void LoadSettings();
        void SaveSettings();
    }
}
