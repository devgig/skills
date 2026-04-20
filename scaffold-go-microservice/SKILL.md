---
name: scaffold-go-microservice
description: Scaffold a new Go microservice with a hexagonal (cmd/internal/adapter) layout, Kustomize manifests for Kubernetes deployment, and a GitHub Actions CI workflow. Use when the user asks to create a new Go backend service, bootstrap a Go microservice repo, or set up a Go service following the existing house style.
---

# Scaffold a Go Microservice

Generates the files for a new Go HTTP microservice that matches the house layout: a Go backend in `assets/backend/`, Kustomize manifests in `kustomize/backend/`, docs in `docs/`, and a GitHub Actions workflow in `.github/workflows/`.

The templates in `templates/` are the source of truth for file content. Copy each one into the target repo at the same relative path, substituting every `{{PLACEHOLDER}}` token.

## Inputs to collect

Before generating files, ask the user (batch the questions — do not ask one at a time):

| Placeholder | Meaning | Example |
|---|---|---|
| `{{SERVICE_NAME}}` | Kebab-case service name, used for workload names, image repo, container name | `billing-service` |
| `{{MODULE_PATH}}` | Go module path | `github.com/acme/billing-service` |
| `{{APP_LABEL}}` | Kubernetes `app:` label (often the service name without `-service`) | `billing` |
| `{{NAMESPACE}}` | Kubernetes namespace for the dev overlay | `billing-service` |
| `{{IMAGE_REGISTRY}}` | Container registry hostname | `ghcr.io/acme` or `bytecourier.azurecr.io` |
| `{{PORT}}` | HTTP port the service listens on | `8000` |
| `{{GO_VERSION}}` | Go toolchain version | `1.24` |
| `{{TARGET_DIR}}` | Absolute path where files should be written | `/Users/me/repos/billing-service` |

Derived tokens (compute — do not re-ask):

- `{{IMAGE_REPO}}` = `{{SERVICE_NAME}}-backend`
- `{{WORKLOAD_NAME}}` = `{{APP_LABEL}}-backend`

## Workflow

1. Collect inputs above in one batched question.
2. Verify `{{TARGET_DIR}}` exists and is (or is inside) a git repo. If it has existing files at the paths this skill writes, stop and ask the user before overwriting.
3. For every file under `templates/`, create the same relative path under `{{TARGET_DIR}}` and write the content with all placeholders substituted.
4. Run `go mod tidy` in `{{TARGET_DIR}}/assets/backend` if Go is available. If not, tell the user to run it.
5. Report what was created as a short tree.

## Substitution rules

- Substitute tokens literally — do not rename identifiers beyond placeholders.
- `go.mod.tmpl` → rename to `go.mod` after substitution.
- Placeholders appear in file contents only; directory names are already generic.
- Do not introduce additional dependencies, CLI flags, or abstractions beyond what the templates contain. If the user needs extras (Postgres, Redis, Kafka, JWT), they can add them afterward — this skill produces a minimal skeleton.

## What the skeleton includes

- **`cmd/server/main.go`** — slog JSON logger, config load, HTTP server with graceful shutdown on SIGINT/SIGTERM.
- **`internal/config/config.go`** — env-var loader with `GetEnvStr` / `GetEnvInt` helpers.
- **`internal/adapter/httpapi/server.go`** — `net/http` mux with `/health` and `/ready` endpoints. No external HTTP framework.
- **`Dockerfile`** — multi-stage: `golang:{{GO_VERSION}}-alpine` builder, `gcr.io/distroless/static:nonroot` runtime, ARM64 by default.
- **`kustomize/backend/base/`** — Deployment (probes, resource limits, Prometheus scrape annotations), Service (ClusterIP), image-patch, ACR/registry external secret, kustomization.
- **`kustomize/backend/overlays/dev/`** — namespace + overlay kustomization with `environment: dev` label.
- **`docs/README.md`** — dev setup, env vars, build/test/run.
- **`.github/workflows/backend.yml`** — vet + staticcheck + race-tested coverage + docker build & push + `kubectl kustomize` artifact publish. Triggered on changes to `assets/backend/**` or `kustomize/backend/**`.

## Out of scope

- Frontend scaffolding.
- Azure DevOps pipelines.
- Domain-specific adapters (auth, database, cache, message bus). Add these per-service after scaffolding.
