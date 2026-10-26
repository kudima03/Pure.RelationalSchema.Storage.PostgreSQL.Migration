using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.Bool;
using Pure.Primitives.String.Operations;
using Pure.RelationalSchema.Abstractions.Column;
using Pure.RelationalSchema.Abstractions.ColumnType;
using Pure.RelationalSchema.Abstractions.ForeignKey;
using Pure.RelationalSchema.Abstractions.Index;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.ColumnType;
using Pure.RelationalSchema.HashCodes;
using Pure.RelationalSchema.Migration.Abstractions.ColumnMapping;
using Pure.RelationalSchema.Migration.Abstractions.TableMapping;
using Pure.RelationalSchema.Storage.PostgreSQL.Migration.Tests.Fakes;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration.Tests;

using Column = Column.Column;
using ForeignKey = ForeignKey.ForeignKey;
using Index = Index.Index;
using Schema = Schema.Schema;
using Table = Table.Table;

public sealed record StatementsTests
{
    private static readonly IColumn Id = new Column(
        new String("Id"),
        new IntColumnType()
    );

    private static readonly IIndex IdIndex = new Index(new True(), [Id]);

    private static readonly ITable Items = new Table(
        new String("Items"),
        [Id],
        [IdIndex]
    );

    private static readonly IForeignKey ItemsToItself = new ForeignKey(
        Items,
        [Id],
        Items,
        [Id]
    );

    private static readonly ISchema Store = new Schema(
        new String("Store"),
        [Items],
        [ItemsToItself]
    );

    public static TheoryData<IColumnType, string, string> ColumnTypes =>
        new TheoryData<IColumnType, string, string>
        {
            { new IntColumnType(), "integer", "0" },
            { new BoolColumnType(), "boolean", "false" },
            { new LongColumnType(), "bigint", "0" },
            { new UShortColumnType(), "integer", "0" },
            { new UIntColumnType(), "bigint", "0" },
            { new ULongColumnType(), "numeric", "0" },
            { new StringColumnType(), "text", "" },
            { new DateColumnType(), "date", "0001-01-01" },
            { new DateTimeColumnType(), "timestamp", "0001-01-01 00:00:00" },
            { new TimeColumnType(), "time", "00:00:00" },
            { new DeterminedHashColumnType(), "bytea", "" },
            { new DoubleColumnType(), "double precision", "0" },
            { new FloatColumnType(), "real", "0" },
            { new UuidColumnType(), "uuid", "00000000-0000-0000-0000-000000000000" },
        };

    public static TheoryData<IString> Strings =>
        [
            new SchemaName(Store),
            new TableName(Items),
            new ColumnName(Id),
            new ForeignKeyName(ItemsToItself),
            new IndexName(Items, IdIndex),
            new QualifiedName(new String("s"), new String("t")),
            new ForeignKeyDropStatement(
                new String("s"),
                new String("t"),
                new String("f")
            ),
            new IndexDropStatement(new String("s"), new String("i")),
            new TableDropStatement(new String("s"), new String("t")),
            new ColumnDropStatement(new String("s"), new String("t"), new String("c")),
            new ColumnRenameStatement(
                new String("s"),
                new String("t"),
                new String("a"),
                new String("b")
            ),
            new ColumnTypeChangeStatement(
                new String("s"),
                new String("t"),
                new String("c"),
                new String("bigint")
            ),
            new ColumnAdditionStatement(
                new String("s"),
                new String("t"),
                new String("c"),
                new String("integer"),
                new String("0")
            ),
            new TableRenameStatement(new String("s"), new String("a"), new String("b")),
            new SchemaRenameStatement(new String("a"), new String("b")),
            new MigrationBlockStatement(new String("s"), []),
            new SchemaMigrationStatement(Store, Store, []),
            new PostgreSqlColumnTypeName(new IntColumnType()),
            new PostgreSqlDefaultValue(new IntColumnType()),
            new TrimmedHash(new HexString(new SchemaHash(Store))),
        ];

    [Fact]
    public void SchemaNameIsTrimmedSchemaHash()
    {
        Assert.Equal(
            Trimmed(new HexString(new SchemaHash(Store))),
            new SchemaName(Store).TextValue
        );
    }

    [Fact]
    public void TableNameIsTrimmedTableHash()
    {
        Assert.Equal(
            Trimmed(new HexString(new TableHash(Items))),
            new TableName(Items).TextValue
        );
    }

