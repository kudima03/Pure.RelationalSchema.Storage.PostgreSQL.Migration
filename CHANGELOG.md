# Changelog

All notable changes to `Pure.RelationalSchema.Storage.PostgreSQL.Migration` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Schema migration: `PostgreSqlMigratedSchema` lazily migrates the stored data of a source schema to a target schema. One `DO` block drops foreign keys, indexes, tables and columns missing from the target, renames mapped columns and changes their type with a cast, adds new columns filled with the column type's default value, and renames tables and the schema; `PostgreSqlCreatedSchema` then creates the new tables, indexes and foreign keys. A source that was never stored yields empty target storage.
- `TableMapping` and `ColumnMapping` pair changed tables and columns, implementing `ITableMapping` and `IColumnMapping` from `Pure.RelationalSchema.Migration.Abstractions`. Unchanged tables and columns map themselves; invalid or duplicate mappings throw `ArgumentException`.
- `SchemaMigration` implements `ISchemaMigration`, the whole migration as one value; `PostgreSqlMigratedSchema(ISchemaMigration, IDbConnection)` runs it, so migrations can be stored as data and replayed.
