using Pure.Primitives.Abstractions.String;
using Pure.RelationalSchema.Abstractions.ForeignKey;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.Migration.Abstractions.SchemaMigration;
using Pure.RelationalSchema.Migration.Abstractions.TableMapping;
using Pure.RelationalSchema.Storage.PostgreSQL.Creation;
using IDbConnection = System.Data.IDbConnection;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

public sealed record PostgreSqlMigratedSchema : ISchema
{
    private readonly ISchema _schema;

    public PostgreSqlMigratedSchema(
        ISchema source,
        ISchema target,
        IDbConnection connection
    )
        : this(new SchemaMigration(source, target), connection)
    { }

    public PostgreSqlMigratedSchema(
        ISchema source,
        ISchema target,
        IEnumerable<ITableMapping> tables,
        IDbConnection connection
    )
        : this(new SchemaMigration(source, target, tables), connection)
    { }

    public PostgreSqlMigratedSchema(ISchemaMigration migration, IDbConnection connection)
        : this(
            new PostgreSqlCreatedSchema(
                new PostgreSqlReshapedSchema(migration, connection),
                connection
            )
        )
    { }

    private PostgreSqlMigratedSchema(ISchema schema)
    {
        _schema = schema;
    }

    public IString Name => _schema.Name;

    public IEnumerable<ITable> Tables => _schema.Tables;

    public IEnumerable<IForeignKey> ForeignKeys => _schema.ForeignKeys;
}