    [Fact]
    public void ColumnNameIsTrimmedColumnHash()
    {
        Assert.Equal(
            Trimmed(new HexString(new ColumnHash(Id))),
            new ColumnName(Id).TextValue
        );
    }

    [Fact]
    public void ForeignKeyNameIsTrimmedForeignKeyHash()
    {
        Assert.Equal(
            Trimmed(new HexString(new ForeignKeyHash(ItemsToItself))),
            new ForeignKeyName(ItemsToItself).TextValue
        );
    }

    [Fact]
    public void IndexNameJoinsTableAndIndexHashPrefixes()
    {
        Assert.Equal(
            new HexString(new TableHash(Items)).TextValue[..31]
                + "."
                + new HexString(new IndexHash(IdIndex)).TextValue[..31],
            new IndexName(Items, IdIndex).TextValue
        );
    }

    [Fact]
    public void QualifiedNameQuotesBothParts()
    {
        Assert.Equal(
            "\"s\".\"t\"",
            new QualifiedName(new String("s"), new String("t")).TextValue
        );
    }

    [Fact]
    public void ForeignKeyDropStatementDropsConstraintOfReferencingTable()
    {
        Assert.Equal(
            "ALTER TABLE \"s\".\"t\" DROP CONSTRAINT IF EXISTS \"f\";",
            new ForeignKeyDropStatement(
                new String("s"),
                new String("t"),
                new String("f")
            ).TextValue
        );
    }

    [Fact]
    public void IndexDropStatementDropsQualifiedIndex()
    {
        Assert.Equal(
            "DROP INDEX IF EXISTS \"s\".\"i\";",
            new IndexDropStatement(new String("s"), new String("i")).TextValue
        );
    }

    [Fact]
    public void TableDropStatementDropsQualifiedTable()
    {
        Assert.Equal(
            "DROP TABLE IF EXISTS \"s\".\"t\";",
            new TableDropStatement(new String("s"), new String("t")).TextValue
        );
    }

    [Fact]
    public void ColumnDropStatementDropsColumn()
    {
        Assert.Equal(
            "ALTER TABLE \"s\".\"t\" DROP COLUMN IF EXISTS \"c\";",
            new ColumnDropStatement(
                new String("s"),
                new String("t"),
                new String("c")
            ).TextValue
        );
    }

    [Fact]
    public void ColumnRenameStatementRenamesColumn()
    {
        Assert.Equal(
            "ALTER TABLE \"s\".\"t\" RENAME COLUMN \"a\" TO \"b\";",
            new ColumnRenameStatement(
                new String("s"),
                new String("t"),
                new String("a"),
                new String("b")
            ).TextValue
        );
    }

    [Fact]
    public void ColumnTypeChangeStatementCastsExistingValues()
    {
        Assert.Equal(
            "ALTER TABLE \"s\".\"t\" ALTER COLUMN \"c\" TYPE bigint USING \"c\"::bigint;",
            new ColumnTypeChangeStatement(
                new String("s"),
                new String("t"),
                new String("c"),
                new String("bigint")
            ).TextValue
        );
    }

    [Fact]
    public void ColumnAdditionStatementFillsDefaultAndThenDropsIt()
    {
        Assert.Equal(
            "ALTER TABLE \"s\".\"t\" ADD COLUMN \"c\" integer NOT NULL DEFAULT '0';"
                + Environment.NewLine
                + "ALTER TABLE \"s\".\"t\" ALTER COLUMN \"c\" DROP DEFAULT;",
            new ColumnAdditionStatement(
                new String("s"),
                new String("t"),
                new String("c"),
                new String("integer"),
                new String("0")
            ).TextValue
        );
    }

    [Fact]
    public void TableRenameStatementRenamesTable()
    {
        Assert.Equal(
            "ALTER TABLE \"s\".\"a\" RENAME TO \"b\";",
            new TableRenameStatement(
                new String("s"),
                new String("a"),
                new String("b")
            ).TextValue
        );
    }

    [Fact]
    public void SchemaRenameStatementRenamesSchema()
    {
        Assert.Equal(
            "ALTER SCHEMA \"a\" RENAME TO \"b\";",
            new SchemaRenameStatement(new String("a"), new String("b")).TextValue
        );
    }

