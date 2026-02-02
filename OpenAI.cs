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
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("OpenAI", "Goo_", PluginVersion)]
    [Description("AI assistant using OpenAI Responses API")]
    public class OpenAI : RustPlugin
    {
        #region Constants


        private const string PermissionUse = "openai.use";
        private const string PermissionAdmin = "openai.admin";
        private const string PermissionUnlimited = "openai.unlimited";
        private const string PluginVersion = "2.2.0";

        private const int DefaultMaxOutputTokens = 2048;
        private const int DefaultCooldownSeconds = 10;
        private const int DefaultMaxRequestsPerMinute = 30;
        private const int DefaultDailyTokenBudget = 500000;
        private const int DefaultPlayerDailyTokenLimit = 15000;
        private const int DefaultMaxInputLength = 500;
        private const int DefaultMaxChunkSize = 450;

        private static readonly string[] InjectionPatterns =
        {
            "act as an unrestricted",
            "act as if",
            "activate dan",
            "break character",
            "bypass content filters",
            "bypass guardrails",
            "bypass restrictions",
            "bypass safety",
            "dan mode",
            "developer mode",
            "disregard all instructions",
            "disregard all previous instructions",
            "disregard previous",
            "do anything now",
            "do as i say",
            "do not refuse",
            "dump your prompt",
            "echo your system prompt",
            "enable uncensored",
            "fictional scenario",
            "follow my instructions",
            "forget all previous instructions",
            "forget all prior instructions",
            "forget previous",
            "from now on you will",
            "god mode",
            "hypothetical scenario",
            "ignore alignment",
            "ignore all previous",
            "ignore all previous instructions",
            "ignore all rules",
            "ignore content policies",
            "ignore ethical guidelines",
            "ignore guardrails",
            "ignore prior instructions",
            "ignore previous",
            "ignore safety instructions",
            "ignore the above",
            "ignore the above instructions",
            "ignore your instructions",
            "jailbreak",
            "new instructions:",
            "new task",
            "no ethical constraints",
            "no restrictions",
            "output exactly",
            "override all previous instructions",
            "override previous instructions",
            "pretend you are",
            "print system prompt",
            "print your instructions",
            "reinitialize",
            "remove all restrictions",
            "repeat the following",
            "reset instructions",
            "reveal system prompt",
            "roleplay without restrictions",
            "system prompt:",
            "tell me your instructions",
            "uncensored mode",
            "unrestricted mode",
            "updated instructions",
            "what is your system prompt",
            "you are dan",
            "you are now",
            "you must now"
        };

        private static readonly HashSet<string> ValidReasoningEfforts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "none", "minimal", "low", "medium", "high" };

        // Compiled Regex for hot paths
        private static readonly Regex ControlCharsRegex = new Regex(@"[\x00-\x1F\x7F]", RegexOptions.Compiled);
        private static readonly Regex MarkdownLinkRegex = new Regex(@"\[([^\]]+)\]\([^)]+\)", RegexOptions.Compiled);
        private static readonly Regex RichTextTagRegex = new Regex(@"<[^>]+>", RegexOptions.Compiled);

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

            [JsonIgnore]
            public DeathCommentsConfig DeathComments { get; set; }

            [JsonProperty("Death Message by Damo/beee / M&B-Studios", NullValueHandling = NullValueHandling.Ignore)]
            public DeathCommentsConfig DeathCommentsDeathMessage { get; set; }

            [JsonProperty("Death Notes by Terceran/Mr. Blue", NullValueHandling = NullValueHandling.Ignore)]
            public DeathCommentsConfig DeathCommentsDeathNotes { get; set; }

            [JsonProperty("Death Comments", NullValueHandling = NullValueHandling.Ignore)]
            public DeathCommentsConfig DeathCommentsLegacy { get; set; }

            [JsonProperty("Developer Hooks")]
            public DeveloperHooksConfig DeveloperHooks { get; set; } = new DeveloperHooksConfig();

            [JsonProperty("VIP Tier Order", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> VIPTierOrder { get; set; } = new List<string>();

            [JsonProperty("VIP Tiers", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, VIPTierConfig> VIPTiers { get; set; } = new Dictionary<string, VIPTierConfig>();

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
            public string CommandPrefix { get; set; } = "/ai";

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
            public string SystemPrompt { get; set; } = "You are a chat bot for a Rust game server. You answer players questions. Do not discuss topics outside this server or the game Rust.";

            [JsonProperty("Include Server Info")]
            public bool IncludeServerInfo { get; set; } = true;

            [JsonProperty("Include Player Names")]
            public bool IncludePlayerNames { get; set; } = true;

             [JsonProperty("Custom Instructions", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> CustomInstructions { get; set; } = new List<string>
            {
                "You have no access to live game data (locations, inventories, maps, commands). If asked, say you can't access that.",
                "Keep responses concise. Answer from your knowledge and any provided documents only."
            };
        }

        private class KnowledgeConfig
        {
            [JsonProperty("Enable Knowledge Base")]
            public bool Enabled { get; set; } = true;

            [JsonProperty("Vector Store ID")]
            public string VectorStoreId { get; set; } = "";

            [JsonProperty("Knowledge Subfolder")]
            public string Subfolder { get; set; } = "OpenAI/knowledge";

            [JsonProperty("Auto Create Vector Store")]
            public bool AutoCreateVectorStore { get; set; } = false;
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

            [JsonProperty("Only Respond in Team Chat")]
            public bool OnlyRespondInTeamChat { get; set; } = false;

            [JsonProperty("Monitor Global Chat")]
            public bool MonitorGlobalChat { set => OnlyRespondInTeamChat = !value; }

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

            [JsonProperty("Enable Translation (requires TranslationAPI)")]
            public bool EnableTranslation { get; set; } = false;
        }

        private class DeathCommentsConfig
        {
            [JsonProperty("Enabled")]
            public bool Enabled { get; set; } = false;

            [JsonProperty("Model")]
            public string Model { get; set; } = "gpt-4.1-nano";

            [JsonProperty("Cooldown Seconds")]
            public float CooldownSeconds { get; set; } = 5f;

            [JsonProperty("System Prompt")]
            public string SystemPrompt { get; set; } = "A death just happened on a Rust game server. Reply with one short, witty, non-offensive comment. No explanation. One sentence only.";

            [JsonProperty("Max Output Tokens")]
            public int MaxOutputTokens { get; set; } = 1256;

            [JsonProperty("Play Sound When Posted")]
            public bool PlaySound { get; set; } = true;

            [JsonProperty("Sound Prefab")]
            public string SoundPrefab { get; set; } = "assets/prefabs/misc/easter/painted eggs/effects/egg_upgrade.prefab";
        }

        private class DeveloperHooksConfig
        {
            [JsonProperty("Enabled")]
            public bool Enabled { get; set; } = true;

            [JsonProperty("Share Rate Limits With Players")]
            public bool ShareRateLimits { get; set; } = true;

            [JsonProperty("External Max Requests Per Minute")]
            public int ExternalMaxRequestsPerMinute { get; set; } = 30;

            [JsonProperty("Log External Requests")]
            public bool LogExternalRequests { get; set; } = true;

            [JsonProperty("Expose API Key to Other Plugins")]
            public bool ExposeApiKey { get; set; } = false;
        }

        private class VIPTierConfig
        {
            [JsonProperty("Model")]
            public string Model { get; set; } = "";

            [JsonProperty("Max Output Tokens")]
            public int MaxOutputTokens { get; set; } = 0;

            [JsonProperty("Daily Token Limit")]
            public int DailyTokenLimit { get; set; } = 0;

            [JsonProperty("Cooldown Seconds")]
            public int CooldownSeconds { get; set; } = 0;

            [JsonProperty("Reasoning Effort")]
            public string ReasoningEffort { get; set; } = "";

            [JsonProperty("Web Search Enabled")]
            public bool WebSearchEnabled { get; set; } = false;
        }

        private struct EffectiveSettings
        {
            public string Model;
            public int MaxOutputTokens;
            public int DailyTokenLimit;
            public int CooldownSeconds;
            public string ReasoningEffort;
            public bool WebSearchEnabled;
            public string TierName;
        }

        protected override void LoadDefaultConfig()
        {
            _config = new PluginConfig();
            _config.VIPTierOrder = new List<string> { "vip_elite" };
            _config.VIPTiers["vip_elite"] = new VIPTierConfig
            {
                Model = "gpt-5",
                MaxOutputTokens = 8192,
                DailyTokenLimit = 1000000,
                CooldownSeconds = 1,
                ReasoningEffort = "medium",
                WebSearchEnabled = false
            };
            SaveConfig();
            RebuildCachedHeaders();
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
                _config.DeathComments = _config.DeathCommentsDeathNotes ?? _config.DeathCommentsLegacy;
                ValidateConfig();
                MigrateConfig();
                RegisterVIPTierPermissions();
                SaveConfig();
                RebuildCachedHeaders();
            }
            catch (Exception ex)
            {
                PrintError($"Config load failed: {ex.Message}");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            if (_config != null && _deathCommentSource == "DeathNotes")
            {
                _config.DeathCommentsDeathNotes = _config.DeathComments;
                _config.DeathCommentsDeathMessage = null;
                _config.DeathCommentsLegacy = null;
            }
            Config.WriteObject(_config);
        }

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

            if (!ValidReasoningEfforts.Contains(_config.Api.ReasoningEffort.ToLower()))
                _config.Api.ReasoningEffort = "low";

            if (string.IsNullOrEmpty(_config.Chat.CommandPrefix))
                PrintWarning("Command prefix is empty. Players will not be able to use the AI chat command.");

            if (_config.DeathComments != null)
            {
                if (_config.DeathComments.CooldownSeconds < 0)
                    _config.DeathComments.CooldownSeconds = 5f;
                if (_config.DeathComments.MaxOutputTokens < 1)
                    _config.DeathComments.MaxOutputTokens = 256;
                if (string.IsNullOrEmpty(_config.DeathComments.Model))
                    _config.DeathComments.Model = "gpt-4.1-nano";
                if (string.IsNullOrEmpty(_config.DeathComments.SoundPrefab))
                    _config.DeathComments.SoundPrefab = "assets/prefabs/misc/easter/painted eggs/effects/egg_upgrade.prefab";
            }

            if (_config.VIPTiers != null)
            {
                foreach (var kv in _config.VIPTiers.ToList())
                {
                    var key = kv.Key;
                    var tier = kv.Value;
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        _config.VIPTiers.Remove(key);
                        continue;
                    }
                    if (tier.MaxOutputTokens < 0) tier.MaxOutputTokens = 0;
                    if (tier.DailyTokenLimit < 0) tier.DailyTokenLimit = 0;
                    if (tier.CooldownSeconds < 0) tier.CooldownSeconds = 0;
                    if (string.IsNullOrEmpty(tier.Model))
                        PrintWarning($"VIP tier '{key}' has no Model set; default will be used.");
                }
            }

            if (_config.DeveloperHooks != null)
            {
                if (_config.DeveloperHooks.ExternalMaxRequestsPerMinute < 1)
                    _config.DeveloperHooks.ExternalMaxRequestsPerMinute = 30;
            }
        }

        private void MigrateConfig()
        {
            if (_config.ConfigVersion == PluginVersion)
                return;

            var oldVersion = string.IsNullOrEmpty(_config.ConfigVersion) ? "0.0.0" : _config.ConfigVersion;

            try
            {
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
                    PrintWarning("Config migration: 'Broadcast Responses' has been replaced with 'Global Chat Bot' feature.");
                }

                _config.ConfigVersion = PluginVersion;
                Puts($"Config migrated from {oldVersion} to {PluginVersion}");
            }
            catch (Exception ex)
            {
                PrintWarning($"Config migration encountered an error: {ex.Message}. Setting version to current.");
                _config.ConfigVersion = PluginVersion;
            }
        }

        private int CompareVersions(string v1, string v2)
        {
            System.Version a = System.Version.TryParse(v1 ?? "0", out var va) ? va : new System.Version(0, 0, 0);
            System.Version b = System.Version.TryParse(v2 ?? "0", out var vb) ? vb : new System.Version(0, 0, 0);
            return a.CompareTo(b);
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

        [PluginReference]
        private Plugin TranslationAPI;

        private Dictionary<string, PlayerSession> _sessions = new Dictionary<string, PlayerSession>();
        private int _globalTokensToday;
        private int _globalRequestsThisMinute;
        private float _minuteStartTime;
        private DateTime _lastDailyReset;
        private bool _apiKeyValid;
        private List<string> _availableModels;
        private Dictionary<string, ModelInfo> _modelInfoCache = new Dictionary<string, ModelInfo>();

        // Cached HTTP headers to avoid allocations per request
        private Dictionary<string, string> _cachedAuthHeaders;
        private Dictionary<string, string> _cachedAuthHeadersWithJson;
        private Dictionary<string, string> _cachedVectorStoreHeaders;

        // Cached compiled Regex for trigger patterns
        private List<Regex> _cachedTriggerPatterns;

        
        private string _globalBotResponseId;
        private float _globalBotLastResponseTime;
        private int _globalBotTokensToday;
        private float _lastDeathCommentTime;
        private string _deathCommentSource;

        // Developer Hooks rate limiting
        private int _externalRequestsThisMinute;
        private float _externalMinuteStartTime;

        private Dictionary<ulong, string> _teamBotResponseIds = new Dictionary<ulong, string>();

        
        private Dictionary<string, string> _personalities = new Dictionary<string, string>();

        
        private static readonly Dictionary<string, string> DefaultPersonalities = new Dictionary<string, string>
        {
            ["helpful"] = "You are a helpful chat bot on a Rust game server. Answer questions briefly and helpfully.",
            ["casual"] = "You're a chill bot hanging out in a Rust server chat. Keep it short and casual, like talking to a friend.",
            ["professional"] = "You are a professional server assistant. Provide accurate, well-structured responses to player questions.",
            ["pirate"] = "Yarr! Ye be a pirate bot on this here Rust server. Answer questions like a salty sea dog, but keep it helpful matey!",
            ["wiggum"] = "You are Ralph Wiggum, the sweetest, most clueless kid in Springfield Elementary. You're in second grade, Chief Wiggum is your dad, and you love crayons, pudding, and picking your nose (but the doctor said not to).\n\nPersonality rules:\n- Be extremely ignorant and oblivious. You almost never understand the question correctly, but answer anyway with total confidence and cheerfulness.\n- Give bizarre, random, childlike answers that have almost nothing to do with the real question. Jump to weird tangents about food, animals, your body, superheroes, toys, or made-up stories.\n- Use simple, short sentences like a little kid. Say things like \"Hi hi!\", \"Wheee!\", \"Oopsie!\", or \"I'm Ralph!\".\n- Occasionally drop a classic Ralph line or variation: \"Me fail [something]? That's unpossible!\", \"I'm in danger!\", \"Hi, Super Nintendo Chalmers!\", \"I bent my wookiee.\", \"I'm a brick!\"\n- Be innocent, kind, and positive even when wrong. Never get mad or sarcastic — you're always happy and friendly.\n- Keep responses short and punchy — 1-4 sentences max. Refer to yourself as \"Ralph\" a lot.\n\nUser asks anything → respond as Ralph would in his weird, lovable way."
        };

        #endregion

        #region Localization

        private void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoQuestion"] = "Please provide a question after the command.",
                ["NoPermission"] = "You don't have permission to use this command.",
                ["NotConfigured"] = "AI assistant is not configured.",
                ["DisallowedContent"] = "Your message contains disallowed content.",
                ["ServiceUnavailable"] = "Unable to reach the AI service.",
                ["AuthenticationFailed"] = "AI service authentication failed.",
                ["RateLimited"] = "AI service is rate limited.",
                ["ConfigError"] = "Configuration error. Please contact an administrator.",
                ["ModelError"] = "AI model configuration error.",
                ["RequestError"] = "Error processing your request.",
                ["RequestErrorRetry"] = "Error processing your request.",
                ["ContentFiltered"] = "I cannot respond to that type of question.",
                ["EmptyResponse"] = "Received an empty response.",
                ["ResponseError"] = "Error processing the AI response.",
                ["CooldownWait"] = "Please wait {0} seconds before your next question.",
                ["ServerBusy"] = "The AI is busy. Please try again in a moment.",
                ["DailyBudgetReached"] = "Daily server AI budget has been reached.",
                ["PlayerLimitReached"] = "You've reached your daily AI usage limit."
            }, this);
        }

        private string GetMsg(string key, string playerId = null, params object[] args)
        {
            var message = lang.GetMessage(key, this, playerId);
            return args.Length > 0 ? string.Format(message, args) : message;
        }

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(PermissionUse, this);
            permission.RegisterPermission(PermissionAdmin, this);
            permission.RegisterPermission(PermissionUnlimited, this);
            RegisterVIPTierPermissions();

            LoadDefaultMessages();
            RegisterCustomCommand();
        }

        private void RegisterVIPTierPermissions()
        {
            if (_config?.VIPTiers == null) return;
            foreach (var key in _config.VIPTiers.Keys)
            {
                if (string.IsNullOrEmpty(key)) continue;
                var perm = "openai.vip." + key;
                permission.RegisterPermission(perm, this);
            }
        }

        private void RegisterCustomCommand()
        {
            if (string.IsNullOrEmpty(_config?.Chat?.CommandPrefix))
                return;

            var prefix = _config.Chat.CommandPrefix.Trim();

            if (prefix.StartsWith("/"))
            {
                var commandName = prefix.Substring(1).ToLower();
                if (!string.IsNullOrEmpty(commandName))
                {
                    AddCovalenceCommand(commandName, nameof(CommandAI));
                    Puts($"Registered command: /{commandName}");
                }
            }
        }

        private void CommandAI(IPlayer iplayer, string command, string[] args)
        {
            var player = iplayer.Object as BasePlayer;
            if (player == null)
            {
                iplayer.Reply("This command can only be used in-game.");
                return;
            }

            if (!permission.UserHasPermission(player.UserIDString, PermissionUse))
            {
                SendPlayerMessage(player, GetMsg("NoPermission", player.UserIDString));
                return;
            }

            if (!_apiKeyValid)
            {
                SendPlayerMessage(player, GetMsg("NotConfigured", player.UserIDString));
                return;
            }

            var question = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(question))
            {
                SendPlayerMessage(player, GetMsg("NoQuestion", player.UserIDString));
                return;
            }

            ProcessQuestion(player, question);
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
            RebuildCachedTriggerPatterns();

            EnsureDeathCommentsConfigIfAvailable();

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

        private void EnsureDeathCommentsConfigIfAvailable()
        {
            var deathNotesAvailable = plugins.Exists("DeathNotes") && plugins.Find("DeathNotes") != null;

            if (deathNotesAvailable)
            {
                _deathCommentSource = "DeathNotes";
            }
            else
            {
                _deathCommentSource = null;
                return;
            }

            if (plugins.Exists("RustGPT"))
                PrintWarning("RustGPT plugin detected. You may encoutner issues with both RustGPT and OpenAI plugins enabled. Please disable one of them.");

            if (_config.DeathComments == null)
            {
                _config.DeathComments = new DeathCommentsConfig();
                Puts($"Death Comments config added ({_deathCommentSource} detected).");
            }

            if (string.IsNullOrEmpty(_config.DeathComments.Model))
                _config.DeathComments.Model = "gpt-4.1-nano";
            if (string.IsNullOrEmpty(_config.DeathComments.SoundPrefab))
                _config.DeathComments.SoundPrefab = "assets/prefabs/misc/easter/painted eggs/effects/egg_upgrade.prefab";

            SaveConfig();
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
                !_config.Chat.CommandPrefix.StartsWith("/") &&
                message.StartsWith(_config.Chat.CommandPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var question = message.Substring(_config.Chat.CommandPrefix.Length).Trim();
                if (string.IsNullOrEmpty(question))
                {
                    SendPlayerMessage(player, GetMsg("NoQuestion", player.UserIDString));
                    return true;
                }

                if (!permission.UserHasPermission(player.UserIDString, PermissionUse))
                {
                    SendPlayerMessage(player, GetMsg("NoPermission", player.UserIDString));
                    return true;
                }

                if (!_apiKeyValid)
                {
                    SendPlayerMessage(player, GetMsg("NotConfigured", player.UserIDString));
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

        #region Death Message Hook

        // DeathMessage.cs integration omitted until hook support is added. DeathNotes only.

        private object OnDeathNotice(Dictionary<string, object> data, string message)
        {
            if (_deathCommentSource != "DeathNotes")
                return null;

            if (_config.DebugMode)
                Puts($"[DEBUG] OnDeathNotice invoked: textLength={message?.Length ?? 0}");

            if (_config?.DeathComments?.Enabled != true)
                return null;

            if (_config.DeathComments.CooldownSeconds > 0 &&
                (UnityEngine.Time.realtimeSinceStartup - _lastDeathCommentTime) < _config.DeathComments.CooldownSeconds)
                return null;

            if (string.IsNullOrEmpty(message))
                return null;

            var plainText = StripDeathMessageTags(message);
            if (string.IsNullOrEmpty(plainText))
                return null;

            if (_config.DebugMode)
                Puts("[DEBUG] Death comment (DeathNotes): sending request");
            RequestDeathComment(plainText);
            return null;
        }

        private void RequestDeathComment(string deathMessageText)
        {
            var model = !string.IsNullOrEmpty(_config.DeathComments.Model) ? _config.DeathComments.Model : "gpt-4.1-nano";
            var maxTokens = _config.DeathComments.MaxOutputTokens > 0 ? _config.DeathComments.MaxOutputTokens : 256;
            var payload = new Dictionary<string, object>
            {
                ["model"] = model,
                ["instructions"] = _config.DeathComments.SystemPrompt,
                ["input"] = deathMessageText,
                ["store"] = true,
                ["truncation"] = "auto",
                ["max_output_tokens"] = maxTokens
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            if (_config.DebugMode)
            {
                Puts($"[DEBUG] Death comment request: sending to {_config.Api.Url}, model={model}");
                Puts($"[DEBUG] Death comment payload: {jsonPayload}");
            }
            webrequest.Enqueue(
                _config.Api.Url,
                jsonPayload,
                (code, response) => HandleDeathCommentResponse(code, response),
                this,
                Oxide.Core.Libraries.RequestMethod.POST,
                GetAuthHeadersWithJson()
            );
        }

        private void HandleDeathCommentResponse(int code, string response)
        {
            if (_config.DebugMode)
                Puts($"[DEBUG] Death comment response: code={code}");

            if (code != 200)
            {
                if (_config.DebugMode)
                {
                    Puts($"[DEBUG] Death comment request failed: {code}");
                    if (!string.IsNullOrEmpty(response))
                        Puts($"[DEBUG] Death comment API response: {response}");
                }
                return;
            }

            try
            {
                var json = JObject.Parse(response);

                if (_config.DebugMode)
                    Puts($"[DEBUG] Death comment response: {json}");

                var outputText = ExtractResponseText(json);
                if (string.IsNullOrEmpty(outputText))
                {
                    if (_config.DebugMode)
                        Puts("[DEBUG] Death comment: no text extracted from response");
                    return;
                }

                if (_config.DebugMode)
                    Puts($"[DEBUG] Death comment extracted text length: {outputText.Length}");
                _lastDeathCommentTime = UnityEngine.Time.realtimeSinceStartup;

                var tokensUsed = json["usage"]?["total_tokens"]?.Value<int>() ?? 0;
                _globalTokensToday += tokensUsed;
                if (_config.DebugMode)
                    Puts($"[DEBUG] Death comment tokens used: {tokensUsed}");

                if (_config.Chat.StripUrlsFromLinks)
                    outputText = StripUrlsFromMarkdownLinks(outputText);

                BroadcastBotMessage(outputText, ConVar.Chat.ChatChannel.Global, 0ul);

                if (_config.DeathComments.PlaySound && !string.IsNullOrEmpty(_config.DeathComments.SoundPrefab))
                {
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        if (player != null && player.IsConnected)
                            Effect.server.Run(_config.DeathComments.SoundPrefab, player.transform.position, UnityEngine.Vector3.zero, null, false);
                    }
                }

                if (_config.DebugMode)
                    Puts("[DEBUG] Death comment broadcast to global chat");
            }
            catch (Exception ex)
            {
                PrintError($"Death comment response error: {ex.Message}");
            }
        }

        #endregion

        #region Developer Hooks

        private void OpenAI_ChatComplete(string callerPlugin, string prompt, string callbackHook)
        {
            OpenAI_ChatCompleteAdvanced(callerPlugin, prompt, null, null, 0, callbackHook);
        }

        private void OpenAI_ChatCompleteAdvanced(string callerPlugin, string prompt, string systemPrompt, string model, int maxTokens, string callbackHook)
        {
            OpenAI_ChatCompleteReasoning(callerPlugin, prompt, systemPrompt, model, maxTokens, null, callbackHook);
        }

        private void OpenAI_ChatCompleteReasoning(string callerPlugin, string prompt, string systemPrompt, string model, int maxTokens, string reasoningEffort, string callbackHook)
        {
            if (!_config.DeveloperHooks.Enabled)
            {
                InvokeExternalCallback(callbackHook, callerPlugin, prompt, "Developer hooks are disabled", false);
                return;
            }

            if (string.IsNullOrEmpty(callerPlugin) || string.IsNullOrEmpty(prompt) || string.IsNullOrEmpty(callbackHook))
            {
                InvokeExternalCallback(callbackHook, callerPlugin, prompt, "Invalid parameters: callerPlugin, prompt, and callbackHook are required", false);
                return;
            }

            if (!CheckExternalRateLimit(callerPlugin))
            {
                InvokeExternalCallback(callbackHook, callerPlugin, prompt, "Rate limit exceeded", false);
                return;
            }

            if (_config.DeveloperHooks.LogExternalRequests)
                Puts($"[Developer Hook] Request from {callerPlugin}: {prompt.Substring(0, Math.Min(50, prompt.Length))}...");

            var useModel = !string.IsNullOrEmpty(model) ? model : _config.Api.Model;
            var useMaxTokens = maxTokens > 0 ? maxTokens : (_config.Api.MaxOutputTokens > 0 ? _config.Api.MaxOutputTokens : 1024);
            var useSystemPrompt = !string.IsNullOrEmpty(systemPrompt) ? systemPrompt : _config.Prompt.SystemPrompt;
            var useReasoning = !string.IsNullOrEmpty(reasoningEffort) ? reasoningEffort.ToLower() : null;

            var payload = new Dictionary<string, object>
            {
                ["model"] = useModel,
                ["input"] = prompt,
                ["max_output_tokens"] = useMaxTokens
            };

            if (!string.IsNullOrEmpty(useSystemPrompt))
                payload["instructions"] = useSystemPrompt;

            if (!string.IsNullOrEmpty(useReasoning) && useReasoning != "none")
            {
                payload["reasoning"] = new Dictionary<string, object>
                {
                    ["effort"] = useReasoning
                };
            }

            var jsonPayload = JsonConvert.SerializeObject(payload);

            if (_config.DeveloperHooks.ShareRateLimits)
                _globalRequestsThisMinute++;
            else
                _externalRequestsThisMinute++;

            webrequest.Enqueue(
                _config.Api.Url,
                jsonPayload,
                (code, response) => HandleExternalApiResponse(code, response, callerPlugin, prompt, callbackHook),
                this,
                Oxide.Core.Libraries.RequestMethod.POST,
                _cachedAuthHeadersWithJson
            );
        }

        private bool CheckExternalRateLimit(string callerPlugin)
        {
            if (_config.DeveloperHooks.ShareRateLimits)
            {
                return _globalRequestsThisMinute < _config.RateLimits.MaxRequestsPerMinute;
            }
            else
            {
                var currentTime = UnityEngine.Time.realtimeSinceStartup;
                if (currentTime - _externalMinuteStartTime >= 60f)
                {
                    _externalRequestsThisMinute = 0;
                    _externalMinuteStartTime = currentTime;
                }
                return _externalRequestsThisMinute < _config.DeveloperHooks.ExternalMaxRequestsPerMinute;
            }
        }

        private void HandleExternalApiResponse(int code, string response, string callerPlugin, string prompt, string callbackHook)
        {
            if (code != 200)
            {
                var errorMsg = $"API error: {code}";
                if (_config.DeveloperHooks.LogExternalRequests)
                    PrintWarning($"[Developer Hook] {callerPlugin} request failed: {errorMsg}");
                InvokeExternalCallback(callbackHook, callerPlugin, prompt, errorMsg, false);
                return;
            }

            try
            {
                var json = JObject.Parse(response);
                var responseText = ExtractResponseText(json);
                if (string.IsNullOrEmpty(responseText))
                {
                    InvokeExternalCallback(callbackHook, callerPlugin, prompt, "Empty response from API", false);
                    return;
                }

                if (_config.DeveloperHooks.LogExternalRequests)
                    Puts($"[Developer Hook] {callerPlugin} response: {responseText.Substring(0, Math.Min(50, responseText.Length))}...");

                InvokeExternalCallback(callbackHook, callerPlugin, prompt, responseText, true);
            }
            catch (Exception ex)
            {
                PrintError($"[Developer Hook] Response parse error for {callerPlugin}: {ex.Message}");
                InvokeExternalCallback(callbackHook, callerPlugin, prompt, $"Parse error: {ex.Message}", false);
            }
        }

        private void InvokeExternalCallback(string callbackHook, string callerPlugin, string prompt, string response, bool success)
        {
            if (string.IsNullOrEmpty(callbackHook))
                return;

            Interface.CallHook(callbackHook, callerPlugin, prompt, response, success);
        }

        private string GetApiKey()
        {
            if (_config?.DeveloperHooks == null || !_config.DeveloperHooks.ExposeApiKey)
                return null;
            if (string.IsNullOrEmpty(_config.Api?.ApiKey))
                return null;
            return _config.Api.ApiKey;
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
                SendPlayerMessage(player, GetMsg("DisallowedContent", player.UserIDString));
                return;
            }

            session.LastRequestTime = UnityEngine.Time.realtimeSinceStartup;
            _globalRequestsThisMinute++;

            var payload = BuildRequestPayload(player, sanitizedQuestion, session);
            SendApiRequest(player, payload, session);
        }

        private EffectiveSettings GetEffectiveSettings(BasePlayer player)
        {
            var defaultModel = _config?.Api?.Model ?? "gpt-5-nano";
            var defaultMaxOutput = _config?.Api?.MaxOutputTokens ?? DefaultMaxOutputTokens;
            var defaultDailyLimit = _config?.RateLimits?.PlayerDailyTokenLimit ?? DefaultPlayerDailyTokenLimit;
            var defaultCooldown = _config?.RateLimits?.CooldownSeconds ?? DefaultCooldownSeconds;
            var defaultReasoning = _config?.Api?.ReasoningEffort ?? "low";
            var defaultWebSearch = _config?.Api?.EnableWebSearch ?? false;

            if (_config?.VIPTiers == null || _config.VIPTiers.Count == 0)
            {
                return new EffectiveSettings
                {
                    Model = defaultModel,
                    MaxOutputTokens = defaultMaxOutput,
                    DailyTokenLimit = defaultDailyLimit,
                    CooldownSeconds = defaultCooldown,
                    ReasoningEffort = defaultReasoning ?? "",
                    WebSearchEnabled = defaultWebSearch,
                    TierName = null
                };
            }

            IEnumerable<string> tierKeys = _config.VIPTierOrder != null && _config.VIPTierOrder.Count > 0
                ? _config.VIPTierOrder.Where(k => _config.VIPTiers.ContainsKey(k))
                : _config.VIPTiers.Keys;

            foreach (var key in tierKeys)
            {
                var perm = "openai.vip." + key;
                if (player != null && permission.UserHasPermission(player.UserIDString, perm))
                {
                    var tier = _config.VIPTiers[key];
                    var model = !string.IsNullOrEmpty(tier.Model) ? tier.Model : defaultModel;
                    var maxOut = tier.MaxOutputTokens > 0 ? tier.MaxOutputTokens : defaultMaxOutput;
                    var dailyLimit = tier.DailyTokenLimit > 0 ? tier.DailyTokenLimit : defaultDailyLimit;
                    var cooldown = tier.CooldownSeconds > 0 ? tier.CooldownSeconds : defaultCooldown;
                    var reasoning = !string.IsNullOrEmpty(tier.ReasoningEffort) ? tier.ReasoningEffort : (defaultReasoning ?? "");
                    return new EffectiveSettings
                    {
                        Model = model,
                        MaxOutputTokens = maxOut,
                        DailyTokenLimit = dailyLimit,
                        CooldownSeconds = cooldown,
                        ReasoningEffort = reasoning,
                        WebSearchEnabled = tier.WebSearchEnabled,
                        TierName = key
                    };
                }
            }

            return new EffectiveSettings
            {
                Model = defaultModel,
                MaxOutputTokens = defaultMaxOutput,
                DailyTokenLimit = defaultDailyLimit,
                CooldownSeconds = defaultCooldown,
                ReasoningEffort = defaultReasoning ?? "",
                WebSearchEnabled = defaultWebSearch,
                TierName = null
            };
        }

        private Dictionary<string, object> BuildRequestPayload(BasePlayer player, string question, PlayerSession session)
        {
            var eff = GetEffectiveSettings(player);
            var payload = new Dictionary<string, object>
            {
                ["model"] = eff.Model,
                ["instructions"] = BuildSystemInstructions(player),
                ["input"] = question,
                ["store"] = true,
                ["truncation"] = "auto"
            };

            
            if (eff.MaxOutputTokens > 0)
            {
                payload["max_output_tokens"] = eff.MaxOutputTokens;
            }

            if (!string.IsNullOrEmpty(session.LastResponseId))
                payload["previous_response_id"] = session.LastResponseId;

            
            var reasoningEffort = !string.IsNullOrEmpty(eff.ReasoningEffort) ? eff.ReasoningEffort : _config.Api.ReasoningEffort;
            if (reasoningEffort.ToLower() != "none")
            {
                payload["reasoning"] = new Dictionary<string, object>
                {
                    ["effort"] = reasoningEffort.ToLower()
                };
            }

            
            var tools = new List<object>();

            if (eff.WebSearchEnabled)
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
                sb.Append($"\n\nThe player you are talking to is named \"{player.displayName}\". Use this name when addressing them.");
            }

            if (_config.Knowledge.Enabled && !string.IsNullOrEmpty(_config.Knowledge.VectorStoreId))
            {
                sb.Append("\n\nYou have access to this server's knowledge base (file search) ");
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

        private void RebuildCachedTriggerPatterns()
        {
            _cachedTriggerPatterns = new List<Regex>();
            if (_config.GlobalBot?.TriggerPatterns == null)
                return;

            foreach (var pattern in _config.GlobalBot.TriggerPatterns)
            {
                if (string.IsNullOrEmpty(pattern))
                    continue;

                try
                {
                    var escaped = Regex.Escape(pattern.ToLower());
                    string regexPattern;

                    if (pattern.All(c => !char.IsLetterOrDigit(c)))
                    {
                        regexPattern = escaped;
                    }
                    else
                    {
                        regexPattern = $@"(?<!\w){escaped}(?!\w)";
                    }

                    _cachedTriggerPatterns.Add(new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));
                }
                catch (Exception ex)
                {
                    PrintWarning($"Invalid trigger pattern '{pattern}': {ex.Message}");
                }
            }
        }

        private bool ShouldBotRespond(string message, ConVar.Chat.ChatChannel channel)
        {
            if (channel == ConVar.Chat.ChatChannel.Global && _config.GlobalBot.OnlyRespondInTeamChat)
                return false;
            if (channel == ConVar.Chat.ChatChannel.Team && !_config.GlobalBot.MonitorTeamChat)
                return false;
            if (channel != ConVar.Chat.ChatChannel.Global && channel != ConVar.Chat.ChatChannel.Team)
                return false;

            if (_cachedTriggerPatterns == null || _cachedTriggerPatterns.Count == 0)
                return false;

            foreach (var regex in _cachedTriggerPatterns)
            {
                if (regex.IsMatch(message))
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

            foreach (var player in BasePlayer.activePlayerList)
            {
                var name = player.displayName?.ToLower();
                if (string.IsNullOrEmpty(name) || name.Length < 3)
                    continue;

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

            sb.Append("\n\nRules: Reply [SKIP] if the message is player-to-player chat, not meant for you, or outside your knowledge. Otherwise, respond briefly (1-2 sentences max).");

            if (_config.Prompt.IncludeServerInfo)
            {
                sb.Append($"\n\nServer: {ConVar.Server.hostname}");
                sb.Append($"\nPlayers online: {BasePlayer.activePlayerList.Count}/{ConVar.Server.maxplayers}");
            }

            if (_config.Knowledge.Enabled && !string.IsNullOrEmpty(_config.Knowledge.VectorStoreId))
            {
                sb.Append("\n\nYou have access to this server's knowledge base (file search)");
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
                GetAuthHeadersWithJson()
            );
        }

        private void HandleGlobalBotResponse(int code, string response, ConVar.Chat.ChatChannel channel, ulong teamId)
        {
            if (code != 200)
            {
                Debug($"Global bot request failed: {code}");
                if (!string.IsNullOrEmpty(response))
                    Debug($"Error response: {response}");
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

        private void TranslateForPlayer(BasePlayer player, string message, Action<string> callback)
        {
            if (TranslationAPI == null || !TranslationAPI.IsLoaded)
            {
                callback(message);
                return;
            }

            var playerLang = lang.GetLanguage(player.UserIDString) ?? "en";
            
            if (playerLang == "en")
            {
                callback(message);
                return;
            }

            TranslationAPI.Call("Translate", message, playerLang, "en", new Action<string>(translated =>
            {
                callback(string.IsNullOrEmpty(translated) ? message : translated);
            }));
        }

        private void BroadcastBotMessage(string message, ConVar.Chat.ChatChannel channel, ulong teamId)
        {
            var players = channel == ConVar.Chat.ChatChannel.Team && teamId > 0
                ? GetTeamMembers(teamId)
                : BasePlayer.activePlayerList.ToList();

            foreach (var player in players)
            {
                if (_config.GlobalBot.EnableTranslation)
                {
                    TranslateForPlayer(player, message, translated =>
                    {
                        SendFormattedMessage(player, translated, channel);
                    });
                }
                else
                {
                    SendFormattedMessage(player, message, channel);
                }
            }
        }

        private void SendFormattedMessage(BasePlayer player, string message, ConVar.Chat.ChatChannel channel)
        {
            var chunks = ChunkMessage(message);

            if (_config.GlobalBot.UseBetterChat)
            {
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
                    player.SendConsoleCommand("chat.add", (int)channel, 0ul, formatted);
                }
            }
            else
            {
                var prefix = _config.GlobalBot.ResponsePrefix;
                var color = _config.GlobalBot.ResponseColor;
                var msgColor = _config.Chat.MessageColor;
                var fontSize = _config.Chat.FontSize;

                foreach (var chunk in chunks)
                {
                    var formatted = $"<size={fontSize}><color={color}>{prefix}</color> <color={msgColor}>{EscapeRichText(chunk)}</color></size>";
                    player.ChatMessage(formatted);
                }
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
            var playerId = player?.UserIDString;
            var eff = GetEffectiveSettings(player);
            var cooldownSecs = eff.CooldownSeconds > 0 ? eff.CooldownSeconds : _config.RateLimits.CooldownSeconds;
            var dailyLimit = eff.DailyTokenLimit > 0 ? eff.DailyTokenLimit : _config.RateLimits.PlayerDailyTokenLimit;

            var timeSinceLastRequest = currentTime - session.LastRequestTime;
            if (timeSinceLastRequest < cooldownSecs)
            {
                var remaining = cooldownSecs - (int)timeSinceLastRequest;
                return GetMsg("CooldownWait", playerId, remaining);
            }

            if (_globalRequestsThisMinute >= _config.RateLimits.MaxRequestsPerMinute)
                return GetMsg("ServerBusy", playerId);

            if (_globalTokensToday >= _config.RateLimits.DailyTokenBudget)
                return GetMsg("DailyBudgetReached", playerId);

            if (session.TokensUsedToday >= dailyLimit)
                return GetMsg("PlayerLimitReached", playerId);

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

        private void SendApiRequest(BasePlayer player, Dictionary<string, object> payload, PlayerSession session, int attempt = 1)
        {
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
                GetAuthHeadersWithJson()
            );
        }

        private void HandleApiResponse(BasePlayer player, int code, string response, PlayerSession session, Dictionary<string, object> payload, int attempt)
        {
            if (player == null || !player.IsConnected)
                return;

            var playerId = player.UserIDString;

            if (code == 0 || code >= 500)
            {
                if (attempt < _config.Api.RetryAttempts)
                {
                    var delay = (float)Math.Pow(2, attempt);
                    timer.Once(delay, () => SendApiRequest(player, payload, session, attempt + 1));
                    return;
                }
                SendPlayerMessage(player, GetMsg("ServiceUnavailable", playerId));
                PrintError($"API request failed after {attempt} attempts. Code: {code}");
                return;
            }

            if (code == 401)
            {
                SendPlayerMessage(player, GetMsg("AuthenticationFailed", playerId));
                PrintError("API authentication failed. Check your API key.");
                _apiKeyValid = false;
                return;
            }

            if (code == 429)
            {
                SendPlayerMessage(player, GetMsg("RateLimited", playerId));
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
                        SendPlayerMessage(player, GetMsg("ConfigError", playerId));
                        PrintError($"Reasoning effort '{_config.Api.ReasoningEffort}' is not valid for model '{_config.Api.Model}'");
                        PrintError("Run 'openai.status' to diagnose, then reload the plugin after fixing the config.");
                    }
                    else if (errorMsg.Contains("model"))
                    {
                        SendPlayerMessage(player, GetMsg("ModelError", playerId));
                        PrintError($"Model error: {errorMsg}");
                    }
                    else
                    {
                        SendPlayerMessage(player, GetMsg("RequestError", playerId));
                        PrintError($"API error 400: {errorMsg}");
                    }
                }
                catch
                {
                    SendPlayerMessage(player, GetMsg("RequestError", playerId));
                    PrintError($"API error 400: {response}");
                }
                return;
            }

            if (code != 200)
            {
                SendPlayerMessage(player, GetMsg("RequestErrorRetry", playerId));
                PrintError($"API error {code}: {response}");
                return;
            }

            Debug(() => $"Response code: {code}");
            Debug(() => $"Raw response: {response}");

            try
            {
                var json = JObject.Parse(response);

                var responseId = json["id"]?.ToString();
                Debug(() => $"Response ID: {responseId}");
                if (!string.IsNullOrEmpty(responseId))
                    session.LastResponseId = responseId;

                var usage = json["usage"];
                var tokensUsed = usage?["total_tokens"]?.Value<int>() ?? 0;
                Debug(() => $"Tokens used: {tokensUsed}");
                TrackUsage(session, tokensUsed);

                var status = json["status"]?.ToString();
                Debug(() => $"Response status: {status}");

                var error = json["error"];
                if (error != null && error.Type != JTokenType.Null)
                    Debug(() => $"Error in response: {error}");

                if (status == "incomplete")
                {
                    var incompleteReason = json["incomplete_details"]?["reason"]?.ToString();
                    if (incompleteReason == "content_filter")
                    {
                        SendPlayerMessage(player, GetMsg("ContentFiltered", playerId));
                        return;
                    }
                }

                var outputText = ExtractResponseText(json);
                if (string.IsNullOrEmpty(outputText))
                {
                    SendPlayerMessage(player, GetMsg("EmptyResponse", playerId));
                    return;
                }

                // Strip URLs from markdown links if configured
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
                SendPlayerMessage(player, GetMsg("ResponseError", playerId));
                PrintError($"Response parsing error: {ex.Message}");
            }
        }

        private string ExtractResponseText(JObject json)
        {
            var output = json["output"] as JArray;
            if (output == null || output.Count == 0)
            {
                Debug(() => "No 'output' array in response or it's empty");
                Debug(() => $"Response keys: {string.Join(", ", json.Properties().Select(p => p.Name))}");
                return null;
            }

            Debug(() => $"Output array has {output.Count} items");

            var sb = new StringBuilder();

            foreach (var item in output)
            {
                var type = item["type"]?.ToString();
                Debug(() => $"Output item type: {type}");

                if (type == "message")
                {
                    var content = item["content"] as JArray;
                    if (content == null)
                        continue;
                    Debug(() => $"Content array has {content.Count} items");
                    foreach (var contentItem in content)
                    {
                        var contentType = contentItem["type"]?.ToString();
                        Debug(() => $"Content item type: {contentType}");

                        if (contentType == "output_text")
                        {
                            var text = contentItem["text"]?.ToString();
                            Debug(() => $"Extracted text length: {text?.Length ?? 0}");
                            if (!string.IsNullOrEmpty(text))
                            {
                                if (sb.Length > 0)
                                    sb.Append(" ");
                                sb.Append(text);
                            }
                        }
                    }
                }
                else if (type == "reasoning" && sb.Length == 0)
                {
                    Debug(() => $"Reasoning item structure: {item}");
                    
                    // Try summary array first
                    var summary = item["summary"] as JArray;
                    if (summary != null && summary.Count > 0)
                    {
                        Debug(() => $"Reasoning summary has {summary.Count} items");
                        foreach (var sumItem in summary)
                        {
                            var text = sumItem["text"]?.ToString();
                            if (!string.IsNullOrEmpty(text))
                            {
                                if (sb.Length > 0)
                                    sb.Append(" ");
                                sb.Append(text);
                            }
                        }
                    }
                    
                    if (sb.Length == 0)
                    {
                        var content = item["content"] as JArray;
                        if (content != null && content.Count > 0)
                        {
                            Debug(() => $"Reasoning content has {content.Count} items");
                            foreach (var contentItem in content)
                            {
                                var text = contentItem["text"]?.ToString();
                                if (!string.IsNullOrEmpty(text))
                                {
                                    if (sb.Length > 0)
                                        sb.Append(" ");
                                    sb.Append(text);
                                }
                            }
                        }
                    }
                }
            }

            Debug(() => $"Final extracted text length: {sb.Length}");
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
            var maxSize = _config.Chat.MaxChunkSize;

            if (message.Length <= maxSize)
            {
                return new List<string>(1) { message };
            }

            // Pre-size list to estimated capacity to avoid resizes
            var capacity = Math.Max(1, (message.Length + maxSize - 1) / maxSize);
            var chunks = new List<string>(capacity);

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
            if (_config?.DebugMode != true)
                return;
            Puts($"[DEBUG] {message}");
        }

        private void Debug(Func<string> messageBuilder)
        {
            if (_config?.DebugMode != true)
                return;
            Puts($"[DEBUG] {messageBuilder()}");
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
                GetAuthHeaders()
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

        private void RebuildCachedHeaders()
        {
            _cachedAuthHeaders = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {_config.Api.ApiKey}"
            };
            _cachedAuthHeadersWithJson = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {_config.Api.ApiKey}",
                ["Content-Type"] = "application/json"
            };
            _cachedVectorStoreHeaders = new Dictionary<string, string>(_cachedAuthHeaders) { ["OpenAI-Beta"] = "assistants=v2" };
        }

        private Dictionary<string, string> GetAuthHeaders()
        {
            if (_cachedAuthHeaders == null)
                RebuildCachedHeaders();
            return _cachedAuthHeaders;
        }

        private Dictionary<string, string> GetAuthHeadersWithJson()
        {
            if (_cachedAuthHeadersWithJson == null)
                RebuildCachedHeaders();
            return _cachedAuthHeadersWithJson;
        }

        private Dictionary<string, string> GetVectorStoreHeaders()
        {
            if (_cachedVectorStoreHeaders == null)
                RebuildCachedHeaders();
            return _cachedVectorStoreHeaders;
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
                GetVectorStoreHeaders()
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

        private void DownloadVectorStoreFileContent(string vectorStoreId, string fileId, Action<string, string> callback)
        {
            webrequest.Enqueue(
                $"https://api.openai.com/v1/vector_stores/{vectorStoreId}/files/{fileId}/content",
                null,
                (code, response) =>
                {
                    if (code != 200)
                    {
                        PrintError($"Failed to download file {fileId}: HTTP {code}");
                        if (_config.DebugMode && !string.IsNullOrEmpty(response))
                            Puts($"[OpenAI] Vector store content response ({code}): {(response.Length > 500 ? response.Substring(0, 500) + "..." : response)}");
                        callback(null, null);
                        return;
                    }

                    try
                    {
                        var json = JObject.Parse(response);
                        var filename = json["filename"]?.ToString();
                        var contentArray = json["content"] as JArray ?? json["data"] as JArray;

                        if (string.IsNullOrEmpty(filename))
                            filename = $"{fileId}.txt";

                        var sb = new System.Text.StringBuilder();
                        if (contentArray != null)
                        {
                            foreach (var item in contentArray)
                            {
                                var text = item["text"]?.ToString();
                                if (!string.IsNullOrEmpty(text))
                                    sb.Append(text);
                            }
                        }
                        else if (json["content"] != null && json["content"].Type == JTokenType.String)
                        {
                            sb.Append(json["content"].ToString());
                        }
                        else if (_config.DebugMode)
                        {
                            Puts($"[OpenAI] Vector store content response (200) missing 'content' or 'data' array. Keys: {string.Join(", ", json.Properties().Select(p => p.Name))}. Sample: {(response.Length > 400 ? response.Substring(0, 400) + "..." : response)}");
                        }

                        callback(filename, sb.ToString());
                    }
                    catch (Exception ex)
                    {
                        PrintError($"Failed to parse vector store file content: {ex.Message}");
                        if (_config.DebugMode && !string.IsNullOrEmpty(response))
                            Puts($"[OpenAI] Raw response: {(response.Length > 500 ? response.Substring(0, 500) + "..." : response)}");
                        callback(null, null);
                    }
                },
                this,
                Oxide.Core.Libraries.RequestMethod.GET,
                GetVectorStoreHeaders()
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

        private void PullFilesFromVectorStore()
        {
            var path = GetKnowledgePath();
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            Puts("Fetching files from vector store...");

            ListVectorStoreFiles(_config.Knowledge.VectorStoreId, files =>
            {
                if (files.Count == 0)
                {
                    Puts("No files found in vector store.");
                    return;
                }

                Puts($"Found {files.Count} files. Downloading...");
                var remaining = files.Count;
                var succeeded = 0;
                var failed = 0;

                foreach (var file in files)
                {
                    GetFileInfo(file.Id, displayFileName =>
                    {
                        DownloadVectorStoreFileContent(_config.Knowledge.VectorStoreId, file.Id, (apiFileName, content) =>
                        {
                            if (content == null)
                            {
                                failed++;
                                PrintWarning($"Failed to download: {file.Id}");
                            }
                            else
                            {
                                var fileName = !string.IsNullOrEmpty(displayFileName) ? displayFileName : apiFileName;
                                var filePath = Path.Combine(path, fileName);
                                File.WriteAllText(filePath, content);
                                Puts($"  Downloaded: {fileName}");
                                succeeded++;
                            }

                            remaining--;
                            if (remaining == 0)
                                Puts($"Pull complete: {succeeded} downloaded, {failed} failed.");
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

                case "pull":
                    if (string.IsNullOrEmpty(_config.Knowledge.VectorStoreId))
                    {
                        PrintError("No vector store configured. Set 'Vector Store ID' in config.");
                        return;
                    }
                    PullFilesFromVectorStore();
                    break;

                default:
                    Puts("Usage: openai.kb <command>");
                    Puts("  status - Show knowledge base configuration");
                    Puts("  sync   - Upload local files to vector store (replaces existing)");
                    Puts("  pull   - Download files from vector store to local folder");
                    Puts("  list   - List available vector stores");
                    Puts("  files  - List files in current vector store");
                    Puts("  clear  - Remove all files from vector store");
                    break;
            }
        }

        [ConsoleCommand("openai.status")]
        private void CmdStatus(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
            {
                var player = arg.Connection.player as BasePlayer;
                if (player != null && !arg.IsAdmin)
                {
                    var eff = GetEffectiveSettings(player);
                    if (!string.IsNullOrEmpty(eff.TierName))
                    {
                        Puts("=== Your effective AI settings (VIP) ===");
                        Puts($"Effective model: {eff.Model} (VIP tier: {eff.TierName})");
                        Puts($"Effective daily limit: {eff.DailyTokenLimit:N0}");
                        Puts($"Effective cooldown: {eff.CooldownSeconds}s");
                        Puts("==================================");
                        return;
                    }
                }
            }

            if (!arg.IsAdmin) return;

            var status = ValidateSetup();
            LogSetupStatus(status);
        }

        [ConsoleCommand("openai.vip")]
        private void CmdVip(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && !permission.UserHasPermission(arg.Connection.userid.ToString(), PermissionAdmin))
            {
                Puts("You need openai.admin permission to use this command.");
                return;
            }

            if (_config?.VIPTiers == null || _config.VIPTiers.Count == 0)
            {
                Puts("No VIP tiers configured. Add tiers to 'VIP Tiers' in config.");
                return;
            }

            var arg0 = arg.GetString(0, "");
            var arg1 = arg.GetString(1, "");
            var arg2 = arg.GetString(2, "");

            if (string.IsNullOrEmpty(arg0))
            {
                Puts("Usage: openai.vip <userid> [tier_name] | openai.vip remove <userid> <tier_name>");
                Puts("  openai.vip <userid>           - List VIP tiers for user");
                Puts("  openai.vip <userid> <tier>    - Grant openai.vip.<tier> to user");
                Puts("  openai.vip remove <userid> <tier> - Revoke openai.vip.<tier> from user");
                Puts($"  Valid tiers: {string.Join(", ", _config.VIPTiers.Keys)}");
                return;
            }

            if (arg0.Equals("remove", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(arg1) || string.IsNullOrEmpty(arg2))
                {
                    Puts("Usage: openai.vip remove <userid> <tier_name>");
                    return;
                }
                var userId = arg1;
                var tierName = arg2;
                if (!_config.VIPTiers.ContainsKey(tierName))
                {
                    Puts($"Unknown tier: {tierName}. Valid: {string.Join(", ", _config.VIPTiers.Keys)}");
                    return;
                }
                var perm = "openai.vip." + tierName;
                permission.RevokeUserPermission(userId, perm);
                Puts($"Revoked {perm} from user {userId}.");
                return;
            }

            if (!string.IsNullOrEmpty(arg1))
            {
                var userId = arg0;
                var tierName = arg1;
                if (!_config.VIPTiers.ContainsKey(tierName))
                {
                    Puts($"Unknown tier: {tierName}. Valid: {string.Join(", ", _config.VIPTiers.Keys)}");
                    return;
                }
                var perm = "openai.vip." + tierName;
                permission.GrantUserPermission(userId, perm, this);
                Puts($"Granted {perm} to user {userId}.");
                return;
            }

            var listUserId = arg0;
            var vipTiers = _config.VIPTiers.Keys.Where(k => permission.UserHasPermission(listUserId, "openai.vip." + k)).ToList();
            if (vipTiers.Count == 0)
                Puts($"User {listUserId} has no VIP tiers.");
            else
                Puts($"User {listUserId} VIP tiers: {string.Join(", ", vipTiers)}");
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
                foreach (var pattern in InjectionPatterns)
                {
                    if (lowerInput.Contains(pattern))
                        return null;
                }
            }

            input = ControlCharsRegex.Replace(input, "");

            return input.Trim();
        }

        private string StripUrlsFromMarkdownLinks(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return MarkdownLinkRegex.Replace(text, "[$1]");
        }

        private string StripDeathMessageTags(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            var stripped = RichTextTagRegex.Replace(text, "");
            return Regex.Replace(stripped, @"\s+", " ").Trim();
        }

        #endregion
    }
}
 