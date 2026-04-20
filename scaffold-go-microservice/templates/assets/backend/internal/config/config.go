package config

import (
	"os"
	"strconv"
)

type Config struct {
	Port string
}

func Load() *Config {
	return &Config{
		Port: GetEnvStr("PORT", "{{PORT}}"),
	}
}

func GetEnvStr(key, fallback string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return fallback
}

func GetEnvInt(key string, fallback int) int {
	if v := os.Getenv(key); v != "" {
		if i, err := strconv.Atoi(v); err == nil {
			return i
		}
	}
	return fallback
}
