# jira-to-prd — setup

This skill needs a way to read Jira issues and Confluence pages. You
have two options. Pick **one**; the skill probes for both at runtime
and uses whichever answers first.

1. **Atlassian Rovo MCP server** (nicer integration, known re-auth pain).
2. **`acli` CLI fallback** (less elegant, rock-solid, doesn't expire
   mid-session).

The `acli` route is what most Claude Code users settle on after hitting
the MCP re-auth issue a few times. If your sessions are short or you
are evaluating the skill for the first time, start with the MCP; switch
to `acli` the first time you see a mid-run 401.

---

## Option 1 — Rovo MCP server (API-token auth)

Atlassian's OAuth SSE endpoint (`/sse`) has a documented problem where
the token expires every few hours and you have to re-auth mid-session.
The HTTP endpoint with API-token auth does not have that issue. Use it.

**Note:** The SSE endpoint is scheduled to be deprecated after June 30,
2026. Use the HTTP transport below regardless.

### One-time setup

1. Create an Atlassian API token: https://id.atlassian.com/manage-profile/security/api-tokens
2. Export it in your shell profile (`~/.zshrc` / `~/.bashrc`):

   ```bash
   export ATLASSIAN_EMAIL="you@example.com"
   export ATLASSIAN_API_TOKEN="<token from step 1>"
   ```

3. Register the MCP server with Claude Code:

   ```bash
   claude mcp add --transport http atlassian https://mcp.atlassian.com/v1/mcp \
     --header "Authorization: Bearer $(echo -n "$ATLASSIAN_EMAIL:$ATLASSIAN_API_TOKEN" | base64)"
   ```

4. Verify in a fresh Claude Code session:

   ```
   /mcp
   ```

   You should see `atlassian` listed as connected.

### Known reliability caveat

Even with API-token auth, some users report the MCP going silent after
a few hours of idle time. If you see that, fall back to Option 2 — you
do not need to remove the MCP config; the skill will prefer whichever
source answers.

---

## Option 2 — `acli` fallback

`acli` is Atlassian's official CLI. Wrapped by this skill, a call like
`acli jira workitem view BIO-456` is a stable, non-expiring read path
that works regardless of MCP state.

### Install

**macOS (Homebrew):**

```bash
brew install --cask atlassian-acli
```

**Other platforms:** see https://developer.atlassian.com/cloud/acli/

### Authenticate

```bash
acli jira auth login
```

Follow the prompts. Credentials are stored in your OS keychain, not in
this repo.

### Verify

```bash
acli jira workitem view BIO-1   # replace with any real issue key you can read
```

If this prints the issue without prompting, you are done.

### Confluence

`acli` supports Confluence too:

```bash
acli confluence auth login
acli confluence page view --id <page-id>
```

The skill uses `acli confluence` for linked pages when the MCP is not
the active source.

---

## Picking a source

You do not need to tell the skill which source to use. At Phase 0 it:

1. Probes the `atlassian` MCP server with a no-op call.
2. Falls back to `acli` if the MCP is missing or errors.
3. Stops with a clear message if neither is available.

Whatever it picks, it reports at the top of the run so you know which
path produced the PRD.

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| `/mcp` shows `atlassian` as disconnected after a few hours | Expected — either restart Claude Code to re-auth, or rely on the `acli` fallback. |
| `acli: command not found` | Install per Option 2, or re-source your shell. |
| Skill reports "no Jira data source available" | Neither the MCP nor `acli` answered. Run `/mcp` and `acli jira workitem view <any-key>` manually to find which is broken. |
| PRD has no Confluence content even though pages are linked | The linked pages may be in a space your token cannot read. Confirm with `acli confluence page view --id <id>`. |
| Re-auth loop with the SSE endpoint | Do not use `/sse`. Use the HTTP endpoint shown in Option 1. |

---

## Why both?

The MCP gives the model structured tool calls and is more token-efficient
for large hierarchies. `acli` is boring and reliable. Having both
configured means a mid-run MCP outage degrades gracefully instead of
halting the skill.
