-- Adiciona coluna chat_model na tabela avachat_agents
-- Registros existentes recebem 'gpt-4o' como default

ALTER TABLE avachat_agents
    ADD COLUMN IF NOT EXISTS chat_model VARCHAR(100) NOT NULL DEFAULT 'gpt-4o';
