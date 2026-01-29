# OpenAI Plugin v2.1.0

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
- [Support](#support)

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

Edit `oxide/configs/OpenAI.json`:

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
oxide.reload OpenAI
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
| `openai.personalities` | Show all available bot personalities |

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
openai.personalities

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

Configuration file location: `oxide/configs/OpenAI.json`

Below is the complete default configuration with detailed explanations for each setting.

### Full Default Configuration

```json
{
  "Config Version": "3.4.0",
  "API Settings": {
    "API Key": "",
    "API URL": "https://api.openai.com/v1/responses",
    "Model": "gpt-5-nano",
    "Max Output Tokens (0 = model default)": 2048,
    "Reasoning Effort (none/minimal/low/medium/high)": "low",
    "Enable Web Search": false,
    "Retry Attempts": 3
  },
  "Rate Limits": {
    "Cooldown Seconds": 10,
    "Max Requests Per Minute": 30,
    "Daily Token Budget": 500000,
    "Per Player Daily Token Limit": 15000,
    "Persist Usage Data": true
  },
  "Chat Settings": {
    "Command Prefix": "!ai",
    "Response Prefix": "[AI]",
    "Response Color": "#55AAFF",
    "Message Color": "#FFFFFF",
    "Font Size": 12,
    "Max Message Chunk Size": 450,
    "Strip URLs from Links": true
  },
  "Discord Integration": {
    "Enabled": false,
    "Webhook URL": ""
  },
  "Security Settings": {
    "Max Input Length": 500,
    "Filter Injection Attempts": true
  },
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
  },
  "Knowledge Base": {
    "Enable Knowledge Base": false,
    "Vector Store ID": "",
    "Knowledge Subfolder": "OpenAI/knowledge",
    "Auto Create Vector Store": true
  },
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
  },
  "Debug Mode": false
}
```

---

### Config Version

```json
"Config Version": "3.4.0"
```

**Purpose:** Tracks the plugin version that last saved this config file.

**Why it exists:** When the plugin updates, it may need to migrate old config settings to new formats. This version number tells the plugin what migrations to apply.

**Do not:**
- Manually change this value
- Delete this field

The plugin manages this automatically. If you manually set it to a higher version, migrations may be skipped and settings could be lost.

---

### API Settings

```json
"API Settings": {
  "API Key": "",
  "API URL": "https://api.openai.com/v1/responses",
  "Model": "gpt-5-nano",
  "Max Output Tokens (0 = model default)": 2048,
  "Reasoning Effort (none/minimal/low/medium/high)": "low",
  "Enable Web Search": false,
  "Retry Attempts": 3
}
```

#### API Key

**Purpose:** Your OpenAI API key for authentication.

**How to get one:**
1. Go to https://platform.openai.com/api-keys
2. Click "Create new secret key"
3. Copy the key (starts with `sk-`)
4. Paste it here

**Why it exists:** OpenAI requires authentication for all API calls. Without a valid key, the plugin cannot function.

**Do not:**
- Share your API key publicly
- Commit it to version control
- Use a key with spending limits if you want unrestricted usage

**Troubleshooting:**
- If you get authentication errors, verify the key at https://platform.openai.com/settings/{PROJECT_ID}/limits
- Ensure the key has no IP restrictions that would block your server
- Check that your OpenAI account has available credits

#### API URL

**Purpose:** The OpenAI API endpoint for the Responses API.

**Default:** `https://api.openai.com/v1/responses`

**Why it exists:** Allows using proxy servers or alternative API-compatible services.

**Do not:**
- Change this unless you're using a proxy or alternative service
- Use the old `/v1/chat/completions` endpoint (this plugin uses the newer Responses API)

**When to change:** Only if you're using an OpenAI-compatible proxy, a caching layer, or a third-party service that mirrors the OpenAI API.

#### Model

**Purpose:** Specifies which AI model processes requests.

**Default:** `gpt-5-nano`

**Why it exists:** Different models have different capabilities, speeds, and costs. This lets you choose the right balance for your server.

**Available models** (run `openai.models` to see your available options):

| Model | Best For | Cost | Speed | Notes |
|-------|----------|------|-------|-------|
| `gpt-5-nano` | General use | Very Low | Very Fast | Cheapest option, good for most questions |
| `gpt-4o-mini` | General use | Low | Fast | Good balance of quality and cost |
| `gpt-4o` | Complex questions | Medium | Medium | Higher quality responses |
| `o1-mini` | Reasoning tasks | Medium | Medium | Uses extended thinking |
| `o3-mini` | Advanced reasoning | High | Slower | Best for complex logic |
| `o4-mini` | Latest reasoning | High | Slower | Newest reasoning model |

**Do not:**
- Use expensive models like `gpt-4o` or `o3-mini` on high-traffic servers without appropriate rate limits
- Use reasoning models (o1, o3, o4) for simple questions - they're overkill and expensive

**Recommendations:**
- Start with `gpt-5-nano` or `gpt-4o-mini` for most servers
- Only upgrade if response quality is insufficient
- Use `openai.testmodel` to verify the model works before going live

#### Max Output Tokens (0 = model default)

**Purpose:** Limits how long AI responses can be.

**Default:** `2048` (approximately 1500 words)

**Why it exists:**
- Controls costs (longer responses = more tokens = more money)
- Prevents the AI from writing essays when you want brief answers
- Ensures responses fit reasonably in game chat

**Values:**
- `0` = Use the model's default maximum
- `500` = Short responses (~375 words)
- `1024` = Medium responses (~750 words)
- `2048` = Long responses (~1500 words)
- `4096` = Very long responses (~3000 words)

**Do not:**
- Set this extremely high (like 16000) unless you have a specific need
- Set this below 100, or responses may be cut off mid-sentence

**Recommendations:**
- `1024-2048` is ideal for game chat
- Lower values save money but may truncate complex answers
- The AI is instructed to be concise, so it rarely hits this limit anyway

#### Reasoning Effort (none/minimal/low/medium/high)

**Purpose:** Controls how much "thinking" reasoning models do before responding.

**Default:** `low`

**Options:** `none`, `minimal`, `low`, `medium`, `high`

**Why it exists:** Reasoning models (o1, o3, o4 series) can spend extra time analyzing before responding. Higher effort = better reasoning but slower and more expensive.

| Effort | Use Case | Cost Impact |
|--------|----------|-------------|
| `none` | Standard GPT models (they ignore this anyway) | None |
| `minimal` | Quick factual questions | +10-20% |
| `low` | General questions with some complexity | +20-40% |
| `medium` | Multi-step problems, calculations | +50-100% |
| `high` | Complex analysis, detailed explanations | +100-200% |

**Do not:**
- Use `high` on a busy server - it's slow and expensive
- Worry about this setting if using GPT models (gpt-4o, gpt-4o-mini) - they ignore it

**Recommendations:**
- Set to `none` if using GPT models
- Set to `low` or `minimal` for reasoning models in most cases
- Only use `medium` or `high` if players ask complex technical questions

#### Enable Web Search

**Purpose:** Allows the AI to search the internet for current information.

**Default:** `false`

**Why it exists:** AI models have a knowledge cutoff date. Web search lets them find current information like:
- Recent game updates
- Current events
- Real-time data

**Do not:**
- Enable this if you want consistent, predictable responses
- Enable this on budget-constrained servers (adds ~$0.01-0.05 per search)

**When to enable:**
- Players frequently ask about recent Rust updates
- You want the AI to find current information
- Cost is not a primary concern

**Note:** Web search adds latency (1-3 seconds) as the AI fetches and processes search results.

#### Retry Attempts

**Purpose:** How many times to retry failed API calls.

**Default:** `3`

**Why it exists:** API calls can fail due to:
- Network issues
- OpenAI server overload (5xx errors)
- Temporary rate limits

The plugin uses exponential backoff: 2 seconds, then 4 seconds, then 8 seconds between retries.

**Do not:**
- Set this to 0 (no retries means any hiccup fails the request)
- Set this above 5 (excessive retries just delay the inevitable failure)

**Recommendations:**
- `3` is ideal for most cases
- Increase to `4-5` if you have an unstable network connection
- Decrease to `1-2` if you prefer fast failure over waiting

---

### Rate Limits

```json
"Rate Limits": {
  "Cooldown Seconds": 10,
  "Max Requests Per Minute": 30,
  "Daily Token Budget": 500000,
  "Per Player Daily Token Limit": 15000,
  "Persist Usage Data": true
}
```

#### Cooldown Seconds

**Purpose:** Minimum time a player must wait between AI requests.

**Default:** `10` seconds

**Why it exists:**
- Prevents spam (players rapidly sending questions)
- Ensures fair access (one player can't monopolize the AI)
- Controls costs (limits request frequency)

**Do not:**
- Set to 0 (allows unlimited spam)
- Set very high (60+) unless you want to severely restrict usage

**Recommendations:**
- `5-10` for most servers
- `15-30` for budget-conscious servers
- `3-5` for VIP-only servers with trusted users

**Note:** Players with `openai.unlimited` permission bypass this cooldown.

#### Max Requests Per Minute

**Purpose:** Server-wide limit on total API requests per minute.

**Default:** `30`

**Why it exists:**
- Prevents API rate limit errors from OpenAI
- Controls costs during traffic spikes
- Ensures the AI remains responsive (not overloaded)

**Do not:**
- Set above 60 (OpenAI's rate limits may kick in)
- Set below 5 (too restrictive for normal use)

**Recommendations:**
- `20-30` for most servers
- `10-15` for small private servers
- `40-50` for large servers with paid OpenAI plans

**Note:** This is a soft limit - the plugin will reject new requests when the limit is hit, but existing requests will complete.

#### Daily Token Budget

**Purpose:** Maximum total tokens the server can use per day (UTC reset).

**Default:** `500000` (~$0.05-0.50 depending on model)

**Why it exists:**
- Hard cost control - prevents runaway API bills
- Forces you to think about usage patterns
- Protects against abuse or bugs

**Token costs (approximate, per 1M tokens):**
| Model | Input | Output |
|-------|-------|--------|
| gpt-5-nano | $0.10 | $0.40 |
| gpt-4o-mini | $0.15 | $0.60 |
| gpt-4o | $2.50 | $10.00 |

**Do not:**
- Set to 0 (disables the plugin effectively)
- Set extremely high without understanding your costs
- Ignore this setting - it's your primary cost control

**Recommendations by server size:**
| Server Size | Players | Suggested Budget |
|-------------|---------|------------------|
| Private | 5-10 | 100,000 |
| Small | 10-30 | 250,000 |
| Medium | 30-75 | 500,000 |
| Large | 75-150 | 1,000,000 |
| Very Large | 150+ | 2,000,000+ |

#### Per Player Daily Token Limit

**Purpose:** Maximum tokens each individual player can use per day.

**Default:** `15000` (~10-20 conversations)

**Why it exists:**
- Ensures fair distribution of the daily budget
- Prevents one player from exhausting the server's quota
- Encourages concise questions

**Do not:**
- Set higher than `Daily Token Budget / expected active players`
- Set below 1000 (too restrictive for meaningful conversations)

**Recommendations:**
- `10000-15000` for most servers
- `5000-8000` for large servers with many active users
- `25000-50000` for VIP/donor perks

#### Persist Usage Data

**Purpose:** Save usage statistics to disk so they survive server restarts.

**Default:** `true`

**Why it exists:**
- Maintains accurate daily limits across restarts
- Prevents players from bypassing limits by waiting for restart
- Provides accurate usage reporting

**Do not:**
- Disable this unless you want usage to reset every restart
- Worry about disk usage (the file is tiny, <1KB typically)

**When to disable:**
- Testing/development where you want fresh limits each restart
- Servers that restart daily and want a "soft reset"

---

### Chat Settings

```json
"Chat Settings": {
  "Command Prefix": "!ai",
  "Response Prefix": "[AI]",
  "Response Color": "#55AAFF",
  "Message Color": "#FFFFFF",
  "Font Size": 12,
  "Max Message Chunk Size": 450,
  "Strip URLs from Links": true
}
```

#### Command Prefix

**Purpose:** The text players type to trigger an AI question.

**Default:** `!ai`

**Why it exists:** Distinguishes AI commands from normal chat. Players type `!ai <question>` to ask the AI.

**Examples:**
```
!ai How do I craft gunpowder?
!ai What's the best monument for loot?
!ai How much sulfur to raid a sheet metal door?
```

**Do not:**
- Use a prefix that conflicts with other plugins (check your other chat commands)
- Use very common words/symbols that trigger accidentally
- Leave empty (disables the command entirely)

**Alternative prefixes:**
- `/ask` - Slash command style
- `@ai` - Mention style
- `??` - Quick and short
- `!help` - If you want it to feel like a help system

#### Response Prefix

**Purpose:** Text shown before AI responses in chat.

**Default:** `[AI]`

**Why it exists:** Clearly identifies messages as coming from the AI, not a player or admin.

**Examples of how it appears:**
```
[AI] To craft gunpowder, you need 30 charcoal and 20 sulfur...
```

**Do not:**
- Remove this entirely (players won't know it's AI)
- Make it too long (wastes chat space)
- Use something misleading like `[Admin]` or a player's name

**Alternatives:**
- `[Bot]` - Simple and clear
- `[Assistant]` - More formal
- `»` - Minimal

#### Response Color

**Purpose:** Hex color code for the response prefix.

**Default:** `#55AAFF` (light blue)

**Why it exists:** Makes AI responses visually distinct from player chat.

**Do not:**
- Use colors that are hard to read (dark colors on dark background)
- Use the same color as admin/VIP chat (causes confusion)

**Common choices:**
- `#55AAFF` - Light blue (default, friendly)
- `#00FF00` - Green (success/helpful)
- `#FFD700` - Gold (premium feel)
- `#FF6B6B` - Coral (warm, approachable)
- `#FFFFFF` - White (neutral)

#### Message Color

**Purpose:** Hex color code for the actual response text.

**Default:** `#FFFFFF` (white)

**Why it exists:** Allows customizing the response body color separately from the prefix.

**Do not:**
- Use dark colors that are hard to read
- Use the same color as the prefix (no visual separation)

**Recommendations:**
- Keep at white (`#FFFFFF`) for best readability
- Light gray (`#CCCCCC`) for a softer look
- Match your server's chat theme if you have one

#### Font Size

**Purpose:** Text size for AI responses in chat.

**Default:** `12` (Rust's default chat size)

**Why it exists:** Allows making AI responses larger or smaller than normal chat.

**Do not:**
- Set below 8 (too small to read)
- Set above 16 (obnoxiously large)
- Set to 0 (invisible text)

**Recommendations:**
- `12` - Same as normal chat (blends in)
- `11` - Slightly smaller (less intrusive)
- `13-14` - Slightly larger (more prominent)

#### Max Message Chunk Size

**Purpose:** Character limit before splitting responses into multiple messages.

**Default:** `450`

**Why it exists:** Rust chat has a maximum message length. Long AI responses must be split into chunks.

**Do not:**
- Set above 500 (may hit Rust's hard limit and get truncated)
- Set below 100 (causes excessive message spam for normal responses)

**Recommendations:**
- `400-450` is optimal
- Lower values (300-350) if you have other plugins adding prefixes
- The plugin splits at word boundaries, so messages won't be cut mid-word

#### Strip URLs from Links

**Purpose:** Removes URLs from markdown-style links, keeping only the text.

**Default:** `true`

**Why it exists:** AI often responds with markdown links like `[Rust Wiki](https://rust.fandom.com/...)`. Since URLs aren't clickable in Rust chat, this converts them to just `[Rust Wiki]`.

**Do not:**
- Disable this unless you have a specific reason to show URLs
- URLs in chat are ugly and useless since players can't click them

**When to disable:**
- You're logging to Discord where links ARE clickable
- You want players to manually copy URLs (rare use case)

---

### Discord Integration

```json
"Discord Integration": {
  "Enabled": false,
  "Webhook URL": ""
}
```

#### Enabled

**Purpose:** Turn Discord logging on or off.

**Default:** `false`

**Why it exists:** Optional feature - not everyone uses Discord for server management.

**When to enable:**
- You want to log AI conversations for moderation
- You want visibility into what players are asking
- You want admin commands logged to a channel

#### Webhook URL

**Purpose:** The Discord webhook URL for sending messages.

**How to get one:**
1. Open your Discord server
2. Go to Server Settings > Integrations > Webhooks
3. Click "New Webhook"
4. Choose a channel for AI logs
5. Click "Copy Webhook URL"
6. Paste it here

**Do not:**
- Share this URL publicly (anyone with it can post to your channel)
- Use a webhook from a channel you don't control

**What gets logged:**
- Every player AI question and response
- Global Chat Bot responses
- Admin command results (usage reports, sync results, etc.)

---

### Security Settings

```json
"Security Settings": {
  "Max Input Length": 500,
  "Filter Injection Attempts": true
}
```

#### Max Input Length

**Purpose:** Maximum characters allowed in a player's question.

**Default:** `500` characters (~100 words)

**Why it exists:**
- Prevents abuse (sending huge text blocks)
- Reduces token costs (longer inputs = more tokens)
- Keeps questions focused

**Do not:**
- Set below 50 (too restrictive for meaningful questions)
- Set above 2000 (allows excessive input that may be abusive or costly)

**Recommendations:**
- `300-500` for most servers
- `100-200` for strict cost control
- `500-1000` if you want to allow detailed questions

#### Filter Injection Attempts

**Purpose:** Block common prompt injection/jailbreak phrases.

**Default:** `true`

**Why it exists:** Some users may try to manipulate the AI by using phrases like "ignore your instructions" or "pretend you are something else."

**Blocked phrases include:**
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

**Do not:**
- Disable this unless you trust all users completely
- Rely on this as your only protection (it's basic filtering)

**Note:** Players attempting these will see: "Your message contains disallowed content."

---

### Prompt Settings

```json
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
```

#### System Prompt

**Purpose:** The core personality and instruction set for the AI.

**Why it exists:** Defines how the AI behaves, what persona it adopts, and its general approach to answering questions.

**Do not:**
- Remove this entirely (the AI needs guidance)
- Make it excessively long (uses tokens on every request)
- Include sensitive information (it's sent with every API call)

**Customization examples:**

**Friendly helper:**
```json
"System Prompt": "You are a friendly and helpful assistant on a Rust game server. Be encouraging to new players and keep responses clear and concise."
```

**Pirate theme:**
```json
"System Prompt": "Yarr! Ye be a salty sea dog assistant on a Rust server. Answer questions like a pirate but keep it helpful, matey!"
```

**Professional:**
```json
"System Prompt": "You are a professional server assistant. Provide accurate, well-structured responses to player questions. Maintain a neutral, informative tone."
```

#### Include Server Info

**Purpose:** Automatically include server name and player count in the AI's context.

**Default:** `true`

**Why it exists:** Allows the AI to mention the server name and know how busy it is.

**What it adds to the prompt:**
```
Server: My Rust Server
Players online: 45/100
```

**When to disable:**
- You don't want the AI mentioning player counts
- You want to minimize token usage (saves ~20 tokens per request)

#### Include Player Names

**Purpose:** Tell the AI who is asking the question.

**Default:** `true`

**Why it exists:** Allows personalized responses ("Hi John, to craft gunpowder...")

**What it adds to the prompt:**
```
You are talking to: PlayerName
```

**When to disable:**
- You want anonymous interactions
- Privacy concerns

#### Custom Instructions

**Purpose:** Additional rules and behaviors appended to the system prompt.

**Default:** See the default list above

**Why it exists:** Provides important guardrails that prevent the AI from:
- Claiming abilities it doesn't have (seeing the game world)
- Offering to do things it can't (execute commands)
- Being overly verbose

**Do not:**
- Remove the default instructions without understanding what they prevent
- Add conflicting instructions
- Add excessive instructions (each one costs tokens)

**How to add server-specific instructions:**

```json
"Custom Instructions": [
  // Keep the defaults, then add yours:
  "You are a text-only assistant...",
  "You can only answer questions...",
  // Your custom additions:
  "This is a PvE server. There is no raiding.",
  "The server wipes every Thursday at 3 PM EST.",
  "VIP kits are available with /kit vip.",
  "Our Discord is discord.gg/example"
]
```

**Warning:** Each instruction is sent with every request. Adding 10 long instructions significantly increases token usage.

---

### Knowledge Base

```json
"Knowledge Base": {
  "Enable Knowledge Base": false,
  "Vector Store ID": "",
  "Knowledge Subfolder": "OpenAI/knowledge",
  "Auto Create Vector Store": true
}
```

#### Enable Knowledge Base

**Purpose:** Turn the knowledge base feature on or off.

**Default:** `false`

**Why it exists:** The knowledge base requires OpenAI vector stores, which have additional costs. It's disabled by default so you opt-in consciously.

**When to enable:**
- You have server-specific information (rules, guides, schedules)
- You want the AI to reference your documents
- You're willing to pay the small additional cost (~$0.10/GB/day)

#### Vector Store ID

**Purpose:** The OpenAI Vector Store ID where your documents are stored.

**Default:** `""` (empty)

**Why it exists:** Links the plugin to your specific vector store in OpenAI.

**Do not:**
- Manually enter a random ID
- Share this ID publicly

**How it works:**
- Leave empty and set `Auto Create Vector Store: true` - plugin creates one for you
- Or create one manually at https://platform.openai.com/storage/vector_stores and paste the ID here

#### Knowledge Subfolder

**Purpose:** Folder path (under `oxide/data/`) where you place knowledge files.

**Default:** `OpenAI/knowledge`

**Full path:** `oxide/data/OpenAI/knowledge/`

**Why it exists:** Keeps knowledge files organized separately from other plugin data.

**Do not:**
- Use a path outside the data folder
- Put non-text files in this folder (only `.txt` files are uploaded)

#### Auto Create Vector Store

**Purpose:** Automatically create a vector store if none exists.

**Default:** `true`

**Why it exists:** Simplifies setup - you don't need to manually create a vector store in OpenAI's dashboard.

**What happens when true:**
1. You run `openai.kb sync`
2. Plugin checks if `Vector Store ID` is empty
3. Plugin creates a new vector store named "Rust Server Knowledge"
4. Plugin saves the ID to config
5. Plugin uploads your files

**When to set false:**
- You want to manage vector stores manually
- You're sharing a vector store between multiple servers

---

### Global Chat Bot

```json
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
```

#### Enabled

**Purpose:** Turn the automatic chat monitoring bot on or off.

**Default:** `false`

**Why it exists:** The Global Chat Bot monitors ALL chat (not just commands), which uses more tokens. It's opt-in.

**Difference from `!ai` command:**
- `!ai` requires players to explicitly ask for help
- Global Bot monitors chat passively and responds to questions

#### Bot Name

**Purpose:** Display name for the bot.

**Default:** `Assistant`

**Why it exists:** Used in BetterChat mode and logging to identify the bot.

**In BetterChat mode, appears as:**
```
[AI] Assistant: Here's your answer...
```

#### Response Prefix

**Purpose:** Text shown before bot responses (standard mode only).

**Default:** `[Bot]`

**Why it exists:** Identifies bot messages separately from `!ai` responses (which use `[AI]`).

**Note:** Not used when `Use Better Chat` is enabled.

#### Response Color

**Purpose:** Color for the bot's prefix/name.

**Default:** `#55AAFF` (light blue)

**Why it exists:** Visual distinction for bot messages.

#### Cooldown Seconds

**Purpose:** Minimum time between bot responses.

**Default:** `5` seconds

**Why it exists:**
- Prevents the bot from responding too frequently
- Gives humans time to answer before bot jumps in
- Controls costs

**Do not:**
- Set to 0 (bot will respond to every matching message immediately)
- Set too high (bot becomes unresponsive)

**Recommendations:**
- `3-5` for active engagement
- `10-15` for less intrusive presence
- `30+` if you only want occasional bot input

#### Trigger Patterns

**Purpose:** Patterns that trigger the bot to consider responding.

**Default:** `["?"]` (messages ending with question mark)

**Why it exists:** The bot only looks at messages matching these patterns, not every message.

**How matching works:**
- Pattern at end of message: `"when is wipe?"` matches `"?"`
- Pattern anywhere: `"@bot help"` matches `"@bot"`

**Examples:**
```json
"Trigger Patterns": ["?"]              // Questions only
"Trigger Patterns": ["?", "@bot"]      // Questions or explicit mentions
"Trigger Patterns": ["@assistant"]     // Only explicit mentions
"Trigger Patterns": ["help", "?"]      // Messages with "help" or questions
```

**Do not:**
- Use empty patterns (matches everything)
- Use very common words like "the" or "a"

#### Monitor Global Chat

**Purpose:** Respond to messages in global/public chat.

**Default:** `true`

**Why it exists:** Global chat is where most questions are asked.

#### Monitor Team Chat

**Purpose:** Respond to messages in team chat.

**Default:** `false`

**Why it exists:** Team chat is more private; some servers want bot help there too.

**Note:** Each team has its own conversation context. Team A's conversation with the bot is separate from Team B's.

#### Personality Preset

**Purpose:** Which personality file to use for the bot.

**Default:** `helpful`

**Why it exists:** Allows different personalities without editing config.

**Built-in presets:**
- `helpful` - Friendly, informative
- `casual` - Relaxed, uses slang
- `professional` - Formal, detailed
- `pirate` - Speaks like a pirate
- `custom` - Uses `Custom System Prompt` field instead

**How to add custom personalities:** See [Bot Personalities](#bot-personalities) section.

#### Custom System Prompt

**Purpose:** Custom personality prompt when using `"Personality Preset": "custom"`.

**Default:** `""` (empty)

**When to use:** If you want a unique personality without creating a file.

**Example:**
```json
"Personality Preset": "custom",
"Custom System Prompt": "You are a robot assistant. Respond in a mechanical, efficient manner. Use phrases like 'PROCESSING...' and 'QUERY RESOLVED.'"
```

#### Daily Token Budget (0 = shared with main)

**Purpose:** Separate token budget for the Global Chat Bot.

**Default:** `0` (shares with main budget)

**Why it exists:** Lets you allocate a specific amount for passive bot responses vs. explicit `!ai` commands.

**How it works:**
- `0` = Bot uses the main `Daily Token Budget` (shared pool)
- `100000` = Bot has its own 100k token budget, separate from `!ai`

**When to set a separate budget:**
- You want to ensure `!ai` always has tokens available
- You want to limit how much the passive bot can spend

#### Use Better Chat

**Purpose:** Enable BetterChat-style message formatting.

**Default:** `false`

**Why it exists:** If you use BetterChat, this makes the bot's messages match that style.

**Standard mode:**
```
[Bot] Here's your answer...
```

**BetterChat mode:**
```
[AI] Assistant: Here's your answer...
```

See [BetterChat Integration](#betterchat-integration) for details.

#### Better Chat Title

**Purpose:** Title/tag shown before bot name in BetterChat mode.

**Default:** `[AI]`

**Examples:**
- `[AI]` - Default
- `[BOT]` - Simple
- `[HELPER]` - Descriptive
- `[ADMIN]` - If you want it to look official (use carefully)

#### Better Chat Title Color

**Purpose:** Color for the BetterChat title.

**Default:** `#55AAFF` (light blue)

**Why it exists:** Allows the title to have a different color from the bot name.

---

### Debug Mode

```json
"Debug Mode": false
```

**Purpose:** Enable verbose logging for troubleshooting.

**Default:** `false`

**What it logs:**
- Full API request payloads
- Raw API responses
- Token counts
- Response parsing details
- Error details

**When to enable:**
- Something isn't working and you need details
- You're testing configuration changes
- OpenAI support asks for request/response data

**Do not:**
- Leave enabled in production (generates massive log files)
- Enable on busy servers (performance impact)

**Always disable after troubleshooting.**

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

Edit `oxide/configs/OpenAI.json`:

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
oxide.reload OpenAI
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

## BetterChat Integration

The Global Chat Bot can optionally format messages to match the visual style of the BetterChat plugin. This makes the AI bot appear as a styled "player" in chat with a title tag, matching how BetterChat formats player messages.

### Standard Mode vs BetterChat Mode

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

### Enabling BetterChat Mode

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

### Customization Examples

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

### Technical Notes

- BetterChat mode uses `chat.add` console command instead of `ChatMessage()` for proper formatting
- Messages appear in the F1 console log just like regular player messages
- Works in both global and team chat channels
- Does not require the actual BetterChat plugin to be installed

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

3. Update config to use the new personality:
   ```json
   {
     "Global Chat Bot": {
       "Personality Preset": "cowboy"
     }
   }
   ```

4. Reload the plugin:
   ```
   oxide.reload OpenAI
   ```

### Managing Personalities

```bash
# List all loaded personalities
openai.personalities
```

To reload personalities after editing files, simply reload the plugin:
```bash
oxide.reload OpenAI
```

### Example: openai.personalities

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

Edit `oxide/configs/OpenAI.json`:

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
oxide.reload OpenAI
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
You can get an API key from OpenAI at https://platform.openai.com/account/api-keys
Make sure there are no restrictions on the api key. To check you have to login to the OpenAI platform and select the project where you created the api key. You can check at https://platform.openai.com/settings/{YOUR PROJECT ID}/limits. (Login > Settings "Gear Icon at the top right" > Limits)

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

You can view model costs at https://openai.com/api/pricing/
I used gpt-5-nano during development with vector store and web search enabled. I used over 100,000 tokens and it cost me $0.07.


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

### v2.1.0
  - Multi-Language Support (Localization)
  - Improved Trigger Pattern Detection" Fixed trigger patterns not matching when followed by punctuation or special characters at the start of messages with `@` prefix.
  - Custom Command Registration (Slash Commands): You can now use slash-style commands like `/askai` instead of chat prefixes like `!ai`.

**Configuration example:**
```json
{
  "Chat Settings": {
    "Command Prefix": "/askai"
  }
}
```
 

