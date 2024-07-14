using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace AiTool3
{
    internal static class EmbeddingsHelper
    {

        public static async Task<List<Embedding>> CreateEmbeddingsAsync(List<string> texts, string apiKey, string apiUrl = "https://api.openai.com/v1/embeddings")
        {
            // analyse our own root dll
           //var dllPath = Assembly.GetEntryAssembly().Location;
           //var analysis = AnalyzeDll(dllPath);
            



            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var request = new
            {
                model = "text-embedding-3-large", // OpenAI's default embedding model
                input = texts
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            HttpResponseMessage? response = null;
            try
            {
                response = await client.PostAsync(apiUrl, content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                // Handle or log the exception
                Console.WriteLine($"An error occurred: {e.Message}");
                return new List<Embedding>();
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(responseBody);
            JsonElement root = doc.RootElement;

            var embeddings = new List<Embedding>();
            JsonElement dataArray = root.GetProperty("data");

            foreach (JsonElement dataElement in dataArray.EnumerateArray())
            {
                JsonElement embeddingArray = dataElement.GetProperty("embedding");
                var embedding = new List<float>();

                foreach (JsonElement value in embeddingArray.EnumerateArray())
                {
                    embedding.Add(value.GetSingle());
                }

                embeddings.Add(new Embedding { Value = embedding, Code = "" });
            }

            return embeddings;
        }

        public static string AnalyzeDll(string dllPath)
        {
            var assembly = Assembly.LoadFrom(dllPath);
            var sb = new StringBuilder();

            sb.AppendLine($"Analysis of {assembly.GetName().Name}");
            sb.AppendLine(new string('=', 50));
            sb.AppendLine();

            var namespaces = assembly.GetTypes()
                .Select(t => t.Namespace)
                .Distinct()
                .Where(n => !string.IsNullOrEmpty(n))
                .OrderBy(n => n);

            foreach (var ns in namespaces)
            {
                sb.AppendLine($"Namespace: {ns}");
                sb.AppendLine(new string('-', 30));

                var types = assembly.GetTypes()
                    .Where(t => t.Namespace == ns)
                    .OrderBy(t => t.Name);

                foreach (var type in types)
                {
                    sb.AppendLine($"  Class: {type.Name}");

                    // Try to get the source file name
                    var fileName = GetSourceFileName(type);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        sb.AppendLine($"    Source File: {fileName}");
                    }

                    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                        .OrderBy(p => p.Name);

                    foreach (var prop in properties)
                    {
                        sb.AppendLine($"    Property: {prop.PropertyType.Name} {prop.Name}");
                    }

                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                        .Where(m => !m.IsSpecialName) // Exclude property accessor methods
                        .OrderBy(m => m.Name);

                    foreach (var method in methods)
                    {
                        var parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        sb.AppendLine($"    Method: {method.ReturnType.Name} {method.Name}({parameters})");
                    }

                    sb.AppendLine();
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string GetSourceFileName(Type type)
        {
            var attributes = type.GetCustomAttributes(typeof(DebuggerDisplayAttribute), false);
            if (attributes.Length > 0)
            {
                var debuggerDisplay = (DebuggerDisplayAttribute)attributes[0];
                return debuggerDisplay.Value;
            }

            // If DebuggerDisplay attribute is not available, try to get it from a method
            var method = type.GetMethods().FirstOrDefault();
            if (method != null)
            {
                try
                {
                    var fileName = method.GetMethodBody()?.LocalVariables.FirstOrDefault()?.ToString();
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        return System.IO.Path.GetFileName(fileName);
                    }
                }
                catch
                {
                    // Ignore any exceptions and return empty string
                }
            }

            return string.Empty;
        }

    }


}