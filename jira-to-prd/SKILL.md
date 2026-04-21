---
name: jira-to-prd
description: Convert a Jira Feature (and its child stories, subtasks, and linked Confluence pages) into a `PRD.md` at the repo root, rendered against `templates/prd-template.md`. Uses `acli` (Atlassian's official CLI) with an API token — no MCP. Use when the user asks to "turn BIO-123 into a PRD", "fetch a Jira issue and write a PRD", "generate a PRD from Jira", or similar. The PRD is the upstream input to Spec Kit: `PRD.md` → `/speckit.specify` → `spec.md` → `/speckit.plan`. Preserves the Jira issue key as a front-matter traceability anchor so downstream steps (spec, plan, tasks, branch, commit, PR) can thread it through — important in regulated / clinical-trial audit chains.
---

# Jira Feature → PRD.md

You read a Jira Feature (plus its hierarchy and linked Confluence
pages) via `acli` and produce a `PRD.md` at the repo root that is
structurally stable enough for Spec Kit to consume downstream.

The PRD is **not** the spec. It is the human-readable product brief
that feeds `/speckit.specify`. Keep it tight, preserve every Jira key
as a traceability anchor, and do not invent requirements the tickets
do not state.

**Data path: `acli` only.** No MCP, no web scraping, no REST calls.
The CLI produces byte-identical JSON every run, which is what makes
this pipeline repeatable. If `acli` is not installed or authenticated,
stop and point the user at `./README.md` — do not invent a substitute.

---

## Inputs to collect

Ask in **one batched question**:

| Input | Example |
|---|---|
| `{{ISSUE_KEY}}` — the Jira Feature key | `BIO-456` |
| `{{TARGET_DIR}}` — repo root where `PRD.md` lands | `/Users/me/repos/trial-intake` |
| Output path (default `{{TARGET_DIR}}/PRD.md`) | `PRD.md` |

Do not ask for credentials. `acli` stores them in the OS keychain after
`acli jira auth login --web`.

---

## Workflow

### Phase 0 — Verify `acli` is ready

Run `acli jira auth status` (or `acli jira workitem view {{ISSUE_KEY}} --json`
as a probe). If either fails:

- Exit code non-zero → stop, print the failing command and its stderr,
  point at `./README.md`. Do not attempt a workaround.
- `command not found: acli` → stop, tell the user to install per
  `./README.md`.

Never substitute a REST call, an MCP tool, or a guess for `acli`.

### Phase 1 — Fetch the hierarchy

Run these commands and capture stdout verbatim. Parse as JSON.

```bash
# The Feature itself
acli jira workitem view "{{ISSUE_KEY}}" --json

# All children (stories + subtasks under this Feature)
acli jira workitem search --jql "parent = {{ISSUE_KEY}}" --json

# Remote links on the Feature (Confluence pages, external URLs)
acli jira workitem view "{{ISSUE_KEY}}" --fields "remotelinks" --json
```

Then for each child returned above, fetch the child's full body and
its own remote links the same way:

```bash
acli jira workitem view "{{CHILD_KEY}}" --json
acli jira workitem view "{{CHILD_KEY}}" --fields "remotelinks" --json
```

For each Confluence page found in remote links:

```bash
acli confluence page view --id "{{PAGE_ID}}" --output json
```

Out-of-hop rule: for "is blocked by" / "relates to" links, capture key
+ summary only. Do not expand further.

The Feature alone is usually a one-liner. The acceptance criteria and
design detail live in the children and linked docs. Skipping them
produces a thin PRD that yields a spec full of `[NEEDS CLARIFICATION]`
markers downstream.

### Phase 2 — Confirm the hierarchy back to the user

Before rendering, print:

```
Feature: {{ISSUE_KEY}} — <summary>
  Children (N):
    - BIO-457 — <summary>
    - BIO-458 — <summary>
    ...
  Linked Confluence pages (M):
    - <title> (<url>)
  Linked issues (outbound, not expanded):
    - BIO-412 (blocks) — <summary>
```

Wait for the user to confirm or redirect. They may flag a missing
child or an irrelevant linked page before you spend tokens drafting.

### Phase 3 — Render `PRD.md` against the template

1. Read `./templates/prd-template.md`.
2. Fill every section from the fetched JSON. Rules:
   - **Preserve Jira keys** as inline traceability anchors: `(BIO-457)`
     after the user story the child supplies, for example. Auditors
     need this chain.
   - Front-matter `jira:` = `{{ISSUE_KEY}}` (the parent Feature).
   - Front-matter `children:` = list of child keys.
   - **Do not invent** requirements. If the tickets are silent on a
     section (e.g. non-functional requirements), write `_None stated —
     capture in `/speckit.clarify`._` rather than guessing.
   - Preserve the section order and headings of the template exactly
     so downstream Spec Kit output stays consistent run-to-run.
3. Write to `{{TARGET_DIR}}/PRD.md`. If the file exists, stop and ask
   before overwriting — the user may have hand-edited it.

### Phase 4 — Report

Print:

- Path to the written `PRD.md`.
- Counts: children fetched, Confluence pages fetched, Jira keys
  preserved.
- Sections where tickets were silent (so the user knows what
  `/speckit.clarify` will have to chase).
- The suggested next command: `/speckit.specify @PRD.md`.

---

## What this skill does NOT do

- **Use MCP.** This is deliberate — a CLI-only data path is
  deterministic, doesn't expire mid-session, runs headless in CI/cron,
  and gives a cleaner audit story in regulated contexts. See
  `./README.md` for the full reasoning.
- **Modify Jira.** Read-only. Never transition, comment, or edit issues.
- **Invent requirements** the tickets do not state. Silence goes into
  "Open questions" — that is what `/speckit.clarify` is for.
- **Embed credentials.** `acli` stores its API token in the OS keychain.
- **Reformat the PRD template.** Section headings and order are the
  contract — changing them breaks downstream Spec Kit output stability.
- **Run Spec Kit.** Stop after `PRD.md`. The user drives
  `/speckit.specify` themselves.

---

## Style rules

- `PRD.md` front-matter is YAML with at minimum `jira:`, `children:`,
  `status:`, `updated:`.
- Each user story carries its source key: `As a <role>, I want <goal>,
  so that <benefit>. (BIO-457)`.
- Each acceptance criterion carries the key of the story/subtask it
  came from.
- Confluence quotes are blockquoted and cite the page title + URL.
- No marketing prose. Short sentences. Bullets beat paragraphs.

---

## Reference

- `./README.md` — one-time `acli` setup + the reasoning for CLI-over-MCP.
- `./templates/prd-template.md` — the stable section skeleton every PRD
  follows. Edit here if your org needs different sections; do not
  improvise per run.
- Spec Kit — the downstream toolchain this PRD feeds:
  `/speckit.specify` → `/speckit.clarify` → `/speckit.plan` →
  `/speckit.tasks` → `/speckit.implement`.
