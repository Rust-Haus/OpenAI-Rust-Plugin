# VIP Tiers

This guide explains how to set up and use the **VIP Tiers** feature so selected players get a different AI model and higher token limits (daily limit, max output tokens, cooldown, reasoning effort, web search) without editing Steam ID lists.

---

## Overview

- **VIP Tiers** are defined in the plugin config (`oxide/config/OpenAI.json`). Each tier has a **name** (e.g. `vip_elite`, `vip1`) and settings: model, max output tokens, daily token limit, cooldown, reasoning effort, web search.
- The **permission** for a tier is always `openai.vip.<tier_name>`. For example, the tier `vip_elite` uses permission `openai.vip.vip_elite`. You do not set the permission in the config — it is derived from the tier name.
- When a player uses the AI chat, the plugin picks the tier they have permission for and applies that tier's settings. If a player has more than one VIP permission, **VIP Tier Order** (optional) decides which tier wins; otherwise you can omit it. If they have no VIP tier, default API and rate-limit settings are used.
- **openai.unlimited** is unchanged: it bypasses all rate limits. VIP does not bypass limits; it only overrides model and per-player limits.

---

## 1. Config Setup

Edit `oxide/config/OpenAI.json` and add **VIP Tiers**. **VIP Tier Order** is optional and only needed in special cases (see below).

### VIP Tier Order (optional — most admins leave this empty)

A list of tier names in **priority order**. It only matters when a player can have **more than one** VIP permission (e.g. two groups granting different tiers). The first tier in this list that the player has is used.

**If you assign VIP programmatically** (e.g. another plugin verifies payment and runs `oxide.grant user <id> openai.vip.vip1`), each player typically has **one** VIP tier. In that case **you do not need VIP Tier Order** — omit it or leave it `[]`. The plugin will still resolve the player's single tier correctly.

Only set **VIP Tier Order** if players can have multiple VIP permissions and you want to control which tier wins (e.g. premium over basic):

```json
"VIP Tier Order": ["vip_ultra", "vip1"],
```

### VIP Tiers (required for VIP to do anything)

An **object** (key-value map). Each **key** is the tier name; each **value** is the settings for that tier.

| Field | Type | Meaning |
|-------|------|--------|
| **Model** | string | AI model for this tier (e.g. `gpt-4`, `gpt-4-mini`). |
| **Max Output Tokens** | int | Max tokens per response. `0` = use default from API Settings. |
| **Daily Token Limit** | int | Per-player daily token limit for this tier. `0` = use default from Rate Limits. |
| **Cooldown Seconds** | int | Cooldown between requests. `0` = use default from Rate Limits. |
| **Reasoning Effort** | string | e.g. `none`, `low`, `medium`, `high`. Empty = use default from API Settings. |
| **Web Search Enabled** | bool | Override API-level "Enable Web Search" for this tier. |

### Example config block

You can omit **VIP Tier Order** (or use `[]`) when each player has only one tier (e.g. assigned by a payment/verification plugin).

```json
"VIP Tier Order": [],
"VIP Tiers": {
  "vip1": {
    "Model": "gpt-4-mini",
    "Max Output Tokens": 4096,
    "Daily Token Limit": 50000,
    "Cooldown Seconds": 0,
    "Reasoning Effort": "low",
    "Web Search Enabled": false
  },
  "vip_ultra": {
    "Model": "gpt-4",
    "Max Output Tokens": 8192,
    "Daily Token Limit": 100000,
    "Cooldown Seconds": 5,
    "Reasoning Effort": "",
    "Web Search Enabled": true
  }
}
```

- Tier name `vip1` → permission **openai.vip.vip1**
- Tier name `vip_ultra` → permission **openai.vip.vip_ultra**

Save the config and reload the plugin (or restart the server). New tiers are registered automatically on config reload.

---

## 2. Giving Players a VIP Tier

You can assign a tier by granting the corresponding permission. Many servers use **another plugin** (e.g. payment/verification) that runs a command like `oxide.grant user <Steam64ID> openai.vip.vip1` when a player pays for VIP — that works without **VIP Tier Order**; just define the tier in **VIP Tiers** and have the other plugin grant `openai.vip.<tier_name>`.

### Option A: Console command (recommended)

You need **openai.admin** to use the VIP command (server console or F1 console if you have the permission).

**Grant a tier**

```text
openai.vip <Steam64ID> <tier_name>
```

Example: grant `vip_elite` to a player:

```text
openai.vip 76561198012345678 vip_elite
```

This grants **openai.vip.vip_elite** to that Steam ID.

**Revoke a tier**

```text
openai.vip remove <Steam64ID> <tier_name>
```

Example:

```text
openai.vip remove 76561198012345678 vip_elite
```

**List a player's VIP tiers**

```text
openai.vip <Steam64ID>
```

Example:

```text
openai.vip 76561198012345678
```

**Help**

```text
openai.vip
```

Shows usage and valid tier names from your config.

### Option B: Oxide permissions (groups / manual)

1. In Oxide, the plugin registers one permission per tier, e.g. `openai.vip.vip_elite`, `openai.vip.vip1`.
2. Grant the permission to a user or to a group:
   - **oxide.grant user** \<Steam64ID\> **openai.vip.vip_elite**
   - Or add **openai.vip.vip_elite** to a group and add the player to that group (**oxide.usergroup add** \<Steam64ID\> \<group\>).

Players with that permission then get that tier's model and limits when using the AI.

---

## 3. Using Oxide Groups (example)

1. Create a group, e.g. `vip_ai`:
   ```text
   oxide.group add vip_ai
   ```
2. Grant the tier permission to the group:
   ```text
   oxide.grant group vip_ai openai.vip.vip_elite
   ```
3. Add players to the group:
   ```text
   oxide.usergroup add 76561198012345678 vip_ai
   ```

Anyone in `vip_ai` will use the `vip_elite` tier (assuming that tier is in your config).

---

## 4. Checking That VIP Is Applied

- **Admins:** Run **openai.status** (server console or F1) for full plugin status.
- **VIP players:** If a player has a VIP tier, they can run **openai.status** in F1; they will see their **effective** model, daily limit, and cooldown for their tier (e.g. "Effective model: gpt-4 (VIP tier: vip_elite)").

---

## 5. Tips

- **One tier per player:** If another plugin (e.g. payment/verification) grants a single permission like `openai.vip.vip1`, you don't need **VIP Tier Order**. Only use it when a player can have multiple VIP permissions and you want to choose which tier applies (e.g. premium over basic).
- **No Steam ID lists:** VIP is entirely permission-based. Use Oxide groups and the **openai.vip** command instead of maintaining Steam ID lists in config.
- **openai.unlimited:** Players with **openai.unlimited** still bypass all rate limits; VIP does not change that. VIP only overrides model and per-player limits for players who do not have unlimited.
- **New tiers:** Add a new key under **VIP Tiers** in config, save, and reload the plugin. The new permission `openai.vip.<new_key>` is registered automatically. Use **openai.vip** (no args) to see valid tier names.

---

## Quick reference

| Task | Command or action |
|------|-------------------|
| Grant tier | `openai.vip <Steam64ID> <tier_name>` |
| Revoke tier | `openai.vip remove <Steam64ID> <tier_name>` |
| List user's tiers | `openai.vip <Steam64ID>` |
| Permission format | `openai.vip.<tier_name>` (e.g. `openai.vip.vip_elite`) |
| Required admin perm | **openai.admin** (for **openai.vip** command) |

Config keys: **VIP Tier Order** (optional list), **VIP Tiers** (object keyed by tier name).
