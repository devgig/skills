package main

import (
	"context"
	"errors"
	"log/slog"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"{{MODULE_PATH}}/internal/adapter/httpapi"
	"{{MODULE_PATH}}/internal/config"
)

func main() {
	slog.SetDefault(slog.New(slog.NewJSONHandler(os.Stdout, nil)))

	cfg := config.Load()

	srv := &http.Server{
		Addr:              ":" + cfg.Port,
		Handler:           httpapi.NewRouter(),
		ReadHeaderTimeout: 5 * time.Second,
	}

	go func() {
		slog.Info("server listening", "port", cfg.Port)
		if err := srv.ListenAndServe(); err != nil && !errors.Is(err, http.ErrServerClosed) {
			slog.Error("server failed", "error", err)
			os.Exit(1)
		}
	}()

	stop := make(chan os.Signal, 1)
	signal.Notify(stop, syscall.SIGINT, syscall.SIGTERM)
	<-stop

	slog.Info("shutting down")
	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()
	if err := srv.Shutdown(ctx); err != nil {
		slog.Error("shutdown failed", "error", err)
		os.Exit(1)
	}
}
