# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

All `dotnet` commands must be run from the `./src` directory.

```bash
dotnet restore
dotnet build --no-restore -warnaserror
dotnet format --verify-no-changes             # check code style (CI enforces this)
dotnet format                                 # auto-fix code style
dotnet test --no-build --verbosity normal     # run integration tests (requires Docker for Testcontainers)
dotnet tool restore && dotnet stryker --mutation-level Complete --break-at 100
dotnet pack --configuration Release -p:Version=<version> --output .
```

CI additionally passes `-p:AssemblyVersion` (pinned to the major) and `-p:FileVersion`; see `.github/workflows/publish-nuget.yml`. Stryker leaves a mutated assembly in `bin/`, so rebuild before running `dotnet test --no-build` after it.

## Architecture

This is the **migration** part of the PostgreSQL storage implementation for the Pure relational schema ecosystem. It reshapes the stored data of a source `ISchema` into a target `ISchema` so the data survives schema changes. Creating storage, reading, inserting and deleting rows, and dropping storage live in the sibling packages `Pure.RelationalSchema.Storage.PostgreSQL.Creation`, `.Querying`, `.Insertion`, `.Deletion` and `.Destruction`.

**Dependencies:** the library references only `Pure.RelationalSchema.Storage.PostgreSQL.Creation`, which brings in `Pure.RelationalSchema`, `Pure.RelationalSchema.HashCodes`, `Pure.Primitives.String.Operations`, `Pure.Primitives.Switches` and the primitives. Do not add direct references to packages that already come transitively. Tests get Creation through the project reference (`DatabaseFixture.Store` builds real source storage with it) and reference `Querying` to prove migrated rows are readable.

**Key relationships:**

- `PostgreSqlMigratedSchema` (public) is `PostgreSqlCreatedSchema` (from Creation) over the internal `PostgreSqlReshapedSchema`, and delegates its members to it. Creation hashes the reshaped schema, which triggers the reshaping first; its `IF NOT EXISTS` statements then create the new tables, indexes and foreign keys and skip what was kept. A never-stored source therefore yields empty target storage.
- `PostgreSqlReshapedSchema` (internal) mirrors `PostgreSqlCreatedSchema`: a `Lazy<IBool>` runs the `DO` block on first access of `Name`, `Tables` or `ForeignKeys`, then those members return the **target**. The `throw new ArgumentException()` arms are unreachable and account for the uncovered branches. Creation reads all three members, so `PostgreSqlReshapedSchemaTests` triggers each member on its own to keep the per-member mutants killed.
- `ITableMapping` / `TableMapping` and `IColumnMapping` / `ColumnMapping` (public) are the caller's explicit correspondence between source and target. The library is general-purpose; it has no history of its own, so the caller supplies both schema versions and the mapping.
- `CompleteTableMappings` (internal) validates explicit table mappings (both sides present, each side mapped once; `ArgumentException` otherwise) and adds implicit identity mappings for source tables whose hash equals a target table that is not explicitly targeted. Every mapping is wrapped in `CompleteTableMapping`, which does the same for columns.
- `SchemaMigrationStatement` (internal) composes the statements in a fixed order: drop foreign keys missing from the target, drop indexes missing from the target, drop unmapped tables, per-table migration (`TableMigrationStatements`), schema rename. Drops come first so renames can take over names of dropped objects. Everything is wrapped in `MigrationBlockStatement`, a `DO $$ … IF EXISTS (pg_namespace) THEN … END IF; END$$;` block, so a source that was never stored is a no-op.
- `TableMigrationStatements` (internal) per mapped table: drop unmapped source columns, rename columns whose hash changed, `ALTER COLUMN … TYPE … USING "c"::type` only when the **PostgreSQL** type name differs (so `UShort` → `Int` does nothing), add unmapped target columns as `NOT NULL DEFAULT '<default>'` followed by `DROP DEFAULT`, rename the table when its hash changed. All statements address the table by its **source** name; the table rename is last.
- Statement records (`*DropStatement`, `*RenameStatement`, `ColumnTypeChangeStatement`, `ColumnAdditionStatement`) take raw `IString` names so tests can assert exact SQL. Name records (`SchemaName`, `TableName`, `ColumnName`, `ForeignKeyName`, `IndexName`) hold the naming convention.
- `PostgreSqlColumnTypeName` is copied from Creation; `PostgreSqlDefaultValue` is derived from it and maps every column type to its default (`false`, `0`, empty string, `0001-01-01`, `0001-01-01 00:00:00`, `00:00:00`, empty `bytea`, zero UUID).
- New tables, indexes and foreign keys are created by reusing `PostgreSqlCreatedSchema`, not by copying Creation's DDL, so Creation stays the single definition of what complete storage looks like. Indexes and foreign keys of changed tables are dropped by the reshaping (their names follow the table hash) and recreated by Creation.
- All SQL is composed as an `IString` value using `Pure.Primitives.String.Operations` combinators. Do not use string interpolation in library code.
- The caller owns the `IDbConnection` and the transaction. Reshaping and creation are two commands; callers that need the whole migration atomic run it inside their transaction. No type here may dispose the connection (the `IDbCommand` is disposed).

