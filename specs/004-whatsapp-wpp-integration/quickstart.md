# Quickstart: Integracao WhatsApp via WPP Connect

**Date**: 2026-04-12

## Pre-requisitos

- .NET 9.0 SDK
- Docker e Docker Compose (para WPP Connect)
- PostgreSQL rodando
- Projeto compilando (`dotnet build`)

## Ordem de Implementacao

### 1. Infrastructure — Container WPP Connect

- Adicionar servico `wppconnect` ao `docker-compose.yml`
- Adicionar variavel `WPP_SECRET_KEY` ao `.env.example`
- Verificar que o container sobe com `docker compose up wppconnect`

### 2. Domain Layer (AvaBot.Domain)

- Adicionar propriedade `WhatsappToken` (string?, nullable) ao `Agent.cs`

### 3. DTO Layer (AvaBot.DTO)

- Adicionar campo `whatsappToken` ao `AgentInfo` e `AgentInsertInfo`
- Criar `WhatsappQrCodeInfo` e `WhatsappStatusInfo` DTOs

### 4. Infrastructure Layer (AvaBot.Infra)

- Atualizar `AvaBotContext.OnModelCreating()` com Fluent API para nova coluna
- Adicionar unique filtered index em `whatsapp_token`
- Gerar migration EF Core
- Adicionar `GetByWhatsappTokenAsync` ao `AgentRepository`
- Criar `IWppConnectService` interface e `WppConnectService` implementacao

### 5. Interface Layer (AvaBot.Infra.Interfaces)

- Adicionar `GetByWhatsappTokenAsync` ao `IAgentRepository`
- Criar `IWppConnectService` interface

### 6. Application Layer (AvaBot.Application)

- Atualizar `AgentProfile` (AutoMapper)
- Atualizar `AgentService` com validacao de unicidade do WhatsappToken
- Criar `WhatsappService` (orquestra WPP Connect + ChatService)
- Registrar `WppConnectService` e `WhatsappService` no DI

### 7. API Layer (AvaBot.API)

- Adicionar secao `WppConnect` ao `appsettings.Docker.json`
- Criar `WhatsappController` com endpoints

### 8. Config

- Adicionar `WppConnect:BaseUrl` e `WppConnect:SecretKey` ao appsettings

## Comandos Uteis

```bash
# Subir WPP Connect
docker compose up -d wppconnect

# Gerar migration
cd AvaBot.Infra
dotnet ef migrations add AddWhatsappTokenToAgent --startup-project ../AvaBot.API

# Build
dotnet build

# Run
cd AvaBot.API
dotnet run
```

## Verificacao Rapida

1. Subir containers: `docker compose up -d`
2. Criar/atualizar agente com WhatsappToken via API
3. Chamar `POST /whatsapp/{slug}/start-session` para iniciar sessao
4. Chamar `GET /whatsapp/{slug}/qrcode` e escanear QR code no celular
5. Chamar `GET /whatsapp/{slug}/status` e verificar que esta CONNECTED
6. Enviar mensagem no WhatsApp e verificar resposta automatica do agente
7. Chamar `POST /whatsapp/{slug}/disconnect` para encerrar
