# 📅 aqua-os-calendar

Game calendar microservice — polls external sources for water polo events, generates .ics feeds, and notifies on changes.

## Architecture

ASP.NET Core service with EF Core + PostgreSQL for persistence. Polls websites for game schedules, detects changes, and publishes notifications via RabbitMQ.

```
PublishGameCalendar/         → main application
PublishGameCalendar.Tests/   → unit/integration tests
docker-compose.yml           → local dev stack (app + PostgreSQL + RabbitMQ)
```

## Prerequisites

- .NET 8 SDK
- PostgreSQL
- RabbitMQ

## Run Locally

**With Docker Compose (recommended):**

```bash
docker-compose up
```

**Without Docker:**

```bash
cd PublishGameCalendar
dotnet run
```

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__Default` | PostgreSQL connection string | see appsettings |
| `RabbitMQ__Host` | RabbitMQ hostname | `localhost` |
| `RabbitMQ__Port` | RabbitMQ port | `5672` |

## Tests

```bash
cd PublishGameCalendar.Tests
dotnet test
```

## Docker

```bash
docker-compose up -d
```

Starts the application with PostgreSQL and RabbitMQ.

## Related Repos

| Repo | Description |
|------|-------------|
| [aqua-os-backend](../aqua-os-backend) | Go REST API |
| [aqua-os-web](../aqua-os-web) | React frontend |
| [aqua-os-crew](../aqua-os-crew) | AI agents (CrewAI) |
| [aqua-os-infrastructure](../aqua-os-infrastructure) | Terraform AWS infra |

## License

GPL-3.0
