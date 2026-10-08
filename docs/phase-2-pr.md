Implements Phase 2 persistence for the project management system: Employee, Project and ProjectTask entities, explicit enums, SQL Server DbContext/DI, Fluent API constraints and the initial EF migration. Adds a complete ERD and database design documentation, plus repeatable migration/test commands without committed credentials.

The schema enforces case-insensitive unique employee emails, required foreign keys, bounded enums and ordered project dates. Referenced employee deletion is restricted; deleting a project cascades its tasks while preserving employees.

Validation: locked restore and build passed with zero warnings/errors; all 24 tests passed (22 real SQL Server integration cases and 2 host startup checks). The migration was applied and reapplied safely to SQL Server. EF reports no pending model changes; SQL metadata confirms the expected tables/indexes/constraints and no disposable test databases remain.

Phase 3 has not started. This change adds no CRUD endpoints or JWT authentication. UTC request validation, active-employee eligibility and other service rules remain deferred as documented in PLAN.md and docs/database-design.md.
