# Tasks: Integracao WhatsApp via WPP Connect

**Input**: Design documents from `/specs/004-whatsapp-wpp-integration/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Nao solicitados na spec. Tarefas de teste nao incluidas.

**Organization**: Tasks agrupadas por user story para implementacao e teste independentes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependencias)
- **[Story]**: User story associada (US1, US2, US3, US4)
- Caminhos de arquivo incluidos nas descricoes

---

## Phase 1: Setup (Docker + Configuracao)

**Purpose**: Configurar o container WPP Connect e as variaveis de ambiente.

- [x] T001 Adicionar servico wppconnect ao docker-compose.yml usando imagem wppconnect/server-cli:latest, porta 21465, volume wpp_tokens, rede emagine-network
- [x] T002 [P] Adicionar variavel WPP_SECRET_KEY ao .env.example
- [x] T003 [P] Adicionar secao WppConnect com BaseUrl e SecretKey ao appsettings.Docker.json em AvaBot.API/appsettings.Docker.json

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Alteracoes no modelo, banco de dados, DTOs, servicos de infra e DI que DEVEM estar completas antes de qualquer user story.

**CRITICAL**: Nenhuma user story pode comecar ate esta fase estar completa.

- [x] T004 [P] Adicionar propriedade WhatsappToken ao modelo Agent em AvaBot.Domain/Models/Agent.cs
- [x] T005 [P] Adicionar campo whatsappToken ao AgentInfo DTO em AvaBot.DTO/AgentDTOs.cs
- [x] T006 [P] Adicionar campo whatsappToken ao AgentInsertInfo DTO em AvaBot.DTO/AgentDTOs.cs
- [x] T007 [P] Criar WhatsappQrCodeInfo DTO com campos agentSlug e qrCode em AvaBot.DTO/AgentDTOs.cs
- [x] T008 [P] Criar WhatsappStatusInfo DTO com campos agentSlug, status e isConnected em AvaBot.DTO/AgentDTOs.cs
- [x] T009 Adicionar configuracao Fluent API para coluna whatsapp_token e unique filtered index no AvaBotContext em AvaBot.Infra/Context/AvaBotContext.cs
- [x] T010 Gerar migration EF Core AddWhatsappTokenToAgent via dotnet ef migrations add no projeto AvaBot.Infra com startup-project AvaBot.API
- [x] T011 [P] Adicionar metodo GetByWhatsappTokenAsync a IAgentRepository em AvaBot.Infra.Interfaces/Repository/IAgentRepository.cs
- [x] T012 Implementar GetByWhatsappTokenAsync no AgentRepository em AvaBot.Infra/Repository/AgentRepository.cs
- [x] T013 Atualizar mapeamento AutoMapper no AgentProfile para incluir WhatsappToken em AvaBot.Application/Profiles/AgentProfile.cs
- [x] T014 Criar interface IWppConnectService com metodos StartSessionAsync, GetQrCodeAsync, GetStatusAsync, CloseSessionAsync, SendMessageAsync, GenerateTokenAsync em AvaBot.Infra.Interfaces/AppServices/IWppConnectService.cs
- [x] T015 Implementar WppConnectService usando IHttpClientFactory para comunicar com WPP Connect API REST em AvaBot.Infra/AppServices/WppConnectService.cs
- [x] T016 Registrar IHttpClientFactory com named client "WppConnect", WppConnectService e WhatsappService no DependencyInjection em AvaBot.Application/DependencyInjection.cs
- [x] T017 Atualizar AgentService.CreateAsync e UpdateAsync para validar unicidade do WhatsappToken via GetByWhatsappTokenAsync em AvaBot.Application/Services/AgentService.cs

**Checkpoint**: Fundacao pronta — modelo, banco, DTOs, WppConnectService e DI configurados.

---

## Phase 3: User Story 1 - Conectar Agente ao WhatsApp via QR Code (Priority: P1) MVP

**Goal**: Permitir que o administrador inicie uma sessao WhatsApp e obtenha o QR code para escanear.

**Independent Test**: Chamar POST /whatsapp/{slug}/start-session e depois GET /whatsapp/{slug}/qrcode e verificar que o QR code base64 e retornado.

### Implementation for User Story 1

- [x] T018 [US1] Criar WhatsappService com metodo StartSessionAsync(string slug) que resolve o agente, valida WhatsappToken, chama WppConnectService.GenerateTokenAsync e WppConnectService.StartSessionAsync com webhook URL em AvaBot.Application/Services/WhatsappService.cs
- [x] T019 [US1] Adicionar metodo GetQrCodeAsync(string slug) ao WhatsappService que resolve o agente e chama WppConnectService.GetQrCodeAsync em AvaBot.Application/Services/WhatsappService.cs
- [x] T020 [US1] Criar WhatsappController com endpoint POST /whatsapp/{slug}/start-session [Authorize] em AvaBot.API/Controllers/WhatsappController.cs
- [x] T021 [US1] Adicionar endpoint GET /whatsapp/{slug}/qrcode [Authorize] ao WhatsappController em AvaBot.API/Controllers/WhatsappController.cs

**Checkpoint**: Administrador pode iniciar sessao e obter QR code para escanear.

---

## Phase 4: User Story 2 - Receber e Responder Mensagens do WhatsApp (Priority: P1)

**Goal**: Receber mensagens via webhook do WPP Connect, processar com ChatService e responder automaticamente.

**Independent Test**: Enviar request simulado para POST /whatsapp/{slug}/webhook com payload de mensagem de texto e verificar que a resposta e processada.

### Implementation for User Story 2

- [x] T022 [US2] Adicionar metodo ProcessWebhookAsync(string slug, JsonElement payload) ao WhatsappService que parseia o evento, filtra (apenas onmessage, tipo chat, nao grupo), processa via ChatService e envia resposta via WppConnectService.SendMessageAsync em AvaBot.Application/Services/WhatsappService.cs
- [x] T023 [US2] Adicionar endpoint POST /whatsapp/{slug}/webhook [AllowAnonymous] ao WhatsappController que recebe o payload JSON e chama WhatsappService.ProcessWebhookAsync em AvaBot.API/Controllers/WhatsappController.cs

**Checkpoint**: Mensagens de texto recebidas via WhatsApp sao respondidas automaticamente pelo agente correto.

---

## Phase 5: User Story 3 - Configurar WhatsApp no Agente (Priority: P2)

**Goal**: Permitir que o administrador configure o WhatsappToken ao criar ou atualizar um agente.

**Independent Test**: Criar/atualizar agente com WhatsappToken via API e verificar persistencia e unicidade.

### Implementation for User Story 3

Nenhuma tarefa adicional necessaria — ja implementado na Phase 2 (T004-T008 DTOs, T009 banco, T017 validacao no AgentService). A configuracao do WhatsappToken no agente funciona automaticamente via CRUD existente.

**Checkpoint**: WhatsappToken pode ser configurado via API de agentes existente.

---

## Phase 6: User Story 4 - Verificar Status e Desconectar Sessao (Priority: P2)

**Goal**: Permitir que o administrador consulte o status da sessao e desconecte quando necessario.

**Independent Test**: Chamar GET /whatsapp/{slug}/status e POST /whatsapp/{slug}/disconnect e verificar respostas corretas.

### Implementation for User Story 4

- [x] T024 [P] [US4] Adicionar metodo GetStatusAsync(string slug) ao WhatsappService que resolve o agente e chama WppConnectService.GetStatusAsync em AvaBot.Application/Services/WhatsappService.cs
- [x] T025 [P] [US4] Adicionar metodo DisconnectAsync(string slug) ao WhatsappService que resolve o agente e chama WppConnectService.CloseSessionAsync em AvaBot.Application/Services/WhatsappService.cs
- [x] T026 [US4] Adicionar endpoint GET /whatsapp/{slug}/status [Authorize] ao WhatsappController em AvaBot.API/Controllers/WhatsappController.cs
- [x] T027 [US4] Adicionar endpoint POST /whatsapp/{slug}/disconnect [Authorize] ao WhatsappController em AvaBot.API/Controllers/WhatsappController.cs

**Checkpoint**: Administrador pode verificar status e desconectar sessao WhatsApp de qualquer agente.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Limpeza, documentacao e validacao final.

- [x] T028 [P] Criar collection Bruno com requests para todos os endpoints WhatsApp (start-session, qrcode, status, disconnect, webhook) em bruno/WhatsApp/
- [x] T029 [P] Criar documentacao docs/WHATSAPP_SETUP.md com guia de configuracao do WhatsApp por agente
- [x] T030 [P] Criar script SQL de migracao manual em scripts/add-whatsapp-token-to-agent.sql
- [x] T031 Validar build completo com dotnet build e testar fluxo end-to-end conforme quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sem dependencias — pode comecar imediatamente
- **Foundational (Phase 2)**: Depende de Phase 1 (appsettings para WppConnectService). BLOQUEIA todas as user stories
- **US1 (Phase 3)**: Depende de Phase 2. Primeira story a implementar.
- **US2 (Phase 4)**: Depende de Phase 2 e Phase 3 (precisa de sessao ativa para receber mensagens)
- **US3 (Phase 5)**: Ja implementado na Phase 2. Nenhuma tarefa adicional.
- **US4 (Phase 6)**: Depende de Phase 2. Pode rodar em paralelo com US1/US2.
- **Polish (Phase 7)**: Depende de todas as user stories

### User Story Dependencies

- **US1 (P1)**: Apos Phase 2. Base para US2.
- **US2 (P1)**: Apos US1 (precisa de sessao ativa para testar webhook).
- **US3 (P2)**: Ja coberto pela Phase 2.
- **US4 (P2)**: Apos Phase 2. Pode rodar em paralelo com US1.

### Within Each User Story

- Services antes de endpoints
- Core antes de integracao

### Parallel Opportunities

- Phase 1: T002 e T003 em paralelo
- Phase 2: T004-T008 em paralelo (arquivos diferentes), T011 em paralelo
- Phase 6: T024 e T025 em paralelo
- Phase 7: T028, T029, T030 em paralelo

---

## Parallel Example: Phase 2 (Foundational)

```text
# Batch 1 - Modelo e DTOs (arquivos diferentes):
T004: Agent.cs (Domain)
T005: AgentInfo DTO
T006: AgentInsertInfo DTO
T007: WhatsappQrCodeInfo DTO
T008: WhatsappStatusInfo DTO
(T005-T008 sao no mesmo arquivo, executar juntos)

