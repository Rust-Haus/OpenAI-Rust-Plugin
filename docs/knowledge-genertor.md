# Rust Server Knowledge Generator

The **Rust Server Knowledge Generator** is a web app that builds **knowledge files** for the OpenAI Chat plugin. You fill out a form (server info, wipe schedule, rules, commands, FAQ, etc.), then either **deploy** those files to an OpenAI vector store from the site or **download a .zip** and sync from your server.

**Live site:** [https://openai-chat.lx1.dev/](https://openai-chat.lx1.dev/)

---

## What it does

- Generates `.txt` knowledge files that the OpenAI plugin uses to answer player questions in chat.
- Form sections drive the output: Server Basics, Wipe Schedule, Server Settings, Rules, Commands, VIP Features, Events, Map Info, Server Highlights, and FAQ.
- You can **deploy** directly to an OpenAI vector store (Push/Pull) or **download a .zip** and run `openai.kb sync` on your server.

You need to **sign in** and **select or create a project** before you can edit files or deploy.

---

## Getting started

1. **Sign in** at [https://openai-chat.lx1.dev/signin](https://openai-chat.lx1.dev/signin) to create an account or use an existing one.
2. Use the **project switcher** in the header to create a new project or switch between projects. All form data and knowledge files are stored per project.
3. On the **home** page: use the file list (left), form (center), and file preview (right). Select a file in the list to preview its content. You can also add **custom files** and edit their content in the preview pane.

---

## Form sections and generated files

| Section | Generated file |
|--------|-----------------|
| Server Basics | `server-info.txt` |
| Wipe Schedule | `wipe-schedule.txt` |
| Server Settings | `server-settings.txt` |
| Rules | `rules.txt` |
| Commands | `commands.txt` |
| VIP Features | `vip-features.txt` |
| Events | `events.txt` |
| Map Info | `map-info.txt` |
| Server Highlights | `server-highlights.txt` |
| FAQ | `faq.txt` |

In the **Commands** section you can search and add plugins (from the default catalog or your own), add chat commands, and contribute commands for plugins. These feed into `commands.txt`.

---

## AI Assistant and Quick Server Lookup

- **AI Assistant** — Use chat to fill out the form. Tell it your server name, wipe schedule, rules, plugins with commands, FAQ, etc., and it will update the form and add plugins/commands for you. The assistant uses its own OpenAI API key (separate from the one you enter on the Deploy page), so you can use it without deploying.
- **Quick Server Lookup** — If your server is listed on BattleMetrics, enter your server IP and port. The tool fetches server name, region, players, map size, wipe info, and other details and prefills the form.

---

## Deploy to OpenAI

1. Open **Deploy** in the header. You must have a project selected and an OpenAI API key.
2. The left sidebar lists your **vector stores**. Create a new one or select an existing one.
3. The main area shows the selected store’s name, **Vector Store ID** (copy button), file list, and actions:
   - **Push** — Upload the current project’s knowledge files (form-generated plus custom files) to the selected vector store. Use after editing the form or adding custom files.
   - **Pull** — Download files from the vector store into your current project. After that, Deploy and Download use those pulled files until you click **Regenerate from form**.
   - **Refresh** — Refresh the file list from OpenAI.
   - **Clear** — Remove all files from the vector store.
4. Copy the **Vector Store ID** into your plugin config. Set **Auto Create Vector Store** to `false` when using a store you create on the site.

See [Plugin documentation — Knowledge Base](plugin.md#knowledge-base-system) for config details.

---

## Download .zip (server sync)

On the Deploy page sidebar, use **Download .zip** to generate all knowledge files and download them as a zip. Then:

1. Extract to your server at `oxide/data/OpenAI/knowledge/` (Oxide) or `carbon/data/OpenAI/knowledge/` (Carbon).
2. Run `openai.kb sync` in the F1 console to upload them to your vector store.

See [Plugin documentation — Knowledge Base](plugin.md#knowledge-base-system) for enabling the knowledge base and syncing.

---

## How it fits with the plugin

```text
Server Admin → This website (form + generated files) → OpenAI Vector Store
                                                              ↓
Players → Rust server (OpenAI plugin) → OpenAI API (searches knowledge + AI response)
```

The generator produces the same `.txt` files you would place in `oxide/data/OpenAI/knowledge/`. You can either push them to a vector store from the site (and paste the Vector Store ID into config) or download the zip, extract to the knowledge folder, and run `openai.kb sync` on the server.

---

## Help

For a step-by-step guide with screenshots, use **Help** in the site header or go to [https://openai-chat.lx1.dev/help](https://openai-chat.lx1.dev/help).
