# Implementation Plan: Integracao WhatsApp via WPP Connect

**Branch**: `004-whatsapp-wpp-integration` | **Date**: 2026-04-12 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-whatsapp-wpp-integration/spec.md`

## Summary

Integrar o AvaBot com o WhatsApp via WPP Connect Server, permitindo que cada agente tenha sua propria sessao WhatsApp. O WPP Connect roda como container Docker separado e expoe uma API REST. O AvaBot consome essa API para gerenciar sessoes (QR code, status, desconexao) e processar mensagens recebidas via webhook. Um novo campo WhatsappToken na entidade Agent identifica a sessao no WPP Connect. Todos os endpoints ficam sob o prefixo `/whatsapp/{slug}/`.

## Technical Context

**Language/Version**: C# / .NET 9.0 + ASP.NET Core 9.0
**Primary Dependencies**: Entity Framework Core 9.x, HttpClient (nativo), AutoMapper, NAuth
**Storage**: PostgreSQL (via EF Core)
**Testing**: xUnit (AvaBot.Tests, AvaBot.Tests.API)
**Target Platform**: Linux server (Docker) / Windows (Development)
**Project Type**: web-service (REST API)
**External Service**: WPP Connect Server (wppconnect/server-cli) — container Docker, porta 21465, API REST com Bearer token
**Performance Goals**: N/A — feature administrativa + mensageria assíncrona
**Constraints**: WPP Connect container apenas em docker-compose.yml (nao em Development/Production)
**Scale/Scope**: Dezenas de agentes, cada um com no maximo 1 sessao WhatsApp

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Status | Notas |
|-----------|--------|-------|
| I. Skills Obrigatorias | PASS | Usar `dotnet-architecture` para alteracoes em entidade/repository/service/DI |
| II. Stack Tecnologica | PASS | Nenhuma tecnologia nova no backend. WPP Connect e servico externo (container). HttpClient e nativo do .NET |
| III. Case Sensitivity | N/A | Sem alteracoes em frontend |
| IV. Convencoes de Codigo | PASS | PascalCase para propriedades C#, _camelCase para campos privados |
| V. Convencoes de Banco | PASS | Coluna snake_case: `whatsapp_token` |
| VI. Autenticacao | PASS | Endpoints admin protegidos com `[Authorize]`, webhook `[AllowAnonymous]` |
| VII. Variaveis de Ambiente | PASS | Nova config `WppConnect:BaseUrl` no appsettings |
| VIII. Tratamento de Erros | PASS | Padrao try/catch com StatusCode(500, ex.Message) |

## Project Structure

### Documentation (this feature)

```text
specs/004-whatsapp-wpp-integration/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── whatsapp-endpoints.md
└── tasks.md
```

### Source Code (repository root)

```text
AvaBot.Domain/
└── Models/
    └── Agent.cs                    # +1 campo WhatsappToken

AvaBot.DTO/
└── AgentDTOs.cs                   # +1 campo em AgentInfo, +1 em AgentInsertInfo
                                    # +WhatsappQrCodeInfo, +WhatsappStatusInfo DTOs

AvaBot.Infra/
├── Context/
│   └── AvaBotContext.cs            # +1 coluna, +unique filtered index
└── Repository/
    └── AgentRepository.cs          # +GetByWhatsappTokenAsync

AvaBot.Infra.Interfaces/
├── Repository/
│   └── IAgentRepository.cs         # +GetByWhatsappTokenAsync
└── AppServices/
    └── IWppConnectService.cs       # Nova interface para comunicacao com WPP Connect

AvaBot.Infra/
└── AppServices/
    └── WppConnectService.cs        # Implementacao HttpClient para WPP Connect API

AvaBot.Application/
├── DependencyInjection.cs          # +HttpClient para WppConnectService, +WhatsappService
├── Profiles/
│   └── AgentProfile.cs             # Atualizar mapeamento
└── Services/
    └── WhatsappService.cs          # Novo: orquestra WPP Connect + ChatService

AvaBot.API/
├── Controllers/
│   └── WhatsappController.cs       # Novo: endpoints /whatsapp/{slug}/*
└── appsettings.Docker.json         # +secao WppConnect com BaseUrl

docker-compose.yml                  # +servico wppconnect
```

**Structure Decision**: Projeto Clean Architecture existente mantido. Nenhum projeto novo. WppConnectService como AppService (Infra) pois e comunicacao com servico externo. WhatsappService como Service (Application) pois orquestra logica de negocio.

## Complexity Tracking

Nenhuma violacao de constituicao. Tabela nao aplicavel.
