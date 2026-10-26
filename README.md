# Pure.RelationalSchema.Storage.PostgreSQL.Migration

PostgreSQL schema migration for the **Pure** relational schema ecosystem: carries the data stored for a relational schema over to a changed version of that schema.

[![.NET build & test](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Migration/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Migration/actions/workflows/build-and-test.yml)
[![publish NuGet](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Migration/actions/workflows/publish-nuget.yml/badge.svg?branch=main)](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Migration/actions/workflows/publish-nuget.yml)
[![NuGet](https://img.shields.io/nuget/v/Pure.RelationalSchema.Storage.PostgreSQL.Migration)](https://www.nuget.org/packages/Pure.RelationalSchema.Storage.PostgreSQL.Migration)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## Overview

The PostgreSQL storage packages name every physical object after a content hash: the schema after `SchemaHash`, each table after `TableHash`, each column after `ColumnHash`, each index and foreign key after its own hash. Any change to a schema therefore changes the names its data must live under. Without a migration, the changed schema gets fresh, empty storage and the old data is left behind.

`PostgreSqlMigratedSchema` takes the schema as it was (the *source*), the schema as it is now (the *target*) and an optional mapping between their tables and columns, either separately or as one `ISchemaMigration` value. The first time one of its members is read, it does the whole migration:

1. drops foreign keys and indexes that the target no longer has;
2. drops tables that are not mapped to a target table;
3. for every mapped table: drops unmapped columns, renames mapped columns, changes their type when the PostgreSQL type differs (`USING "column"::type`), adds new columns filled with a default value, and renames the table;
4. renames the schema;
5. creates everything the target has and the stored schema does not: new tables, indexes, and foreign keys (including those of changed tables, whose names follow the table hash).

Steps 1–4 run in one `DO` block, only if the source storage exists. Step 5 runs `PostgreSqlCreatedSchema` from `Pure.RelationalSchema.Storage.PostgreSQL.Creation` over the target, whose `IF NOT EXISTS` statements skip everything that was kept. After the first member access the target storage is complete, and holds the source's data if the source was stored; if it was not, the target storage is created empty.

## Public API

| Type | Role |
|---|---|
| `PostgreSqlMigratedSchema : ISchema` | `(ISchema source, ISchema target, IDbConnection connection)`, `(ISchema source, ISchema target, IEnumerable<ITableMapping> tables, IDbConnection connection)` and `(ISchemaMigration migration, IDbConnection connection)`. Lazily migrates once, then exposes the target's members. |
| `SchemaMigration : ISchemaMigration` | The whole migration as one value: `(ISchema source, ISchema target)` and `(ISchema source, ISchema target, IEnumerable<ITableMapping> tables)`. |
| `ITableMapping` / `TableMapping` | Pairs a source table with a target table, with optional column mappings: `(ITable source, ITable target)` and `(ITable source, ITable target, IEnumerable<IColumnMapping> columns)`. |
| `IColumnMapping` / `ColumnMapping` | Pairs a source column with a target column: `(IColumn source, IColumn target)`. |

`ISchemaMigration`, `ITableMapping` and `IColumnMapping` come from [`Pure.RelationalSchema.Migration.Abstractions`](https://github.com/kudima03/Pure.RelationalSchema.Migration.Abstractions); this package provides the PostgreSQL migration and the plain record implementations.

## Mapping rules

- **Identical tables and columns map themselves.** A source table whose `TableHash` equals a target table's is mapped implicitly; so is a column whose `ColumnHash` equals a column of the mapped target table. Only *changed* tables and columns need an explicit mapping.
- **A changed table needs a mapping to keep its data.** Adding, removing or changing a column or an index changes a table's hash. An unmapped source table is dropped and the target table is created empty.
- **A renamed or retyped column needs a mapping to keep its data.** Otherwise it is dropped and the target column is added with the default value.
- **Explicit mappings win.** If a target table or column is explicitly mapped from something else, an identical source counterpart is not mapped implicitly; it is dropped before the mapped one is renamed into its place.
- **Mappings are validated.** A mapped table or column that is missing from its schema or table, or that is mapped more than once on either side, throws `ArgumentException` when the migration runs.

## Storing migrations as data

A migration is fully described by its source, its target and its explicit mappings; the SQL is derived from them. Persist those instead of the SQL: implement `ISchemaMigration`, `ITableMapping` and `IColumnMapping` on your own stored types (for example EF Core entities, next to stored schema versions) and pass the migration to `PostgreSqlMigratedSchema(migration, connection)`. Tables and columns are matched by hash, so a stored mapping only needs to resolve to the source and target tables and columns with those hashes.

Replay stored migrations in order. Replaying one that was already applied does nothing: its source storage is gone and creation skips what exists. Running one whose source was never stored creates empty target storage, so check that the stored version is the migration's source first.

## Default values

Added columns are `NOT NULL`, like every column the Creation package creates. Existing rows get the default value of the column type, and the default is dropped from the column afterwards, so the result matches a column created by Creation.

| Column type | PostgreSQL type | Default |
|---|---|---|
| `Bool` | `boolean` | `false` |
| `Int`, `UShort` | `integer` | `0` |
| `Long`, `UInt` | `bigint` | `0` |
| `ULong` | `numeric` | `0` |
| `Double` | `double precision` | `0` |
| `Float` | `real` | `0` |
| `String` | `text` | empty string |
| `Date` | `date` | `0001-01-01` |
| `DateTime` | `timestamp` | `0001-01-01 00:00:00` |
| `Time` | `time` | `00:00:00` |
| `DeterminedHash` | `bytea` | empty |
| `Uuid` | `uuid` | `00000000-0000-0000-0000-000000000000` |

## Dependencies

- [`Pure.RelationalSchema.Migration.Abstractions`](https://github.com/kudima03/Pure.RelationalSchema.Migration.Abstractions/tree/0.1.0-preview.0.1.0) — `ISchemaMigration`, `ITableMapping` and `IColumnMapping`
- [`Pure.RelationalSchema.Storage.PostgreSQL.Creation`](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Creation/tree/0.1.0-preview.0.4.0) — `PostgreSqlCreatedSchema`, which creates what is new in the target; brings in `Pure.RelationalSchema`, `Pure.RelationalSchema.HashCodes`, `Pure.Primitives.String.Operations`, `Pure.Primitives.Switches` and the primitives used to build the SQL

The hash-based naming convention is the contract with the Creation, Querying and Insertion packages: migrated storage is found and read by them exactly like storage they created.

## Target Frameworks

- .NET 8
- .NET 9
- .NET 10

## Installation

```bash
dotnet add package Pure.RelationalSchema.Storage.PostgreSQL.Migration
```

## Usage

```csharp
using System.Data;
using Pure.RelationalSchema.Abstractions.Column;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.ColumnType;
using Pure.RelationalSchema.Migration.Abstractions.SchemaMigration;
using Pure.RelationalSchema.Storage.PostgreSQL.Migration;
using Column = Pure.RelationalSchema.Column.Column;
using Schema = Pure.RelationalSchema.Schema.Schema;
using String = Pure.Primitives.String.String;
using Table = Pure.RelationalSchema.Table.Table;

IColumn id = new Column(new String("Id"), new IntColumnType());
IColumn name = new Column(new String("Name"), new StringColumnType());
IColumn title = new Column(new String("Title"), new StringColumnType());
IColumn price = new Column(new String("Price"), new DoubleColumnType());

// Version 1, already stored and filled with rows.
ITable products = new Table(new String("Products"), [id, name], []);
ISchema source = new Schema(new String("Shop"), [products], []);

// Version 2: "Name" is renamed to "Title" and "Price" is added.
ITable renamedProducts = new Table(new String("Products"), [id, title, price], []);
ISchema target = new Schema(new String("Shop"), [renamedProducts], []);

// Reading a member runs the whole migration once.
_ = new PostgreSqlMigratedSchema(
    source,
    target,
    [new TableMapping(products, renamedProducts, [new ColumnMapping(name, title)])],
    connection
).Name;
```

`Id` maps itself because it is unchanged. Every stored row keeps its `Id`, its `Name` value moves to `Title`, and `Price` is `0`.

The same migration as one value, for example after loading it from storage:

```csharp
ISchemaMigration migration = new SchemaMigration(
    source,
    target,
    [new TableMapping(products, renamedProducts, [new ColumnMapping(name, title)])]
);

_ = new PostgreSqlMigratedSchema(migration, connection).Name;
```

`System.Data` also declares `ITableMapping` and `IColumnMapping`. In a file that imports both `System.Data` and the abstraction namespaces, alias what you need from `System.Data` (`using IDbConnection = System.Data.IDbConnection;`) instead of importing the whole namespace.

## Known limitations

- **Swapped or chained renames fail.** Renames run one at a time, so renaming `A` to `B` while `B` is renamed to `C` (or swapping two names) collides with an existing name. PostgreSQL rejects it and the whole block rolls back.
- **Type changes rely on PostgreSQL casts.** A value that cannot be cast to the new type (for example `'abc'` to `integer`) fails the migration. Types without a cast between them (for example `integer` to `bytea`) always fail.
- **Indexes and foreign keys of changed tables are rebuilt.** Their names include the table hash, so they are dropped and created again. Re-creating a foreign key validates existing rows; if that fails, the reshaped data stays under the target names and the foreign key is missing until the cause is fixed.
- **The target storage must not exist yet.** Renaming the schema fails if storage named after the target hash already exists.
- **The source must be the exact stored schema.** The storage is found by hash, so a source that differs in any detail from what was stored is treated as not stored: nothing is carried over and the target is created empty.
- **The caller owns the connection and the transaction.** Reshaping (one `DO` block) and creation run as separate commands. To make the whole migration atomic, and atomic with other work such as saving the new schema definition, run it inside the caller's transaction.

## Related packages

- [`Pure.RelationalSchema.Storage.PostgreSQL.Creation`](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Creation) — creates the hash-named storage; used by this package for step 5
- [`Pure.RelationalSchema.Storage.PostgreSQL.Querying`](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Querying) — reads rows
- [`Pure.RelationalSchema.Storage.PostgreSQL.Insertion`](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Insertion) — inserts rows
- [`Pure.RelationalSchema.Storage.PostgreSQL.Deletion`](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Deletion) — deletes rows
- [`Pure.RelationalSchema.Storage.PostgreSQL.Destruction`](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Destruction) — drops the storage of a schema