**Naming convention:** schema, table, column and foreign key identifiers are the first 63 characters (PostgreSQL's identifier limit) of `HexString` over `SchemaHash` / `TableHash` / `ColumnHash` / `ForeignKeyHash` from `Pure.RelationalSchema.HashCodes`. Index names are `"<first 31 of table hash>.<first 31 of index hash>"`. This must match the Creation package exactly, since it is the only contract between them. `TrimmedHash` is deliberately duplicated in each repo rather than shared.

**Testing:** Integration tests use `Testcontainers.PostgreSql` with one container per test class (`IClassFixture<DatabaseFixture>`). Every test uses its own schema name so the shared database never collides. `DatabaseFixture.Store` creates source storage through the published `PostgreSqlCreatedSchema`; rows are inserted and read back with raw SQL (`Values` casts to `text`, ordered), and the catalog is inspected through `information_schema` / `pg_indexes`. `StatementsTests` asserts the exact SQL text of every statement and name (mutation-testing targets) and enumerates every `IString` generically and through `Fakes/NonGenericItems`. Theory data with non-serializable values uses `DisableDiscoveryEnumeration = true` to satisfy xUnit1045.

CI requires >=99% line coverage (warning at 99%). The mutation testing step (`dotnet-stryker` at `Complete` level with a 100% break threshold) is commented out in `.github/workflows/build-and-test.yml`; run it locally.

**CI style check:** the `Code style check by csharpier` step in `.github/workflows/build-and-test.yml` is commented out on purpose: csharpier wants `) { }` on one line for chained constructors with an empty body, while `dotnet format` (IDE0055 with `csharp_new_line_before_open_brace = all`) wants `{ }` on its own line. `dotnet format` is the authoritative check.

**Multi-targeting:** net8.0, net9.0, net10.0. `IsAotCompatible` is `true` and the build must stay free of IL2026/IL3050 warnings.

**Package validation:** `EnablePackageValidation = true`. Set `PackageValidationBaselineVersion` to the last published version after the first release, and update it after each release so breaking API changes fail the build.

**Publishing:** triggered by pushing a semver tag matching `*.*.*`. The tag name becomes the `PackageVersion`. Packages are published to both GitHub Packages and NuGet.org.

## Code Style

Enforced via `.editorconfig` and `dotnet format --verify-no-changes` in CI:

- No `var` — always use explicit types
- No expression-bodied methods or constructors — properties, indexers, accessors, and lambdas may be expression-bodied
- File-scoped namespace declarations
- Private fields prefixed with `_` (underscore camelCase)
- No non-private instance fields
- `using` directives outside the namespace, System directives sorted first
- Max line length: 90 characters
- Braces on new lines (`csharp_new_line_before_open_brace = all`)
- No implicit object creation (`new()` without type) when the type is apparent

## Commit Messages

Do not mention Claude or AI assistance in commit messages.
