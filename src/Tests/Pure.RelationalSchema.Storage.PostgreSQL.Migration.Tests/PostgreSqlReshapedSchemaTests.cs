using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.ColumnType;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration.Tests;

using Column = Column.Column;
using Schema = Schema.Schema;
using Table = Table.Table;

public sealed record PostgreSqlReshapedSchemaTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public PostgreSqlReshapedSchemaTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void ReshapesOnNameAccess()
    {
        ISchema source = Stored("ReshapedNameSource");
        ISchema target = Renamed(source, "ReshapedNameTarget");

        _ = new PostgreSqlReshapedSchema(source, target, [], _fixture.Connection).Name;

        Assert.False(_fixture.SchemaExists(source));
        Assert.True(_fixture.SchemaExists(target));
    }

    [Fact]
    public void ReshapesOnTablesAccess()
    {
        ISchema source = Stored("ReshapedTablesSource");
        ISchema target = Renamed(source, "ReshapedTablesTarget");

        _ = new PostgreSqlReshapedSchema(source, target, [], _fixture.Connection).Tables;

        Assert.False(_fixture.SchemaExists(source));
        Assert.True(_fixture.SchemaExists(target));
    }

    [Fact]
    public void ReshapesOnForeignKeysAccess()
    {
        ISchema source = Stored("ReshapedForeignKeysSource");
        ISchema target = Renamed(source, "ReshapedForeignKeysTarget");

        _ = new PostgreSqlReshapedSchema(
            source,
            target,
            [],
            _fixture.Connection
        ).ForeignKeys;

        Assert.False(_fixture.SchemaExists(source));
        Assert.True(_fixture.SchemaExists(target));
    }

    private static ISchema Renamed(ISchema schema, string name)
    {
        return new Schema(new String(name), schema.Tables, schema.ForeignKeys);
    }

    private ISchema Stored(string name)
    {
        ITable table = new Table(
            new String("Items"),
            [new Column(new String("Id"), new IntColumnType())],
            []
        );
        ISchema schema = new Schema(new String(name), [table], []);
        _fixture.Store(schema);
        return schema;
    }
}
