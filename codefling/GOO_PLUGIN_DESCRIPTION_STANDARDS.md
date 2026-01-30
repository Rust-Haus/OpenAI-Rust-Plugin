# Goo_ Plugin Description Standards

Standards for writing Codefling plugin listing descriptions for Goo_ plugins.

---

## Purpose and Scope

**Who it's for:** Goo_ plugins published on Codefling.

**Goal:** Consistent, readable descriptions with a reusable structure (intro, optional feature table, links, video, code examples).

---

## Codefling Context

- Codefling listing descriptions are **HTML only** (no Markdown).
- Descriptions are maintained in `codefling/description.html` (or equivalent per repo) and synced via the Edit API or updated manually on Codefling.
- Codefling uses IPS (Invision Power Suite), so certain IPS-specific classes and components are supported.

---

## Structure Standards

A typical Goo_ plugin description should follow this order:

1. **Intro paragraph** – Brief summary of what the plugin does (1–3 sentences).
2. **"Full Documentation" link** – Link to the README on GitHub (or other docs).
3. **Feature table** (optional) – Multi-column summary of features, layout, visual, etc.
4. **Default config / code example** (optional) – Use `ipsCode` for syntax-highlighted JSON/code.
5. **Embedded video** (optional) – Demo or tutorial video using `ipsEmbeddedVideo`.
6. **Images / GIFs** (optional) – Screenshots or animated demos.

---

## Allowed HTML Elements

### Basic HTML

| Element | Notes |
|---------|-------|
| `<p>` | Paragraphs; supports `style="text-align:center;"` etc. |
| `<span>` | Inline text; supports `style="font-size:...;"`, `style="color:...;"` |
| `<br>` | Line breaks |
| `<a href="..." rel="external nofollow">` | External links |
| `<em>`, `<strong>` | Emphasis and bold |
| `<img src="..." width="..." alt="...">` | Images (external URLs allowed) |
| `<table>`, `<tr>`, `<td>`, `<tbody>` | Tables with inline styles |

### IPS-Specific Components

| Component | Class / Structure |
|-----------|-------------------|
| **Spoiler** | `<div class="ipsSpoiler" data-ipsspoiler="">` with `ipsSpoiler_header` and `ipsSpoiler_contents` |
| **Quote** | `<blockquote class="ipsQuote" data-ipsquote="">` with `ipsQuote_citation` and `ipsQuote_contents` |
| **Code block** | `<pre class="ipsCode prettyprint lang-javascript prettyprinted">` with syntax spans |
| **Embedded video** | `<div class="ipsEmbeddedVideo" contenteditable="false">` with `<iframe>` |
| **Emoji** | `<span class="ipsEmoji">📁</span>` |

### Inline Styles That Work

- `font-size` (e.g. `font-size:24px;`)
- `color` (e.g. `color:#3366ff;`)
- `text-align` (e.g. `text-align:center;`)
- Table styles: `border-spacing`, `vertical-align`, `padding`, `width`, `max-width`, `height`

---

## Feature Table Pattern

Use this pattern for a multi-column feature summary. Adapt column headers and content per plugin.

**When to use:** Feature overviews, comparisons, or any short/scannable multi-category info.

### Example HTML

```html
<table style="border-spacing:50px;height:200px;width:900px;" width="856">
	<tbody>
		<tr>
			<td style="vertical-align:top;padding-right:50px;width:80px;">
				<p>
					<span style="font-size:16px;"><span style="color:#3366ff;"><strong>Features</strong></span></span>
				</p>
				<p>
					Feature 1<br>
					Feature 2<br>
					Feature 3
				</p>
			</td>
			<td style="max-width:250px;vertical-align:top;padding-right:50px;width:80px;">
				<p>
					<span style="font-size:16px;"><span style="color:#3366ff;"><strong>Layout</strong></span></span>
				</p>
				<p>
					Layout item 1<br>
					Layout item 2<br>
					Layout item 3
				</p>
			</td>
			<td style="max-width:250px;vertical-align:top;width:80px;">
				<p>
					<span style="font-size:16px;"><span style="color:#3366ff;"><strong>Visual</strong></span></span>
				</p>
				<p>
					Visual item 1<br>
					Visual item 2<br>
					Visual item 3
				</p>
			</td>
			<td style="max-width:250px;vertical-align:top;width:80px;">
				<p>
					<span style="font-size:16px;"><span style="color:#3366ff;"><strong>Other</strong></span></span>
				</p>
				<p>
					Other item 1<br>
					Other item 2<br>
					Other item 3
				</p>
			</td>
		</tr>
	</tbody>
</table>
```

### Notes

- Section headers use `<span style="font-size:16px;"><span style="color:#3366ff;"><strong>Section Name</strong></span></span>`.
- Body items are short lines separated by `<br>` inside `<p>`.
- Adjust `width`, `height`, and column count as needed.

---

## Other Element Patterns

### External Link

```html
<p>
	<span style="font-size:24px;"><a href="https://github.com/..." rel="external nofollow">Full Documentation Here</a></span>
</p>
```

### Embedded Video (YouTube)

```html
<div class="ipsEmbeddedVideo" contenteditable="false">
	<div>
		<iframe allowfullscreen="" frameborder="0" height="315" width="560" src="https://www.youtube-nocookie.com/embed/VIDEO_ID?feature=oembed"></iframe>
	</div>
</div>
```

### Code Block (JSON example)

```html
<pre class="ipsCode prettyprint lang-javascript prettyprinted">{
  "Config Version": "1.0.0",
  "Setting": true
}</pre>
```

For full syntax highlighting, Codefling's editor adds `<span>` classes (`pun`, `pln`, `str`, `kwd`, `lit`) automatically when you paste code. If editing raw HTML, you can omit those spans and just use the `<pre class="ipsCode ...">` wrapper.

### Spoiler

```html
<div class="ipsSpoiler" data-ipsspoiler="">
	<div class="ipsSpoiler_header">
		<span>Spoiler</span>
	</div>
	<div class="ipsSpoiler_contents">
		<p>Hidden content here.</p>
	</div>
</div>
```

### Quote

```html
<blockquote class="ipsQuote" data-ipsquote="">
	<div class="ipsQuote_citation">Quote</div>
	<div class="ipsQuote_contents">
		<p>Quoted text here.</p>
	</div>
</blockquote>
```

### Centered Heading with Emoji

```html
<p style="text-align:center;">
	<span style="font-size:24px;"><em><strong><span class="ipsEmoji">📁</span> Section Title</strong></em></span>
</p>
```

### Image / GIF

```html
<p>
	<img alt="Description" title="Description" width="200" src="https://example.com/image.gif">
</p>
```

---

## Do / Don't

| Do | Don't |
|----|-------|
| Use HTML; Codefling does not render Markdown. | Don't use Markdown syntax (**, ##, etc.). |
| Keep tables compact and scannable. | Don't put long prose in tables. |
| Use `rel="external nofollow"` on external links. | Don't omit `rel` on external links. |
| Test changes on Codefling after editing. | Don't assume raw HTML will render identically everywhere. |
| Use IPS classes (`ipsCode`, `ipsSpoiler`, etc.) for rich components. | Don't invent custom classes; Codefling may strip them. |

---

## Reuse

This standard can be copied or linked to from other Goo_ plugin repos. Keep a local `codefling/description.html` per repo and follow this structure for consistency across all listings.
