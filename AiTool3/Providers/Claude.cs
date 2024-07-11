using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Interfaces;
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Windows.Forms.Design.AxImporter;
using System.Net;

namespace AiTool3.Providers
{
    internal class Claude : IAiService
    {
        HttpClient client = new HttpClient();

        bool clientInitialised = false;

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, Control textbox = null, bool useStreaming = false)
        {
            if (!clientInitialised)
            {
                client.DefaultRequestHeaders.Add("x-api-key", apiModel.Key);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                clientInitialised = true;
            }

            var req = new JObject
            {
                ["model"] = apiModel.ModelName,
                ["system"] = conversation.SystemPromptWithDateTime(),
                ["max_tokens"] = 4096,
                ["messages"] = new JArray(
                    conversation.messages.Select(m => new JObject
                    {
                        ["role"] = m.role,
                        ["content"] = new JArray(
                              new JObject
                              {
                                  ["type"] = "text",
                                  ["text"] = m.content
                              }
                            )
                    }
                    )
                    )
            };
            
            if (!string.IsNullOrWhiteSpace(base64image))
            {
                // add the base64 image to a new item inside content
                var imageContent = new JObject
                {
                    ["type"] = "image",
                    ["source"] = new JObject
                    {
                        ["type"] = "base64",
                        ["media_type"] = base64ImageType,
                        ["data"] = base64image
                    }
                };

                // add the image content to the last message
                req["messages"].Last["content"].Last.AddAfterSelf(imageContent);



            }

            var json = JsonConvert.SerializeObject(req);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(apiModel.Url, content, cancellationToken).ConfigureAwait(false);

            var stream = await response.Content.ReadAsStreamAsync();
            var buffer = new byte[256];
            var bytesRead = 0;

            StringBuilder sb = new StringBuilder();

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                var chunk = new byte[bytesRead];
                Array.Copy(buffer, chunk, bytesRead);
                var chunkTxt = Encoding.UTF8.GetString(chunk);

                sb.Append(chunkTxt);
                Debug.WriteLine(chunkTxt);
            }
            var allTxt = sb.ToString();

            // deserialize the response

            var completion = JsonConvert.DeserializeObject<JObject>(allTxt);

            // get the number of input and output tokens but don't b0rk if either is missing
            var inputTokens = completion["usage"]?["input_tokens"]?.ToString();
            var outputTokens = completion["usage"]?["output_tokens"]?.ToString();

            if (completion["type"].ToString() == "error")
            {
                return new AiResponse { ResponseText = "error - " + completion["error"]["message"].ToString(), Success = false };
            }

            var responseText = completion["content"][0]["text"].ToString();

            // use a regex to extract the first json object
            var regex = new System.Text.RegularExpressions.Regex(@"\{.*\}");
            var match = regex.Match(responseText);

            string suggestedNextPrompt = null;

            if (false && match.Success) 
            {
                // get the matched text
                string jsonText = match.Value;
                if (!string.IsNullOrWhiteSpace(jsonText))
                {

                    // {{pull:['https://api.github.com/repos/stringandstickytape/MaxsAitool/commits/main', 'https://raw.githubusercontent.com/stringandstickytape/MaxsAitool/main/README.md']}}

                    // deserialize the json dynamic
                    dynamic pullReqJson = JsonConvert.DeserializeObject(jsonText);

                    // get the pull array, if there is one
                    var pullArray = pullReqJson.pull;

                    // convert to array of strings
                    var pullUrls = new List<string>();
                    if (pullArray != null)
                    {
                        foreach (var item in pullArray)
                        {
                            pullUrls.Add(item.ToString());
                        }
                    }

                    // download all the urls as strings, adn concat them with ```url above and below
                    var pullText = new StringBuilder();
                    foreach (var url in pullUrls)
                    {
                        Debug.WriteLine(url);
                        // download url using httpclient

                        string urlContent = "";
                        string body = "";
                        try
                        {
                            urlContent = client.GetStringAsync(url.ToString()).Result;
                            // parse the urlContent, get all the text fragments and turn them into a string using XmlDocument

                            // load into html doc
                            var parsedContent = new HtmlAgilityPack.HtmlDocument();

                            // get all the text nodes and concat them on separate lines
                            var lines = new List<string>(); 
                            urlContent = urlContent.Replace("<!DOCTYPE html>", "");
                            parsedContent.LoadHtml(urlContent);
                            // remove all scripts and that
                            foreach (var script in parsedContent.DocumentNode.DescendantsAndSelf("script").ToArray())
                            {
                                script.Remove();
                            }
                            // remove all css
                            foreach (var style in parsedContent.DocumentNode.DescendantsAndSelf("style").ToArray())
                            {
                                style.Remove();
                            }
                            foreach (var node in parsedContent.DocumentNode.DescendantsAndSelf())
                            {
                                if (!node.HasChildNodes)
                                {
                                    lines.Add(node.InnerText);
                                }
                            }

                            // remove all blank lines
                            lines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

                            body = string.Join("\n", lines);

                            // remove <!DOCTYPE html> prefix
                            

                            // use a regex to get the body
                            //body = new System.Text.RegularExpressions.Regex(@"<body.*?>(.*?)</body>", System.Text.RegularExpressions.RegexOptions.Singleline).Match(urlContent).ToString();
                            // load into xml doc
                            //parsedContent.LoadXml(body);
                        }
                        catch (Exception ex)
                        {
                            body = ex.Message;
                        }




                        pullText.AppendLine($"```{url}\n{body}\n```\n");
                    }

                    suggestedNextPrompt = pullText.ToString();

                    if(suggestedNextPrompt.Length > 100000)
                        {
                        suggestedNextPrompt = suggestedNextPrompt.Substring(0, 100000);
                    }
                }
            }

            return new AiResponse { SuggestedNextPrompt = suggestedNextPrompt , ResponseText = completion["content"][0]["text"].ToString(), Success = true, TokenUsage = new TokenUsage(inputTokens, outputTokens) };
        }
    }
}