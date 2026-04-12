# Research: Integracao WhatsApp via WPP Connect

**Date**: 2026-04-12

## R1: WPP Connect Server — API REST

**Decision**: Usar WPP Connect Server (wppconnect/server-cli) como servico de mensageria externo, comunicando via API REST.

**Rationale**: WPP Connect Server e open-source, mantido pela comunidade, expoe API REST completa com Swagger, e tem imagem Docker oficial. Suporta multiplas sessoes simultaneas, cada uma identificada por um token/nome de sessao.

**API Endpoints Relevantes**:

| Metodo | Endpoint | Funcao |
|--------|----------|--------|
| POST | `/api/{session}/{secretKey}/generate-token` | Gerar token de acesso |
| POST | `/api/{session}/start-session` | Iniciar sessao (aceita webhook URL no body) |
| GET | `/api/{session}/qrcode-session` | Obter QR code em base64 |
| GET | `/api/{session}/status-session` | Status da sessao |
| GET | `/api/{session}/check-connection-session` | Verificar conexao |
| POST | `/api/{session}/close-session` | Encerrar sessao |
| POST | `/api/{session}/send-message` | Enviar mensagem: `{ "phone": "5511...", "message": "texto" }` |

**Autenticacao**: Bearer token gerado via `/generate-token` usando secretKey configurada no WPP Connect.

**Alternatives considered**:
- Baileys (lib Node.js direta): Requer implementar servidor proprio. Mais complexo. Descartado.
- WhatsApp Business API (oficial): Requer aprovacao Meta, custos, limitacoes. Descartado para este caso.

## R2: Container Docker para WPP Connect

**Decision**: Adicionar servico `wppconnect` ao `docker-compose.yml` usando imagem `wppconnect/server-cli:latest`. Porta 21465. Montar volume para persistir sessoes.

**Rationale**: A imagem oficial suporta configuracao via argumentos de linha de comando (--secretKey, --port). Volume necessario para persistir tokens de sessao entre restarts.

**Docker Compose Config**:
```yaml
wppconnect:
  image: wppconnect/server-cli:latest
  container_name: avabot-wppconnect
  command: ["--secretKey", "${WPP_SECRET_KEY}", "--port", "21465"]
  ports:
    - "21465:21465"
  volumes:
    - wpp_tokens:/usr/src/wpp-server/tokens
  networks:
    - emagine-network
```

**Alternatives considered**:
- Config via config.ts montado como volume: Mais complexo, requer arquivo adicional. Descartado em favor de CLI args.

## R3: Comunicacao AvaBot → WPP Connect

**Decision**: Criar `WppConnectService` como AppService no layer Infra, usando `HttpClient` nativo do .NET com `IHttpClientFactory`. URL base configuravel via appsettings (`WppConnect:BaseUrl` e `WppConnect:SecretKey`).

**Rationale**: `IHttpClientFactory` e o padrao recomendado pelo .NET para HttpClient gerenciado. Evita socket exhaustion. A URL e secretKey no appsettings permitem configuracao por ambiente.

**Appsettings Config**:
```json
"WppConnect": {
  "BaseUrl": "http://wppconnect:21465",
  "SecretKey": ""
}
```

**Alternatives considered**:
- HttpClient manual (new HttpClient()): Nao recomendado pelo .NET (socket exhaustion). Descartado.
- Pacote NuGet de terceiros: Nao existe pacote C# oficial para WPP Connect. Descartado.

## R4: Webhook do WPP Connect → AvaBot

**Decision**: Ao iniciar a sessao via `start-session`, enviar a URL do webhook do AvaBot no body: `{ "webhook": { "url": "https://avabot.net/whatsapp/{slug}/webhook" } }`. O WPP Connect envia eventos como POST para essa URL.

**Rationale**: O WPP Connect suporta configurar webhook por sessao no momento do start-session. Isso permite que cada agente tenha seu proprio webhook URL com o slug correto.

**Webhook Payload (onmessage)**:
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

- `type`: "chat" para texto, "ptt" para audio, "image", etc.
- `isGroupMsg`: Filtrar mensagens de grupo.
- `from`: Numero do remetente no formato `NUMERO@c.us`.

## R5: Fluxo de Inicializacao de Sessao

**Decision**: O fluxo de conexao de um agente ao WhatsApp segue estes passos:

1. Admin configura `WhatsappToken` no agente (ex: "agente-vendas")
2. Admin chama `POST /whatsapp/{slug}/start-session` (novo endpoint — inicia sessao no WPP Connect com webhook URL)
3. Admin chama `GET /whatsapp/{slug}/qrcode` para obter QR code
4. Admin escaneia QR code no celular
5. Sessao fica ativa, mensagens comecam a chegar no webhook

**Rationale**: Separar start-session e qrcode em dois endpoints da mais controle. O start-session registra o webhook e inicia o processo, o qrcode retorna a imagem para escaneamento.

**Nota**: Isso adiciona um endpoint nao previsto na spec original (start-session). E necessario porque o WPP Connect precisa que a sessao seja iniciada antes de gerar o QR code.
