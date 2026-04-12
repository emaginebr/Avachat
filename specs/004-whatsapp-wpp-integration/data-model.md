# Data Model: Integracao WhatsApp via WPP Connect

**Date**: 2026-04-12

## Entity Changes

### Agent (atualizado)

**Table**: `avabot_agents`

#### New Columns

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `whatsapp_token` | `varchar(260)` | Yes | NULL | Token/nome da sessao no WPP Connect |

#### New Indexes

| Index Name | Column(s) | Type | Filter |
|-----------|-----------|------|--------|
| `ix_avabot_agents_whatsapp_token` | `whatsapp_token` | Unique | WHERE `whatsapp_token` IS NOT NULL |

#### C# Property (adicionada ao Agent.cs)

```csharp
public string? WhatsappToken { get; set; }
```

#### Fluent API Configuration (adicionada ao OnModelCreating)

```csharp
entity.Property(e => e.WhatsappToken).HasColumnName("whatsapp_token").HasMaxLength(260);
entity.HasIndex(e => e.WhatsappToken)
    .IsUnique()
    .HasDatabaseName("ix_avabot_agents_whatsapp_token")
    .HasFilter("whatsapp_token IS NOT NULL");
```

## Migration SQL (referencia)

```sql
ALTER TABLE avabot_agents ADD COLUMN whatsapp_token varchar(260);

CREATE UNIQUE INDEX ix_avabot_agents_whatsapp_token
    ON avabot_agents (whatsapp_token)
    WHERE whatsapp_token IS NOT NULL;
```

## DTO Changes

### AgentInfo (response — adicionar campo)

```csharp
[JsonPropertyName("whatsappToken")]
public string? WhatsappToken { get; set; }
```

### AgentInsertInfo (input — adicionar campo opcional)

```csharp
[JsonPropertyName("whatsappToken")]
public string? WhatsappToken { get; set; }
```

### WhatsappQrCodeInfo (novo DTO)

```csharp
public class WhatsappQrCodeInfo
{
    [JsonPropertyName("agentSlug")]
    public string AgentSlug { get; set; } = string.Empty;

    [JsonPropertyName("qrCode")]
    public string QrCode { get; set; } = string.Empty;
}
```

### WhatsappStatusInfo (novo DTO)

```csharp
public class WhatsappStatusInfo
{
    [JsonPropertyName("agentSlug")]
    public string AgentSlug { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("isConnected")]
    public bool IsConnected { get; set; }
}
```

## Relationships

```
Agent (1) ----< (N) TelegramChat     [existente, sem alteracao]
Agent (1) ----< (N) ChatSession      [existente, sem alteracao]
Agent (1) ----< (N) KnowledgeFile    [existente, sem alteracao]
```

Nenhum novo relacionamento. O WhatsappToken e atributo do Agent. A sessao WhatsApp e gerenciada externamente pelo WPP Connect.
