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

So for the **Plugins** category, every `POST .../history` request (every new version upload) **must** include a `screenshots` object. The API does **not** support "upload new version and keep existing screenshots." If you omit screenshots, the server returns NO_SS (and HTTP 401 in our case).

So we are doing a **replacement** when we call history: we send the new file + description + changelog, and for this category we must also send screenshots (which replace the current set). There is no "edit in place" for the downloadable file that leaves screenshots unchanged.

## Options from here

1. **Two-step workflow (no screenshots in repo)**  
   - **Edit** – update description (and title/tags if needed) from `codefling/description.html`.  
   - New .cs releases – do **manually** on Codefling (upload new version + re-add screenshots there).  
   - Changelog is only on the listing via Edit if we can put it in the description; the Edit endpoint has no dedicated changelog field.

2. **Single-step workflow (screenshots in repo)**  
   - Store the same screenshots you want on the listing in the repo (e.g. `codefling/screenshots/`).  
   - On each push that updates the plugin, call **History** with: new .cs file + description + changelog + those screenshots.  
   - Each upload **replaces** the listing’s screenshots with the ones from the repo (same images = same result, but sent every time to satisfy NO_SS).

3. **Ask Codefling**  
   - Confirm whether the Plugins category can be configured to not require screenshots on history, or if there is another way to publish a new version without re-sending screenshots.

Until we choose one of these (or get different guidance from Codefling), the workflow will keep hitting **NO_SS** when it calls `POST .../history` without a `screenshots` parameter.
