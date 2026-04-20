# {{SERVICE_NAME}}

Go microservice. HTTP server on port `{{PORT}}`, deployed to Kubernetes namespace `{{NAMESPACE}}`.

## Layout

```
assets/backend/        Go source
  cmd/server/          main package
  internal/adapter/    I/O adapters (httpapi, etc.)
  internal/config/     env-var config loader
kustomize/backend/     k8s manifests (base + overlays/dev)
.github/workflows/     CI (test, image build, manifest render)
```

## Local dev

```
cd assets/backend
go mod tidy
go test ./...
go run ./cmd/server
```

Hit `http://localhost:{{PORT}}/health` to verify.

## Environment variables

| Var | Default | Notes |
|---|---|---|
| `PORT` | `{{PORT}}` | HTTP listen port |

## Build

```
docker build -f assets/backend/Dockerfile -t {{IMAGE_REGISTRY}}/{{IMAGE_REPO}}:dev assets/backend
```

## Deploy (dev)

```
kubectl kustomize kustomize/backend/overlays/dev | kubectl apply -f -
```
