FROM golang:{{GO_VERSION}}-alpine AS builder
WORKDIR /app
COPY go.mod go.sum* ./
RUN go mod download
COPY cmd/ cmd/
COPY internal/ internal/
RUN CGO_ENABLED=0 GOOS=linux GOARCH=arm64 go build -ldflags="-s -w" -o /{{SERVICE_NAME}} ./cmd/server

FROM gcr.io/distroless/static:nonroot
COPY --from=builder /{{SERVICE_NAME}} /{{SERVICE_NAME}}
USER 65534:65534
EXPOSE {{PORT}}
ENTRYPOINT ["/{{SERVICE_NAME}}"]
