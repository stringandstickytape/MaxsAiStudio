﻿using AiTool3.DataModels;
using AiTool3.ExtensionMethods;
using AiTool3.FileAttachments;
using AiTool3.Helpers;
using AiTool3.AiServices;
using AiTool3.Tools;
using AiTool3.UI;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using SharedClasses.Providers;
using SharedClasses.Helpers;

namespace AiTool3.Conversations
{
    public class AiResponseHandler
    {
        private readonly ConversationManager _conversationManager;
        private ChatWebView _chatWebView;
        private readonly ToolManager _toolManager;
        private readonly FileAttachmentManager _fileAttachmentManager;
        private WebViewManager _webViewManager;

        public AiResponseHandler(ConversationManager conversationManager, ToolManager toolManager, FileAttachmentManager fileAttachmentManager)
        {
            _conversationManager = conversationManager;
            _toolManager = toolManager;
            _fileAttachmentManager = fileAttachmentManager;
        }

        public async Task<string> FetchAiInputResponse(SettingsSet currentSettings, CancellationToken cancellationToken,
                                                       List<string> toolIDs = null, string? overrideUserPrompt = null,
                                                       bool sendSecondary = false, bool addEmbeddings = false,
                                                       Action<AiResponse> updateUiMethod = null,
                                                       string prefill = null
            )
        {
            toolIDs = toolIDs ?? new List<string>();
            string retVal = "";
            try
            {
                var model = sendSecondary ? await _chatWebView.GetDropdownModel("summaryAI", currentSettings) : await _chatWebView.GetDropdownModel("mainAI", currentSettings);

                var userPrompt = overrideUserPrompt == null ? await _chatWebView.GetUserPrompt() : overrideUserPrompt;

                if (currentSettings.AllowUserPromptUrlPulls && userPrompt != null)
                {
                    var matches = Regex.Matches(userPrompt, @"\[pull:(.*?)\]");
                    foreach (Match match in matches)
                    {
                        var url = match.Groups[1].Value;
                        var extractedText = await HtmlTextExtractor.ExtractTextFromUrlAsync(url);
                        if (extractedText != "")
                        {
                            userPrompt = userPrompt.Replace(match.Value, $"\n{MaxsAiStudio.ThreeTicks}{url}\n{extractedText}\n{MaxsAiStudio.ThreeTicks}\n");
                        }
                    }
                }

                var conversation = await _conversationManager.PrepareConversationData(model, await _chatWebView.GetSystemPrompt(), overrideUserPrompt != null ? overrideUserPrompt : userPrompt, _fileAttachmentManager);
                var response = await FetchAndProcessAiResponse(currentSettings, conversation, model, toolIDs, overrideUserPrompt, cancellationToken, prefill, addEmbeddings);
                retVal = response.ResponseText;
                await _chatWebView.SetUserPrompt("");
                await _chatWebView.DisableCancelButton();

                // dgvConversations.Enabled = true;
                _webViewManager.Enable();

                await _chatWebView.EnableSendButton();

                // stopwatch.Stop();
                // updateTimer.Stop();

                if (overrideUserPrompt == null)
                {
                    if (response.SuggestedNextPrompt != null)
                    {
                        await _chatWebView.SetUserPrompt(response.SuggestedNextPrompt);
                    }
                }

                updateUiMethod?.Invoke(response);

                await EnableUI();

                await _conversationManager.UpdateConversationSummary(currentSettings);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex is OperationCanceledException ? "Operation was cancelled." : $"An error occurred: {ex.Message}");

                _chatWebView.ClearTemp();
            }
            finally
            {
                await EnableUI();
            }
            return retVal;
        }

        private async Task EnableUI()
        {
            await _chatWebView.DisableCancelButton();

            _webViewManager.Enable();

            await _chatWebView.EnableSendButton();
        }

