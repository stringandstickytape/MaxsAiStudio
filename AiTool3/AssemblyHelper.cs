using System.Reflection;

namespace AiTool3
{
    public static class AssemblyHelper
    {

        public static string GetEmbeddedAssembly(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(resourceName);

            string result = "";
            using (StreamReader reader = new StreamReader(stream))
            {
                result = reader.ReadToEnd();
            }

            return result;
        }
    }
}