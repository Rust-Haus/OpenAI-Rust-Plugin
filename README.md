# OpenAI Plugin v3.4.0

AI assistant plugin for Rust game servers using the OpenAI Responses API. Players can ask questions in-game and receive AI-powered responses.

---

## Table of Contents

- [Features](#features)
- [What This Plugin Can Do](#what-this-plugin-can-do)
- [What This Plugin Cannot Do](#what-this-plugin-cannot-do)
- [Quick Start](#quick-start)
- [Permissions](#permissions)
- [Player Usage](#player-usage)
- [Console Commands](#console-commands)
- [Configuration Reference](#configuration-reference)
- [Global Chat Bot](#global-chat-bot)
- [BetterChat Integration](#betterchat-integration)
- [Bot Personalities](#bot-personalities)
- [Knowledge Base System](#knowledge-base-system)
- [Rate Limiting](#rate-limiting)
- [Error Messages](#error-messages)
- [Discord Integration](#discord-integration)
- [Security Features](#security-features)
- [Troubleshooting](#troubleshooting)
- [Changelog](#changelog)

---

## Features

- **Chat-based AI assistant** triggered by customizable command prefix (default: `!ai`)
- **Global Chat Bot** - Monitors server chat and responds to questions automatically
- **Conversation continuity** - AI remembers context within a session using `previous_response_id` chaining
- **Knowledge Base** - Upload server-specific documents to customize AI responses
- **Customizable bot personalities** - Edit personality prompts via simple text files
- **BetterChat integration** - Optional formatting to match BetterChat's styled output
- **Smart setup validation** with automatic model discovery and configuration checking
- **Multi-tier rate limiting** (per-player cooldown, global requests/minute, daily token budgets)
- **Reasoning model support** (o1, o3, o4 series)
- **Optional web search** - Allow AI to search the internet for current information
- **Discord webhook integration** for logging all AI interactions
- **Prompt injection filtering** - Blocks common jailbreak attempts
- **Automatic retry** with exponential backoff on API failures
- **Message chunking** - Long responses are split into readable chat messages
- **URL stripping** - Cleans markdown links for better in-game readability
- **Persistent usage tracking** - Token usage persists across server restarts

---

## What This Plugin Can Do

| Capability | Description |
|------------|-------------|
| Answer general questions | The AI can answer questions about Rust gameplay, mechanics, tips, and strategies |
| Use knowledge base | If configured, the AI can reference server-specific documents (rules, guides, etc.) |
| Search the web | If enabled, the AI can search for current information online |
| Remember conversation context | Follow-up questions within a session reference previous exchanges |
| Personalize responses | AI knows the player's name and basic server info |
| Monitor chat automatically | Global Chat Bot can respond to questions without requiring a command prefix |
| Respect rate limits | Prevents abuse through configurable cooldowns and token budgets |

## What This Plugin Cannot Do

| Limitation | Explanation |
|------------|-------------|
| See the game world | The AI cannot view player locations, inventories, or any live game data |
| Execute game commands | The AI cannot run server commands or modify game state |
| Track players | The AI has no access to real-time player positions or activities |
| Access server databases | The AI cannot query player stats, economy data, or plugin databases |
| Monitor in-game events | The AI does not receive notifications about raids, deaths, or other events |
| Interact with other plugins | The AI operates independently and cannot communicate with other plugins |

The AI is a **text-only assistant** that can only respond based on its training data and any documents you provide in the knowledge base.

---

## Quick Start

### 1. Get an OpenAI API Key

1. Go to [OpenAI Platform](https://platform.openai.com/)
2. Create an account or sign in
3. Navigate to API Keys and create a new key
4. Copy the key (starts with `sk-`)

### 2. Configure the Plugin

Edit `carbon/configs/OpenAI.json`:

```json
{
  "API Settings": {
    "API Key": "sk-your-api-key-here",
    "Model": "gpt-4o-mini"
  }
}
```

### 3. Grant Permissions

```
oxide.grant group default openai.use
```

### 4. Reload the Plugin

```
carbon.reload OpenAI
```

### 5. Test It

In game chat, type:
```
!ai Hello, what can you help me with?
```

---

## Permissions

| Permission | Description |
|------------|-------------|
| `openai.use` | Required to use the AI chat command. Grant to players who should have access. |
| `openai.admin` | Reserved for future admin-only features |
| `openai.unlimited` | Bypass all rate limits and token budgets. Use sparingly for trusted users. |

### Permission Examples

```bash
# Grant to all players
oxide.grant group default openai.use

# Grant to specific player
oxide.grant user 76561198000000000 openai.use

# Grant unlimited access to VIPs
oxide.grant group vip openai.unlimited

# Revoke permission
oxide.revoke user 76561198000000000 openai.use
```

---

## Player Usage

### Basic Usage

Players with the `openai.use` permission can interact with the AI using the command prefix (default: `!ai`).

```
!ai <your question>
```

### Examples

```
!ai What's the best way to get started in Rust?
!ai How do I research blueprints?
!ai What are the monument puzzles?
!ai How much sulfur do I need to raid a sheet metal door?
!ai What's the difference between HQM and metal fragments?
```

### Conversation Context

The AI remembers your previous messages within a session. You can ask follow-up questions:

```
Player: !ai How do I make a furnace?
AI: To craft a furnace, you need 200 stones, 100 wood, and 50 low-grade fuel...

Player: !ai What about a large furnace?
AI: A large furnace requires 500 stones and 200 wood. It processes resources much faster...
```

Context is cleared when:
- The player disconnects
- An admin runs `openai.clearcontext`
- The server restarts

---

## Console Commands

All commands require admin access. Run from the server console or RCON.

### Status & Diagnostics

| Command | Description |
|---------|-------------|
| `openai.status` | Display configuration validation status with errors, warnings, and suggestions |
| `openai.test` | Test API connectivity and verify the configured model is available |
| `openai.testmodel` | Run comprehensive capability tests (basic chat, web search, file search) |
| `openai.models` | List all models available to your API key with their capabilities |

### Usage Management

| Command | Description |
|---------|-------------|
| `openai.usage` | Show current token usage for the server, bot, and all players |
| `openai.resetusage` | Reset all usage counters to zero |
| `openai.clearcontext [player]` | Clear conversation context for a player or all players |

### Global Chat Bot

| Command | Description |
|---------|-------------|
| `openai.clearbot [all\|global\|teams]` | Clear bot conversation contexts |
| `openai.personalities list` | Show all available bot personalities |
| `openai.personalities reload` | Reload personalities from disk |

### Knowledge Base

| Command | Description |
|---------|-------------|
| `openai.kb status` | Show knowledge base configuration and local files |
| `openai.kb sync` | Upload local files to OpenAI vector store (creates store if needed) |
| `openai.kb list` | List all vector stores in your OpenAI account |
| `openai.kb files` | List files in the current vector store |
| `openai.kb clear` | Remove all files from the current vector store |

### Command Examples

```bash
# Check if everything is configured correctly
openai.status

# See which models you can use
openai.models

# View today's usage (includes bot stats)
openai.usage

# Clear a specific player's context
openai.clearcontext "PlayerName"

# Clear everyone's context
openai.clearcontext all

# Clear all bot contexts (global + teams)
openai.clearbot all

# List available personalities
openai.personalities list

# Reload personalities after editing files
openai.personalities reload

# Sync knowledge base files
openai.kb sync
```

### openai.status Output Example

```
=== OpenAI Plugin Configuration ===
API Key: Valid
Model: gpt-4o-mini (Available)
Reasoning: none
Web Search: Disabled
--- WARNINGS ---
  * Model 'gpt-4o-mini' does not use reasoning. Effort 'low' will be ignored.
--- SUGGESTIONS ---
  -> Set 'Reasoning Effort' to 'none' for this model
==================================
```

---

## Configuration Reference

Configuration file location: `carbon/configs/OpenAI.json`

### Config Version

```json
{
  "Config Version": "3.4.0"
}
```

The plugin automatically tracks its configuration version and applies migrations when updating. Do not modify this value manually.

---

### API Settings

```json
{
  "API Settings": {
    "API Key": "",
    "API URL": "https://api.openai.com/v1/responses",
    "Model": "gpt-5-nano",
    "Max Output Tokens (0 = model default)": 2048,
    "Reasoning Effort (none/minimal/low/medium/high)": "low",
    "Enable Web Search": false,
    "Retry Attempts": 3
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| **API Key** | `""` | Your OpenAI API key. **Required.** Get one from [platform.openai.com](https://platform.openai.com/) |
| **API URL** | `https://api.openai.com/v1/responses` | OpenAI Responses API endpoint. Do not change unless using a proxy. |
| **Model** | `gpt-5-nano` | The AI model to use. Run `openai.models` to see available options. |
| **Max Output Tokens** | `2048` | Maximum length of AI responses in tokens. Set to `0` to use the model's default. Higher values allow longer responses but cost more. |
| **Reasoning Effort** | `low` | For reasoning models (o1, o3, o4 series), controls how much the model "thinks" before responding. Options: `none`, `minimal`, `low`, `medium`, `high`. |
| **Enable Web Search** | `false` | Allow the AI to search the internet for current information. Increases cost slightly. |
| **Retry Attempts** | `3` | Number of times to retry API calls on server errors (5xx). Uses exponential backoff. |

#### Model Selection Guide

| Model | Best For | Cost | Speed |
|-------|----------|------|-------|
| `gpt-4o-mini` | General use, good balance | Low | Fast |
| `gpt-4o` | Complex questions, high quality | Medium | Medium |
| `o1-mini` | Reasoning tasks | Medium | Medium |
| `o3-mini` | Advanced reasoning | High | Slower |

#### Reasoning Effort Explained

Reasoning effort only affects reasoning models (o1, o3, o4 series). Standard GPT models ignore this setting.

| Effort | Description | Use Case |
|--------|-------------|----------|
| `none` | No extended reasoning | Standard GPT models only |
| `minimal` | Quick reasoning | Simple logic questions |
| `low` | Light reasoning | General questions with some complexity |
| `medium` | Moderate reasoning | Multi-step problems |
| `high` | Deep reasoning | Complex analysis (highest cost) |

---

### Rate Limits

```json
{
  "Rate Limits": {
    "Cooldown Seconds": 10,
    "Max Requests Per Minute": 30,
    "Daily Token Budget": 500000,
    "Per Player Daily Token Limit": 15000,
    "Persist Usage Data": true
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| **Cooldown Seconds** | `10` | Minimum seconds between requests for each player. Prevents spam. |
| **Max Requests Per Minute** | `30` | Maximum total requests the server can make per minute. Prevents API abuse during high traffic. |
| **Daily Token Budget** | `500000` | Maximum total tokens the server can use per day. Helps control costs. |
| **Per Player Daily Token Limit** | `15000` | Maximum tokens each player can use per day. Ensures fair usage. |
| **Persist Usage Data** | `true` | Save usage data to disk. Survives server restarts. |

#### Token Budget Examples

| Scenario | Suggested Daily Budget | Per-Player Limit |
|----------|------------------------|------------------|
| Small private server (5-10 players) | 100,000 | 15,000 |
| Medium community server (20-50 players) | 500,000 | 10,000 |
| Large public server (100+ players) | 1,000,000 | 5,000 |

---

### Chat Settings

```json
{
  "Chat Settings": {
    "Command Prefix": "!ai",
    "Response Prefix": "[AI]",
    "Response Color": "#55AAFF",
    "Message Color": "#FFFFFF",
    "Font Size": 12,
    "Max Message Chunk Size": 450,
    "Strip URLs from Links": true
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| **Command Prefix** | `!ai` | The phrase that triggers the AI. Players type this followed by their question. |
| **Response Prefix** | `[AI]` | Text shown before AI responses in chat. |
| **Response Color** | `#55AAFF` | Hex color for the response prefix. Light blue by default. |
| **Message Color** | `#FFFFFF` | Hex color for the response text. White by default. |
| **Font Size** | `12` | Font size for chat messages. Rust's default is 12. |
| **Max Message Chunk Size** | `450` | Long responses are split at this character count. Rust chat has limits on message length. |
| **Strip URLs from Links** | `true` | Convert `[text](url)` to `[text]`. URLs don't work in Rust chat, so this cleans up responses. |

#### Custom Command Prefix Examples

```json
"Command Prefix": "!ai"      // !ai How do I craft gunpowder?
"Command Prefix": "/ask"     // /ask What's the best base design?
"Command Prefix": "@bot"     // @bot Tell me about monuments
"Command Prefix": "??"       // ?? How do I get scrap fast?
```

---

### Global Chat Bot

```json
{
  "Global Chat Bot": {
    "Enabled": false,
    "Bot Name": "Assistant",
    "Response Prefix": "[Bot]",
    "Response Color": "#55AAFF",
    "Cooldown Seconds": 5,
    "Trigger Patterns": ["?"],
    "Monitor Global Chat": true,
    "Monitor Team Chat": false,
    "Personality Preset": "helpful",
    "Custom System Prompt": "",
    "Daily Token Budget (0 = shared with main)": 0,
    "Use Better Chat": false,
    "Better Chat Title": "[AI]",
    "Better Chat Title Color": "#55AAFF"
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| **Enabled** | `false` | Enable the global chat bot feature |
| **Bot Name** | `Assistant` | Display name for the bot (used in BetterChat mode) |
| **Response Prefix** | `[Bot]` | Text shown before bot responses in chat (standard mode) |
| **Response Color** | `#55AAFF` | Hex color for the bot's response prefix / name |
| **Cooldown Seconds** | `5` | Minimum seconds between bot responses |
| **Trigger Patterns** | `["?"]` | Patterns that trigger the bot (see below) |
| **Monitor Global Chat** | `true` | Respond to messages in global chat |
| **Monitor Team Chat** | `false` | Respond to messages in team chat |
| **Personality Preset** | `helpful` | Which personality file to use (see [Bot Personalities](#bot-personalities)) |
| **Custom System Prompt** | `""` | Custom prompt when using "custom" preset |
| **Daily Token Budget** | `0` | Separate token budget for bot (0 = share with main `!ai` command) |
| **Use Better Chat** | `false` | Enable BetterChat-style formatting (see [BetterChat Integration](#betterchat-integration)) |
| **Better Chat Title** | `[AI]` | Title/tag shown before bot name in BetterChat mode |
| **Better Chat Title Color** | `#55AAFF` | Hex color for the BetterChat title |

#### Trigger Patterns

The bot responds to messages containing any of the configured trigger patterns:

| Pattern | Example Messages |
|---------|------------------|
| `?` | "when is wipe?" "how do I craft?" |
| `@bot` | "what's the best base design @bot" |
| `hey bot` | "hey bot tell me about monuments" |

Multiple patterns can be combined. Default is just `?`.

#### Channel Monitoring

| Setting | Description |
|---------|-------------|
| `Monitor Global Chat` | Respond to messages in global/public chat |
| `Monitor Team Chat` | Respond to messages in team chat (each team has separate context) |

Both can be enabled simultaneously. Team chat responses are only visible to team members.

#### Context Architecture

```
Global Chat → Shared context (all players see same conversation)

Team Chat:
  Team A → Private context for Team A
  Team B → Private context for Team B
  Team C → Private context for Team C
```

Each team's conversation with the bot is completely separate from other teams and from global chat.

---

### BetterChat Integration

The Global Chat Bot can optionally format messages to match the visual style of the BetterChat plugin. This makes the AI bot appear as a styled "player" in chat with a title tag, matching how BetterChat formats player messages.

#### Standard Mode vs BetterChat Mode

**Standard Mode** (default):
```
[Bot] This is the bot's response message.
```

**BetterChat Mode**:
```
[AI] Assistant: This is the bot's response message.
```

In BetterChat mode:
- The title (e.g., `[AI]`) appears first with its own color
- The bot name (e.g., `Assistant`) appears next with the Response Color
- The message follows after a colon

#### Enabling BetterChat Mode

```json
{
  "Global Chat Bot": {
    "Enabled": true,
    "Bot Name": "Assistant",
    "Response Color": "#55AAFF",
    "Use Better Chat": true,
    "Better Chat Title": "[AI]",
    "Better Chat Title Color": "#55AAFF"
  }
}
```

#### Customization Examples

**Admin Bot Style:**
```json
{
  "Use Better Chat": true,
  "Better Chat Title": "[ADMIN]",
  "Better Chat Title Color": "#FF5555",
  "Bot Name": "ServerBot",
  "Response Color": "#FFAA00"
}
```
Output: `[ADMIN] ServerBot: response text`

**Helper Bot Style:**
```json
{
  "Use Better Chat": true,
  "Better Chat Title": "[HELPER]",
  "Better Chat Title Color": "#55FF55",
  "Bot Name": "Guide",
  "Response Color": "#FFFFFF"
}
```
Output: `[HELPER] Guide: response text`

#### Technical Notes

- BetterChat mode uses `chat.add` console command instead of `ChatMessage()` for proper formatting
- Messages appear in the F1 console log just like regular player messages
- Works in both global and team chat channels
- Does not require the actual BetterChat plugin to be installed

---

### Discord Integration

```json
{
  "Discord Integration": {
    "Enabled": false,
    "Webhook URL": ""
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| **Enabled** | `false` | Enable Discord webhook logging |
| **Webhook URL** | `""` | Discord webhook URL for the channel to log to |

When enabled, the plugin sends rich embeds to Discord showing:
- Player name
- Question asked
- AI response
- Timestamp

Console commands (`openai.usage`, `openai.kb sync`, etc.) also send summary messages to Discord.

#### Setting Up Discord Logging

1. In Discord, go to your server's channel settings
2. Go to Integrations > Webhooks
3. Click "New Webhook"
4. Copy the webhook URL
5. Paste it in the config

---

### Security Settings

```json
{
  "Security Settings": {
    "Max Input Length": 500,
    "Filter Injection Attempts": true
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| **Max Input Length** | `500` | Maximum characters allowed in player questions. Prevents excessively long inputs. |
| **Filter Injection Attempts** | `true` | Block common prompt injection/jailbreak phrases. |

#### Blocked Injection Phrases

When `Filter Injection Attempts` is enabled, the following phrases are blocked:
- "ignore previous"
- "ignore all previous"
- "disregard previous"
- "forget previous"
- "ignore your instructions"
- "new instructions:"
- "system prompt:"
- "you are now"
- "act as if"
- "pretend you are"
- "jailbreak"
- "dan mode"

Players attempting these will see: "Your message contains disallowed content."

---

### Prompt Settings

```json
{
  "Prompt Settings": {
    "System Prompt": "You are a helpful assistant on a Rust game server. Keep responses concise and relevant to the game Rust by Facepunch Studios and this server specifically.",
    "Include Server Info": true,
    "Include Player Names": true,
    "Custom Instructions": [
      "You are a text-only assistant. You cannot see the game world, player locations, inventories, or any live server data.",
      "You can only answer questions based on your knowledge and any documents provided to you.",
      "Never offer to: track locations, show maps, execute commands for players, check inventories, monitor players, or interact with the game in any way.",
      "Never say 'tell me your position' or offer to give directions - you cannot see where players are.",
      "If asked about something you cannot do, simply say you don't have access to that information.",
      "Keep responses concise and factual. Do not over-explain or pad responses with unnecessary details.",
      "When answering about Rust gameplay, stick to general knowledge unless server-specific info is in your knowledge base."
    ]
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| **System Prompt** | (see above) | The base personality and instructions for the AI. This is always sent with every request. |
| **Include Server Info** | `true` | Include server name and current player count in the system prompt. |
| **Include Player Names** | `true` | Tell the AI which player is asking the question. |
| **Custom Instructions** | (see above) | Array of additional instruction strings appended to the system prompt. |

#### Customizing the AI Personality

You can modify the system prompt to change how the AI behaves:

```json
"System Prompt": "You are a friendly pirate-themed assistant on the [YARR] Rust server. Speak like a pirate but keep responses helpful and concise. Arrr!"
```

#### Adding Custom Instructions

Custom instructions are great for server-specific rules:

```json
"Custom Instructions": [
  "This is a PvE server. There is no raiding or PvP.",
  "The server wipes every Thursday at 3 PM EST.",
  "Players can earn VIP status by voting at rustservers.gg/vote/12345",
  "Custom events are held every Saturday at 8 PM EST."
]
```

---

### Knowledge Base

```json
{
  "Knowledge Base": {
    "Enable Knowledge Base": false,
    "Vector Store ID": "",
    "Knowledge Subfolder": "OpenAI/knowledge",
    "Auto Create Vector Store": true
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| **Enable Knowledge Base** | `false` | Enable the knowledge base feature |
| **Vector Store ID** | `""` | OpenAI Vector Store ID (auto-created if empty and auto-create is enabled) |
| **Knowledge Subfolder** | `OpenAI/knowledge` | Folder under `oxide/data/` where knowledge files are stored |
| **Auto Create Vector Store** | `true` | Automatically create a vector store if none exists |

---

### Debug Mode

```json
{
  "Debug Mode": false
}
```

When enabled, the plugin logs detailed information about API requests and responses. Useful for troubleshooting but generates a lot of log output.

---

## Global Chat Bot

The Global Chat Bot feature allows the AI to monitor server chat and automatically respond to questions without players needing to use a command prefix.

### How It Works

1. Bot monitors configured chat channels (global and/or team)
2. When a message matches a trigger pattern (default: ends with `?`), the bot considers responding
3. Bot uses heuristic filters to avoid responding to player-to-player conversations
4. Bot sends the message to AI with a `[SKIP]` instruction - AI can choose not to respond
5. If AI responds with `[SKIP]`, no message is sent
6. Otherwise, the response is broadcast to the appropriate channel

### Smart Filtering

The bot uses multiple layers of filtering to avoid unwanted responses:

**Trigger Pattern Check:**
- Message must match at least one trigger pattern (e.g., end with `?`)

**Heuristic Player Filter:**
- Messages starting with a player's name are skipped (e.g., "John, you coming?")
- Messages with `@playername` are skipped
- Messages starting with "hey [player]" or "yo [player]" are skipped

**AI-Level Skip:**
- The AI is instructed to respond with `[SKIP]` if the question is:
  - Directed at another player
  - Personal chat between players
  - Something the AI can't help with

### Enabling the Global Chat Bot

Edit `carbon/configs/OpenAI.json`:

```json
{
  "Global Chat Bot": {
    "Enabled": true,
    "Trigger Patterns": ["?"],
    "Monitor Global Chat": true,
    "Monitor Team Chat": false,
    "Personality Preset": "helpful"
  }
}
```

Reload the plugin:
```
carbon.reload OpenAI
```

### Testing the Bot

In global chat, type messages ending with `?`:
```
when is wipe?
how do I get scrap?
what monuments have puzzles?
```

The bot should respond to these. It should NOT respond to:
```
John, you coming to raid?
anyone want to trade?
hey mike what's up?
```

### Managing Bot Contexts

```bash
# Clear all bot contexts (global + all teams)
openai.clearbot all

# Clear only global chat context
openai.clearbot global

# Clear only team chat contexts
openai.clearbot teams
```

### Separate Token Budget

Set a separate daily token budget for the bot:

```json
{
  "Global Chat Bot": {
    "Daily Token Budget (0 = shared with main)": 100000
  }
}
```

With `0`, the bot shares the main daily token budget with the `!ai` command.

---

## Bot Personalities

Bot personalities are stored as simple text files in the data folder. This allows easy customization without editing plugin code.

### Personality Files Location

```
oxide/data/OpenAI/personalities/
├── helpful.txt
├── casual.txt
├── professional.txt
├── pirate.txt
└── (your custom personalities)
```

### Default Personalities

The plugin creates these default personalities on first run:

| Preset | Description |
|--------|-------------|
| `helpful` | Friendly, informative, concise (default) |
| `casual` | Relaxed, uses slang, brief responses |
| `professional` | Formal, detailed, thorough |
| `pirate` | Speaks like a pirate (for themed servers) |

### Creating Custom Personalities

1. Create a new `.txt` file in the personalities folder:
   ```
   oxide/data/OpenAI/personalities/cowboy.txt
   ```

2. Write your personality prompt:
   ```
   Howdy partner! You're a friendly cowboy bot on this here Rust server.
   Talk like you're from the Old West, but keep your answers helpful and
   on-topic. Don't overdo the accent - just enough flavor to be fun.
   ```

3. Reload personalities:
   ```
   openai.personalities reload
   ```

4. Update config to use the new personality:
   ```json
   {
     "Global Chat Bot": {
       "Personality Preset": "cowboy"
     }
   }
   ```

5. Reload the plugin:
   ```
   carbon.reload OpenAI
   ```

### Managing Personalities

```bash
# List all loaded personalities
openai.personalities list

# Reload personalities from disk (after editing files)
openai.personalities reload
```

### Example: openai.personalities list

```
=== Bot Personalities (5) ===
Folder: /oxide/data/OpenAI/personalities
Current: helpful

  casual
    You're a chill bot hanging out in a Rust server chat. Keep it s...
  cowboy
    Howdy partner! You're a friendly cowboy bot on this here Rust s...
  helpful [ACTIVE]
    You are a helpful chat bot on a Rust game server. Answer questi...
  pirate
    Yarr! Ye be a pirate bot on this here Rust server. Answer quest...
  professional
    You are a professional server assistant. Provide accurate, well...
================================
```

### Using Custom System Prompt

Instead of a personality file, you can use a custom prompt directly in config:

```json
{
  "Global Chat Bot": {
    "Personality Preset": "custom",
    "Custom System Prompt": "You are a robot assistant. Respond in a mechanical, efficient manner. Use phrases like 'PROCESSING...' and 'QUERY RESOLVED.' but still be helpful."
  }
}
```

When `Personality Preset` is set to `"custom"`, the plugin uses `Custom System Prompt` instead of loading from a file.

---

## Knowledge Base System

The knowledge base lets you upload server-specific documents that the AI can reference when answering questions. This is perfect for:

- Server rules and guidelines
- Custom plugin documentation
- Event schedules
- Economy guides
- Base building tips specific to your server
- FAQ documents

### Setting Up the Knowledge Base

#### Step 1: Enable Knowledge Base

Edit `carbon/configs/OpenAI.json`:

```json
{
  "Knowledge Base": {
    "Enable Knowledge Base": true,
    "Auto Create Vector Store": true
  }
}
```

Reload the plugin:
```
carbon.reload OpenAI
```

#### Step 2: Add Knowledge Files

The plugin creates a knowledge folder at:
```
oxide/data/OpenAI/knowledge/
```

Add `.txt` files with your server information:

**server-rules.txt:**
```
Server Rules:
1. No hacking or exploiting
2. Be respectful to other players
3. No racism or hate speech
4. Raiding is only allowed between 6 PM and 10 PM
5. No base camping for more than 15 minutes
```

**vip-info.txt:**
```
VIP Benefits:
- Instant teleport (no cooldown)
- 2x gather rate
- Reserved slot during full server
- Custom chat tag
- Access to /kit vip

Purchase VIP at: example.com/shop
```

**wipe-schedule.txt:**
```
Wipe Schedule:
- Map wipe: Every Thursday at 3 PM EST
- Blueprint wipe: First Thursday of each month
- Next wipe: January 2, 2025
```

#### Step 3: Sync Files

Run the sync command:
```
openai.kb sync
```

The plugin will:
1. Create a vector store if needed
2. Upload all `.txt` files
3. Index them for AI retrieval

#### Step 4: Test It

In game:
```
!ai What are the server rules?
!ai When is the next wipe?
!ai How do I become VIP?
```

### Managing the Knowledge Base

```bash
# Check status and see local files
openai.kb status

# Upload/update files to OpenAI
openai.kb sync

# List all vector stores in your account
openai.kb list

# See files in current vector store
openai.kb files

# Remove all files from vector store
openai.kb clear
```

### Knowledge Base Best Practices

1. **Keep files focused** - One topic per file works better than one giant file
2. **Use clear formatting** - Headers, lists, and sections help the AI understand structure
3. **Update regularly** - Re-run `openai.kb sync` after editing files
4. **Test your documents** - Ask questions to verify the AI finds the right information
5. **Remove outdated info** - Delete old files and sync to keep the knowledge base current

---

## Rate Limiting

The plugin implements multiple layers of rate limiting to prevent abuse and control costs.

### Rate Limit Layers

| Layer | Scope | Default | Purpose |
|-------|-------|---------|---------|
| **Cooldown** | Per player | 10 seconds | Prevent spam from individual players |
| **Bot Cooldown** | Global bot | 5 seconds | Prevent bot spam |
| **Requests/Minute** | Server-wide | 30 | Prevent API overload during high traffic |
| **Daily Token Budget** | Server-wide | 500,000 | Control total daily costs |
| **Player Daily Limit** | Per player | 15,000 | Ensure fair usage among players |
| **Bot Token Budget** | Bot only | 0 (shared) | Optional separate budget for bot |

### Bypassing Rate Limits

Players with `openai.unlimited` permission bypass all rate limits:

```
oxide.grant user 76561198000000000 openai.unlimited
```

Use sparingly - only for trusted admins or VIP players.

### Monitoring Usage

Check current usage anytime:
```
openai.usage
```

Output:
```
=== Token Usage ===
Server Daily Total: 45,230 / 500,000
Requests This Minute: 3 / 30
Last Reset: 2025-01-28

=== Global Chat Bot ===
Bot Tokens: 12,500 / 100,000
Global Context: Active
Team Contexts: 3 active

=== Player Usage (5) ===
  PlayerOne: 12,340 tokens, 15 requests [active context]
  PlayerTwo: 8,920 tokens, 11 requests
  PlayerThree: 6,450 tokens, 8 requests
  ...
```

### Resetting Usage

To reset all counters (e.g., after increasing budget):
```
openai.resetusage
```

---

## Error Messages

### Player-Facing Messages

| Situation | Message Player Sees |
|-----------|---------------------|
| No permission | "You don't have permission to use this command." |
| Plugin not configured | "AI assistant is not configured. Please contact an administrator." |
| On cooldown | "Please wait X seconds before your next question." |
| Server rate limited | "The AI is busy. Please try again in a moment." |
| Server daily budget exceeded | "Daily server AI budget has been reached. Try again tomorrow." |
| Player daily limit exceeded | "You've reached your daily AI usage limit. Try again tomorrow." |
| API authentication failure | "AI service authentication failed. Please contact an administrator." |
| OpenAI rate limited | "AI service is rate limited. Please try again in a moment." |
| Content filtered | "I cannot respond to that type of question." |
| Injection attempt blocked | "Your message contains disallowed content." |
| Empty response | "Received an empty response. Please try rephrasing your question." |
| General error | "Error processing your request. Please try again." |

### Console Error Messages

| Message | Meaning | Solution |
|---------|---------|----------|
| "No API key configured" | API key is empty | Add your OpenAI API key to the config |
| "API authentication failed" | API key is invalid | Check your API key is correct and not expired |
| "Model 'X' is not available" | Model not accessible | Run `openai.models` to see available models |
| "Reasoning effort 'X' not valid for model" | Wrong reasoning config | Match reasoning effort to model type |
| "Failed to create vector store" | Knowledge base issue | Check API key has vector store permissions |
| "Configured personality 'X' not found" | Missing personality file | Create the file or use an existing preset |

---

## Discord Integration

### Features

When Discord integration is enabled:

1. **Player Interactions** - Logs every AI question and response
2. **Bot Responses** - Logs Global Chat Bot responses
3. **Admin Commands** - Sends summaries of usage reports, context clears, etc.
4. **Knowledge Base Sync** - Reports sync results

### Setting Up

1. Create a Discord webhook in your desired channel
2. Add the webhook URL to config:

```json
{
  "Discord Integration": {
    "Enabled": true,
    "Webhook URL": "https://discord.com/api/webhooks/123456789/abcdefg..."
  }
}
```

### Example Discord Embed

```
AI Chat
───────────────────────────
Player: SteelForge42
Question: How do I get scrap fast?
Response: The fastest ways to get scrap are: 1) Running monuments like
Launch Site, Military Tunnels, or Oil Rig 2) Recycling components at
Outpost or Bandit Camp 3) Completing puzzles at monuments 4) Farming
road barrels and crates...
───────────────────────────
Today at 3:45 PM
```

---

## Security Features

### Prompt Injection Protection

The plugin blocks common jailbreak attempts that try to override the AI's instructions.

**Blocked patterns include:**
- "ignore previous instructions"
- "you are now [something else]"
- "pretend you are"
- "jailbreak"
- "dan mode"

### Input Sanitization

All player input is:
1. Truncated to max length (default: 500 characters)
2. Checked against injection patterns
3. Stripped of control characters

### Custom Instructions

The default custom instructions explicitly tell the AI:
- It cannot see the game world
- It cannot execute commands
- It should not pretend to have capabilities it doesn't have

---

## Troubleshooting

### "AI assistant is not configured"

**Cause:** No API key or API key validation failed.

**Solutions:**
1. Check `openai.status` for details
2. Verify API key is correct in config
3. Test with `openai.test`
4. Check OpenAI account has available credits

### "Model 'X' is not available"

**Cause:** The configured model is not accessible to your API key.

**Solutions:**
1. Run `openai.models` to see available models
2. Update the Model setting in config
3. Check if your OpenAI account tier has access to that model

### Responses are too short/cut off

**Cause:** Max output tokens is set too low.

**Solution:** Increase `Max Output Tokens` in API Settings (try 2048 or higher).

### AI doesn't know about server-specific things

**Cause:** Knowledge base not configured or files not synced.

**Solutions:**
1. Enable knowledge base in config
2. Add `.txt` files to `oxide/data/OpenAI/knowledge/`
3. Run `openai.kb sync`
4. Test with `openai.kb status`

### Global Chat Bot not responding

**Causes and solutions:**
- Bot not enabled: Set `"Enabled": true` in Global Chat Bot config
- Wrong trigger pattern: Check messages match patterns (default: must end with `?`)
- Cooldown active: Wait for cooldown (default: 5 seconds)
- Token budget exhausted: Check `openai.usage`
- AI returning [SKIP]: Bot is choosing not to respond (working as intended)

### Bot responding to everything

**Solutions:**
- Make trigger patterns more specific (e.g., `"@bot"` instead of `"?"`)
- Enable heuristic filtering (automatic)
- The AI will [SKIP] irrelevant messages

### High API costs

**Solutions:**
1. Use a cheaper model like `gpt-4o-mini`
2. Lower `Max Output Tokens`
3. Reduce `Daily Token Budget`
4. Lower `Per Player Daily Token Limit`
5. Increase `Cooldown Seconds`
6. Disable web search if not needed
7. Set a separate lower budget for Global Chat Bot

### Responses take a long time

**Causes and solutions:**
- Using a reasoning model with high effort: Lower reasoning effort
- Web search enabled: Disable if not needed
- Network latency: Check server's internet connection
- OpenAI API slowdown: Wait and retry

### Debug Mode

Enable debug mode for detailed logging:

```json
{
  "Debug Mode": true
}
```

This logs:
- Full API request payloads
- Response parsing details
- Token counts
- Error details

Remember to disable after troubleshooting to reduce log spam.

---

## Changelog

### v3.4.0
- Added Global Chat Bot feature for automatic chat monitoring
- Added bot personality system with external .txt files
- Added team chat support with separate contexts per team
- Added BetterChat integration toggle for styled bot messages
- Added `openai.clearbot` command to clear bot contexts
- Added `openai.personalities` command to manage personalities
- Added separate token budget option for bot
- Added heuristic filtering to avoid responding to player-to-player chat
- Added AI-level [SKIP] response for irrelevant questions
- Removed `Broadcast Responses` setting (replaced by Global Chat Bot)
- Updated `openai.usage` to show bot statistics

### v3.3.0
- Added config version tracking and automatic migration system
- Fixed duplicate CustomInstructions bug on config reload
- Added `ObjectCreationHandling.Replace` to prevent JSON.NET list duplication

### v3.2.0
- Added Knowledge Base system with OpenAI Vector Stores
- Added `openai.kb` commands for knowledge base management
- Added `openai.testmodel` for comprehensive capability testing
- Added `openai.clearcontext` command
- Added `openai.resetusage` command
- Added persistent usage data (survives restarts)
- Added Discord logging for admin commands
- Improved file search integration

### v3.1.0
- Added smart setup validation system
- Added model discovery via OpenAI `/v1/models` endpoint
- Added automatic reasoning model detection
- Added console commands: `openai.status`, `openai.models`, `openai.test`
- Added "Strip URLs from Links" option
- Added `minimal` reasoning effort option
- Enhanced error handling with specific messages
- Improved startup logging

### v3.0.0
- Complete rewrite with modern architecture
- Added `previous_response_id` for conversation continuity
- Added multi-tier rate limiting
- Added `openai.unlimited` permission
- Added reasoning mode and web search support
- Added prompt injection filtering
- Added retry with exponential backoff
- Added Discord rich embeds
- Improved message chunking


