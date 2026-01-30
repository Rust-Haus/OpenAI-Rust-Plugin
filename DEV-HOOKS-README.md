# OpenAI Developer Hooks

This document describes the Developer Hooks exposed by the OpenAI plugin, allowing other Oxide plugin developers to leverage OpenAI API features without configuring their own API keys.

## Configuration

The server admin can configure Developer Hooks in `OpenAI.json`:

```json
"Developer Hooks": {
  "Enabled": true,
  "Share Rate Limits With Players": true,
  "External Max Requests Per Minute": 30,
  "Log External Requests": true
}
```

| Setting | Description |
|---------|-------------|
| `Enabled` | Enable/disable all developer hooks |
| `Share Rate Limits With Players` | If true, external requests count toward the same rate limits as player requests. If false, external requests have their own separate pool. |
| `External Max Requests Per Minute` | Rate limit for external requests (only used if not sharing limits) |
| `Log External Requests` | Log all external requests to console for debugging |

---

## Available Hooks

### 1. OpenAI_ChatComplete (Simple)

The simplest way to get an AI response. Uses the server's configured model and system prompt.

```csharp
Interface.CallHook("OpenAI_ChatComplete", 
    callerPlugin,    // string: Your plugin name (for tracking)
    prompt,          // string: The question/prompt to send
    callbackHook     // string: Hook name to receive the response
);
```

**Example:**

```csharp
void SomeMethod()
{
    Interface.CallHook("OpenAI_ChatComplete", Name, "What is the best weapon in Rust?", "OnAIResponse");
}

void OnAIResponse(string callerPlugin, string prompt, string response, bool success)
{
    if (callerPlugin != Name) return;  // Filter responses meant for your plugin
    
    if (success)
        Puts($"AI said: {response}");
    else
        Puts($"Request failed: {response}");  // response contains error message
}
```

---

### 2. OpenAI_ChatCompleteAdvanced (Custom Options)

Full control over model, system prompt, and token limits.

```csharp
Interface.CallHook("OpenAI_ChatCompleteAdvanced",
    callerPlugin,     // string: Your plugin name
    prompt,           // string: The question/prompt
    systemPrompt,     // string: Custom system prompt (null = use server default)
    model,            // string: Model name (null = use server default)
    maxTokens,        // int: Max output tokens (0 = use server default)
    callbackHook      // string: Hook name to receive the response
);
```

**Example - Custom Translator:**

```csharp
void TranslateToFrench(string text)
{
    Interface.CallHook("OpenAI_ChatCompleteAdvanced",
        Name,
        text,
        "You are a translator. Translate the input to French. Reply with only the translation.",
        "gpt-4.1-nano",
        128,
        "OnTranslation"
    );
}

void OnTranslation(string callerPlugin, string prompt, string response, bool success)
{
    if (callerPlugin != Name) return;
    
    if (success)
        Puts($"French: {response}");
}
```

---

### 3. OpenAI_ChatCompleteReasoning (With Reasoning)

For reasoning models (o1, o3, o4-mini, etc.) that support chain-of-thought reasoning.

```csharp
Interface.CallHook("OpenAI_ChatCompleteReasoning",
    callerPlugin,       // string: Your plugin name
    prompt,             // string: The question/prompt
    systemPrompt,       // string: Custom system prompt (null = use server default)
    model,              // string: Model name (null = use server default)
    maxTokens,          // int: Max output tokens (0 = use server default)
    reasoningEffort,    // string: "minimal", "low", "medium", or "high" (null/"none" = no reasoning)
    callbackHook        // string: Hook name to receive the response
);
```

**Reasoning Effort Levels:**

| Level | Description |
|-------|-------------|
| `"minimal"` | Minimal reasoning, fastest, lowest token usage |
| `"low"` | Quick reasoning, lower token usage |
| `"medium"` | Balanced reasoning |
| `"high"` | Deep reasoning, higher token usage |
| `null` or `"none"` | No reasoning (same as Advanced hook) |

