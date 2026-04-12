# Feature Specification: Integracao WhatsApp via WPP Connect

**Feature Branch**: `004-whatsapp-wpp-integration`  
**Created**: 2026-04-12  
**Status**: Draft  
**Input**: User description: "Integracao com WhatsApp usando WPP Connect. Container Docker para WPP apenas em ambiente Docker. Configuracao de URL no appsettings. Campo WhatsappToken no Agent. Endpoints WhatsappController com prefixo /whatsapp/{slug}. Integracao por agente. Metodo para obter QR code por agente."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Conectar Agente ao WhatsApp via QR Code (Priority: P1)

O administrador acessa o endpoint de QR code do agente para vincular uma sessao WhatsApp. O sistema retorna o QR code gerado pelo WPP Connect para que o administrador escaneie com o WhatsApp no celular. Apos o escaneamento, a sessao fica ativa e o agente esta pronto para receber mensagens.

**Why this priority**: Sem a conexao via QR code, nenhuma outra funcionalidade WhatsApp funciona. Este e o ponto de entrada obrigatorio para ativar a integracao.

**Independent Test**: Pode ser testado chamando o endpoint de QR code para um agente com WhatsappToken configurado e verificando que um QR code e retornado.

**Acceptance Scenarios**:

1. **Given** um agente com WhatsappToken configurado, **When** o administrador solicita o QR code, **Then** o sistema retorna a imagem do QR code gerada pelo servico de mensageria.
2. **Given** um agente sem WhatsappToken configurado, **When** o administrador solicita o QR code, **Then** o sistema informa que o WhatsApp nao esta configurado para este agente.
3. **Given** um agente ja conectado ao WhatsApp, **When** o administrador solicita o QR code novamente, **Then** o sistema informa que a sessao ja esta ativa.

---

### User Story 2 - Receber e Responder Mensagens do WhatsApp (Priority: P1)

Quando um usuario envia uma mensagem via WhatsApp para o numero vinculado ao agente, o sistema recebe a mensagem pelo webhook, processa usando o ChatService do agente correspondente e envia a resposta de volta ao usuario no WhatsApp.

**Why this priority**: Esta e a funcionalidade central da integracao — sem ela, o WhatsApp e apenas conectado mas nao funcional.

**Independent Test**: Pode ser testado enviando uma mensagem simulada para o webhook do agente e verificando que a resposta e processada e enviada de volta.

**Acceptance Scenarios**:

1. **Given** um agente com sessao WhatsApp ativa, **When** um usuario envia uma mensagem de texto, **Then** o sistema processa a mensagem via ChatService e envia a resposta no WhatsApp.
2. **Given** dois agentes com sessoes WhatsApp ativas, **When** cada um recebe uma mensagem, **Then** cada mensagem e roteada para o agente correto baseado no slug.
3. **Given** um agente com sessao WhatsApp inativa, **When** uma mensagem chega no webhook, **Then** o sistema rejeita a mensagem com erro apropriado.
4. **Given** um agente com WhatsApp ativo, **When** um usuario envia um tipo nao suportado (imagem, audio), **Then** o sistema responde informando que apenas texto e aceito.

---

### User Story 3 - Configurar WhatsApp no Agente (Priority: P2)

O administrador configura o campo WhatsappToken no agente ao criar ou atualizar o agente. O token identifica a sessao do agente no servico de mensageria e e utilizado em todas as chamadas.

**Why this priority**: Pre-requisito para US1, mas e uma operacao simples de CRUD que estende a entidade existente.

**Independent Test**: Pode ser testado criando/atualizando um agente com WhatsappToken via API e verificando que o campo foi persistido.

**Acceptance Scenarios**:

1. **Given** um agente existente, **When** o administrador preenche o WhatsappToken e salva, **Then** o campo e persistido no banco de dados.
2. **Given** dois agentes, **When** ambos tentam usar o mesmo WhatsappToken, **Then** o sistema impede a duplicidade.
3. **Given** um agente com WhatsappToken configurado, **When** o administrador remove o token, **Then** o campo e limpo no banco.

---

### User Story 4 - Verificar Status da Sessao WhatsApp (Priority: P2)

O administrador pode consultar o status da sessao WhatsApp de um agente para verificar se esta conectada, desconectada ou aguardando escaneamento do QR code.

**Why this priority**: Funcionalidade de diagnostico que ajuda a identificar problemas de conexao, mas nao bloqueia o uso.

**Independent Test**: Pode ser testado chamando o endpoint de status e verificando que o estado atual da sessao e retornado.

**Acceptance Scenarios**:

1. **Given** um agente com sessao WhatsApp ativa, **When** o administrador consulta o status, **Then** o sistema retorna que a sessao esta conectada.
2. **Given** um agente sem sessao ativa, **When** o administrador consulta o status, **Then** o sistema retorna que a sessao esta desconectada.

---

### Edge Cases

- O que acontece quando a sessao WhatsApp expira ou e desconectada pelo celular? O sistema deve detectar a desconexao e informar no status.
- O que acontece quando o servico de mensageria esta indisponivel? O sistema deve retornar erro ao tentar obter QR code ou enviar mensagens.
- O que acontece quando o agente e desativado (status=0)? Mensagens no webhook devem ser rejeitadas.
- O que acontece quando o numero WhatsApp e bloqueado? O sistema deve informar o erro ao tentar enviar mensagens.
- O que acontece quando o agente recebe mensagens de grupo? O sistema deve ignorar mensagens de grupo e processar apenas conversas privadas.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema DEVE armazenar o campo WhatsappToken na entidade Agente, opcional.
- **FR-002**: O sistema DEVE impedir que dois agentes usem o mesmo WhatsappToken.
- **FR-003**: O sistema DEVE fornecer um endpoint para obter o QR code do WhatsApp por agente, acessivel via `/whatsapp/{slug}/qrcode`.
- **FR-004**: O sistema DEVE fornecer um endpoint de webhook por agente em `/whatsapp/{slug}/webhook` para receber mensagens do WhatsApp.
- **FR-005**: O sistema DEVE processar mensagens de texto recebidas via WhatsApp usando o ChatService do agente correspondente e enviar a resposta de volta.
- **FR-006**: O sistema DEVE fornecer um endpoint para verificar o status da sessao WhatsApp por agente em `/whatsapp/{slug}/status`.
- **FR-007**: O sistema DEVE rejeitar mensagens de WhatsApp para agentes inativos (status=0) ou sem WhatsappToken configurado.
- **FR-011**: O sistema DEVE fornecer um endpoint para desconectar a sessao WhatsApp de um agente em `/whatsapp/{slug}/disconnect`.
- **FR-008**: O sistema DEVE comunicar-se com o servico de mensageria atraves de uma URL configuravel.
- **FR-009**: O servico de mensageria DEVE estar disponivel como container apenas no ambiente Docker.
- **FR-010**: O sistema DEVE rejeitar mensagens que nao sejam de texto (imagem, audio, video, etc.) com resposta amigavel.

### Key Entities

- **Agent (atualizado)**: Passa a conter o campo WhatsappToken (token que identifica a sessao do agente no servico de mensageria). Campo opcional, permitindo agentes sem integracao WhatsApp.
- **WhatsApp Session (conceitual)**: Sessao gerenciada pelo servico de mensageria externo. O sistema interage via chamadas HTTP usando o WhatsappToken do agente.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Administradores conseguem conectar um agente ao WhatsApp em menos de 3 minutos (configurar token, obter QR code, escanear).
- **SC-002**: O sistema suporta multiplos agentes com sessoes WhatsApp simultaneas, cada um vinculado a um numero diferente, sem interferencia.
- **SC-003**: Mensagens de texto recebidas via WhatsApp sao respondidas automaticamente pelo agente correto em 100% dos casos.
- **SC-004**: O administrador consegue verificar o status da conexao WhatsApp de qualquer agente a qualquer momento.
- **SC-005**: O servico de mensageria esta disponivel automaticamente ao iniciar o ambiente com containers.

## Clarifications

### Session 2026-04-12

- Q: Em que formato o QR code deve ser retornado pelo endpoint? → A: Base64 da imagem dentro de uma resposta JSON.
- Q: Deve existir um endpoint para desconectar/encerrar a sessao WhatsApp? → A: Sim, criar endpoint `/whatsapp/{slug}/disconnect`.

## Assumptions

- O WPP Connect e o servico de mensageria escolhido e sera executado como container separado no ambiente Docker.
- O WPP Connect expoe uma API REST que o AvaBot consome via HTTP.
- Cada agente usa um WhatsappToken unico para identificar sua sessao no WPP Connect.
- A URL do WPP Connect e configurada via appsettings, sendo diferente por ambiente (Docker apontando para o container, Development usando localhost ou servico externo).
- O sistema processa apenas mensagens de texto. Mensagens de midia, localizacao, contatos e stickers sao rejeitadas com resposta amigavel.
- Mensagens de grupo sao ignoradas; apenas conversas privadas sao processadas.
- O QR code e retornado como string base64 da imagem dentro de uma resposta JSON padrao (Result), para que o administrador possa escanear.
- O WhatsappToken e armazenado em texto puro no banco de dados, seguindo o mesmo padrao do TelegramBotToken.