        public async Task<AiResponse> FetchAndProcessAiResponse(SettingsSet currentSettings, LinearConversation conversation,
                                                                 Model model, List<string> toolIDs,
                                                                 string? overrideUserPrompt,
                                                                 CancellationToken cancellationToken,
                                                                 string prefill,
                                                                 bool addEmbeddings = false)
        {
            if (addEmbeddings != currentSettings.UseEmbeddings)
            {
                currentSettings.UseEmbeddings = addEmbeddings;
                SettingsSet.Save(currentSettings);
            }

            var service = ServiceProvider.GetProviderForGuid(currentSettings.ServiceProviders, model.ProviderGuid);

            var aiService = AiServiceResolver.GetAiService(service.ServiceName, _toolManager);
            aiService.StreamingTextReceived += AiService_StreamingTextReceived;
            aiService.StreamingComplete += (s, e) => { _chatWebView.InvokeIfNeeded(() => _chatWebView.ClearTemp()); };

            toolIDs = toolIDs.Where(x => int.TryParse(x, out _)).ToList();

            var toolLabels = toolIDs.Select(t => _toolManager.Tools[int.Parse(t)].Name).ToList();

            

            if(aiService is Claude && prefill != null)
            {
                (aiService as Claude).SetOneOffPreFill(prefill);
            }
            AiResponse response = null;

            var stopwatch = Stopwatch.StartNew();

            await Task.Run(async () =>
            {
                // Create ApiSettings from currentSettings
                var apiSettings = currentSettings.ToApiSettings();
                
                response = await aiService!.FetchResponse(
                    service,
                   model,
                    conversation,
                    _fileAttachmentManager.Base64Image!,
                    _fileAttachmentManager.Base64ImageType!,
                    cancellationToken,
                    apiSettings,
                    mustNotUseEmbedding: false,
                    toolNames: toolLabels,
                    useStreaming: currentSettings.StreamResponses,
                    addEmbeddings: currentSettings.UseEmbeddings
                );
            });

            stopwatch.Stop();
            response.Duration = stopwatch.Elapsed;

            if (_toolManager != null && toolIDs.Any())
            {
                var tool = _toolManager.GetToolByLabel(toolLabels[0]);

                var sb = new StringBuilder($"{MaxsAiStudio.ThreeTicks}{tool.OutputFilename}\n");

                var firstChar = response.ResponseText.FirstOrDefault(c => !char.IsWhiteSpace(c));

                if (firstChar != '{')
                {
                    sb.Append("{");
                }

                sb.Append(response.ResponseText.Replace("\r", "").Replace("\n", " "));

                if (firstChar != '{')
                {
                    sb.Append("}");
                }

                sb.Append($"\n{MaxsAiStudio.ThreeTicks}\n");

                response.ResponseText = sb.ToString();
            }

            var modelUsageManager = new ModelUsageManager(model);

            modelUsageManager.AddTokensAndSave(response.TokenUsage);

            await ProcessAiResponse(currentSettings, response, model, conversation, overrideUserPrompt);

            return response;
        }

        private async void AiService_StreamingTextReceived(object? sender, string e)
        {
            _chatWebView.InvokeIfNeeded(() => _chatWebView.UpdateTemp(e));

            //_namedPipeListener.SendResponseAsync('s', e);
        }

        private async Task ProcessAiResponse(SettingsSet currentSettings, AiResponse response, Model model, LinearConversation conversation, string? overrideUserPrompt)
        {
            var inputText = conversation.messages.Last().content;
            var systemPrompt = await _chatWebView.GetSystemPrompt();

            CompletionMessage completionInput, completionResponse;
            _conversationManager.AddInputAndResponseToConversation(response, model, conversation, overrideUserPrompt == null ? inputText : overrideUserPrompt, systemPrompt, out completionInput, out completionResponse);

            _fileAttachmentManager.ClearBase64();

            if (overrideUserPrompt != null)
            {
                return;
            }

            if (currentSettings.NarrateResponses)
            {
                Task.Run(() => TtsHelper.ReadAloud(response.ResponseText));
            }

            await _chatWebView.AddMessage(completionInput);
            await _chatWebView.AddMessage(completionResponse);
            await WebNdcDrawNetworkDiagram(currentSettings);
            _webViewManager!.CentreOnNode(completionResponse.Guid);
        }

        private async Task WebNdcDrawNetworkDiagram(SettingsSet currentSettings)
        {
            if (_webViewManager == null || _webViewManager.webView.CoreWebView2 == null) return;

            await _webViewManager.Clear();

            var nodes = new List<D3Node>();
            foreach (var message in _conversationManager.Conversation!.Messages)
            {
                var model = WebViewManager.GetModelForModelGuid(currentSettings.ModelList, message.ModelGuid);

                var colorHex = model == null ? "#DDDDDD" : $"#{model.Color.R:X2}{model.Color.G:X2}{model.Color.B:X2}";

                if (message.Role != CompletionRole.Root)
                {
                    var node = new D3Node
                    {
                        id = message.Guid!,
                        label = message.Content!,
                        role = message.Role.ToString(),
                        colour = colorHex
                    };
                    nodes.Add(node);
                }
            }

            var links2 = _conversationManager.Conversation.Messages
                .Where(x => x.Parent != null)
                .Select(x => new Link { source = x.Parent!, target = x.Guid! }).ToList();

            await _webViewManager.EvaluateJavascriptAsync($"addNodes({JsonConvert.SerializeObject(nodes)});");
            await _webViewManager.EvaluateJavascriptAsync($"addLinks({JsonConvert.SerializeObject(links2)});");
        }

        internal void InjectDependencies(ChatWebView chatWebView, WebViewManager webViewManager)
        {
            _chatWebView = chatWebView;
            _webViewManager = webViewManager;

        }
    }
}