**Note:** Only reasoning models (o1, o3, o4-*) accept reasoning efforts. For non-reasoning models (e.g. gpt-*), use `null` or `"none"`.

**Example - Raid Calculator:**

```csharp
void CalculateRaid(string question)
{
    Interface.CallHook("OpenAI_ChatCompleteReasoning",
        Name,
        question,
        "You are a Rust raid calculator. Be precise and concise.",
        "o4-mini",
        2048,
        "low",
        "OnRaidCalculation"
    );
}

void OnRaidCalculation(string callerPlugin, string prompt, string response, bool success)
{
    if (callerPlugin != Name) return;
    
    if (success)
        Puts($"Raid calculation: {response}");
}
```

**Note:** Reasoning models require more tokens. Use at least 1024-2048 `maxTokens` for reasoning requests to ensure the model has room for both reasoning and the final answer.

---

## Callback Response Format

All callbacks receive the same parameters:

```csharp
void YourCallback(string callerPlugin, string prompt, string response, bool success)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `callerPlugin` | string | The plugin name that made the request |
| `prompt` | string | The original prompt that was sent |
| `response` | string | The AI response (or error message if failed) |
| `success` | bool | True if request succeeded, false if failed |

**Important:** Always filter by `callerPlugin` to ensure you only process responses meant for your plugin, as all plugins share the same callback namespace.

---

## Error Handling

When `success` is `false`, the `response` parameter contains the error message:

| Error | Description |
|-------|-------------|
| `"Developer hooks are disabled"` | Admin disabled hooks in config |
| `"Invalid parameters: ..."` | Missing required parameters |
| `"Rate limit exceeded"` | Too many requests |
| `"API error: {code}"` | OpenAI API returned an error |
| `"Empty response from API"` | Model returned no text |
| `"Parse error: ..."` | Failed to parse API response |

---

## Best Practices

1. **Always filter by plugin name** in your callback to avoid processing other plugins' responses
2. **Use appropriate token limits** - simple questions need 128-256, complex ones need 512-1024+
3. **Handle failures gracefully** - always check `success` before using `response`
4. **Respect rate limits** - don't spam requests; implement your own cooldowns if needed
5. **Use the simplest hook** that meets your needs - `OpenAI_ChatComplete` is cheapest
6. **For reasoning models**, use at least 1024 tokens and prefer `"minimal"` or `"low"` effort unless you need deep reasoning

---

## Available Models

Common models you can use:

| Model | Type | Best For |
|-------|------|----------|
| `gpt-4.1-nano` | Fast | Simple questions, translations |
| `gpt-4.1-mini` | Balanced | General purpose |
| `gpt-5-nano` | Fast | Quick responses |
| `gpt-5.2` | Advanced | Complex questions |
| `o4-mini` | Reasoning | Step-by-step analysis |
| `o3` | Reasoning | Deep reasoning tasks |

Use `null` to let the server use its configured default model.

---

## Complete Example Plugin

```csharp
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("MyAIPlugin", "Author", "1.0.0")]
    public class MyAIPlugin : RustPlugin
    {
        [ChatCommand("ask")]
        void AskCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                player.ChatMessage("Usage: /ask <question>");
                return;
            }

            var question = string.Join(" ", args);
            player.ChatMessage("Thinking...");
            
            // Store player ID to send response later
            _pendingPlayer = player.userID;
            
            Interface.CallHook("OpenAI_ChatComplete", Name, question, "OnAIAnswer");
        }

        private ulong _pendingPlayer;

        void OnAIAnswer(string callerPlugin, string prompt, string response, bool success)
        {
            if (callerPlugin != Name) return;
            
            var player = BasePlayer.FindByID(_pendingPlayer);
            if (player == null) return;

            if (success)
                player.ChatMessage($"<color=#00ff00>AI:</color> {response}");
            else
                player.ChatMessage($"<color=#ff0000>Error:</color> {response}");
        }
    }
}
```
