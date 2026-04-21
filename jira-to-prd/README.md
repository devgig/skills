# jira-to-prd — setup

This skill reads Jira via [`acli`](https://developer.atlassian.com/cloud/acli/),
Atlassian's official CLI, using an API token. **No MCP.** See the end
of this doc for the reasoning — it matters for how you think about
extending the skill.

---

## One-time setup

### 1. Install `acli`

**macOS (Homebrew):**

```bash
brew install --cask acli
```

**Other platforms:** https://developer.atlassian.com/cloud/acli/

### 2. Authenticate

```bash
acli jira auth login --web
```

This stores the API token in your OS keychain. No token in env vars,
no token in repo configs, no token in shell history. If you also need
Confluence:

```bash
acli confluence auth login --web
```

### 3. Verify

```bash
acli jira workitem view BIO-1 --json | head
```

Replace `BIO-1` with any issue key you can read. JSON output with no
auth prompt means you are done.

---

## Commands the skill runs

You don't run these yourself — the skill does — but here they are so
you can reproduce exactly what it sees:

```bash
# The Feature
acli jira workitem view "$KEY" --json

# All direct children (stories + subtasks)
acli jira workitem search --jql "parent = $KEY" --json

# Remote links (Confluence pages etc.) on any issue
acli jira workitem view "$KEY" --fields "remotelinks" --json

# A linked Confluence page
acli confluence page view --id "$PAGE_ID" --output json
```

That's the entire data path. Byte-identical JSON in, a markdown PRD
out.

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| `acli: command not found` | Install per step 1, then re-source your shell. |
| `acli jira workitem view <key>` prompts for auth | Your token expired or was never stored. Re-run `acli jira auth login --web`. |
| Skill reports "acli probe failed" | Run `acli jira auth status` yourself to see the underlying error. |
| PRD has no Confluence content even though pages are linked | Either `acli confluence auth login` was never run, or the linked pages are in a space your token cannot read. Confirm with `acli confluence page view --id <id> --output json`. |
| API token needs narrower scope | Re-mint the token at https://id.atlassian.com/manage-profile/security/api-tokens scoped read-only to the project(s) you care about. |

---

## Why CLI, not MCP

This is a deliberate choice, worth capturing because it governs how
you should extend the skill (and how you should design sibling skills
for GitHub, Linear, etc.):

1. **Determinism beats agentic flexibility once the workflow is
   defined.** MCP's value is Claude discovering tools and composing
   them dynamically. But "fetch a Feature + children + linked
   Confluence, render against template" is a *known* workflow. You
   don't want the LLM choosing between `searchJiraIssues` and
   `getJiraIssue` differently each run. `acli ... --json` produces
   byte-identical input every time; the only variability left is
   Claude's rendering of the PRD. That's what a repeatable pipeline
   feeding `/speckit.specify` needs.

2. **Auth that doesn't expire mid-session.** The official Atlassian
   MCP's OAuth/SSE path has the well-documented re-auth problem, and
   even the newer `/mcp` endpoint with API tokens has been flaky.
   `acli` with an API token has been stable for years and has no
   session concept to expire.

3. **Headless and portable.** A CLI runs in GitHub Actions on GKE,
   cron jobs on K3s, and LiteLLM-fronted agents equally well. An MCP
   server tied to an interactive OAuth flow does not, cleanly. Same
   reasoning as choosing Workload Identity Federation + OIDC over SA
   keys — the credential model should work in every execution context,
   not just the interactive-terminal happy path.

4. **Regulated-environment audit story.** When someone asks "what AI
   tools are touching our Jira and what's the data path," the answer
   "an API-token-scoped CLI on the engineer's machine, output captured
   to a local markdown file" is a much cleaner story than "OAuth token
   federated into Atlassian's cloud-hosted MCP gateway which talks to
   a hosted model service." Fewer hops, fewer trust boundaries, token
   scope is trivial to restrict to read-only on the relevant project.

5. **Skill shape fits the existing stack.** The skill/PRP pattern is
   already wired across the agent infrastructure. Adding a skill is a
   twenty-minute job; wiring MCP into every agent is not.

**The one case to flip back to MCP:** a team working primarily in
Claude.ai web (not Claude Code) that won't run CLIs locally. The
skill+CLI pattern does not exist in the web client, so Atlassian's
Rovo MCP on Claude Teams/Enterprise is the right call there. That is
not this setup.
