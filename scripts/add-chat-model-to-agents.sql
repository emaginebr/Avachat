-- Adiciona coluna chat_model na tabela avabot_agents
-- Registros existentes recebem 'gpt-4o' como default

ALTER TABLE avabot_agents
    ADD COLUMN IF NOT EXISTS chat_model VARCHAR(100) NOT NULL DEFAULT 'gpt-4o';
