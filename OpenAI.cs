using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("OpenAI", "Goo_", "2.0.0")]
    [Description("AI assistant using OpenAI Responses API")]
    public class OpenAI : RustPlugin
    {
        #region Constants

        private const string PermissionUse = "openai.use";
        private const string PermissionAdmin = "openai.admin";
        private const string PermissionUnlimited = "openai.unlimited";
        private const string PluginVersion = "2.0.0";

        private const int DefaultMaxOutputTokens = 2048;
        private const int DefaultCooldownSeconds = 10;
        private const int DefaultMaxRequestsPerMinute = 30;
        private const int DefaultDailyTokenBudget = 500000;
        private const int DefaultPlayerDailyTokenLimit = 15000;
        private const int DefaultMaxInputLength = 500;
        private const int DefaultMaxChunkSize = 450;

        #endregion

        #region Configuration

        private PluginConfig _config;

        private class PluginConfig
        {
            [JsonProperty("Config Version")]
            public string ConfigVersion { get; set; } = "";

            [JsonProperty("API Settings")]
            public ApiConfig Api { get; set; } = new ApiConfig();

            [JsonProperty("Rate Limits")]
            public RateLimitConfig RateLimits { get; set; } = new RateLimitConfig();

            [JsonProperty("Chat Settings")]
            public ChatConfig Chat { get; set; } = new ChatConfig();

            [JsonProperty("Discord Integration")]
            public DiscordConfig Discord { get; set; } = new DiscordConfig();

            [JsonProperty("Security Settings")]
            public SecurityConfig Security { get; set; } = new SecurityConfig();

            [JsonProperty("Prompt Settings")]
            public PromptConfig Prompt { get; set; } = new PromptConfig();

            [JsonProperty("Knowledge Base")]
            public KnowledgeConfig Knowledge { get; set; } = new KnowledgeConfig();

            [JsonProperty("Global Chat Bot")]
            public GlobalBotConfig GlobalBot { get; set; } = new GlobalBotConfig();

            [JsonProperty("Debug Mode")]
            public bool DebugMode { get; set; } = false;
        }

        private class ApiConfig
        {
            [JsonProperty("API Key")]
            public string ApiKey { get; set; } = "";

            [JsonProperty("API URL")]
            public string Url { get; set; } = "https://api.openai.com/v1/responses";

            [JsonProperty("Model")]
            public string Model { get; set; } = "gpt-5-nano";

            [JsonProperty("Max Output Tokens (0 = model default)")]
            public int MaxOutputTokens { get; set; } = DefaultMaxOutputTokens;

            [JsonProperty("Reasoning Effort (none/minimal/low/medium/high)")]
            public string ReasoningEffort { get; set; } = "low";

            [JsonProperty("Enable Web Search")]
            public bool EnableWebSearch { get; set; } = false;

            [JsonProperty("Retry Attempts")]
            public int RetryAttempts { get; set; } = 3;
        }

        private class RateLimitConfig
        {
            [JsonProperty("Cooldown Seconds")]
            public int CooldownSeconds { get; set; } = DefaultCooldownSeconds;

            [JsonProperty("Max Requests Per Minute")]
            public int MaxRequestsPerMinute { get; set; } = DefaultMaxRequestsPerMinute;

            [JsonProperty("Daily Token Budget")]
            public int DailyTokenBudget { get; set; } = DefaultDailyTokenBudget;

            [JsonProperty("Per Player Daily Token Limit")]
            public int PlayerDailyTokenLimit { get; set; } = DefaultPlayerDailyTokenLimit;

            [JsonProperty("Persist Usage Data")]
            public bool PersistUsageData { get; set; } = true;
        }

        private class ChatConfig
        {
            [JsonProperty("Command Prefix")]
            public string CommandPrefix { get; set; } = "!ai";

            [JsonProperty("Response Prefix")]
            public string ResponsePrefix { get; set; } = "[AI]";

            [JsonProperty("Response Color")]
            public string ResponseColor { get; set; } = "#55AAFF";

            [JsonProperty("Message Color")]
            public string MessageColor { get; set; } = "#FFFFFF";

            [JsonProperty("Font Size")]
            public int FontSize { get; set; } = 12;

            [JsonProperty("Max Message Chunk Size")]
            public int MaxChunkSize { get; set; } = DefaultMaxChunkSize;

            [JsonProperty("Strip URLs from Links")]
            public bool StripUrlsFromLinks { get; set; } = true;
        }

        private class DiscordConfig
        {
            [JsonProperty("Enabled")]
            public bool Enabled { get; set; } = false;

            [JsonProperty("Webhook URL")]
            public string WebhookUrl { get; set; } = "";
        }

        private class SecurityConfig
        {
            [JsonProperty("Max Input Length")]
            public int MaxInputLength { get; set; } = DefaultMaxInputLength;

            [JsonProperty("Filter Injection Attempts")]
            public bool FilterInjection { get; set; } = true;
        }

        private class PromptConfig
        {
            [JsonProperty("System Prompt")]
            public string SystemPrompt { get; set; } = "You are a helpful assistant on a Rust game server. Keep responses concise and relevant to the game Rust by Facepunch Studios and this server specifically.";

            [JsonProperty("Include Server Info")]
            public bool IncludeServerInfo { get; set; } = true;

            [JsonProperty("Include Player Names")]
            public bool IncludePlayerNames { get; set; } = true;

            [JsonProperty("Custom Instructions", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> CustomInstructions { get; set; } = new List<string>
            {
                "You are a text-only assistant. You cannot see the game world, player locations, inventories, or any live server data.",
                "You can only answer questions based on your knowledge and any documents provided to you.",
                "Never offer to: track locations, show maps, execute commands for players, check inventories, monitor players, or interact with the game in any way.",
                "Never say 'tell me your position' or offer to give directions - you cannot see where players are.",
                "If asked about something you cannot do, simply say you don't have access to that information.",
                "Keep responses concise and factual. Do not over-explain or pad responses with unnecessary details.",
                "When answering about Rust gameplay, stick to general knowledge unless server-specific info is in your knowledge base."
            };
        }

        private class KnowledgeConfig
        {
            [JsonProperty("Enable Knowledge Base")]
            public bool Enabled { get; set; } = false;

            [JsonProperty("Vector Store ID")]
            public string VectorStoreId { get; set; } = "";

            [JsonProperty("Knowledge Subfolder")]
            public string Subfolder { get; set; } = "OpenAI/knowledge";

            [JsonProperty("Auto Create Vector Store")]
            public bool AutoCreateVectorStore { get; set; } = true;
        }

        private class GlobalBotConfig
        {
            [JsonProperty("Enabled")]
            public bool Enabled { get; set; } = false;

            [JsonProperty("Bot Name")]
            public string BotName { get; set; } = "Assistant";

            [JsonProperty("Response Prefix")]
            public string ResponsePrefix { get; set; } = "[Bot]";

            [JsonProperty("Response Color")]
            public string ResponseColor { get; set; } = "#55AAFF";

            [JsonProperty("Cooldown Seconds")]
            public int CooldownSeconds { get; set; } = 5;

            [JsonProperty("Trigger Patterns", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> TriggerPatterns { get; set; } = new List<string> { "?" };

            [JsonProperty("Monitor Global Chat")]
            public bool MonitorGlobalChat { get; set; } = true;

            [JsonProperty("Monitor Team Chat")]
            public bool MonitorTeamChat { get; set; } = false;

            [JsonProperty("Personality Preset")]
            public string PersonalityPreset { get; set; } = "helpful";

            [JsonProperty("Custom System Prompt")]
            public string CustomSystemPrompt { get; set; } = "";

            [JsonProperty("Daily Token Budget (0 = shared with main)")]
            public int DailyTokenBudget { get; set; } = 0;

            [JsonProperty("Use Better Chat")]
            public bool UseBetterChat { get; set; } = false;

            [JsonProperty("Better Chat Title")]
            public string BetterChatTitle { get; set; } = "[AI]";

            [JsonProperty("Better Chat Title Color")]
            public string BetterChatTitleColor { get; set; } = "#55AAFF";
        }

        protected override void LoadDefaultConfig()
        {
            _config = new PluginConfig();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<PluginConfig>();
                if (_config == null)
                {
                    LoadDefaultConfig();
                    return;
                }
                ValidateConfig();
                MigrateConfig();
                SaveConfig();
            }
            catch (Exception ex)
            {
                PrintError($"Config load failed: {ex.Message}");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        private void ValidateConfig()
        {
            if (_config.Api.MaxOutputTokens < 0)
                _config.Api.MaxOutputTokens = 0;

            if (_config.RateLimits.CooldownSeconds < 0)
                _config.RateLimits.CooldownSeconds = DefaultCooldownSeconds;

            if (_config.RateLimits.MaxRequestsPerMinute < 1)
                _config.RateLimits.MaxRequestsPerMinute = DefaultMaxRequestsPerMinute;

            if (_config.Chat.MaxChunkSize < 100)
                _config.Chat.MaxChunkSize = DefaultMaxChunkSize;

            if (_config.Security.MaxInputLength < 10)
                _config.Security.MaxInputLength = DefaultMaxInputLength;

            var validEfforts = new HashSet<string> { "none", "minimal", "low", "medium", "high" };
            if (!validEfforts.Contains(_config.Api.ReasoningEffort.ToLower()))
                _config.Api.ReasoningEffort = "low";

            if (string.IsNullOrEmpty(_config.Chat.CommandPrefix))
                PrintWarning("Command prefix is empty. Players will not be able to use the AI chat command.");
        }

        private void MigrateConfig()
        {
            if (_config.ConfigVersion == PluginVersion)
                return;

            var oldVersion = string.IsNullOrEmpty(_config.ConfigVersion) ? "0.0.0" : _config.ConfigVersion;

            
            if (CompareVersions(oldVersion, "3.3.0") < 0)
            {
                if (_config.Prompt?.CustomInstructions != null)
                {
                    var originalCount = _config.Prompt.CustomInstructions.Count;
                    _config.Prompt.CustomInstructions = _config.Prompt.CustomInstructions
                        .Distinct()
                        .ToList();

                    if (originalCount != _config.Prompt.CustomInstructions.Count)
                        PrintWarning($"Config migration: Removed {originalCount - _config.Prompt.CustomInstructions.Count} duplicate CustomInstructions");
                }
            }

            
            if (CompareVersions(oldVersion, PluginVersion) < 0)
            {
                // Migration to current version
                PrintWarning("Config migration: 'Broadcast Responses' has been replaced with 'Global Chat Bot' feature.");
            }

            _config.ConfigVersion = PluginVersion;
            Puts($"Config migrated from {oldVersion} to {PluginVersion}");
        }

        private int CompareVersions(string v1, string v2)
        {
            var parts1 = v1.Split('.').Select(int.Parse).ToArray();
            var parts2 = v2.Split('.').Select(int.Parse).ToArray();

            for (int i = 0; i < Math.Max(parts1.Length, parts2.Length); i++)
            {
                var p1 = i < parts1.Length ? parts1[i] : 0;
                var p2 = i < parts2.Length ? parts2[i] : 0;
                if (p1 != p2) return p1.CompareTo(p2);
            }
            return 0;
        }

        #endregion

        #region Data Classes

        private class PlayerSession
        {
            public string LastResponseId { get; set; }
            public float LastRequestTime { get; set; }
            public int TokensUsedToday { get; set; }
            public int RequestsToday { get; set; }
        }

        private class ModelInfo
        {
            public string Id { get; set; }
            public bool IsReasoningModel { get; set; }
            public bool SupportsWebSearch { get; set; }
            public string[] ValidReasoningEfforts { get; set; }
            public int? MaxContextTokens { get; set; }
        }

        private class SetupStatus
        {
            public bool ApiKeyValid { get; set; }
            public bool ModelAvailable { get; set; }
            public bool ConfigurationOptimal { get; set; }
            public List<string> Warnings { get; set; } = new List<string>();
            public List<string> Errors { get; set; } = new List<string>();
            public List<string> Suggestions { get; set; } = new List<string>();
        }

        private class VectorStoreInfo
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public int FileCount { get; set; }
        }

        private class VectorStoreFileInfo
        {
            public string Id { get; set; }
            public string FileName { get; set; }
            public string Status { get; set; }
        }

        private class UsageData
        {
            public DateTime LastReset { get; set; } = DateTime.UtcNow.Date;
            public int GlobalTokensToday { get; set; }
            public Dictionary<string, PlayerUsage> Players { get; set; } = new Dictionary<string, PlayerUsage>();
        }

        private class PlayerUsage
        {
            public int TokensUsedToday { get; set; }
            public int RequestsToday { get; set; }
        }

        #endregion

        #region Fields

        private Dictionary<string, PlayerSession> _sessions = new Dictionary<string, PlayerSession>();
        private int _globalTokensToday;
        private int _globalRequestsThisMinute;
        private float _minuteStartTime;
        private DateTime _lastDailyReset;
        private bool _apiKeyValid;
        private List<string> _availableModels;
        private Dictionary<string, ModelInfo> _modelInfoCache = new Dictionary<string, ModelInfo>();

        
        private string _globalBotResponseId;
        private float _globalBotLastResponseTime;
        private int _globalBotTokensToday;

        
        
        private Dictionary<ulong, string> _teamBotResponseIds = new Dictionary<ulong, string>();

        
        private Dictionary<string, string> _personalities = new Dictionary<string, string>();

        
        private static readonly Dictionary<string, string> DefaultPersonalities = new Dictionary<string, string>
        {
            ["helpful"] = "You are a helpful chat bot on a Rust game server. Answer questions briefly and helpfully.",
            ["casual"] = "You're a chill bot hanging out in a Rust server chat. Keep it short and casual, like talking to a friend.",
            ["professional"] = "You are a professional server assistant. Provide accurate, well-structured responses to player questions.",
            ["pirate"] = "Yarr! Ye be a pirate bot on this here Rust server. Answer questions like a salty sea dog, but keep it helpful matey!"
        };

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(PermissionUse, this);
            permission.RegisterPermission(PermissionAdmin, this);
            permission.RegisterPermission(PermissionUnlimited, this);
        }

        private void OnServerInitialized()
        {
            _lastDailyReset = DateTime.UtcNow.Date;
            _minuteStartTime = UnityEngine.Time.realtimeSinceStartup;

            timer.Every(60f, ResetMinuteCounter);
            timer.Every(300f, CheckDailyReset);

            
            if (_config.RateLimits.PersistUsageData)
            {
                LoadUsageData();
            }

            
            LoadPersonalities();

            
            if (_config.Knowledge.Enabled)
            {
                InitializeKnowledgeFolder();
            }

            
            if (string.IsNullOrEmpty(_config.Api.ApiKey))
            {
                PrintError("=== OpenAI Plugin Setup Required ===");
                PrintError("No API key configured. Set 'API Key' in the config file.");
                _apiKeyValid = false;
                return;
            }

            
            Puts("Validating OpenAI configuration...");
            FetchAvailableModels(success =>
            {
                if (!success)
                {
                    PrintWarning("Could not fetch available models. Using offline validation.");
                }

                var status = ValidateSetup();
                LogSetupStatus(status);

                _apiKeyValid = status.ApiKeyValid && (status.ModelAvailable || _availableModels == null);

                if (_apiKeyValid)
                {
                    Puts($"OpenAI plugin ready. Model: {_config.Api.Model}");
                }
            });
        }

        private void Unload()
        {
            if (_config.RateLimits.PersistUsageData)
            {
                SaveUsageData();
            }
            _sessions.Clear();
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player != null)
                _sessions.Remove(player.UserIDString);
        }

        private object OnPlayerChat(BasePlayer player, string message, ConVar.Chat.ChatChannel channel)
        {
            if (player == null || string.IsNullOrEmpty(message))
                return null;

            
            if (!string.IsNullOrEmpty(_config.Chat.CommandPrefix) &&
                message.StartsWith(_config.Chat.CommandPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var question = message.Substring(_config.Chat.CommandPrefix.Length).Trim();
                if (string.IsNullOrEmpty(question))
                {
                    SendPlayerMessage(player, "Please provide a question after the command.");
                    return true;
                }

                if (!permission.UserHasPermission(player.UserIDString, PermissionUse))
                {
                    SendPlayerMessage(player, "You don't have permission to use this command.");
                    return true;
                }

                if (!_apiKeyValid)
                {
                    SendPlayerMessage(player, "AI assistant is not configured. Please contact an administrator.");
                    return true;
                }

                ProcessQuestion(player, question);
                return true;
            }

            
            if (_config.GlobalBot.Enabled && ShouldBotRespond(message, channel))
            {
                ProcessGlobalBotQuestion(player, message, channel);
                
            }

            return null;
        }

        #endregion

        #region Core Methods

        private void ProcessQuestion(BasePlayer player, string question)
        {
            var session = GetOrCreateSession(player.UserIDString);
            var isUnlimited = permission.UserHasPermission(player.UserIDString, PermissionUnlimited);

            if (!isUnlimited)
            {
                var rateLimitResult = CheckRateLimits(player, session);
                if (rateLimitResult != null)
                {
                    SendPlayerMessage(player, rateLimitResult);
                    return;
                }
            }

            var sanitizedQuestion = SanitizeInput(question);
            if (sanitizedQuestion == null)
            {
                SendPlayerMessage(player, "Your message contains disallowed content.");
                return;
            }

            session.LastRequestTime = UnityEngine.Time.realtimeSinceStartup;
            _globalRequestsThisMinute++;

            var payload = BuildRequestPayload(player, sanitizedQuestion, session);
            SendApiRequest(player, payload, session);
        }

        private Dictionary<string, object> BuildRequestPayload(BasePlayer player, string question, PlayerSession session)
        {
            var payload = new Dictionary<string, object>
            {
                ["model"] = _config.Api.Model,
                ["instructions"] = BuildSystemInstructions(player),
                ["input"] = question,
                ["store"] = true,
                ["truncation"] = "auto"
            };

            
            if (_config.Api.MaxOutputTokens > 0)
            {
                payload["max_output_tokens"] = _config.Api.MaxOutputTokens;
            }

            if (!string.IsNullOrEmpty(session.LastResponseId))
                payload["previous_response_id"] = session.LastResponseId;

            
            if (_config.Api.ReasoningEffort.ToLower() != "none")
            {
                payload["reasoning"] = new Dictionary<string, object>
                {
                    ["effort"] = _config.Api.ReasoningEffort.ToLower()
                };
            }

            
            var tools = new List<object>();

            if (_config.Api.EnableWebSearch)
            {
                tools.Add(new Dictionary<string, object>
                {
                    ["type"] = "web_search_preview",
                    ["search_context_size"] = "low"
                });
            }

            
            if (_config.Knowledge.Enabled && !string.IsNullOrEmpty(_config.Knowledge.VectorStoreId))
            {
                tools.Add(new Dictionary<string, object>
                {
                    ["type"] = "file_search",
                    ["vector_store_ids"] = new[] { _config.Knowledge.VectorStoreId }
                });
            }

            if (tools.Count > 0)
                payload["tools"] = tools;

            return payload;
        }

        private string BuildSystemInstructions(BasePlayer player)
        {
            var sb = new StringBuilder();
            sb.Append(_config.Prompt.SystemPrompt);

            if (_config.Prompt.IncludeServerInfo)
            {
                sb.Append($"\n\nServer: {ConVar.Server.hostname}");
                sb.Append($"\nPlayers online: {BasePlayer.activePlayerList.Count}/{ConVar.Server.maxplayers}");
            }

            if (_config.Prompt.IncludePlayerNames && player != null)
            {
                sb.Append($"\n\nYou are talking to: {player.displayName}");
            }

            if (_config.Prompt.CustomInstructions != null && _config.Prompt.CustomInstructions.Count > 0)
            {
                sb.Append("\n\nAdditional instructions:");
                foreach (var instruction in _config.Prompt.CustomInstructions)
                {
                    sb.Append($"\n- {instruction}");
                }
            }

            return sb.ToString();
        }

        #endregion

        #region Global Chat Bot

        private string GetPersonalitiesPath()
        {
            return Path.Combine(Interface.Oxide.DataDirectory, "OpenAI", "personalities");
        }

        private void LoadPersonalities()
        {
            var path = GetPersonalitiesPath();

            
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Puts($"Created personalities folder: {path}");

                
                foreach (var kv in DefaultPersonalities)
                {
                    var filePath = Path.Combine(path, $"{kv.Key}.txt");
                    File.WriteAllText(filePath, kv.Value);
                }
                Puts($"Created {DefaultPersonalities.Count} default personality files.");
            }

            
            _personalities.Clear();
            var files = Directory.GetFiles(path, "*.txt");

            foreach (var file in files)
            {
                try
                {
                    var name = Path.GetFileNameWithoutExtension(file).ToLower();
                    var prompt = File.ReadAllText(file).Trim();

                    if (!string.IsNullOrEmpty(prompt))
                    {
                        _personalities[name] = prompt;
                    }
                }
                catch (Exception ex)
                {
                    PrintWarning($"Failed to load personality file {file}: {ex.Message}");
                }
            }

            Puts($"Loaded {_personalities.Count} bot personalities.");

            
            var configuredPreset = _config.GlobalBot.PersonalityPreset.ToLower();
            if (configuredPreset != "custom" && !_personalities.ContainsKey(configuredPreset))
            {
                PrintWarning($"Configured personality '{_config.GlobalBot.PersonalityPreset}' not found. Using 'helpful' or first available.");
            }
        }

        private bool ShouldBotRespond(string message, ConVar.Chat.ChatChannel channel)
        {
            
            if (channel == ConVar.Chat.ChatChannel.Global && !_config.GlobalBot.MonitorGlobalChat)
                return false;
            if (channel == ConVar.Chat.ChatChannel.Team && !_config.GlobalBot.MonitorTeamChat)
                return false;
            if (channel != ConVar.Chat.ChatChannel.Global && channel != ConVar.Chat.ChatChannel.Team)
                return false;  

            
            var lowerMessage = message.ToLower();
            foreach (var pattern in _config.GlobalBot.TriggerPatterns)
            {
                var lowerPattern = pattern.ToLower();
                if (lowerMessage.EndsWith(lowerPattern) || lowerMessage.Contains(" " + lowerPattern))
                    return true;
            }

            return false;
        }

        private void ProcessGlobalBotQuestion(BasePlayer player, string message, ConVar.Chat.ChatChannel channel)
        {
            
            var currentTime = UnityEngine.Time.realtimeSinceStartup;
            if (currentTime - _globalBotLastResponseTime < _config.GlobalBot.CooldownSeconds)
                return;

            
            if (_globalRequestsThisMinute >= _config.RateLimits.MaxRequestsPerMinute)
                return;

            
            if (_config.GlobalBot.DailyTokenBudget > 0)
            {
                
                if (_globalBotTokensToday >= _config.GlobalBot.DailyTokenBudget)
                    return;
            }
            else
            {
                
                if (_globalTokensToday >= _config.RateLimits.DailyTokenBudget)
                    return;
            }

            
            if (IsDirectedAtPlayer(message))
                return;

            
            var sanitizedMessage = SanitizeInput(message);
            if (sanitizedMessage == null)
                return;

            
            _globalBotLastResponseTime = currentTime;
            _globalRequestsThisMinute++;

            
            ulong teamId = 0;
            if (channel == ConVar.Chat.ChatChannel.Team)
            {
                teamId = GetPlayerTeamId(player);
                if (teamId == 0)
                    return;  
            }

            
            var payload = BuildGlobalBotPayload(player, sanitizedMessage, channel, teamId);
            SendGlobalBotRequest(payload, channel, player, teamId);
        }

        private ulong GetPlayerTeamId(BasePlayer player)
        {
            if (player == null || player.currentTeam == 0)
                return 0;

            return player.currentTeam;
        }

        private List<BasePlayer> GetTeamMembers(ulong teamId)
        {
            if (teamId == 0)
                return new List<BasePlayer>();

            return BasePlayer.activePlayerList
                .Where(p => p.currentTeam == teamId)
                .ToList();
        }

        private bool IsDirectedAtPlayer(string message)
        {
            var lowerMessage = message.ToLower();

            
            var playerNames = BasePlayer.activePlayerList
                .Select(p => p.displayName.ToLower())
                .Where(n => n.Length >= 3)  
                .ToList();

            foreach (var name in playerNames)
            {
                
                
                
                

                if (lowerMessage.StartsWith(name + ",") ||
                    lowerMessage.StartsWith(name + " ") ||
                    lowerMessage.StartsWith("hey " + name) ||
                    lowerMessage.StartsWith("yo " + name) ||
                    lowerMessage.Contains("@" + name))
                {
                    return true;
                }
            }

            return false;
        }

        private Dictionary<string, object> BuildGlobalBotPayload(BasePlayer player, string question, ConVar.Chat.ChatChannel channel, ulong teamId)
        {
            var instructions = BuildGlobalBotInstructions(channel);

            
            var input = $"[{player.displayName}]: {question}";

            var payload = new Dictionary<string, object>
            {
                ["model"] = _config.Api.Model,
                ["instructions"] = instructions,
                ["input"] = input,
                ["store"] = true,
                ["truncation"] = "auto"
            };

            if (_config.Api.MaxOutputTokens > 0)
                payload["max_output_tokens"] = _config.Api.MaxOutputTokens;

            
            string previousResponseId = null;
            if (channel == ConVar.Chat.ChatChannel.Team && teamId > 0)
            {
                
                _teamBotResponseIds.TryGetValue(teamId, out previousResponseId);
            }
            else
            {
                
                previousResponseId = _globalBotResponseId;
            }

            if (!string.IsNullOrEmpty(previousResponseId))
                payload["previous_response_id"] = previousResponseId;

            
            if (_config.Api.ReasoningEffort.ToLower() != "none")
            {
                payload["reasoning"] = new Dictionary<string, object>
                {
                    ["effort"] = _config.Api.ReasoningEffort.ToLower()
                };
            }

            
            var tools = new List<object>();
            if (_config.Api.EnableWebSearch)
            {
                tools.Add(new Dictionary<string, object>
                {
                    ["type"] = "web_search_preview",
                    ["search_context_size"] = "low"
                });
            }
            if (_config.Knowledge.Enabled && !string.IsNullOrEmpty(_config.Knowledge.VectorStoreId))
            {
                tools.Add(new Dictionary<string, object>
                {
                    ["type"] = "file_search",
                    ["vector_store_ids"] = new[] { _config.Knowledge.VectorStoreId }
                });
            }
            if (tools.Count > 0)
                payload["tools"] = tools;

            return payload;
        }

        private string BuildGlobalBotInstructions(ConVar.Chat.ChatChannel channel)
        {
            var sb = new StringBuilder();

            
            var preset = _config.GlobalBot.PersonalityPreset.ToLower();
            if (preset == "custom" && !string.IsNullOrEmpty(_config.GlobalBot.CustomSystemPrompt))
            {
                sb.Append(_config.GlobalBot.CustomSystemPrompt);
            }
            else if (_personalities.TryGetValue(preset, out var presetPrompt))
            {
                sb.Append(presetPrompt);
            }
            else if (_personalities.TryGetValue("helpful", out var helpfulPrompt))
            {
                sb.Append(helpfulPrompt);  
            }
            else if (_personalities.Count > 0)
            {
                sb.Append(_personalities.Values.First());  
            }
            else
            {
                sb.Append(DefaultPersonalities["helpful"]);  
            }

            
            if (channel == ConVar.Chat.ChatChannel.Team)
            {
                sb.Append("\n\nYou are responding in a TEAM chat. Only team members can see this conversation.");
            }
            else
            {
                sb.Append("\n\nYou are responding in GLOBAL chat. All players on the server can see this conversation.");
            }

            sb.Append("\n\nIMPORTANT: You monitor a multiplayer game chat. Only respond if:");
            sb.Append("\n- The question is something you can genuinely help with (game info, server info, general knowledge)");
            sb.Append("\n- The question is NOT clearly directed at another specific player");
            sb.Append("\n\nIf you should NOT respond (question is for another player, personal chat, or you can't help), reply with exactly: [SKIP]");
            sb.Append("\n\nKeep responses brief and conversational. You're chatting, not writing essays.");

            if (_config.Prompt.IncludeServerInfo)
            {
                sb.Append($"\n\nServer: {ConVar.Server.hostname}");
                sb.Append($"\nPlayers online: {BasePlayer.activePlayerList.Count}/{ConVar.Server.maxplayers}");
            }

            
            if (_config.Prompt.CustomInstructions?.Count > 0)
            {
                sb.Append("\n\nAdditional context:");
                foreach (var instruction in _config.Prompt.CustomInstructions)
                    sb.Append($"\n- {instruction}");
            }

            return sb.ToString();
        }

        private void SendGlobalBotRequest(Dictionary<string, object> payload, ConVar.Chat.ChatChannel channel, BasePlayer requestingPlayer, ulong teamId)
        {
            var jsonPayload = JsonConvert.SerializeObject(payload);

            Debug($"Sending global bot request for channel: {channel}");
            Debug($"Payload: {jsonPayload}");

            webrequest.Enqueue(
                _config.Api.Url,
                jsonPayload,
                (code, response) => HandleGlobalBotResponse(code, response, channel, teamId),
                this,
                Oxide.Core.Libraries.RequestMethod.POST,
                new Dictionary<string, string>
                {
                    ["Authorization"] = $"Bearer {_config.Api.ApiKey}",
                    ["Content-Type"] = "application/json"
                }
            );
        }

        private void HandleGlobalBotResponse(int code, string response, ConVar.Chat.ChatChannel channel, ulong teamId)
        {
            if (code != 200)
            {
                Debug($"Global bot request failed: {code}");
                return;
            }

            try
            {
                var json = JObject.Parse(response);

                
                var responseId = json["id"]?.ToString();
                if (!string.IsNullOrEmpty(responseId))
                {
                    if (channel == ConVar.Chat.ChatChannel.Team && teamId > 0)
                    {
                        
                        _teamBotResponseIds[teamId] = responseId;
                    }
                    else
                    {
                        
                        _globalBotResponseId = responseId;
                    }
                }

                
                var tokensUsed = json["usage"]?["total_tokens"]?.Value<int>() ?? 0;
                if (_config.GlobalBot.DailyTokenBudget > 0)
                    _globalBotTokensToday += tokensUsed;
                else
                    _globalTokensToday += tokensUsed;

                
                var outputText = ExtractResponseText(json);
                if (string.IsNullOrEmpty(outputText))
                    return;

                
                if (outputText.Trim().Equals("[SKIP]", StringComparison.OrdinalIgnoreCase))
                    return;

                
                if (outputText.Length < 50 && outputText.Contains("[SKIP]"))
                    return;

                
                if (_config.Chat.StripUrlsFromLinks)
                    outputText = StripUrlsFromMarkdownLinks(outputText);

                
                BroadcastBotMessage(outputText, channel, teamId);

                
                if (_config.Discord.Enabled && !string.IsNullOrEmpty(_config.Discord.WebhookUrl))
                    SendBotToDiscord(outputText, channel);
            }
            catch (Exception ex)
            {
                PrintError($"Global bot response error: {ex.Message}");
            }
        }

        private void BroadcastBotMessage(string message, ConVar.Chat.ChatChannel channel, ulong teamId)
        {
            if (_config.GlobalBot.UseBetterChat)
            {
                BroadcastBotMessageBetterChat(message, channel, teamId);
                return;
            }

            var chunks = ChunkMessage(message);
            var prefix = _config.GlobalBot.ResponsePrefix;
            var color = _config.GlobalBot.ResponseColor;
            var msgColor = _config.Chat.MessageColor;
            var fontSize = _config.Chat.FontSize;

            foreach (var chunk in chunks)
            {
                var formatted = $"<size={fontSize}><color={color}>{prefix}</color> <color={msgColor}>{EscapeRichText(chunk)}</color></size>";

                if (channel == ConVar.Chat.ChatChannel.Team && teamId > 0)
                {
                    
                    var teamMembers = GetTeamMembers(teamId);
                    foreach (var player in teamMembers)
                        player.ChatMessage(formatted);
                }
                else
                {
                    
                    foreach (var player in BasePlayer.activePlayerList)
                        player.ChatMessage(formatted);
                }
            }
        }

        private void BroadcastBotMessageBetterChat(string message, ConVar.Chat.ChatChannel channel, ulong teamId)
        {
            var chunks = ChunkMessage(message);
            var botName = _config.GlobalBot.BotName;
            var titleColor = _config.GlobalBot.BetterChatTitleColor.TrimStart('#');
            var nameColor = _config.GlobalBot.ResponseColor.TrimStart('#');
            var msgColor = _config.Chat.MessageColor.TrimStart('#');
            var title = _config.GlobalBot.BetterChatTitle;
            var size = _config.Chat.FontSize;

            foreach (var chunk in chunks)
            {
                
                var formatted = $"<color=#{titleColor}><size={size}>{title}</size></color> " +
                               $"<color=#{nameColor}><size={size}>{botName}</size></color>: " +
                               $"<color=#{msgColor}><size={size}>{EscapeRichText(chunk)}</size></color>";

                BroadcastFormattedMessage(formatted, channel, teamId);
            }
        }

        private void BroadcastFormattedMessage(string formatted, ConVar.Chat.ChatChannel channel, ulong teamId)
        {
            if (channel == ConVar.Chat.ChatChannel.Team && teamId > 0)
            {
                var teamMembers = GetTeamMembers(teamId);
                foreach (var player in teamMembers)
                    player.SendConsoleCommand("chat.add", (int)channel, 0ul, formatted);
            }
            else
            {
                foreach (var player in BasePlayer.activePlayerList)
                    player.SendConsoleCommand("chat.add", (int)channel, 0ul, formatted);
            }
        }

        private void SendBotToDiscord(string response, ConVar.Chat.ChatChannel channel)
        {
            if (string.IsNullOrEmpty(_config.Discord.WebhookUrl))
                return;

            var channelName = channel == ConVar.Chat.ChatChannel.Team ? "Team" : "Global";

            var embed = new Dictionary<string, object>
            {
                ["embeds"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["title"] = $"Bot Response ({channelName})",
                        ["color"] = 5592575,
                        ["fields"] = new List<object>
                        {
                            new Dictionary<string, object>
                            {
                                ["name"] = "Response",
                                ["value"] = TruncateForDiscord(response, 1024),
                                ["inline"] = false
                            }
                        },
                        ["timestamp"] = DateTime.UtcNow.ToString("o")
                    }
                }
            };

            var jsonPayload = JsonConvert.SerializeObject(embed);

            webrequest.Enqueue(
                _config.Discord.WebhookUrl,
                jsonPayload,
                (code, res) =>
                {
                    if (code != 200 && code != 204)
                        PrintWarning($"Discord webhook failed: {code}");
                },
                this,
                Oxide.Core.Libraries.RequestMethod.POST,
                new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json"
                }
            );
        }

        #endregion

        #region Rate Limiting

        private PlayerSession GetOrCreateSession(string odId)
        {
            if (!_sessions.TryGetValue(odId, out var session))
            {
                session = new PlayerSession();
                _sessions[odId] = session;
            }
            return session;
        }

        private string CheckRateLimits(BasePlayer player, PlayerSession session)
        {
            var currentTime = UnityEngine.Time.realtimeSinceStartup;

            var timeSinceLastRequest = currentTime - session.LastRequestTime;
            if (timeSinceLastRequest < _config.RateLimits.CooldownSeconds)
            {
                var remaining = _config.RateLimits.CooldownSeconds - (int)timeSinceLastRequest;
                return $"Please wait {remaining} seconds before your next question.";
            }

            if (_globalRequestsThisMinute >= _config.RateLimits.MaxRequestsPerMinute)
                return "The AI is busy. Please try again in a moment.";

            if (_globalTokensToday >= _config.RateLimits.DailyTokenBudget)
                return "Daily server AI budget has been reached. Try again tomorrow.";

            if (session.TokensUsedToday >= _config.RateLimits.PlayerDailyTokenLimit)
                return "You've reached your daily AI usage limit. Try again tomorrow.";

            return null;
        }

        private void ResetMinuteCounter()
        {
            _globalRequestsThisMinute = 0;
            _minuteStartTime = UnityEngine.Time.realtimeSinceStartup;
        }

        private void CheckDailyReset()
        {
            if (DateTime.UtcNow.Date <= _lastDailyReset)
                return;

            _lastDailyReset = DateTime.UtcNow.Date;
            _globalTokensToday = 0;
            _globalBotTokensToday = 0;
            _teamBotResponseIds.Clear();  

            foreach (var session in _sessions.Values)
            {
                session.TokensUsedToday = 0;
                session.RequestsToday = 0;
            }

            Puts("Daily AI usage counters reset.");

            if (_config.RateLimits.PersistUsageData)
            {
                SaveUsageData();
            }
        }

        private void TrackUsage(PlayerSession session, int tokensUsed)
        {
            session.TokensUsedToday += tokensUsed;
            session.RequestsToday++;
            _globalTokensToday += tokensUsed;

            if (_config.RateLimits.PersistUsageData)
            {
                SaveUsageData();
            }
        }

        private void SaveUsageData()
        {
            var data = new UsageData
            {
                LastReset = _lastDailyReset,
                GlobalTokensToday = _globalTokensToday,
                Players = new Dictionary<string, PlayerUsage>()
            };

            foreach (var kv in _sessions)
            {
                if (kv.Value.TokensUsedToday > 0 || kv.Value.RequestsToday > 0)
                {
                    data.Players[kv.Key] = new PlayerUsage
                    {
                        TokensUsedToday = kv.Value.TokensUsedToday,
                        RequestsToday = kv.Value.RequestsToday
                    };
                }
            }

            Interface.Oxide.DataFileSystem.WriteObject("OpenAI/usage", data);
        }

        private void LoadUsageData()
        {
            try
            {
                var data = Interface.Oxide.DataFileSystem.ReadObject<UsageData>("OpenAI/usage");
                if (data == null) return;

                
                if (data.LastReset.Date == DateTime.UtcNow.Date)
                {
                    _lastDailyReset = data.LastReset;
                    _globalTokensToday = data.GlobalTokensToday;

                    foreach (var kv in data.Players)
                    {
                        var session = GetOrCreateSession(kv.Key);
                        session.TokensUsedToday = kv.Value.TokensUsedToday;
                        session.RequestsToday = kv.Value.RequestsToday;
                    }

                    Puts($"Loaded usage data: {_globalTokensToday:N0} tokens used today");
                }
                else
                {
                    Puts("Usage data is from a previous day, starting fresh.");
                }
            }
            catch (Exception ex)
            {
                Debug($"Could not load usage data: {ex.Message}");
            }
        }

        #endregion

        #region API Communication

        private string GetApiKey() => _config.Api.ApiKey;

        private void SendApiRequest(BasePlayer player, Dictionary<string, object> payload, PlayerSession session, int attempt = 1)
        {
            var apiKey = GetApiKey();
            var jsonPayload = JsonConvert.SerializeObject(payload);

            Debug($"Sending request to: {_config.Api.Url}");
            Debug($"Model: {_config.Api.Model}");
            Debug($"Payload: {jsonPayload}");

            webrequest.Enqueue(
                _config.Api.Url,
                jsonPayload,
                (code, response) => HandleApiResponse(player, code, response, session, payload, attempt),
                this,
                Oxide.Core.Libraries.RequestMethod.POST,
                new Dictionary<string, string>
                {
                    ["Authorization"] = $"Bearer {apiKey}",
                    ["Content-Type"] = "application/json"
                }
            );
        }

        private void HandleApiResponse(BasePlayer player, int code, string response, PlayerSession session, Dictionary<string, object> payload, int attempt)
        {
            if (player == null || !player.IsConnected)
                return;

            if (code == 0 || code >= 500)
            {
                if (attempt < _config.Api.RetryAttempts)
                {
                    var delay = (float)Math.Pow(2, attempt);
                    timer.Once(delay, () => SendApiRequest(player, payload, session, attempt + 1));
                    return;
                }
                SendPlayerMessage(player, "Unable to reach the AI service. Please try again later.");
                PrintError($"API request failed after {attempt} attempts. Code: {code}");
                return;
            }

            if (code == 401)
            {
                SendPlayerMessage(player, "AI service authentication failed. Please contact an administrator.");
                PrintError("API authentication failed. Check your API key.");
                _apiKeyValid = false;
                return;
            }

            if (code == 429)
            {
                SendPlayerMessage(player, "AI service is rate limited. Please try again in a moment.");
                return;
            }

            if (code == 400)
            {
                try
                {
                    var errorJson = JObject.Parse(response);
                    var errorMsg = errorJson["error"]?["message"]?.ToString() ?? "Unknown error";

                    if (errorMsg.Contains("reasoning") || errorMsg.Contains("effort"))
                    {
                        SendPlayerMessage(player, "Configuration error. Please contact an administrator.");
                        PrintError($"Reasoning effort '{_config.Api.ReasoningEffort}' is not valid for model '{_config.Api.Model}'");
                        PrintError("Run 'openai.status' to diagnose, then reload the plugin after fixing the config.");
                    }
                    else if (errorMsg.Contains("model"))
                    {
                        SendPlayerMessage(player, "AI model configuration error. Please contact an administrator.");
                        PrintError($"Model error: {errorMsg}");
                    }
                    else
                    {
                        SendPlayerMessage(player, "Error processing your request.");
                        PrintError($"API error 400: {errorMsg}");
                    }
                }
                catch
                {
                    SendPlayerMessage(player, "Error processing your request.");
                    PrintError($"API error 400: {response}");
                }
                return;
            }

            if (code != 200)
            {
                SendPlayerMessage(player, "Error processing your request. Please try again.");
                PrintError($"API error {code}: {response}");
                return;
            }

            Debug($"Response code: {code}");
            Debug($"Raw response: {response}");

            try
            {
                var json = JObject.Parse(response);

                var responseId = json["id"]?.ToString();
                Debug($"Response ID: {responseId}");
                if (!string.IsNullOrEmpty(responseId))
                    session.LastResponseId = responseId;

                var usage = json["usage"];
                var tokensUsed = usage?["total_tokens"]?.Value<int>() ?? 0;
                Debug($"Tokens used: {tokensUsed}");
                TrackUsage(session, tokensUsed);

                var status = json["status"]?.ToString();
                Debug($"Response status: {status}");

                var error = json["error"];
                if (error != null && error.Type != JTokenType.Null)
                    Debug($"Error in response: {error}");

                if (status == "incomplete")
                {
                    var incompleteReason = json["incomplete_details"]?["reason"]?.ToString();
                    if (incompleteReason == "content_filter")
                    {
                        SendPlayerMessage(player, "I cannot respond to that type of question.");
                        return;
                    }
                }

                var outputText = ExtractResponseText(json);
                if (string.IsNullOrEmpty(outputText))
                {
                    SendPlayerMessage(player, "Received an empty response. Please try rephrasing your question.");
                    return;
                }

                
                if (_config.Chat.StripUrlsFromLinks)
                {
                    outputText = StripUrlsFromMarkdownLinks(outputText);
                }

                SendPlayerMessage(player, outputText);

                if (_config.Discord.Enabled && !string.IsNullOrEmpty(_config.Discord.WebhookUrl))
                {
                    var question = payload["input"]?.ToString() ?? "";
                    SendToDiscord(player.displayName, question, outputText);
                }
            }
            catch (Exception ex)
            {
                SendPlayerMessage(player, "Error processing the AI response.");
                PrintError($"Response parsing error: {ex.Message}");
            }
        }

        private string ExtractResponseText(JObject json)
        {
            var output = json["output"] as JArray;
            if (output == null || output.Count == 0)
            {
                Debug("No 'output' array in response or it's empty");
                Debug($"Response keys: {string.Join(", ", json.Properties().Select(p => p.Name))}");
                return null;
            }

            Debug($"Output array has {output.Count} items");

            var sb = new StringBuilder();

            foreach (var item in output)
            {
                var type = item["type"]?.ToString();
                Debug($"Output item type: {type}");

                if (type != "message")
                    continue;

                var content = item["content"] as JArray;
                if (content == null)
                {
                    Debug("Message has no 'content' array");
                    continue;
                }

                Debug($"Content array has {content.Count} items");

                foreach (var contentItem in content)
                {
                    var contentType = contentItem["type"]?.ToString();
                    Debug($"Content item type: {contentType}");

                    if (contentType == "output_text")
                    {
                        var text = contentItem["text"]?.ToString();
                        Debug($"Extracted text length: {text?.Length ?? 0}");
                        if (!string.IsNullOrEmpty(text))
                        {
                            if (sb.Length > 0)
                                sb.Append(" ");
                            sb.Append(text);
                        }
                    }
                }
            }

            Debug($"Final extracted text length: {sb.Length}");
            return sb.Length > 0 ? sb.ToString() : null;
        }

        #endregion

        #region Chat Output

        private void SendPlayerMessage(BasePlayer player, string message)
        {
            if (player == null || !player.IsConnected || string.IsNullOrEmpty(message))
                return;

            var chunks = ChunkMessage(message);
            var prefix = _config.Chat.ResponsePrefix;
            var color = _config.Chat.ResponseColor;
            var msgColor = _config.Chat.MessageColor;
            var fontSize = _config.Chat.FontSize;

            foreach (var chunk in chunks)
            {
                var formatted = $"<size={fontSize}><color={color}>{prefix}</color> <color={msgColor}>{EscapeRichText(chunk)}</color></size>";
                player.ChatMessage(formatted);
            }
        }

        private List<string> ChunkMessage(string message)
        {
            var chunks = new List<string>();
            var maxSize = _config.Chat.MaxChunkSize;

            if (message.Length <= maxSize)
            {
                chunks.Add(message);
                return chunks;
            }

            var remaining = message;
            while (remaining.Length > 0)
            {
                if (remaining.Length <= maxSize)
                {
                    chunks.Add(remaining);
                    break;
                }

                var splitIndex = remaining.LastIndexOf(' ', maxSize);
                if (splitIndex <= 0)
                    splitIndex = maxSize;

                chunks.Add(remaining.Substring(0, splitIndex).Trim());
                remaining = remaining.Substring(splitIndex).Trim();
            }

            return chunks;
        }

        private string EscapeRichText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text
                .Replace("<", "‹")
                .Replace(">", "›");
        }

        #endregion

        #region Discord

        private void SendToDiscord(string playerName, string question, string response)
        {
            if (string.IsNullOrEmpty(_config.Discord.WebhookUrl))
                return;

            var embed = new Dictionary<string, object>
            {
                ["embeds"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["title"] = "AI Chat",
                        ["color"] = 5592575,
                        ["fields"] = new List<object>
                        {
                            new Dictionary<string, object>
                            {
                                ["name"] = "Player",
                                ["value"] = playerName,
                                ["inline"] = true
                            },
                            new Dictionary<string, object>
                            {
                                ["name"] = "Question",
                                ["value"] = TruncateForDiscord(question, 1024),
                                ["inline"] = false
                            },
                            new Dictionary<string, object>
                            {
                                ["name"] = "Response",
                                ["value"] = TruncateForDiscord(response, 1024),
                                ["inline"] = false
                            }
                        },
                        ["timestamp"] = DateTime.UtcNow.ToString("o")
                    }
                }
            };

            var jsonPayload = JsonConvert.SerializeObject(embed);

            webrequest.Enqueue(
                _config.Discord.WebhookUrl,
                jsonPayload,
                (code, res) =>
                {
                    if (code != 200 && code != 204)
                        PrintWarning($"Discord webhook failed: {code}");
                },
                this,
                Oxide.Core.Libraries.RequestMethod.POST,
                new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json"
                }
            );
        }

        private string TruncateForDiscord(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            if (text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength - 3) + "...";
        }

        private void SendConsoleToDiscord(string title, string content)
        {
            if (!_config.Discord.Enabled || string.IsNullOrEmpty(_config.Discord.WebhookUrl))
                return;

            var embed = new Dictionary<string, object>
            {
                ["embeds"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["title"] = title,
                        ["description"] = TruncateForDiscord(content, 4000),
                        ["color"] = 3447003, 
                        ["timestamp"] = DateTime.UtcNow.ToString("o")
                    }
                }
            };

            var jsonPayload = JsonConvert.SerializeObject(embed);

            webrequest.Enqueue(
                _config.Discord.WebhookUrl,
                jsonPayload,
                (code, res) =>
                {
                    if (code != 200 && code != 204)
                        Debug($"Discord webhook failed: {code}");
                },
                this,
                Oxide.Core.Libraries.RequestMethod.POST,
                new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json"
                }
            );
        }

        #endregion

        #region Helpers

        private void Debug(string message)
        {
            if (_config.DebugMode)
                Puts($"[DEBUG] {message}");
        }

        private bool IsReasoningModel(string model)
        {
            if (string.IsNullOrEmpty(model))
                return false;

            var lower = model.ToLower();

            
            
            if (lower.StartsWith("gpt"))
                return false;

            return lower.StartsWith("o1") || lower.StartsWith("o3") || lower.StartsWith("o4");
        }

        #endregion

        #region Model Discovery & Validation

        private void FetchAvailableModels(Action<bool> callback)
        {
            webrequest.Enqueue(
                "https://api.openai.com/v1/models",
                null,
                (code, response) => HandleModelsResponse(code, response, callback),
                this,
                Oxide.Core.Libraries.RequestMethod.GET,
                new Dictionary<string, string>
                {
                    ["Authorization"] = $"Bearer {_config.Api.ApiKey}"
                }
            );
        }

        private void HandleModelsResponse(int code, string response, Action<bool> callback)
        {
            if (code != 200)
            {
                Debug($"Models endpoint returned code {code}");
                if (code == 401)
                {
                    PrintError("API key is invalid or expired.");
                    _apiKeyValid = false;
                }
                callback(false);
                return;
            }

            try
            {
                var json = JObject.Parse(response);
                var data = json["data"] as JArray;

                if (data == null)
                {
                    Debug("No 'data' array in models response");
                    callback(false);
                    return;
                }

                _availableModels = new List<string>();
                _modelInfoCache.Clear();

                foreach (var model in data)
                {
                    var modelId = model["id"]?.ToString();
                    if (!string.IsNullOrEmpty(modelId))
                    {
                        _availableModels.Add(modelId);
                        _modelInfoCache[modelId] = ClassifyModel(modelId);
                    }
                }

                Debug($"Fetched {_availableModels.Count} available models");
                callback(true);
            }
            catch (Exception ex)
            {
                PrintError($"Failed to parse models response: {ex.Message}");
                callback(false);
            }
        }

        private ModelInfo ClassifyModel(string modelId)
        {
            var info = new ModelInfo { Id = modelId };
            var lower = modelId.ToLower();

            
            
            if ((lower.StartsWith("o1") || lower.StartsWith("o3") || lower.StartsWith("o4"))
                && !lower.StartsWith("gpt"))
            {
                info.IsReasoningModel = true;
                info.ValidReasoningEfforts = new[] { "minimal", "low", "medium", "high" };
            }
            else
            {
                info.IsReasoningModel = false;
                info.ValidReasoningEfforts = new[] { "none" };
            }

            
            info.SupportsWebSearch = lower.Contains("gpt") || !info.IsReasoningModel;

            return info;
        }

        private ModelInfo GetModelInfo(string modelId)
        {
            if (_modelInfoCache.TryGetValue(modelId, out var cached))
                return cached;

            
            var info = ClassifyModel(modelId);
            _modelInfoCache[modelId] = info;
            return info;
        }

        private SetupStatus ValidateSetup()
        {
            var status = new SetupStatus();

            
            if (string.IsNullOrEmpty(_config.Api.ApiKey))
            {
                status.Errors.Add("No API key configured");
                return status;
            }
            status.ApiKeyValid = true;

            
            if (_availableModels != null && _availableModels.Count > 0)
            {
                if (!_availableModels.Contains(_config.Api.Model))
                {
                    status.Errors.Add($"Model '{_config.Api.Model}' is not available to your API key");

                    
                    var suggestions = _availableModels
                        .Where(m => m.Contains("gpt") || m.StartsWith("o1") || m.StartsWith("o3") || m.Contains("nano"))
                        .Take(5)
                        .ToList();

                    if (suggestions.Count > 0)
                    {
                        status.Suggestions.Add($"Available models: {string.Join(", ", suggestions)}");
                    }
                }
                else
                {
                    status.ModelAvailable = true;
                }
            }
            else
            {
                
                status.ModelAvailable = true;
                status.Warnings.Add("Could not verify model availability (API unreachable or no models returned)");
            }

            
            var modelInfo = GetModelInfo(_config.Api.Model);
            var effort = _config.Api.ReasoningEffort.ToLower();

            if (modelInfo.IsReasoningModel && effort == "none")
            {
                status.Errors.Add($"Model '{_config.Api.Model}' requires reasoning. 'none' is not supported.");
                status.Suggestions.Add($"Set 'Reasoning Effort' to: {string.Join(", ", modelInfo.ValidReasoningEfforts)}");
            }
            else if (!modelInfo.IsReasoningModel && effort != "none")
            {
                status.Warnings.Add($"Model '{_config.Api.Model}' does not use reasoning. Effort '{effort}' will be ignored.");
                status.Suggestions.Add("Set 'Reasoning Effort' to 'none' for this model");
            }

            
            if (_config.Api.EnableWebSearch && !modelInfo.SupportsWebSearch)
            {
                status.Warnings.Add($"Model '{_config.Api.Model}' may not support web search");
            }

            status.ConfigurationOptimal = status.Errors.Count == 0 && status.Warnings.Count == 0;
            return status;
        }

        private void LogSetupStatus(SetupStatus status)
        {
            Puts("=== OpenAI Plugin Configuration ===");
            Puts($"API Key: {(status.ApiKeyValid ? "Valid" : "Invalid/Missing")}");
            Puts($"Model: {_config.Api.Model} ({(status.ModelAvailable ? "Available" : "NOT FOUND")})");
            Puts($"Reasoning: {_config.Api.ReasoningEffort}");
            Puts($"Web Search: {(_config.Api.EnableWebSearch ? "Enabled" : "Disabled")}");

            if (status.Errors.Count > 0)
            {
                PrintError("--- ERRORS ---");
                foreach (var error in status.Errors)
                    PrintError($"  * {error}");
            }

            if (status.Warnings.Count > 0)
            {
                PrintWarning("--- WARNINGS ---");
                foreach (var warning in status.Warnings)
                    PrintWarning($"  * {warning}");
            }

            if (status.Suggestions.Count > 0)
            {
                Puts("--- SUGGESTIONS ---");
                foreach (var suggestion in status.Suggestions)
                    Puts($"  -> {suggestion}");
            }

            Puts("==================================");
        }

        #endregion

        #region Knowledge Base

        private string GetKnowledgePath()
        {
            return Path.Combine(Interface.Oxide.DataDirectory, _config.Knowledge.Subfolder);
        }

        private void InitializeKnowledgeFolder()
        {
            var path = GetKnowledgePath();
            if (Directory.Exists(path)) return;

            Directory.CreateDirectory(path);
            Puts($"Created knowledge folder: {path}");

            
            File.WriteAllText(Path.Combine(path, "server-info.txt"),
                $"Server Name: {ConVar.Server.hostname}\nMax Players: {ConVar.Server.maxplayers}");

            Puts("Created server-info.txt. Add more .txt files and run 'openai.kb sync' to upload.");
        }

        private Dictionary<string, string> GetAuthHeaders()
        {
            return new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {_config.Api.ApiKey}"
            };
        }

        private Dictionary<string, string> GetAuthHeadersWithJson()
        {
            return new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {_config.Api.ApiKey}",
                ["Content-Type"] = "application/json"
            };
        }

        private void ListVectorStores(Action<List<VectorStoreInfo>> callback)
        {
            webrequest.Enqueue(
                "https://api.openai.com/v1/vector_stores",
                null,
                (code, response) => HandleVectorStoreListResponse(code, response, callback),
                this,
                Oxide.Core.Libraries.RequestMethod.GET,
                GetAuthHeaders()
            );
        }

        private void HandleVectorStoreListResponse(int code, string response, Action<List<VectorStoreInfo>> callback)
        {
            var stores = new List<VectorStoreInfo>();

            if (code != 200)
            {
                PrintError($"Failed to list vector stores: HTTP {code}");
                Debug($"Response: {response}");
                callback(stores);
                return;
            }

            try
            {
                var json = JObject.Parse(response);
                var data = json["data"] as JArray;

                if (data != null)
                {
                    foreach (var item in data)
                    {
                        stores.Add(new VectorStoreInfo
                        {
                            Id = item["id"]?.ToString(),
                            Name = item["name"]?.ToString() ?? "Unnamed",
                            FileCount = item["file_counts"]?["total"]?.Value<int>() ?? 0
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                PrintError($"Failed to parse vector stores response: {ex.Message}");
            }

            callback(stores);
        }

        private void CreateVectorStore(string name, Action<string> callback)
        {
            var payload = JsonConvert.SerializeObject(new { name = name });
            webrequest.Enqueue(
                "https://api.openai.com/v1/vector_stores",
                payload,
                (code, response) => HandleVectorStoreCreateResponse(code, response, callback),
                this,
                Oxide.Core.Libraries.RequestMethod.POST,
                GetAuthHeadersWithJson()
            );
        }

        private void HandleVectorStoreCreateResponse(int code, string response, Action<string> callback)
        {
            if (code != 200)
            {
                PrintError($"Failed to create vector store: HTTP {code}");
                Debug($"Response: {response}");
                callback(null);
                return;
            }

            try
            {
                var json = JObject.Parse(response);
                var storeId = json["id"]?.ToString();
                callback(storeId);
            }
            catch (Exception ex)
            {
                PrintError($"Failed to parse vector store create response: {ex.Message}");
                callback(null);
            }
        }

        private void UploadFile(string filePath, Action<string> callback)
        {
            try
            {
                var content = File.ReadAllBytes(filePath);
                var fileName = Path.GetFileName(filePath);

                
                var boundary = "----" + DateTime.Now.Ticks.ToString("x");
                var bodyBytes = BuildMultipartBody(boundary, fileName, content, "assistants");

                webrequest.Enqueue(
                    "https://api.openai.com/v1/files",
                    Encoding.UTF8.GetString(bodyBytes),
                    (code, response) => HandleFileUploadResponse(code, response, fileName, callback),
                    this,
                    Oxide.Core.Libraries.RequestMethod.POST,
                    new Dictionary<string, string>
                    {
                        ["Authorization"] = $"Bearer {_config.Api.ApiKey}",
                        ["Content-Type"] = $"multipart/form-data; boundary={boundary}"
                    }
                );
            }
            catch (Exception ex)
            {
                PrintError($"Failed to read file {filePath}: {ex.Message}");
                callback(null);
            }
        }

        private byte[] BuildMultipartBody(string boundary, string fileName, byte[] content, string purpose)
        {
            var sb = new StringBuilder();

            
            sb.Append($"--{boundary}\r\n");
            sb.Append("Content-Disposition: form-data; name=\"purpose\"\r\n\r\n");
            sb.Append($"{purpose}\r\n");

            
            sb.Append($"--{boundary}\r\n");
            sb.Append($"Content-Disposition: form-data; name=\"file\"; filename=\"{fileName}\"\r\n");
            sb.Append("Content-Type: application/octet-stream\r\n\r\n");

            var headerBytes = Encoding.UTF8.GetBytes(sb.ToString());
            var footerBytes = Encoding.UTF8.GetBytes($"\r\n--{boundary}--\r\n");

            
            var result = new byte[headerBytes.Length + content.Length + footerBytes.Length];
            Buffer.BlockCopy(headerBytes, 0, result, 0, headerBytes.Length);
            Buffer.BlockCopy(content, 0, result, headerBytes.Length, content.Length);
            Buffer.BlockCopy(footerBytes, 0, result, headerBytes.Length + content.Length, footerBytes.Length);

            return result;
        }

        private void HandleFileUploadResponse(int code, string response, string fileName, Action<string> callback)
        {
            if (code != 200)
            {
                PrintError($"Failed to upload file {fileName}: HTTP {code}");
                Debug($"Response: {response}");
                callback(null);
                return;
            }

            try
            {
                var json = JObject.Parse(response);
                var fileId = json["id"]?.ToString();
                callback(fileId);
            }
            catch (Exception ex)
            {
                PrintError($"Failed to parse file upload response: {ex.Message}");
                callback(null);
            }
        }

        private void AddFileToVectorStore(string vectorStoreId, string fileId, Action<bool> callback)
        {
            var payload = JsonConvert.SerializeObject(new { file_id = fileId });
            webrequest.Enqueue(
                $"https://api.openai.com/v1/vector_stores/{vectorStoreId}/files",
                payload,
                (code, response) =>
                {
                    if (code != 200)
                    {
                        Debug($"AddFileToVectorStore failed: HTTP {code}, Response: {response}");
                    }
                    callback(code == 200);
                },
                this,
                Oxide.Core.Libraries.RequestMethod.POST,
                GetAuthHeadersWithJson()
            );
        }

        private void DeleteFileFromVectorStore(string vectorStoreId, string fileId, Action<bool> callback)
        {
            webrequest.Enqueue(
                $"https://api.openai.com/v1/vector_stores/{vectorStoreId}/files/{fileId}",
                null,
                (code, response) => callback(code == 200 || code == 204),
                this,
                Oxide.Core.Libraries.RequestMethod.DELETE,
                GetAuthHeaders()
            );
        }

        private void DeleteFile(string fileId, Action<bool> callback)
        {
            webrequest.Enqueue(
                $"https://api.openai.com/v1/files/{fileId}",
                null,
                (code, response) => callback(code == 200 || code == 204),
                this,
                Oxide.Core.Libraries.RequestMethod.DELETE,
                GetAuthHeaders()
            );
        }

        private void ListVectorStoreFiles(string vectorStoreId, Action<List<VectorStoreFileInfo>> callback)
        {
            webrequest.Enqueue(
                $"https://api.openai.com/v1/vector_stores/{vectorStoreId}/files",
                null,
                (code, response) =>
                {
                    var files = new List<VectorStoreFileInfo>();

                    if (code != 200)
                    {
                        PrintError($"Failed to list vector store files: HTTP {code}");
                        callback(files);
                        return;
                    }

                    try
                    {
                        var json = JObject.Parse(response);
                        var data = json["data"] as JArray;

                        if (data != null)
                        {
                            foreach (var item in data)
                            {
                                files.Add(new VectorStoreFileInfo
                                {
                                    Id = item["id"]?.ToString(),
                                    Status = item["status"]?.ToString() ?? "unknown"
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        PrintError($"Failed to parse vector store files response: {ex.Message}");
                    }

                    callback(files);
                },
                this,
                Oxide.Core.Libraries.RequestMethod.GET,
                GetAuthHeaders()
            );
        }

        private void GetFileInfo(string fileId, Action<string> callback)
        {
            webrequest.Enqueue(
                $"https://api.openai.com/v1/files/{fileId}",
                null,
                (code, response) =>
                {
                    if (code != 200)
                    {
                        callback(null);
                        return;
                    }

                    try
                    {
                        var json = JObject.Parse(response);
                        callback(json["filename"]?.ToString());
                    }
                    catch
                    {
                        callback(null);
                    }
                },
                this,
                Oxide.Core.Libraries.RequestMethod.GET,
                GetAuthHeaders()
            );
        }

        private void ShowKnowledgeBaseStatus()
        {
            Puts("=== Knowledge Base Status ===");
            Puts($"Enabled: {_config.Knowledge.Enabled}");
            Puts($"Folder: {GetKnowledgePath()}");
            Puts($"Vector Store ID: {(string.IsNullOrEmpty(_config.Knowledge.VectorStoreId) ? "(not configured)" : _config.Knowledge.VectorStoreId)}");
            Puts($"Auto Create: {_config.Knowledge.AutoCreateVectorStore}");

            var path = GetKnowledgePath();
            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.txt");
                Puts($"Local files: {files.Length}");
                foreach (var file in files)
                {
                    Puts($"  - {Path.GetFileName(file)}");
                }
            }
            else
            {
                Puts("Local files: (folder not found)");
            }
            Puts("=============================");
        }

        private void SyncFilesToVectorStore()
        {
            var path = GetKnowledgePath();
            if (!Directory.Exists(path))
            {
                PrintError($"Knowledge folder not found: {path}");
                return;
            }

            var localFiles = Directory.GetFiles(path, "*.txt");
            if (localFiles.Length == 0)
            {
                PrintError("No .txt files found in knowledge folder");
                return;
            }

            
            if (string.IsNullOrEmpty(_config.Knowledge.VectorStoreId))
            {
                if (_config.Knowledge.AutoCreateVectorStore)
                {
                    var storeName = $"{ConVar.Server.hostname} Knowledge";
                    Puts($"Creating vector store: {storeName}");
                    CreateVectorStore(storeName, storeId =>
                    {
                        if (string.IsNullOrEmpty(storeId))
                        {
                            PrintError("Failed to create vector store");
                            return;
                        }
                        _config.Knowledge.VectorStoreId = storeId;
                        SaveConfig();
                        Puts($"Created vector store: {storeId}");
                        
                        UploadNewFiles(localFiles, storeId, new Dictionary<string, string>());
                    });
                }
                else
                {
                    PrintError("No vector store configured. Set 'Vector Store ID' in config or enable auto-create.");
                }
                return;
            }

            
            Puts("Checking existing files in vector store...");
            GetRemoteFileMap(_config.Knowledge.VectorStoreId, remoteFiles =>
            {
                PerformSync(localFiles, _config.Knowledge.VectorStoreId, remoteFiles);
            });
        }

        private void GetRemoteFileMap(string vectorStoreId, Action<Dictionary<string, string>> callback)
        {
            
            ListVectorStoreFiles(vectorStoreId, files =>
            {
                if (files.Count == 0)
                {
                    callback(new Dictionary<string, string>());
                    return;
                }

                var fileMap = new Dictionary<string, string>();
                var remaining = files.Count;

                foreach (var file in files)
                {
                    GetFileInfo(file.Id, fileName =>
                    {
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            fileMap[fileName] = file.Id;
                        }

                        remaining--;
                        if (remaining == 0)
                        {
                            callback(fileMap);
                        }
                    });
                }
            });
        }

        private void PerformSync(string[] localFiles, string vectorStoreId, Dictionary<string, string> remoteFiles)
        {
            var localFileNames = localFiles.Select(f => Path.GetFileName(f)).ToHashSet();

            
            var toDelete = remoteFiles.Where(kv => !localFileNames.Contains(kv.Key)).ToList();

            
            var toUpdate = localFiles.Where(f => remoteFiles.ContainsKey(Path.GetFileName(f))).ToArray();

            
            var toAdd = localFiles.Where(f => !remoteFiles.ContainsKey(Path.GetFileName(f))).ToArray();

            Puts($"Sync plan: {toAdd.Length} new, {toUpdate.Length} updated, {toDelete.Count} removed");

            
            var totalOps = toDelete.Count + toUpdate.Length + toAdd.Length;
            if (totalOps == 0)
            {
                Puts("Everything is in sync.");
                return;
            }

            var completed = 0;
            var failed = 0;

            Action checkComplete = () =>
            {
                if (completed + failed >= totalOps)
                {
                    Puts($"Sync complete: {completed} succeeded, {failed} failed");
                    SendConsoleToDiscord("OpenAI: Knowledge Base Sync",
                        $"**New:** {toAdd.Length}\n**Updated:** {toUpdate.Length}\n**Removed:** {toDelete.Count}\n\n**Result:** {completed} succeeded, {failed} failed");
                }
            };

            
            foreach (var file in toDelete)
            {
                DeleteFileFromVectorStore(vectorStoreId, file.Value, vsSuccess =>
                {
                    DeleteFile(file.Value, fileSuccess =>
                    {
                        if (vsSuccess || fileSuccess)
                        {
                            completed++;
                            Puts($"Removed: {file.Key}");
                        }
                        else
                        {
                            failed++;
                            PrintWarning($"Failed to remove: {file.Key}");
                        }
                        checkComplete();
                    });
                });
            }

            
            foreach (var filePath in toUpdate)
            {
                var fileName = Path.GetFileName(filePath);
                var oldFileId = remoteFiles[fileName];

                
                DeleteFileFromVectorStore(vectorStoreId, oldFileId, vsSuccess =>
                {
                    DeleteFile(oldFileId, fileSuccess =>
                    {
                        
                        UploadFile(filePath, newFileId =>
                        {
                            if (string.IsNullOrEmpty(newFileId))
                            {
                                failed++;
                                PrintWarning($"Failed to upload: {fileName}");
                                checkComplete();
                                return;
                            }

                            AddFileToVectorStore(vectorStoreId, newFileId, addSuccess =>
                            {
                                if (addSuccess)
                                {
                                    completed++;
                                    Puts($"Updated: {fileName}");
                                }
                                else
                                {
                                    failed++;
                                    PrintWarning($"Failed to add to store: {fileName}");
                                }
                                checkComplete();
                            });
                        });
                    });
                });
            }

            
            UploadNewFiles(toAdd, vectorStoreId, remoteFiles, (succeeded, failedCount) =>
            {
                completed += succeeded;
                failed += failedCount;
                checkComplete();
            });
        }

        private void UploadNewFiles(string[] files, string vectorStoreId, Dictionary<string, string> remoteFiles, Action<int, int> callback = null)
        {
            if (files.Length == 0)
            {
                callback?.Invoke(0, 0);
                return;
            }

            var uploaded = 0;
            var failed = 0;
            var remaining = files.Length;

            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);

                UploadFile(filePath, fileId =>
                {
                    if (string.IsNullOrEmpty(fileId))
                    {
                        failed++;
                        PrintWarning($"Failed to upload: {fileName}");
                        remaining--;
                        if (remaining == 0) callback?.Invoke(uploaded, failed);
                        return;
                    }

                    AddFileToVectorStore(vectorStoreId, fileId, success =>
                    {
                        if (success)
                        {
                            uploaded++;
                            Puts($"Added: {fileName}");
                        }
                        else
                        {
                            failed++;
                            PrintWarning($"Failed to add to store: {fileName}");
                        }

                        remaining--;
                        if (remaining == 0) callback?.Invoke(uploaded, failed);
                    });
                });
            }
        }

        private void ClearVectorStoreFiles(string vectorStoreId, Action onComplete)
        {
            ListVectorStoreFiles(vectorStoreId, files =>
            {
                if (files.Count == 0)
                {
                    onComplete();
                    return;
                }

                Puts($"Removing {files.Count} files...");
                var remaining = files.Count;
                var deleted = 0;

                foreach (var file in files)
                {
                    DeleteFileFromVectorStore(vectorStoreId, file.Id, vsSuccess =>
                    {
                        DeleteFile(file.Id, fileSuccess =>
                        {
                            if (vsSuccess || fileSuccess)
                                deleted++;

                            remaining--;
                            if (remaining == 0)
                            {
                                Puts($"Removed {deleted} files");
                                onComplete();
                            }
                        });
                    });
                }
            });
        }

        #endregion

        #region Console Commands

        [ConsoleCommand("openai.kb")]
        private void CmdKnowledgeBase(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;

            var subcmd = arg.GetString(0, "help");

            switch (subcmd.ToLower())
            {
                case "status":
                    ShowKnowledgeBaseStatus();
                    break;

                case "sync":
                    if (!_config.Knowledge.Enabled)
                    {
                        PrintWarning("Knowledge base is not enabled. Set 'Enable Knowledge Base' to true in config.");
                        return;
                    }
                    SyncFilesToVectorStore();
                    break;

                case "list":
                    ListVectorStores(stores =>
                    {
                        Puts($"=== Vector Stores ({stores.Count}) ===");
                        foreach (var store in stores)
                            Puts($"  {store.Id} - {store.Name} ({store.FileCount} files)");
                        if (stores.Count == 0)
                            Puts("  (no vector stores found)");
                    });
                    break;

                case "files":
                    if (string.IsNullOrEmpty(_config.Knowledge.VectorStoreId))
                    {
                        PrintError("No vector store configured. Run 'openai.kb sync' first.");
                        return;
                    }
                    ListVectorStoreFiles(_config.Knowledge.VectorStoreId, files =>
                    {
                        Puts($"=== Vector Store Files ({files.Count}) ===");
                        var remaining = files.Count;
                        if (remaining == 0)
                        {
                            Puts("  (no files found)");
                            return;
                        }
                        foreach (var file in files)
                        {
                            GetFileInfo(file.Id, fileName =>
                            {
                                Puts($"  {file.Id} - {fileName ?? "unknown"} ({file.Status})");
                            });
                        }
                    });
                    break;

                case "clear":
                    if (string.IsNullOrEmpty(_config.Knowledge.VectorStoreId))
                    {
                        PrintError("No vector store configured.");
                        return;
                    }
                    ClearVectorStoreFiles(_config.Knowledge.VectorStoreId, () =>
                    {
                        Puts("Vector store cleared.");
                    });
                    break;

                default:
                    Puts("Usage: openai.kb <command>");
                    Puts("  status - Show knowledge base configuration");
                    Puts("  sync   - Upload local files to vector store (replaces existing)");
                    Puts("  list   - List available vector stores");
                    Puts("  files  - List files in current vector store");
                    Puts("  clear  - Remove all files from vector store");
                    break;
            }
        }

        [ConsoleCommand("openai.status")]
        private void CmdStatus(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;

            var status = ValidateSetup();
            LogSetupStatus(status);
        }

        [ConsoleCommand("openai.clearcontext")]
        private void CmdClearContext(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;

            var target = arg.GetString(0, "");

            if (string.IsNullOrEmpty(target) || target.ToLower() == "all")
            {
                var count = 0;
                foreach (var session in _sessions.Values)
                {
                    if (!string.IsNullOrEmpty(session.LastResponseId))
                    {
                        session.LastResponseId = null;
                        count++;
                    }
                }
                Puts($"Cleared conversation context for {count} player(s).");
                SendConsoleToDiscord("OpenAI: Context Cleared", $"Cleared conversation context for {count} player(s).");
            }
            else
            {
                
                var player = BasePlayer.activePlayerList.FirstOrDefault(p =>
                    p.UserIDString == target ||
                    p.displayName.ToLower().Contains(target.ToLower()));

                if (player == null)
                {
                    PrintError($"Player not found: {target}");
                    return;
                }

                if (_sessions.TryGetValue(player.UserIDString, out var session))
                {
                    session.LastResponseId = null;
                    Puts($"Cleared conversation context for {player.displayName}.");
                    SendConsoleToDiscord("OpenAI: Context Cleared", $"Cleared conversation context for **{player.displayName}**.");
                }
                else
                {
                    Puts($"No conversation context found for {player.displayName}.");
                }
            }
        }

        [ConsoleCommand("openai.usage")]
        private void CmdUsage(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;

            var sb = new StringBuilder();
            sb.AppendLine($"**Server Daily Total:** {_globalTokensToday:N0} / {_config.RateLimits.DailyTokenBudget:N0}");
            sb.AppendLine($"**Requests This Minute:** {_globalRequestsThisMinute} / {_config.RateLimits.MaxRequestsPerMinute}");

            Puts("=== Token Usage ===");
            Puts($"Server Daily Total: {_globalTokensToday:N0} / {_config.RateLimits.DailyTokenBudget:N0}");
            Puts($"Requests This Minute: {_globalRequestsThisMinute} / {_config.RateLimits.MaxRequestsPerMinute}");
            Puts($"Last Reset: {_lastDailyReset:yyyy-MM-dd}");

            
            if (_config.GlobalBot.Enabled)
            {
                Puts("\n=== Global Chat Bot ===");
                sb.AppendLine("\n**Global Chat Bot:**");
                if (_config.GlobalBot.DailyTokenBudget > 0)
                {
                    Puts($"Bot Tokens: {_globalBotTokensToday:N0} / {_config.GlobalBot.DailyTokenBudget:N0}");
                    sb.AppendLine($"• Bot Tokens: {_globalBotTokensToday:N0} / {_config.GlobalBot.DailyTokenBudget:N0}");
                }
                else
                {
                    Puts("Bot Tokens: Using shared budget");
                    sb.AppendLine("• Bot Tokens: Using shared budget");
                }
                Puts($"Global Context: {(!string.IsNullOrEmpty(_globalBotResponseId) ? "Active" : "None")}");
                Puts($"Team Contexts: {_teamBotResponseIds.Count} active");
                sb.AppendLine($"• Global Context: {(!string.IsNullOrEmpty(_globalBotResponseId) ? "Active" : "None")}");
                sb.AppendLine($"• Team Contexts: {_teamBotResponseIds.Count} active");
            }

            
            var playersWithUsage = _sessions
                .Where(kv => kv.Value.TokensUsedToday > 0 || kv.Value.RequestsToday > 0)
                .ToList();

            if (playersWithUsage.Count > 0)
            {
                Puts($"\n=== Player Usage ({playersWithUsage.Count}) ===");
                sb.AppendLine($"\n**Player Usage ({playersWithUsage.Count}):**");
                foreach (var kv in playersWithUsage.OrderByDescending(x => x.Value.TokensUsedToday))
                {
                    var player = BasePlayer.activePlayerList.FirstOrDefault(p => p.UserIDString == kv.Key);
                    var name = player?.displayName ?? kv.Key;
                    var session = kv.Value;
                    var hasContext = !string.IsNullOrEmpty(session.LastResponseId);
                    Puts($"  {name}: {session.TokensUsedToday:N0} tokens, {session.RequestsToday} requests{(hasContext ? " [active context]" : "")}");
                    sb.AppendLine($"• {name}: {session.TokensUsedToday:N0} tokens, {session.RequestsToday} requests");
                }
            }
            else
            {
                Puts("\nNo player usage recorded today.");
                sb.AppendLine("\nNo player usage recorded today.");
            }
            Puts("===================");

            SendConsoleToDiscord("OpenAI: Usage Report", sb.ToString());
        }

        [ConsoleCommand("openai.clearbot")]
        private void CmdClearBotContext(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;

            var target = arg.GetString(0, "all");

            if (target.ToLower() == "all")
            {
                _globalBotResponseId = null;
                _teamBotResponseIds.Clear();
                Puts("All bot conversation contexts cleared (global + all teams).");
            }
            else if (target.ToLower() == "global")
            {
                _globalBotResponseId = null;
                Puts("Global bot conversation context cleared.");
            }
            else if (target.ToLower() == "teams")
            {
                var count = _teamBotResponseIds.Count;
                _teamBotResponseIds.Clear();
                Puts($"Cleared {count} team bot conversation contexts.");
            }
            else
            {
                Puts("Usage: openai.clearbot [all|global|teams]");
            }
        }

        [ConsoleCommand("openai.personalities")]
        private void CmdPersonalities(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;

            Puts($"=== Bot Personalities ({_personalities.Count}) ===");
            Puts($"Folder: {GetPersonalitiesPath()}");
            Puts($"Current: {_config.GlobalBot.PersonalityPreset}");
            Puts("");
            foreach (var kv in _personalities.OrderBy(p => p.Key))
            {
                var preview = kv.Value.Length > 60 ? kv.Value.Substring(0, 60) + "..." : kv.Value;
                var current = kv.Key.Equals(_config.GlobalBot.PersonalityPreset, StringComparison.OrdinalIgnoreCase) ? " [ACTIVE]" : "";
                Puts($"  {kv.Key}{current}");
                Puts($"    {preview}");
            }
            Puts("================================");
            Puts("To reload personalities, use: oxide.reload OpenAI");
        }

        [ConsoleCommand("openai.resetusage")]
        private void CmdResetUsage(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;

            _globalTokensToday = 0;
            _globalBotTokensToday = 0;
            _lastDailyReset = DateTime.UtcNow.Date;

            foreach (var session in _sessions.Values)
            {
                session.TokensUsedToday = 0;
                session.RequestsToday = 0;
            }

            if (_config.RateLimits.PersistUsageData)
            {
                SaveUsageData();
            }

            Puts("Usage data has been reset.");
            SendConsoleToDiscord("OpenAI: Usage Reset", "All usage data has been reset to zero.");
        }

        [ConsoleCommand("openai.models")]
        private void CmdModels(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;

            if (_availableModels == null || _availableModels.Count == 0)
            {
                Puts("No models cached. Fetching...");
                FetchAvailableModels(success =>
                {
                    if (success && _availableModels != null)
                    {
                        Puts($"Available models ({_availableModels.Count}):");
                        foreach (var model in _availableModels.OrderBy(m => m))
                        {
                            var info = GetModelInfo(model);
                            var flags = new List<string>();
                            if (info.IsReasoningModel) flags.Add("reasoning");
                            if (info.SupportsWebSearch) flags.Add("web-search");
                            var flagStr = flags.Count > 0 ? $" [{string.Join(", ", flags)}]" : "";
                            Puts($"  - {model}{flagStr}");
                        }
                    }
                    else
                    {
                        PrintError("Failed to fetch models");
                    }
                });
            }
            else
            {
                Puts($"Available models ({_availableModels.Count}):");
                foreach (var model in _availableModels.OrderBy(m => m))
                {
                    var info = GetModelInfo(model);
                    var flags = new List<string>();
                    if (info.IsReasoningModel) flags.Add("reasoning");
                    if (info.SupportsWebSearch) flags.Add("web-search");
                    var flagStr = flags.Count > 0 ? $" [{string.Join(", ", flags)}]" : "";
                    Puts($"  - {model}{flagStr}");
                }
            }
        }

        [ConsoleCommand("openai.test")]
        private void CmdTest(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;

            Puts("Testing API connection...");

            if (string.IsNullOrEmpty(_config.Api.ApiKey))
            {
                PrintError("No API key configured. Cannot test connection.");
                return;
            }

            FetchAvailableModels(success =>
            {
                if (success)
                {
                    Puts("API connection successful!");
                    Puts($"Found {_availableModels?.Count ?? 0} available models");

                    if (_availableModels != null && _availableModels.Contains(_config.Api.Model))
                    {
                        Puts($"Configured model '{_config.Api.Model}' is available.");
                    }
                    else if (_availableModels != null)
                    {
                        PrintWarning($"Configured model '{_config.Api.Model}' is NOT available.");
                    }
                }
                else
                {
                    PrintError("API connection failed. Check your API key and network connectivity.");
                }
            });
        }

        private List<string> _testResults;

        [ConsoleCommand("openai.testmodel")]
        private void CmdTestModel(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;

            if (string.IsNullOrEmpty(_config.Api.ApiKey))
            {
                PrintError("No API key configured.");
                return;
            }

            _testResults = new List<string>();
            _testResults.Add($"**Model:** {_config.Api.Model}");

            Puts($"=== Testing Model: {_config.Api.Model} ===");
            Puts("Running capability tests...\n");

            
            TestBasicChat(() =>
            {
                
                if (_config.Api.EnableWebSearch)
                {
                    TestWebSearch(() =>
                    {
                        
                        TestFileSearch();
                    });
                }
                else
                {
                    Puts("[Web Search] Skipped (not enabled in config)");
                    _testResults.Add("• Web Search: Skipped");
                    TestFileSearch();
                }
            });
        }

        private void TestBasicChat(Action onComplete)
        {
            Puts("[Basic Chat] Testing...");

            var payload = new Dictionary<string, object>
            {
                ["model"] = _config.Api.Model,
                ["input"] = "Reply with only the word: OK",
                ["max_output_tokens"] = 50
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);

            webrequest.Enqueue(
                _config.Api.Url,
                jsonPayload,
                (code, response) =>
                {
                    if (code == 200)
                    {
                        Puts("[Basic Chat] PASSED");
                        _testResults.Add("• Basic Chat: PASSED");
                    }
                    else
                    {
                        PrintError($"[Basic Chat] FAILED (HTTP {code})");
                        _testResults.Add($"• Basic Chat: FAILED (HTTP {code})");
                        Debug($"Response: {response}");
                    }
                    onComplete();
                },
                this,
                Oxide.Core.Libraries.RequestMethod.POST,
                GetAuthHeadersWithJson()
            );
        }

        private void TestWebSearch(Action onComplete)
        {
            Puts("[Web Search] Testing...");

            var payload = new Dictionary<string, object>
            {
                ["model"] = _config.Api.Model,
                ["input"] = "What is 1+1? Reply with only the number.",
                ["max_output_tokens"] = 50,
                ["tools"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["type"] = "web_search_preview",
                        ["search_context_size"] = "low"
                    }
                }
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);

            webrequest.Enqueue(
                _config.Api.Url,
                jsonPayload,
                (code, response) =>
                {
                    if (code == 200)
                    {
                        Puts("[Web Search] PASSED");
                        _testResults.Add("• Web Search: PASSED");
                    }
                    else
                    {
                        PrintError($"[Web Search] FAILED (HTTP {code})");
                        _testResults.Add($"• Web Search: FAILED (HTTP {code})");
                        if (response.Contains("tool") || response.Contains("web_search"))
                        {
                            PrintError("[Web Search] Model does not support web_search tool");
                        }
                        Debug($"Response: {response}");
                    }
                    onComplete();
                },
                this,
                Oxide.Core.Libraries.RequestMethod.POST,
                GetAuthHeadersWithJson()
            );
        }

        private void TestFileSearch()
        {
            if (!_config.Knowledge.Enabled || string.IsNullOrEmpty(_config.Knowledge.VectorStoreId))
            {
                Puts("[File Search] Skipped (knowledge base not configured)");
                _testResults.Add("• File Search: Skipped");
                Puts("\n=== Test Complete ===");
                SendConsoleToDiscord("OpenAI: Model Test", string.Join("\n", _testResults));
                return;
            }

            Puts("[File Search] Testing...");

            var payload = new Dictionary<string, object>
            {
                ["model"] = _config.Api.Model,
                ["input"] = "What information do you have? Reply briefly.",
                ["max_output_tokens"] = 100,
                ["tools"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["type"] = "file_search",
                        ["vector_store_ids"] = new[] { _config.Knowledge.VectorStoreId }
                    }
                }
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);

            webrequest.Enqueue(
                _config.Api.Url,
                jsonPayload,
                (code, response) =>
                {
                    if (code == 200)
                    {
                        Puts("[File Search] PASSED");
                        _testResults.Add("• File Search: PASSED");
                    }
                    else
                    {
                        PrintError($"[File Search] FAILED (HTTP {code})");
                        _testResults.Add($"• File Search: FAILED (HTTP {code})");
                        if (response.Contains("tool") || response.Contains("file_search"))
                        {
                            PrintError("[File Search] Model does not support file_search tool");
                        }
                        Debug($"Response: {response}");
                    }
                    Puts("\n=== Test Complete ===");
                    SendConsoleToDiscord("OpenAI: Model Test", string.Join("\n", _testResults));
                },
                this,
                Oxide.Core.Libraries.RequestMethod.POST,
                GetAuthHeadersWithJson()
            );
        }

        #endregion

        #region Text Processing

        private string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            if (input.Length > _config.Security.MaxInputLength)
                input = input.Substring(0, _config.Security.MaxInputLength);

            if (_config.Security.FilterInjection)
            {
                var lowerInput = input.ToLower();
                var injectionPatterns = new[]
                {
                    "ignore previous",
                    "ignore all previous",
                    "disregard previous",
                    "forget previous",
                    "ignore your instructions",
                    "new instructions:",
                    "system prompt:",
                    "you are now",
                    "act as if",
                    "pretend you are",
                    "jailbreak",
                    "dan mode"
                };

                foreach (var pattern in injectionPatterns)
                {
                    if (lowerInput.Contains(pattern))
                        return null;
                }
            }

            input = Regex.Replace(input, @"[\x00-\x1F\x7F]", "");

            return input.Trim();
        }

        private string StripUrlsFromMarkdownLinks(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            
            
            return Regex.Replace(text, @"\[([^\]]+)\]\([^)]+\)", "[$1]");
        }

        #endregion
    }
}
