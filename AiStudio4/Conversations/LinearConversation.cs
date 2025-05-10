

using AiStudio4.DataModels;

namespace AiStudio4.Convs
{
    public class LinearConv
    {
        public List<LinearConvMessage> messages { get; set; }
        public string systemprompt { get; set; }
        public DateTime ConvCreationDateTime { get; set; }
        public LinearConv(DateTime creationDateTime)
        {
            ConvCreationDateTime = creationDateTime;

        }
        public string SystemPromptWithDateTime()
        {
            //return $"{systemprompt}\r\n\r\nThis conv began at {ConvCreationDateTime.ToString("yyyy-MM-dd HH:mm:ss")}.";
            return systemprompt;
        }
    }
}