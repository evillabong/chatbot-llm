# Changelog

Todos los cambios importantes del CRM omnicanal multi-tenant se documentan en este archivo.

El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/) y el proyecto usará [Semantic Versioning](https://semver.org/lang/es/).

## [Unreleased]

### Added

- `README.md` inicial con visión general, arquitectura objetivo, stack, flujo de sesión y estado del proyecto.
- `CONTRIBUTING.md` con reglas de commits, branches, versionado, seguridad y convenciones de idioma.
- `CHANGELOG.md` bajo formato Keep a Changelog.
- Documento de planificación en `docs/PLAN.md` como referencia canónica de arquitectura.

### Changed

- No hay cambios publicados todavía.

### Fixed

- No hay correcciones publicadas todavía.

## [0.1.0] - Próximamente

### Planned

- Crear solución .NET 10 base.
- Crear proyectos `Mimo.Core`, `Mimo.Infrastructure` y `Mimo.Api`.
- Agregar `docker-compose.yml` con PostgreSQL, pgvector y Redis.
- Implementar resolución inicial de tenant.
- Definir modelo base de `Tenant`, `Agent`, `Role`, `Document`, `Conversation`, `Message` y `Ticket`.
- Implementar WebChat mínimo con SignalR.
- Implementar búsqueda documental semántica inicial.

[Unreleased]: https://github.com/evillabong/chatbot-llm/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/evillabong/chatbot-llm/releases/tag/v0.1.0
