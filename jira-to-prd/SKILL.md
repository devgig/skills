---
name: jira-to-prd
description: Convert a Jira Feature (and its child stories, subtasks, and linked Confluence pages) into a `PRD.md` at the repo root, rendered against `templates/prd-template.md`. Use when the user asks to "turn BIO-123 into a PRD", "fetch a Jira issue and write a PRD", "generate a PRD from Jira", or similar. The PRD is the upstream input to Spec Kit: `PRD.md` → `/speckit.specify` → `spec.md` → `/speckit.plan`. Preserves the Jira issue key as a front-matter traceability anchor so downstream steps (spec, plan, tasks, branch, commit, PR) can thread it through — critical for regulated / clinical-trial audit chains.
---

# Jira Feature → PRD.md

You read a Jira Feature (plus its hierarchy and linked Confluence pages)
and produce a `PRD.md` at the repo root that is structurally stable
enough for Spec Kit to consume downstream.

The PRD is **not** the spec. It is the human-readable product brief that
feeds `/speckit.specify`. Keep it tight, preserve every Jira key as a
traceability anchor, and do not invent requirements the tickets do not
state.

---

## Inputs to collect

Ask in **one batched question**:

| Input | Example |
|---|---|
| `{{ISSUE_KEY}}` — the Jira Feature key | `BIO-456` |
| `{{TARGET_DIR}}` — repo root where `PRD.md` lands | `/Users/me/repos/trial-intake` |
| Output path (default `{{TARGET_DIR}}/PRD.md`) | `PRD.md` |

Do not ask for credentials. Auth is configured out-of-band (see
`./README.md`).

---

## Workflow

### Phase 0 — Verify a Jira data source is available

Check in order and use the first that works:

1. **Rovo MCP server** — the `atlassian` MCP server configured per
   `./README.md`. Probe with a no-op tool call (e.g. a `getAccessibleResources`
   or equivalent). If it responds, use it for all fetches below.
2. **`acli` fallback** — `acli jira workitem view {{ISSUE_KEY}}` returns 0.
   If the MCP is unavailable or mid-session re-auth has expired, use
   `acli` shelled out via Bash. This is the durable path.
3. Neither available → stop. Tell the user to follow `./README.md` setup,
   do **not** fabricate ticket content.

Report which source is active.

### Phase 1 — Fetch the hierarchy

Fetch **all** of the following, not just the Feature:

1. The Feature itself — summary, description, status, labels, fix
   versions, reporter, assignee, acceptance criteria field (if set).
2. Every child story and subtask — summary, description, acceptance
   criteria, linked issues.
3. Every Confluence page linked from the Feature description, comments,
   or remote links — full body.
4. Any "is blocked by" / "relates to" issue links one hop out, by key +
   summary only (do not expand further).

The Feature alone is usually a one-liner. The acceptance criteria and
design detail live in the children and linked docs. Skipping them
produces a thin PRD that yields a spec full of `[NEEDS CLARIFICATION]`
markers downstream.

### Phase 2 — Confirm the hierarchy back to the user

Before writing anything, print:

```
Feature: {{ISSUE_KEY}} — <summary>
  Children (N):
    - BIO-457 — <summary>
    - BIO-458 — <summary>
    ...
  Linked Confluence pages (M):
    - <title> (<url>)
  Linked issues (outbound):
    - BIO-412 (blocks) — <summary>
```

Wait for the user to confirm or redirect. They may point out a missing
child or an irrelevant linked page before you spend tokens drafting.

### Phase 3 — Render `PRD.md` against the template

1. Read `./templates/prd-template.md`.
2. Fill every section from the fetched content. Rules:
   - **Preserve Jira keys** as traceability anchors inline in each
     section: `(BIO-457)` after the user story the child supplies, for
     example. Auditors need this chain.
   - Front-matter `jira:` field = `{{ISSUE_KEY}}` (the parent Feature).
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
- Counts: children fetched, Confluence pages fetched, Jira keys preserved.
- Sections where tickets were silent (so the user knows what
  `/speckit.clarify` will have to chase).
- The suggested next command: `/speckit.specify @PRD.md`.

---

## What this skill does NOT do

- **Modify Jira.** Read-only. Never transition, comment, or edit issues.
- **Invent requirements** the tickets do not state. Silence goes into
  "Open questions" — that is what `/speckit.clarify` is for.
- **Embed credentials.** Auth is configured in Claude Code settings or
  the shell environment per `./README.md`.
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

- `./README.md` — one-time setup: Rovo MCP server and `acli` fallback.
- `./templates/prd-template.md` — the stable section skeleton every PRD
  follows. Edit here if your org needs different sections; do not
  improvise per run.
- Spec Kit — the downstream toolchain this PRD feeds:
  `/speckit.specify` → `/speckit.clarify` → `/speckit.plan` →
  `/speckit.tasks` → `/speckit.implement`.
