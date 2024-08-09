using Newtonsoft.Json;

namespace AiTool3
{
    public class ScratchpadManager
    {
        private const string ScratchpadPath = "Settings\\Scratchpad.json";
        private const string ScratchpadBackupPath = "Settings\\Scratchpad.json.bak";

        public void SaveScratchpad(string content)
        {
            if (string.IsNullOrEmpty(content))
                return;

            var json = JsonConvert.SerializeObject(content);

            if (File.Exists(ScratchpadBackupPath))
            {
                File.Delete(ScratchpadBackupPath);
            }

            if (File.Exists(ScratchpadPath))
            {
                File.Move(ScratchpadPath, ScratchpadBackupPath);
            }

            File.WriteAllText(ScratchpadPath, json);
        }

        public string LoadScratchpad()
        {
            if (File.Exists(ScratchpadBackupPath) && !File.Exists(ScratchpadPath))
            {
                File.Move(ScratchpadBackupPath, ScratchpadPath);
            }

            if (File.Exists(ScratchpadPath))
            {
                var scratchpadContent = File.ReadAllText(ScratchpadPath);
                return scratchpadContent;
            }

            return "";
        }
    }
}