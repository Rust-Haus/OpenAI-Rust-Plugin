# OpenAI Plugin

AI assistant plugin for Rust (Oxide/uMod) using the OpenAI Responses API. Players can ask questions in-game and receive AI-powered responses.

## Quick Start

1. **Get an API key** from [OpenAI Platform](https://platform.openai.com/).
2. **Configure** `oxide/configs/OpenAI.json` with your API key and model (e.g. `gpt-5-nano`).
3. **Grant permission:** `oxide.grant group default openai.use`
4. **Reload:** `oxide.reload OpenAI`
5. **Test in chat:** `!ai Hello, what can you help with?`

## Documentation

Full plugin documentation is in the **[docs](docs/)** folder. See **[docs/README.md](docs/README.md)** for the index.

| Document | Description |
|----------|-------------|
| [Plugin documentation](docs/openai-plugin.md) | Full guide: features, permissions, console commands, configuration reference, Global Chat Bot, Knowledge Base, rate limiting, troubleshooting, changelog. |
| [Knowledge Generator](docs/knowledge-genertor.md) | Web app to build knowledge files: form-based generation, deploy to OpenAI or download .zip. Live at [openai-chat.lx1.dev](https://openai-chat.lx1.dev/). |
| [VIP Tiers](docs/vip-tiers.md) | Set up VIP tiers so selected players get a different AI model and higher token limits (permission-based, no Steam ID lists). |
| [Developer Hooks](docs/developer-hooks.md) | For Oxide plugin developers: hook API and examples to use the OpenAI plugin from other plugins. |

## Requirements

- Rust server with Oxide/uMod
- OpenAI API key with credits
