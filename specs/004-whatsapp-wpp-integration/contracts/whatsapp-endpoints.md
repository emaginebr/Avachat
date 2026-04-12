# API Contracts: WhatsApp Endpoints

**Date**: 2026-04-12

## Endpoints Novos (WhatsappController)

Todos os endpoints usam o prefixo `/whatsapp/{slug}/`.

### POST /whatsapp/{slug}/start-session

Inicia uma sessao WhatsApp no WPP Connect para o agente e configura o webhook.

**Auth**: `[Authorize]`

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | string | Slug do agente |

**Body**: Nenhum

**Responses**:
| Status | Body | Condition |
|--------|------|-----------|
| 200 | `Result<WhatsappStatusInfo>` | Sessao iniciada com sucesso |
| 400 | `Result<object>` | Agente sem WhatsappToken configurado |
| 404 | `Result<object>` | Agente nao encontrado |
| 500 | `Result<object>` | Erro ao comunicar com WPP Connect |

---

### GET /whatsapp/{slug}/qrcode

Obtem o QR code da sessao WhatsApp do agente em formato base64.

**Auth**: `[Authorize]`

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | string | Slug do agente |

**Responses**:
| Status | Body | Condition |
|--------|------|-----------|
| 200 | `Result<WhatsappQrCodeInfo>` | QR code retornado |
| 400 | `Result<object>` | Agente sem WhatsappToken ou sessao ja conectada |
| 404 | `Result<object>` | Agente nao encontrado |
| 500 | `Result<object>` | Erro ao obter QR code do WPP Connect |

**Response example (200)**:
```json
{
  "sucesso": true,
  "mensagem": "QR code obtido com sucesso",
  "erros": [],
  "dados": {
    "agentSlug": "assistente-vendas",
    "qrCode": "data:image/png;base64,iVBORw0KGgo..."
  }
}
```

---

### GET /whatsapp/{slug}/status

Verifica o status da sessao WhatsApp do agente.

**Auth**: `[Authorize]`

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | string | Slug do agente |

**Responses**:
| Status | Body | Condition |
|--------|------|-----------|
| 200 | `Result<WhatsappStatusInfo>` | Status retornado |
| 400 | `Result<object>` | Agente sem WhatsappToken configurado |
| 404 | `Result<object>` | Agente nao encontrado |
| 500 | `Result<object>` | Erro ao consultar WPP Connect |

**Response example (200)**:
```json
{
  "sucesso": true,
  "mensagem": "Status obtido com sucesso",
  "erros": [],
  "dados": {
    "agentSlug": "assistente-vendas",
    "status": "CONNECTED",
    "isConnected": true
  }
}
```

---

### POST /whatsapp/{slug}/disconnect

Desconecta/encerra a sessao WhatsApp do agente.

**Auth**: `[Authorize]`

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | string | Slug do agente |

**Responses**:
| Status | Body | Condition |
|--------|------|-----------|
| 200 | `Result<WhatsappStatusInfo>` | Sessao encerrada |
| 400 | `Result<object>` | Agente sem WhatsappToken configurado |
| 404 | `Result<object>` | Agente nao encontrado |
| 500 | `Result<object>` | Erro ao encerrar sessao no WPP Connect |

---

### POST /whatsapp/{slug}/webhook

Recebe eventos/mensagens do WPP Connect para o agente.

**Auth**: `[AllowAnonymous]`

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `slug` | string | Slug do agente |

**Body**: JSON enviado pelo WPP Connect (evento onmessage)

```json
{
  "event": "onmessage",
  "session": "session-name",
  "data": {
    "from": "5511999999999@c.us",
    "to": "5511888888888@c.us",
    "body": "Ola, preciso de ajuda",
    "type": "chat",
    "isGroupMsg": false
  }
}
```

**Responses**:
| Status | Body | Condition |
|--------|------|-----------|
| 200 | empty | Mensagem processada ou ignorada |
| 200 | empty | Erro no processamento (retorna 200 para evitar retries) |

**Logica de processamento**:
- Ignorar se `event` != "onmessage"
- Ignorar se `data.isGroupMsg` == true
- Ignorar se `data.type` != "chat"
- Rejeitar se agente inativo ou sem WhatsappToken
- Processar mensagem via ChatService e enviar resposta via WPP Connect send-message
