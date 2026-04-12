-- Migration: Add WhatsApp token to avabot_agents
-- Feature: 004-whatsapp-wpp-integration
-- Date: 2026-04-12

ALTER TABLE avabot_agents ADD COLUMN whatsapp_token varchar(260);

CREATE UNIQUE INDEX ix_avabot_agents_whatsapp_token
    ON avabot_agents (whatsapp_token)
    WHERE whatsapp_token IS NOT NULL;