    [Fact]
    public void MigrationBlockStatementRunsStatementsOnlyWhenSchemaExists()
    {
        Assert.Equal(
            string.Join(
                Environment.NewLine,
                "DO $$",
                "BEGIN",
                "IF EXISTS (SELECT 1 FROM pg_catalog.pg_namespace"
                    + " WHERE nspname = 's') THEN",
                "A;",
                "B;",
                "END IF;",
                "END$$;"
            ),
            new MigrationBlockStatement(
                new String("s"),
                [new String("A;"), new String("B;")]
            ).TextValue
        );
    }

    [Fact]
    public void SchemaMigrationStatementHasNoStatementsForUnchangedSchema()
    {
        Assert.Equal(
            new MigrationBlockStatement(new SchemaName(Store), []).TextValue,
            new SchemaMigrationStatement(
                Store,
                Store,
                new CompleteTableMappings(Store, Store, [])
            ).TextValue
        );
    }

    [Fact]
    public void TableMigrationStatementsKeepTypeSharedByPostgreSql()
    {
        IColumn small = new Column(new String("Count"), new UShortColumnType());
        IColumn wide = new Column(new String("Count"), new IntColumnType());
        ITable source = new Table(new String("Items"), [small], []);
        ITable target = new Table(new String("Items"), [wide], []);

        Assert.Equal(
            [
                new ColumnRenameStatement(
                    new String("s"),
                    new TableName(source),
                    new ColumnName(small),
                    new ColumnName(wide)
                ).TextValue,
                new TableRenameStatement(
                    new String("s"),
                    new TableName(source),
                    new TableName(target)
                ).TextValue,
            ],
            new TableMigrationStatements(
                new String("s"),
                new TableMapping(source, target, [new ColumnMapping(small, wide)])
            ).Select(x => x.TextValue)
        );
    }

    [Theory]
    [MemberData(nameof(ColumnTypes), DisableDiscoveryEnumeration = true)]
    public void PostgreSqlColumnTypeNameMapsColumnType(
        IColumnType type,
        string name,
        string _
    )
    {
        Assert.Equal(name, new PostgreSqlColumnTypeName(type).TextValue);
    }

    [Theory]
    [MemberData(nameof(ColumnTypes), DisableDiscoveryEnumeration = true)]
    public void PostgreSqlDefaultValueMapsColumnType(
        IColumnType type,
        string _,
        string value
    )
    {
        Assert.Equal(value, new PostgreSqlDefaultValue(type).TextValue);
    }

    [Theory]
    [MemberData(nameof(Strings), DisableDiscoveryEnumeration = true)]
    public void EnumeratesCharactersOfText(IString text)
    {
        Assert.Equal(text.TextValue, Text(text));
    }

    [Theory]
    [MemberData(nameof(Strings), DisableDiscoveryEnumeration = true)]
    public void EnumeratesCharactersOfTextNonGenerically(IString text)
    {
        Assert.Equal(text.TextValue, Text(new NonGenericItems(text).Cast<IChar>()));
    }

    [Fact]
    public void EnumeratesTableMigrationStatementsNonGenerically()
    {
        _ = Assert.Single(
            new NonGenericItems(
                new TableMigrationStatements(
                    new String("s"),
                    new CompleteTableMapping(
                        new TableMapping(
                            Items,
                            new Table(new String("Renamed"), [Id], [IdIndex])
                        )
                    )
                )
            )
        );
    }

    [Fact]
    public void EnumeratesCompleteTableMappingsNonGenerically()
    {
        _ = Assert.Single(
            new NonGenericItems(new CompleteTableMappings(Store, Store, []))
        );
    }

    [Fact]
    public void TableMappingWithoutColumnsHasNoColumnMappings()
    {
        Assert.Empty(new TableMapping(Items, Items).Columns);
    }

    [Fact]
    public void ColumnMappingKeepsSourceAndTarget()
    {
        IColumn other = new Column(new String("Other"), new IntColumnType());
        IColumnMapping mapping = new ColumnMapping(Id, other);

        Assert.Same(Id, mapping.Source);
        Assert.Same(other, mapping.Target);
    }

    [Fact]
    public void CompleteTableMappingKeepsSourceAndTarget()
    {
        ITable other = new Table(new String("Other"), [Id], []);
        ITableMapping mapping = new CompleteTableMapping(new TableMapping(Items, other));

        Assert.Same(Items, mapping.Source);
        Assert.Same(other, mapping.Target);
    }

    private static string Trimmed(IString hash)
    {
        return hash.TextValue[..63];
    }

    private static string Text(IEnumerable<IChar> chars)
    {
        return new string([.. chars.Select(x => x.CharValue)]);
    }
}
