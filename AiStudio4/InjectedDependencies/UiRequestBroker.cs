using AiTool3.AiServices;
using AiTool3.Conversations;
using AiTool3.DataModels;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AiStudio4.InjectedDependencies
{
    public class UiRequestBroker
    {
        private readonly IConfiguration _configuration;
        private readonly SettingsManager _settingsManager;
        private readonly WebSocketServer _webSocketServer;
        private readonly ChatManager _chatManager;

        public UiRequestBroker(IConfiguration configuration, SettingsManager settingsManager, WebSocketServer webSocketServer, ChatManager chatManager)
        {
            _configuration = configuration;
            _settingsManager = settingsManager;
            _webSocketServer = webSocketServer;
            _chatManager = chatManager;
        }

        public async Task<string> HandleRequestAsync(string clientId, string requestType, string requestData)
        {
            var requestObject = JsonConvert.DeserializeObject<JObject>(requestData);

           switch (requestType)
            {
                case "getAllHistoricalConversationTrees":
                    try
                    {
                        return await _chatManager.HandleGetAllHistoricalConversationTreesRequest(clientId, requestObject);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing getAllHistoricalConversationTrees request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }
                
                case "getModels":
                    try
                    {
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            models = _settingsManager.CurrentSettings.ModelList
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing getModels request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }
                
                case "getServiceProviders":
                    try
                    {
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            providers = _settingsManager.CurrentSettings.ServiceProviders
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing getServiceProviders request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }
                case "conversationmessages":
                    try
                    {
                        return await _chatManager.HandleConversationMessagesRequest(clientId, requestObject);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing conversation messages request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }
                case "historicalConversationTree":
                    try
                    {
                        return await _chatManager.HandleHistoricalConversationTreeRequest(clientId, requestObject);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing historical conversation tree request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }

                case "chat":
                    try
                    {
                        return await _chatManager.HandleChatRequest(clientId, requestObject);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing chat request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }

                case "getConfig":
                    try
                    {
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            models = _settingsManager.CurrentSettings.ModelList.Select(x => x.ModelName).ToArray(),
                            defaultModel = _settingsManager.DefaultSettings?.DefaultModel ?? "",
                            secondaryModel = _settingsManager.DefaultSettings?.SecondaryModel ?? ""
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($@"Error processing config request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }
                    
                case "setDefaultModel":
                    try
                    {
                        var modelName = requestObject["modelName"]?.ToString();
                        if (string.IsNullOrEmpty(modelName))
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                success = false,
                                error = "Model name cannot be empty"
                            });
                        }
                        
                        _settingsManager.UpdateDefaultModel(modelName);
                        
                        return JsonConvert.SerializeObject(new
                        {
                            success = true
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($@"Error processing setDefaultModel request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }
                    
                case "setSecondaryModel":
                    try
                    {
                        var modelName = requestObject["modelName"]?.ToString();
                        if (string.IsNullOrEmpty(modelName))
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                success = false,
                                error = "Model name cannot be empty"
                            });
                        }
                        
                        _settingsManager.UpdateSecondaryModel(modelName);
                        
                        return JsonConvert.SerializeObject(new
                        {
                            success = true
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($@"Error processing setSecondaryModel request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }
                case "addModel":
                    try
                    {
                        var model = requestObject.ToObject<SharedClasses.Providers.Model>();
                        if (model == null)
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                success = false,
                                error = "Invalid model data"
                            });
                        }
                        
                        _settingsManager.AddModel(model);
                        
                        return JsonConvert.SerializeObject(new
                        {
                            success = true
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing addModel request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }
                    
                case "updateModel":
                    try
                    {
                        var model = requestObject.ToObject<SharedClasses.Providers.Model>();
                        if (model == null || string.IsNullOrEmpty(model.Guid))
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                success = false,
                                error = "Invalid model data or missing model ID"
                            });
                        }
                        
                        _settingsManager.UpdateModel(model);
                        
                        return JsonConvert.SerializeObject(new
                        {
                            success = true
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing updateModel request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }
                    
                case "deleteModel":
                    try
                    {
                        var modelGuid = requestObject["modelGuid"]?.ToString();
                        if (string.IsNullOrEmpty(modelGuid))
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                success = false,
                                error = "Model ID cannot be empty"
                            });
                        }
                        
                        _settingsManager.DeleteModel(modelGuid);
                        
                        return JsonConvert.SerializeObject(new
                        {
                            success = true
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing deleteModel request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }
                    
                case "addServiceProvider":
                    try
                    {
                        var provider = requestObject.ToObject<SharedClasses.Providers.ServiceProvider>();
                        if (provider == null)
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                success = false,
                                error = "Invalid service provider data"
                            });
                        }
                        
                        _settingsManager.AddServiceProvider(provider);
                        
                        return JsonConvert.SerializeObject(new
                        {
                            success = true
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing addServiceProvider request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }
                    
                case "updateServiceProvider":
                    try
                    {
                        var provider = requestObject.ToObject<SharedClasses.Providers.ServiceProvider>();
                        if (provider == null || string.IsNullOrEmpty(provider.Guid))
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                success = false,
                                error = "Invalid provider data or missing provider ID"
                            });
                        }
                        
                        _settingsManager.UpdateServiceProvider(provider);
                        
                        return JsonConvert.SerializeObject(new
                        {
                            success = true
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing updateServiceProvider request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }
                    
                case "deleteServiceProvider":
                    try
                    {
                        var providerGuid = requestObject["providerGuid"]?.ToString();
                        if (string.IsNullOrEmpty(providerGuid))
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                success = false,
                                error = "Provider ID cannot be empty"
                            });
                        }
                        
                        _settingsManager.DeleteServiceProvider(providerGuid);
                        
                        return JsonConvert.SerializeObject(new
                        {
                            success = true
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing deleteServiceProvider request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }
                    
                default:
                    throw new NotImplementedException();
            }
        }
    }
}