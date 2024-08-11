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
            // if there isn't a scratchpad file but there is a scratchpad bak file, rename the bak file to scratchpad.json
            if (File.Exists(Path.Combine("Settings", "Scratchpad.json.bak")) && !File.Exists(Path.Combine("Settings", "Scratchpad.json")))
            {
                File.Move(Path.Combine("Settings", "Scratchpad.json.bak"), Path.Combine("Settings", "Scratchpad.json"));
            }

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