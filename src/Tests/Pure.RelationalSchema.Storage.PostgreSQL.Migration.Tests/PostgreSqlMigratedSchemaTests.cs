using Pure.Primitives.Bool;
using Pure.Primitives.String.Operations;
using Pure.RelationalSchema.Abstractions.Column;
using Pure.RelationalSchema.Abstractions.ColumnType;
using Pure.RelationalSchema.Abstractions.ForeignKey;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.ColumnType;
using Pure.RelationalSchema.HashCodes;
using Pure.RelationalSchema.Storage.Abstractions;
using Pure.RelationalSchema.Storage.PostgreSQL.Querying;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration.Tests;

using Column = Column.Column;
using ForeignKey = ForeignKey.ForeignKey;
using Index = Index.Index;
using Schema = Schema.Schema;
using Table = Table.Table;

public sealed record PostgreSqlMigratedSchemaTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public PostgreSqlMigratedSchemaTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public static TheoryData<string, IColumnType, string, string> ColumnTypes =>
        new TheoryData<string, IColumnType, string, string>
        {
            { "Int", new IntColumnType(), "0", "integer" },
            { "Bool", new BoolColumnType(), "false", "boolean" },
            { "Long", new LongColumnType(), "0", "bigint" },
            { "UShort", new UShortColumnType(), "0", "integer" },
            { "UInt", new UIntColumnType(), "0", "bigint" },
            { "ULong", new ULongColumnType(), "0", "numeric" },
            { "String", new StringColumnType(), "", "text" },
            { "Date", new DateColumnType(), "0001-01-01", "date" },
            {
                "DateTime",
                new DateTimeColumnType(),
                "0001-01-01 00:00:00",
                "timestamp without time zone"
            },
            { "Time", new TimeColumnType(), "00:00:00", "time without time zone" },
            { "Hash", new DeterminedHashColumnType(), "\\x", "bytea" },
            { "Double", new DoubleColumnType(), "0", "double precision" },
            { "Float", new FloatColumnType(), "0", "real" },
            {
                "Uuid",
                new UuidColumnType(),
                "00000000-0000-0000-0000-000000000000",
                "uuid"
            },
        };

    [Fact]
    public void KeepsRowsWhenSchemaIsUnchanged()
    {
        IColumn id = Column("Id", new IntColumnType());
        ITable table = new Table(new String("Items"), [id], []);
        ISchema schema = Schema(nameof(KeepsRowsWhenSchemaIsUnchanged), [table]);
        _fixture.Store(schema);
        _fixture.Insert(schema, table, "1");

        _ = new PostgreSqlMigratedSchema(schema, schema, _fixture.Connection).Name;

        Assert.Equal(["1"], _fixture.Values(schema, table, id));
    }

    [Fact]
    public void RenamesStoredSchemaWhenSchemaNameChanges()
    {
        IColumn id = Column("Id", new IntColumnType());
        ITable table = new Table(new String("Items"), [id], []);
        ISchema source = Schema("RenameSource", [table]);
        ISchema target = Schema("RenameTarget", [table]);
        _fixture.Store(source);
        _fixture.Insert(source, table, "1");

        _ = new PostgreSqlMigratedSchema(source, target, _fixture.Connection).Name;

        Assert.False(_fixture.SchemaExists(source));
        Assert.Equal(["1"], _fixture.Values(target, table, id));
    }

    [Fact]
    public void MigratesLazily()
    {
        ITable table = new Table(
            new String("Items"),
            [Column("Id", new IntColumnType())],
            []
        );
        ISchema source = Schema("LazySource", [table]);
        ISchema target = Schema("LazyTarget", [table]);
        _fixture.Store(source);

        ISchema migrated = new PostgreSqlMigratedSchema(
            source,
            target,
            _fixture.Connection
        );

        Assert.True(_fixture.SchemaExists(source));
        _ = migrated.Tables;
        Assert.False(_fixture.SchemaExists(source));
        Assert.True(_fixture.SchemaExists(target));
    }

    [Fact]
    public void MigratesOnForeignKeysAccess()
    {
        ITable table = new Table(
            new String("Items"),
            [Column("Id", new IntColumnType())],
            []
        );
        ISchema source = Schema("ForeignKeysAccessSource", [table]);
        ISchema target = Schema("ForeignKeysAccessTarget", [table]);
        _fixture.Store(source);

        _ = new PostgreSqlMigratedSchema(source, target, _fixture.Connection).ForeignKeys;

        Assert.True(_fixture.SchemaExists(target));
    }

    [Fact]
    public void CreatesEmptyTargetWhenSourceIsNotStored()
    {
        ITable table = new Table(
            new String("Items"),
            [Column("Id", new IntColumnType())],
            []
        );
        ISchema source = Schema("NotStoredSource", [table]);
        ISchema target = Schema("NotStoredTarget", [table]);

        _ = new PostgreSqlMigratedSchema(source, target, _fixture.Connection).Name;

        Assert.False(_fixture.SchemaExists(source));
        Assert.Empty(_fixture.Values(target, table, table.Columns.Single()));
    }

    [Fact]
    public void ReturnsTargetMembers()
    {
        IColumn id = Column("Id", new IntColumnType());
        ITable parent = new Table(
            new String("Parent"),
            [id],
            [new Index(new True(), [id])]
        );
        ITable child = new Table(new String("Child"), [id], []);
        ISchema source = Schema("MembersSource", [parent]);
        ISchema target = new Schema(
            new String("MembersTarget"),
            [parent, child],
            [new ForeignKey(child, [id], parent, [id])]
        );

        ISchema migrated = new PostgreSqlMigratedSchema(
            source,
            target,
            _fixture.Connection
        );

        Assert.Equal("MembersTarget", migrated.Name.TextValue);
        Assert.Equal(
            target.Tables.Select(x => new HexString(new TableHash(x)).TextValue),
            migrated.Tables.Select(x => new HexString(new TableHash(x)).TextValue)
        );
        Assert.Equal(
            new HexString(new ForeignKeyHash(target.ForeignKeys.Single())).TextValue,
            new HexString(new ForeignKeyHash(migrated.ForeignKeys.Single())).TextValue
        );
    }

    [Fact]
    public void DropsTableMissingFromTarget()
    {
        IColumn id = Column("Id", new IntColumnType());
        ITable kept = new Table(new String("Kept"), [id], []);
        ITable dropped = new Table(new String("Dropped"), [id], []);
        ISchema source = Schema(nameof(DropsTableMissingFromTarget), [kept, dropped]);
        ISchema target = Schema(nameof(DropsTableMissingFromTarget), [kept]);
        _fixture.Store(source);
        _fixture.Insert(source, kept, "1");

        _ = new PostgreSqlMigratedSchema(source, target, _fixture.Connection).Name;

        Assert.False(_fixture.TableExists(target, dropped));
        Assert.Equal(["1"], _fixture.Values(target, kept, id));
    }

    [Fact]
    public void DropsChangedTableThatIsNotMapped()
    {
        IColumn id = Column("Id", new IntColumnType());
        ITable sourceTable = new Table(new String("Items"), [id], []);
        ITable targetTable = new Table(
            new String("Items"),
            [id, Column("Name", new StringColumnType())],
            []
        );
        ISchema source = Schema(nameof(DropsChangedTableThatIsNotMapped), [sourceTable]);
        ISchema target = Schema(nameof(DropsChangedTableThatIsNotMapped), [targetTable]);
        _fixture.Store(source);
        _fixture.Insert(source, sourceTable, "1");

        _ = new PostgreSqlMigratedSchema(source, target, _fixture.Connection).Name;

        Assert.False(_fixture.TableExists(target, sourceTable));
        Assert.Empty(_fixture.Values(target, targetTable, id));
    }

    [Theory]
    [MemberData(nameof(ColumnTypes), DisableDiscoveryEnumeration = true)]
    public void AddsColumnFilledWithDefaultValue(
        string label,
        IColumnType type,
        string value,
        string dataType
    )
    {
        IColumn id = Column("Id", new IntColumnType());
        IColumn added = Column("Added", type);
        ITable sourceTable = new Table(new String("Items"), [id], []);
        ITable targetTable = new Table(new String("Items"), [id, added], []);
        ISchema source = Schema($"Add{label}", [sourceTable]);
        ISchema target = Schema($"Add{label}", [targetTable]);
        _fixture.Store(source);
        _fixture.Insert(source, sourceTable, "1");

        _ = new PostgreSqlMigratedSchema(
            source,
            target,
            [new TableMapping(sourceTable, targetTable)],
            _fixture.Connection
        ).Name;

        Assert.Equal([value], _fixture.Values(target, targetTable, added));
        Assert.Equal(
            $"{dataType} NO -",
            _fixture.ColumnDescription(target, targetTable, added)
        );
        Assert.Equal(["1"], _fixture.Values(target, targetTable, id));
    }

    [Fact]
    public void DropsColumnMissingFromMappedTable()
    {
        IColumn id = Column("Id", new IntColumnType());
        IColumn name = Column("Name", new StringColumnType());
        ITable sourceTable = new Table(new String("Items"), [id, name], []);
        ITable targetTable = new Table(new String("Items"), [id], []);
        ISchema source = Schema(nameof(DropsColumnMissingFromMappedTable), [sourceTable]);
        ISchema target = Schema(nameof(DropsColumnMissingFromMappedTable), [targetTable]);
        _fixture.Store(source);
        _fixture.Insert(source, sourceTable, "1", "a");

        _ = new PostgreSqlMigratedSchema(
            source,
            target,
            [new TableMapping(sourceTable, targetTable)],
            _fixture.Connection
        ).Name;

        Assert.False(_fixture.ColumnExists(target, targetTable, name));
        Assert.Equal(["1"], _fixture.Values(target, targetTable, id));
    }

    [Fact]
    public void RenamesMappedColumnAndKeepsValues()
    {
        IColumn name = Column("Name", new StringColumnType());
        IColumn title = Column("Title", new StringColumnType());
        ITable sourceTable = new Table(new String("Items"), [name], []);
        ITable targetTable = new Table(new String("Items"), [title], []);
        ISchema source = Schema(nameof(RenamesMappedColumnAndKeepsValues), [sourceTable]);
        ISchema target = Schema(nameof(RenamesMappedColumnAndKeepsValues), [targetTable]);
        _fixture.Store(source);
        _fixture.Insert(source, sourceTable, "a");

        _ = new PostgreSqlMigratedSchema(
            source,
            target,
            [
                new TableMapping(
                    sourceTable,
                    targetTable,
                    [new ColumnMapping(name, title)]
                ),
            ],
            _fixture.Connection
        ).Name;

        Assert.False(_fixture.ColumnExists(target, targetTable, name));
        Assert.Equal(["a"], _fixture.Values(target, targetTable, title));
    }

    [Fact]
    public void ChangesTypeOfMappedColumnAndKeepsValues()
    {
        IColumn number = Column("Count", new IntColumnType());
        IColumn text = Column("Count", new StringColumnType());
        ITable sourceTable = new Table(new String("Items"), [number], []);
        ITable targetTable = new Table(new String("Items"), [text], []);
        ISchema source = Schema(
            nameof(ChangesTypeOfMappedColumnAndKeepsValues),
            [sourceTable]
        );
        ISchema target = Schema(
            nameof(ChangesTypeOfMappedColumnAndKeepsValues),
            [targetTable]
        );
        _fixture.Store(source);
        _fixture.Insert(source, sourceTable, "42");

        _ = new PostgreSqlMigratedSchema(
            source,
            target,
            [
                new TableMapping(
                    sourceTable,
                    targetTable,
                    [new ColumnMapping(number, text)]
                ),
            ],
            _fixture.Connection
        ).Name;

        Assert.Equal("text NO -", _fixture.ColumnDescription(target, targetTable, text));
        Assert.Equal(["42"], _fixture.Values(target, targetTable, text));
    }

    [Fact]
    public void PrefersExplicitColumnMappingOverIdenticalColumn()
    {
        IColumn x = Column("X", new IntColumnType());
        IColumn y = Column("Y", new IntColumnType());
        ITable sourceTable = new Table(new String("Items"), [x, y], []);
        ITable targetTable = new Table(new String("Items"), [x], []);
        ISchema source = Schema("ExplicitColumn", [sourceTable]);
        ISchema target = Schema("ExplicitColumn", [targetTable]);
        _fixture.Store(source);
        _fixture.Insert(source, sourceTable, "1", "2");

        _ = new PostgreSqlMigratedSchema(
            source,
            target,
            [new TableMapping(sourceTable, targetTable, [new ColumnMapping(y, x)])],
            _fixture.Connection
        ).Name;

        Assert.Equal(["2"], _fixture.Values(target, targetTable, x));
    }

    [Fact]
    public void PrefersExplicitTableMappingOverIdenticalTable()
    {
        IColumn id = Column("Id", new IntColumnType());
        ITable a = new Table(new String("A"), [id], []);
        ITable b = new Table(new String("B"), [id], []);
        ISchema source = Schema("ExplicitTable", [a, b]);
        ISchema target = Schema("ExplicitTable", [a]);
        _fixture.Store(source);
        _fixture.Insert(source, a, "1");
        _fixture.Insert(source, b, "2");

        _ = new PostgreSqlMigratedSchema(
            source,
            target,
            [new TableMapping(b, a)],
            _fixture.Connection
        ).Name;

        Assert.Equal(["2"], _fixture.Values(target, a, id));
    }

    [Fact]
    public void DropsIndexMissingFromTarget()
    {
        IColumn id = Column("Id", new IntColumnType());
        ITable sourceTable = new Table(
            new String("Items"),
            [id],
            [new Index(new False(), [id])]
        );
        ITable targetTable = new Table(new String("Items"), [id], []);
        ISchema source = Schema(nameof(DropsIndexMissingFromTarget), [sourceTable]);
        ISchema target = Schema(nameof(DropsIndexMissingFromTarget), [targetTable]);
        _fixture.Store(source);
        _fixture.Insert(source, sourceTable, "1");

        _ = new PostgreSqlMigratedSchema(
            source,
            target,
            [new TableMapping(sourceTable, targetTable)],
            _fixture.Connection
        ).Name;

        Assert.Equal(0, _fixture.IndexCount(target));
        Assert.Equal(["1"], _fixture.Values(target, targetTable, id));
    }

    [Fact]
    public void KeepsIndexPresentInTarget()
    {
        IColumn id = Column("Id", new IntColumnType());
        ITable table = new Table(
            new String("Items"),
            [id],
            [new Index(new False(), [id])]
        );
        ISchema source = Schema("KeepIndexSource", [table]);
        ISchema target = Schema("KeepIndexTarget", [table]);
        _fixture.Store(source);

        _ = new PostgreSqlMigratedSchema(source, target, _fixture.Connection).Name;

        Assert.Equal(1, _fixture.IndexCount(target));
    }

    [Fact]
    public void DropsForeignKeyMissingFromTarget()
    {
        IColumn id = Column("Id", new IntColumnType());
        ITable parent = new Table(
            new String("Parent"),
            [id],
            [new Index(new True(), [id])]
        );
        ITable child = new Table(new String("Child"), [id], []);
        ISchema source = new Schema(
            new String(nameof(DropsForeignKeyMissingFromTarget)),
            [parent, child],
            [new ForeignKey(child, [id], parent, [id])]
        );
        ISchema target = Schema(
            nameof(DropsForeignKeyMissingFromTarget),
            [parent, child]
        );
        _fixture.Store(source);

        _ = new PostgreSqlMigratedSchema(source, target, _fixture.Connection).Name;

        Assert.Equal(0, _fixture.ForeignKeyCount(target));
        Assert.True(_fixture.TableExists(target, child));
    }

    [Fact]
    public void KeepsForeignKeyPresentInTarget()
    {
        IColumn id = Column("Id", new IntColumnType());
        ITable parent = new Table(
            new String("Parent"),
            [id],
            [new Index(new True(), [id])]
        );
        ITable child = new Table(new String("Child"), [id], []);
        IForeignKey foreignKey = new ForeignKey(child, [id], parent, [id]);
        ISchema source = new Schema(
            new String("KeepForeignKeySource"),
            [parent, child],
            [foreignKey]
        );
        ISchema target = new Schema(
            new String("KeepForeignKeyTarget"),
            [parent, child],
            [foreignKey]
        );
        _fixture.Store(source);

        _ = new PostgreSqlMigratedSchema(source, target, _fixture.Connection).Name;

        Assert.Equal(1, _fixture.ForeignKeyCount(target));
    }

    [Fact]
    public void RecreatesForeignKeyOfChangedTable()
    {
        IColumn id = Column("Id", new IntColumnType());
        IColumn note = Column("Note", new StringColumnType());
        ITable parent = new Table(
            new String("Parent"),
            [id],
            [new Index(new True(), [id])]
        );
        ITable sourceChild = new Table(new String("Child"), [id], []);
        ITable targetChild = new Table(new String("Child"), [id, note], []);
        ISchema source = new Schema(
            new String(nameof(RecreatesForeignKeyOfChangedTable)),
            [parent, sourceChild],
            [new ForeignKey(sourceChild, [id], parent, [id])]
        );
        ISchema target = new Schema(
            new String(nameof(RecreatesForeignKeyOfChangedTable)),
            [parent, targetChild],
            [new ForeignKey(targetChild, [id], parent, [id])]
        );
        _fixture.Store(source);
        _fixture.Insert(source, parent, "1");
        _fixture.Insert(source, sourceChild, "1");

        _ = new PostgreSqlMigratedSchema(
            source,
            target,
            [new TableMapping(sourceChild, targetChild)],
            _fixture.Connection
        ).Name;

        Assert.Equal(1, _fixture.ForeignKeyCount(target));
        Assert.Equal(["1"], _fixture.Values(target, targetChild, id));
    }

    [Fact]
    public void CreatesTablesAndIndexesNewInTarget()
    {
        IColumn id = Column("Id", new IntColumnType());
        IColumn code = Column("Code", new StringColumnType());
        ITable sourceTable = new Table(new String("Items"), [id], []);
        ITable targetTable = new Table(
            new String("Items"),
            [id, code],
            [new Index(new False(), [code])]
        );
        ITable tags = new Table(new String("Tags"), [code], []);
        ISchema source = Schema(
            nameof(CreatesTablesAndIndexesNewInTarget),
            [sourceTable]
        );
        ISchema target = Schema(
            nameof(CreatesTablesAndIndexesNewInTarget),
            [targetTable, tags]
        );
        _fixture.Store(source);
        _fixture.Insert(source, sourceTable, "1");

        _ = new PostgreSqlMigratedSchema(
            source,
            target,
            [new TableMapping(sourceTable, targetTable)],
            _fixture.Connection
        ).Name;

        Assert.True(_fixture.TableExists(target, tags));
        Assert.Equal(1, _fixture.IndexCount(target));
        Assert.Equal(["1"], _fixture.Values(target, targetTable, id));
    }

    [Fact]
    public void MigratedRowsAreReadableThroughQuerying()
    {
        IColumn id = Column("Id", new IntColumnType());
        IColumn wideId = Column("Id", new LongColumnType());
        IColumn name = Column("Name", new StringColumnType());
        IColumn title = Column("Title", new StringColumnType());
        IColumn flag = Column("Flag", new BoolColumnType());
        ITable sourceTable = new Table(new String("Items"), [id, name], []);
        ITable targetTable = new Table(new String("Items"), [wideId, title, flag], []);
        ISchema source = Schema(
            nameof(MigratedRowsAreReadableThroughQuerying),
            [sourceTable]
        );
        ISchema target = Schema(
            nameof(MigratedRowsAreReadableThroughQuerying),
            [targetTable]
        );
        _fixture.Store(source);
        _fixture.Insert(source, sourceTable, "1", "a");
        _fixture.Insert(source, sourceTable, "2", "b");

        ISchema migrated = new PostgreSqlMigratedSchema(
            source,
            target,
            [
                new TableMapping(
                    sourceTable,
                    targetTable,
                    [new ColumnMapping(id, wideId), new ColumnMapping(name, title)]
                ),
            ],
            _fixture.Connection
        );
        IEnumerable<IRow> rows = new PostgreSqlStoredSchemaDataSet(
            migrated,
            _fixture.Connection
        )[targetTable];

        Assert.Equal(
            ["1 a False", "2 b False"],
            rows.Select(x =>
                    $"{x.Cells[wideId].Value.TextValue} "
                    + $"{x.Cells[title].Value.TextValue} "
                    + x.Cells[flag].Value.TextValue
                )
                .Order()
        );
    }

    [Fact]
    public void ThrowsWhenMappedSourceTableIsMissing()
    {
        ITable table = new Table(new String("Items"), [], []);
        ITable other = new Table(new String("Other"), [], []);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Migrated([table], [table], [new TableMapping(other, table)]).Name
        );

        Assert.Equal("Mapped table is missing from its schema.", exception.Message);
    }

    [Fact]
    public void ThrowsWhenMappedTargetTableIsMissing()
    {
        ITable table = new Table(new String("Items"), [], []);
        ITable other = new Table(new String("Other"), [], []);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Migrated([table], [table], [new TableMapping(table, other)]).Name
        );

        Assert.Equal("Mapped table is missing from its schema.", exception.Message);
    }

    [Fact]
    public void ThrowsWhenSourceTableIsMappedTwice()
    {
        ITable table = new Table(new String("Items"), [], []);
        ITable first = new Table(new String("First"), [], []);
        ITable second = new Table(new String("Second"), [], []);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Migrated(
                [table],
                [first, second],
                [new TableMapping(table, first), new TableMapping(table, second)]
            ).Name
        );

        Assert.Equal("Table is mapped more than once.", exception.Message);
    }

    [Fact]
    public void ThrowsWhenTargetTableIsMappedTwice()
    {
        ITable table = new Table(new String("Items"), [], []);
        ITable first = new Table(new String("First"), [], []);
        ITable second = new Table(new String("Second"), [], []);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Migrated(
                [first, second],
                [table],
                [new TableMapping(first, table), new TableMapping(second, table)]
            ).Name
        );

        Assert.Equal("Table is mapped more than once.", exception.Message);
    }

    [Fact]
    public void ThrowsWhenMappedSourceColumnIsMissing()
    {
        IColumn id = Column("Id", new IntColumnType());
        IColumn other = Column("Other", new IntColumnType());
        ITable sourceTable = new Table(new String("Source"), [id], []);
        ITable targetTable = new Table(new String("Target"), [id], []);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Migrated(
                [sourceTable],
                [targetTable],
                [
                    new TableMapping(
                        sourceTable,
                        targetTable,
                        [new ColumnMapping(other, id)]
                    ),
                ]
            ).Name
        );

        Assert.Equal("Mapped column is missing from its table.", exception.Message);
    }

    [Fact]
    public void ThrowsWhenMappedTargetColumnIsMissing()
    {
        IColumn id = Column("Id", new IntColumnType());
        IColumn other = Column("Other", new IntColumnType());
        ITable sourceTable = new Table(new String("Source"), [id], []);
        ITable targetTable = new Table(new String("Target"), [id], []);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Migrated(
                [sourceTable],
                [targetTable],
                [
                    new TableMapping(
                        sourceTable,
                        targetTable,
                        [new ColumnMapping(id, other)]
                    ),
                ]
            ).Name
        );

        Assert.Equal("Mapped column is missing from its table.", exception.Message);
    }

    [Fact]
    public void ThrowsWhenSourceColumnIsMappedTwice()
    {
        IColumn id = Column("Id", new IntColumnType());
        IColumn first = Column("First", new IntColumnType());
        IColumn second = Column("Second", new IntColumnType());
        ITable sourceTable = new Table(new String("Source"), [id], []);
        ITable targetTable = new Table(new String("Target"), [first, second], []);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Migrated(
                [sourceTable],
                [targetTable],
                [
                    new TableMapping(
                        sourceTable,
                        targetTable,
                        [new ColumnMapping(id, first), new ColumnMapping(id, second)]
                    ),
                ]
            ).Name
        );

        Assert.Equal("Column is mapped more than once.", exception.Message);
    }

    [Fact]
    public void ThrowsWhenTargetColumnIsMappedTwice()
    {
        IColumn id = Column("Id", new IntColumnType());
        IColumn first = Column("First", new IntColumnType());
        IColumn second = Column("Second", new IntColumnType());
        ITable sourceTable = new Table(new String("Source"), [first, second], []);
        ITable targetTable = new Table(new String("Target"), [id], []);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Migrated(
                [sourceTable],
                [targetTable],
                [
                    new TableMapping(
                        sourceTable,
                        targetTable,
                        [new ColumnMapping(first, id), new ColumnMapping(second, id)]
                    ),
                ]
            ).Name
        );

        Assert.Equal("Column is mapped more than once.", exception.Message);
    }

    private static IColumn Column(string name, IColumnType type)
    {
        return new Column(new String(name), type);
    }

    private static ISchema Schema(string name, IEnumerable<ITable> tables)
    {
        return new Schema(new String(name), tables, []);
    }

    private ISchema Migrated(
        IEnumerable<ITable> sourceTables,
        IEnumerable<ITable> targetTables,
        IEnumerable<ITableMapping> mappings
    )
    {
        return new PostgreSqlMigratedSchema(
            Schema("InvalidSource", sourceTables),
            Schema("InvalidTarget", targetTables),
            mappings,
            _fixture.Connection
        );
    }
}
