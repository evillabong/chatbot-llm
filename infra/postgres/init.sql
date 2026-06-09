-- Script de inicialización de la base de datos MIMO
-- Se ejecuta una sola vez al crear el contenedor de PostgreSQL

-- Habilitar extensión pgvector para búsqueda semántica
CREATE EXTENSION IF NOT EXISTS vector;

-- Habilitar extensión para UUIDs
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Tabla de tenants en el esquema public (catálogo global de la plataforma)
CREATE TABLE IF NOT EXISTS tenants (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name            VARCHAR(200) NOT NULL,
    slug            VARCHAR(50)  NOT NULL UNIQUE,
    is_active       BOOLEAN      NOT NULL DEFAULT true,
    plan            VARCHAR(50)  NOT NULL DEFAULT 'free',
    configuration   JSONB        NOT NULL DEFAULT '{}',
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ
);

-- Índices del catálogo global
CREATE INDEX IF NOT EXISTS idx_tenants_slug     ON tenants (slug);
CREATE INDEX IF NOT EXISTS idx_tenants_is_active ON tenants (is_active);

-- Función auxiliar para crear el esquema de un tenant nuevo
-- Uso: SELECT create_tenant_schema('municipio');
CREATE OR REPLACE FUNCTION create_tenant_schema(p_slug TEXT)
RETURNS VOID LANGUAGE plpgsql AS $$
DECLARE
    v_schema TEXT := 'tenant_' || p_slug;
BEGIN
    -- Crear esquema si no existe
    EXECUTE format('CREATE SCHEMA IF NOT EXISTS %I', v_schema);

    -- Establecer search_path al esquema del tenant
    EXECUTE format('SET search_path TO %I, public', v_schema);

    -- ── Roles / departamentos ─────────────────────────────────────────────
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.roles (
            id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            tenant_id           UUID        NOT NULL,
            name                VARCHAR(100) NOT NULL,
            description         TEXT,
            priority_level      INT         NOT NULL DEFAULT 0,
            can_view_all_tickets BOOLEAN    NOT NULL DEFAULT false,
            is_active           BOOLEAN     NOT NULL DEFAULT true,
            created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
        )', v_schema);

    -- ── Funcionarios ──────────────────────────────────────────────────────
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.agents (
            id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            tenant_id               UUID         NOT NULL,
            email                   VARCHAR(200) NOT NULL,
            full_name               VARCHAR(200) NOT NULL,
            alias                   VARCHAR(100) NOT NULL,
            is_active               BOOLEAN      NOT NULL DEFAULT true,
            max_concurrent_sessions INT          NOT NULL DEFAULT 5,
            created_at              TIMESTAMPTZ  NOT NULL DEFAULT NOW()
        )', v_schema);

    -- ── Pivote funcionario-rol ────────────────────────────────────────────
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.agent_roles (
            agent_id UUID NOT NULL,
            role_id  UUID NOT NULL,
            PRIMARY KEY (agent_id, role_id)
        )', v_schema);

    -- ── Categorías de documentos ──────────────────────────────────────────
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.document_categories (
            id                 UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            tenant_id          UUID         NOT NULL,
            name               VARCHAR(100) NOT NULL,
            parent_category_id UUID
        )', v_schema);

    -- ── Documentos de conocimiento ────────────────────────────────────────
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.documents (
            id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            tenant_id       UUID         NOT NULL,
            title           VARCHAR(300) NOT NULL,
            content         TEXT         NOT NULL,
            visibility      VARCHAR(20)  NOT NULL DEFAULT ''Public'',
            category_id     UUID,
            related_role_id UUID,
            tags            TEXT[]       NOT NULL DEFAULT ''{}'',
            priority_level  INT          NOT NULL DEFAULT 0,
            embedding       vector(1536),
            is_active       BOOLEAN      NOT NULL DEFAULT true,
            created_by      UUID,
            created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
            updated_at      TIMESTAMPTZ
        )', v_schema);

    -- Índice HNSW para búsqueda semántica eficiente
    EXECUTE format('
        CREATE INDEX IF NOT EXISTS idx_%s_documents_embedding
        ON %I.documents USING hnsw (embedding vector_cosine_ops)',
        p_slug, v_schema);

    -- ── Conversaciones ────────────────────────────────────────────────────
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.conversations (
            id                UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            tenant_id         UUID         NOT NULL,
            external_user_id  VARCHAR(200) NOT NULL,
            external_user_name VARCHAR(200),
            channel           VARCHAR(30)  NOT NULL,
            status            VARCHAR(30)  NOT NULL DEFAULT ''BotActive'',
            is_authenticated  BOOLEAN      NOT NULL DEFAULT false,
            customer_email    VARCHAR(200),
            customer_phone    VARCHAR(30),
            created_at        TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
            last_message_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
            resolved_at       TIMESTAMPTZ
        )', v_schema);

    -- ── Mensajes ──────────────────────────────────────────────────────────
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.messages (
            id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            conversation_id UUID        NOT NULL,
            role            VARCHAR(20) NOT NULL,
            sender_id       UUID,
            content         TEXT        NOT NULL,
            metadata        JSONB,
            created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
        )', v_schema);

    -- ── Tickets ───────────────────────────────────────────────────────────
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.tickets (
            id                 UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            conversation_id    UUID        NOT NULL UNIQUE,
            assigned_role_id   UUID        NOT NULL,
            assigned_agent_id  UUID,
            priority           VARCHAR(20) NOT NULL DEFAULT ''Normal'',
            status             VARCHAR(20) NOT NULL DEFAULT ''InQueue'',
            internal_notes     TEXT,
            escalation_reason  TEXT,
            created_at         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            assigned_at        TIMESTAMPTZ,
            first_response_at  TIMESTAMPTZ,
            resolved_at        TIMESTAMPTZ
        )', v_schema);

    -- ── Registros de transferencia ────────────────────────────────────────
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.transfer_records (
            id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            ticket_id       UUID        NOT NULL,
            from_role_id    UUID,
            from_agent_id   UUID,
            to_role_id      UUID        NOT NULL,
            to_agent_id     UUID,
            transferred_by  UUID        NOT NULL,
            reason          TEXT,
            is_partial      BOOLEAN     NOT NULL DEFAULT false,
            context_note    TEXT,
            created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
        )', v_schema);

    -- ── Encuestas de satisfacción ─────────────────────────────────────────
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.satisfaction_surveys (
            id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            ticket_id   UUID        NOT NULL UNIQUE,
            rating      INT         NOT NULL CHECK (rating BETWEEN 1 AND 5),
            observations TEXT,
            recorded_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        )', v_schema);

    -- ── Chat interno ──────────────────────────────────────────────────────
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.internal_chat_messages (
            id                UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            tenant_id         UUID        NOT NULL,
            from_agent_id     UUID        NOT NULL,
            to_agent_id       UUID        NOT NULL,
            related_ticket_id UUID,
            content           TEXT        NOT NULL,
            is_read           BOOLEAN     NOT NULL DEFAULT false,
            created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW()
        )', v_schema);

    -- Índices comunes de tickets y mensajes
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%s_tickets_status_role ON %I.tickets (status, assigned_role_id)', p_slug, v_schema);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%s_messages_conversation ON %I.messages (conversation_id, created_at)', p_slug, v_schema);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%s_conversations_status ON %I.conversations (status, last_message_at)', p_slug, v_schema);

END;
$$;
