using System.Text;

namespace AiTool3.ExtensionMethods
{
    public static class StringBuilderExtensions
    {
        public static StringBuilder AppendMany(this StringBuilder sb, params string[] thingsToAppend)
        {
            if (sb == null)
            {
                throw new ArgumentNullException(nameof(sb));
            }

            if (thingsToAppend != null)
            {
                foreach (string item in thingsToAppend)
                {
                    sb.Append(item);
                }
            }

            return sb;
        }
    }
}