# Batch 2 - Infra (depende de T004):
T009: AvaBotContext.cs
T011: IAgentRepository.cs

# Batch 3 - Pos-infra:
T010: Migration (depende de T009)
T012: AgentRepository.cs (depende de T011)
T013: AgentProfile.cs

# Batch 4 - Servicos:
T014: IWppConnectService.cs
T015: WppConnectService.cs (depende de T014)
T016: DependencyInjection.cs (depende de T015)
T017: AgentService.cs (depende de T012)
```

---

## Implementation Strategy

### MVP First (User Story 1 + 2)

1. Complete Phase 1: Setup (Docker + config)
2. Complete Phase 2: Foundational (modelo, banco, DTOs, WppConnectService, DI)
3. Complete Phase 3: US1 — Conectar via QR Code
4. Complete Phase 4: US2 — Receber e Responder Mensagens
5. **STOP and VALIDATE**: Testar conexao e envio de mensagens
6. Deploy/demo se pronto

### Incremental Delivery

1. Phase 1 + 2 → Fundacao pronta
2. + US1 → Agentes podem conectar ao WhatsApp via QR code
3. + US2 → Mensagens respondidas automaticamente (MVP completo!)
4. + US4 → Status e desconexao
5. + Polish → Documentacao e validacao final

---

## Notes

- [P] tasks = arquivos diferentes, sem dependencias
- [Story] label mapeia task para user story especifica
- US3 nao tem tarefas dedicadas pois a configuracao do WhatsappToken ja e coberta pela Phase 2
- Usar skill `dotnet-architecture` conforme Constitution para alteracoes em entidades/services/repositories
- WPP Connect API requer Bearer token — WppConnectService deve chamar generate-token antes de usar os endpoints
