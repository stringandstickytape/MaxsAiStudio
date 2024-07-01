using System.Text.RegularExpressions;

namespace AiTool3.Conversations
{
    public class ConversationMessage
    {
        public string role { get; set; }
        public string content { get; set; }

        public string contentDisplay
        {
            get
            {
                var retVal = content.Replace("\r", " ").Replace("\n", " ");

                // in retVal, where there is more than one space, replace with a single space
                retVal = Regex.Replace(retVal, @"\s+", " ");

                // shorten to 100 chars with ellipsis if necessary
                if (retVal.Length > 100)
                {
                    retVal = retVal.Substring(0, 100) + "...";
                }
                return retVal;
            }
        }
    }
}