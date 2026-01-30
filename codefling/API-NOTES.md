# Codefling API notes (for this repo)

Reference: https://codefling.com/developers

## Edit vs new version (history)

| Goal | Endpoint | What it does |
|------|----------|--------------|
| **Edit metadata only** | `POST /api/downloads/files/{id}` | Update **description**, title, tags, category, etc. Does **not** upload new file content. Does **not** touch screenshots. Does **not** support changelog (no changelog parameter on this endpoint). |
| **Upload new version** | `POST /api/downloads/files/{id}/history` | New version of the **downloadable file** + optional description, changelog, version. **Required:** `files` (object). **Optional:** description, changelog, version, title, `save`, **screenshots**. |

So:

- To **change only the listing description** (no new .cs file): use **Edit** (`POST /api/downloads/files/{id}`). No new version, no screenshots involved.
- To **publish a new .cs file** (new version): you **must** use **History** (`POST /api/downloads/files/{id}/history`). That endpoint is the only way to update the downloadable file.

## Screenshots and POST .../history

From the docs:

- **Parameter:** `screenshots` (object) – "Will **replace** all current screenshots."
- **Exception:** `1S303/N` **NO_SS** – "No screenshots are provided, but **screenshots are required for the category**."

For the **Plugins** category, every `POST .../history` request (every new version upload) **must** include a `screenshots` object. The API does **not** support "upload new version and keep existing screenshots." If you omit screenshots, the server returns NO_SS (and HTTP 401).

## Current approach

**Edit endpoint for description updates** – The workflow uses `POST /api/downloads/files/{id}` to sync `codefling/description.html`. This avoids the NO_SS error since the Edit endpoint doesn't require screenshots.

**Manual releases for new .cs versions** – New plugin file uploads must be done manually on Codefling (which requires screenshots). The History endpoint cannot be automated without including screenshots in the repo.

## Required secrets

| Secret | Purpose |
|--------|---------|
| `CODEFLING_FILE_ID` | The numeric ID of the Codefling listing |
| `CODEFLING_CREATOR_API_KEY` | Your Codefling Creator API key |

The category ID (`2` for mods) is hardcoded in the workflow.
