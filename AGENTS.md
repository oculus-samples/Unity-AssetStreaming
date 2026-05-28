# Agent Instructions — Asset Streaming

Unity reference project showing how to stream large open-world content on Quest using Unity's Addressables system, with LOD strategies for memory-bounded mobile rendering.

## Source-of-truth files (read these first, do not duplicate their contents in this file)

For setup, build steps, SDK versions, and project layout, read:

- `README.md` — official setup, dependencies, and the custom `AssetStreaming` editor-menu build flow
- `ProjectSettings/ProjectVersion.txt` — Unity editor version
- `Packages/manifest.json` — Unity package versions (Meta XR, Addressables, OpenXR, etc.)
- `Assets/AssetStreaming/Scenes/Startup.unity` — entry-point scene
- `Assets/AddressableAssetsData/` — Addressables groups and profile config
- `ConversionToAddressables.md` — companion guide for porting an existing project to Addressables
- `CHANGELOG.md` — project change history
- `.gitattributes` — Git LFS configuration (LFS is required)
- `LICENSE` — MIT for most of the project; `Assets/TextMesh Pro` is under Unity's Companion License and `Assets/DemoAssets/` is under the Meta Platforms Technologies Examples License (`Assets/DemoAssets/LICENSE.txt`)

## Quest / Horizon-specific notes

- **Always build the Addressables before the APK.** The runtime expects the Addressables catalog to exist on disk, and the `AssetStreaming > Build APK` menu item intentionally skips that step (it's for code-only iterations).
- **Mesh Baker** is recommended in the README for LOD generation but is *not* included due to licensing — do not assume it is in the project.
- `Assets/DemoAssets/` has its own separate license; preserve that distinction when extracting or redistributing assets.
- The project pulls `com.meta.tutorial.framework` from a Git URL in `manifest.json` — Unity needs network access on first import; do not delete the dependency if it fails to resolve, fix the network instead.

## Meta Quest tooling

This repository is part of the Meta Quest / Horizon OS ecosystem (a sample, library, template, or related project — the bespoke intro above describes which). Use that intro and the source-of-truth files it references for project-specific decisions; don't restate or invent facts from memory.

When the user asks anything about Quest device behavior, build / deploy / debug / capture flows, on-device performance, or Horizon OS APIs, reach for these tools instead of generic Unity answers:

- **`hzdb`** — Quest-aware ADB wrapper (device list, install / launch / stop, logs, screenshots, Perfetto traces, on-device docs search). Already wired up as an MCP server via `.mcp.json`, `.vscode/mcp.json`, and `.cursor/mcp.json`. Also runnable directly: `npx -y @meta-quest/hzdb <subcommand>`.
- **Meta Quest Agentic Tools** — the full skill set, including Unity-specific skills: [github.com/meta-quest/agentic-tools](https://github.com/meta-quest/agentic-tools). Install per your client (Claude Code: `/plugin install meta-vr@meta-quest`; Gemini CLI: `gemini extensions install https://github.com/meta-quest/agentic-tools`; Cursor / VS Code: install the **Meta Horizon** extension from the Marketplace).

A few behavior expectations:

- **Read this repo's files first.** Before answering anything project-specific, read `README.md` and whichever source-of-truth files the intro above points at. Don't restate their contents in chat — quote or link instead.
- **Use `hzdb` for device-side work.** Anything that touches an attached Quest (install, launch, logs, screenshot, capture, manifest inspection) goes through `hzdb`, not raw `adb`.
- **Check live Horizon OS docs before answering API questions.** `hzdb docs search "..."` queries the live docs; training data on Horizon OS APIs goes stale fast.
- **Don't fabricate SDK / engine versions.** If a version isn't visible in this repo's files, say so rather than guessing.
