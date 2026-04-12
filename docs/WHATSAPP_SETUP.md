# Configuracao do WhatsApp via WPP Connect

> Guia completo para configurar e integrar o WhatsApp com o AvaBot, por agente.

**Created:** 2026-04-12
**Last Updated:** 2026-04-12

---

## Pre-requisitos

1. **Docker e Docker Compose** instalados
2. **AvaBot API** rodando
3. **Agente cadastrado** no AvaBot com `WhatsappToken` configurado
4. **Celular com WhatsApp** para escanear o QR code

---

## 1. Subir o WPP Connect

O WPP Connect roda como container Docker junto com o AvaBot.

### Configurar o Secret Key

No arquivo `.env`, configure a chave secreta do WPP Connect:

```env
WPP_SECRET_KEY=sua_chave_secreta_aqui
```

### Subir os containers

```bash
docker compose up -d
```

O WPP Connect estara disponivel na porta 21465. O Swagger pode ser acessado em `http://localhost:21465/api-docs`.

---

## 2. Configurar o Agente

Ao criar ou atualizar um agente, inclua o campo `whatsappToken`:

```http
PUT /api/agents/{id}
Authorization: Bearer <seu-token-jwt>
Content-Type: application/json

{
  "name": "Assistente Vendas",
  "systemPrompt": "Voce e um assistente de vendas...",
  "chatModel": "gpt-4o",
  "whatsappToken": "assistente-vendas-wpp"
}
```

> O `whatsappToken` e o identificador da sessao no WPP Connect. Deve ser unico por agente.

---

## 3. Conectar ao WhatsApp

### Passo 1: Iniciar a sessao

```http
POST /whatsapp/{slug}/start-session
Authorization: Bearer <seu-token-jwt>
```

### Passo 2: Obter o QR Code

```http
GET /whatsapp/{slug}/qrcode
Authorization: Bearer <seu-token-jwt>
```

Resposta:
```json
{
  "sucesso": true,
  "mensagem": "QR code obtido com sucesso",
  "dados": {
    "agentSlug": "assistente-vendas",
    "qrCode": "data:image/png;base64,iVBORw0KGgo..."
  }
}
```

### Passo 3: Escanear o QR Code

1. Abra o WhatsApp no celular
2. Va em **Dispositivos conectados** > **Conectar dispositivo**
3. Escaneie o QR code retornado pela API

### Passo 4: Verificar a conexao

```http
GET /whatsapp/{slug}/status
Authorization: Bearer <seu-token-jwt>
```

---

## 4. Como Funciona

### Fluxo de Mensagens

```
Usuario WhatsApp  -->  WPP Connect  -->  POST /whatsapp/{slug}/webhook  -->  WhatsappService
                                                                                   |
                                                                             ChatService
                                                                                   |
                                                                             WPP Connect
                                                                             send-message
                                                                                   |
                                                                          Usuario WhatsApp
```

### Processamento

1. Usuario envia mensagem de texto no WhatsApp
2. WPP Connect recebe e encaminha para o webhook do AvaBot
3. WhatsappService resolve o agente pelo slug
4. ChatService processa a mensagem com o modelo de IA do agente
5. Resposta e enviada de volta pelo WPP Connect

> Apenas mensagens de texto em conversas privadas sao processadas. Mensagens de grupo, imagens, audios e outros tipos sao ignorados ou rejeitados com mensagem amigavel.

---

## 5. Endpoints

| Metodo | Rota | Auth | Descricao |
|--------|------|------|-----------|
| POST | `/whatsapp/{slug}/start-session` | Authorize | Inicia sessao no WPP Connect |
| GET | `/whatsapp/{slug}/qrcode` | Authorize | Obtem QR code em base64 |
| GET | `/whatsapp/{slug}/status` | Authorize | Verifica status da sessao |
| POST | `/whatsapp/{slug}/disconnect` | Authorize | Encerra a sessao |
| POST | `/whatsapp/{slug}/webhook` | AllowAnonymous | Recebe mensagens do WPP Connect |

---

## 6. Desconectar

Para encerrar a sessao WhatsApp de um agente:

```http
POST /whatsapp/{slug}/disconnect
Authorization: Bearer <seu-token-jwt>
```

---

## 7. Testando

### Com Bruno (API Client)

O projeto inclui uma collection do Bruno em `bruno/WhatsApp/`:

- **Start Session**: Inicia a sessao
- **QR Code**: Obtem o QR code
- **Status**: Verifica o status
- **Disconnect**: Encerra a sessao
- **Webhook (Simulate)**: Simula uma mensagem recebida

---

## Troubleshooting

| Problema | Causa Provavel | Solucao |
|---|---|---|
| QR code nao aparece | Sessao nao iniciada | Chame `POST /whatsapp/{slug}/start-session` primeiro |
| Erro de conexao com WPP | Container nao esta rodando | Verifique com `docker compose ps` |
| Mensagens nao chegam | Webhook nao configurado | Reinicie a sessao com start-session |
| Token duplicado | Outro agente usa o mesmo token | Use um token unico por agente |
| Sessao desconectada | WhatsApp desconectou no celular | Reconecte via start-session + qrcode |
