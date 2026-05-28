# ── AquaOS — Top-Level Makefile ───────────────────────────────────
# All services in one repo. Run from the root: make up

.PHONY: up down build logs ps clean test

# ── Docker Compose ────────────────────────────────────────────────
# Uses aqua-os-calendar/docker-compose.yml (unified compose file)

up:
	docker compose -f aqua-os-calendar/docker-compose.yml up -d

up-dev:
	docker compose -f aqua-os-calendar/docker-compose.yml --profile dev up -d

down:
	docker compose -f aqua-os-calendar/docker-compose.yml down

logs:
	docker compose -f aqua-os-calendar/docker-compose.yml logs -f

ps:
	docker compose -f aqua-os-calendar/docker-compose.yml ps

build:
	docker compose -f aqua-os-calendar/docker-compose.yml build

restart: down up

# ── Individual Services ───────────────────────────────────────────

# Go backend
backend-run:
	cd aqua-os-backend && go run ./cmd/server

backend-build:
	cd aqua-os-backend && go build -o bin/aquaos-backend ./cmd/server

backend-test:
	cd aqua-os-backend && go test ./...

backend-fmt:
	cd aqua-os-backend && go fmt ./...

# Calendar service (C#)
calendar-build:
	cd aqua-os-calendar && dotnet build AquaOs.Calendar.sln

calendar-test:
	cd aqua-os-calendar && dotnet test AquaOs.Calendar.sln

calendar-run:
	cd aqua-os-calendar/PublishGameCalendar && dotnet run

# CrewAI microservice
crew-run:
	cd aqua-os-crew && uvicorn main:app --port 8001

crew-install:
	cd aqua-os-crew && pip install -r requirements.txt

# Web frontend
web-dev:
	cd aqua-os-web && npm run dev

web-build:
	cd aqua-os-web && npm run build

web-install:
	cd aqua-os-web && npm install

web-lint:
	cd aqua-os-web && npm run lint

# ── All Tests ─────────────────────────────────────────────────────
test: backend-test calendar-test

# ── Clean ─────────────────────────────────────────────────────────
clean:
	cd aqua-os-backend && rm -rf bin/ aquaos.db
	cd aqua-os-calendar && dotnet clean AquaOs.Calendar.sln
	cd aqua-os-web && rm -rf dist/ node_modules/.vite

# ── Help ───────────────────────────────────────────────────────────
help:
	@echo "AquaOS — Water Polo Club Manager"
	@echo ""
	@echo "Docker:"
	@echo "  make up          Start all services"
	@echo "  make up-dev      Start all services + DynamoDB Local"
	@echo "  make down        Stop all services"
	@echo "  make logs        Tail all logs"
	@echo "  make build       Build all Docker images"
	@echo ""
	@echo "Development (native):"
	@echo "  make backend-run   Go backend on :8080"
	@echo "  make crew-run      CrewAI on :8001"
	@echo "  make web-dev       React dev server on :5173"
	@echo "  make calendar-run  C# calendar on :5233"
	@echo ""
	@echo "Testing:"
	@echo "  make test        Run all tests"
	@echo ""
	@echo "Clean:"
	@echo "  make clean       Remove build artifacts"