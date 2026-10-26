using System.Data;
using Pure.Primitives.Abstractions.String;
using Pure.RelationalSchema.Abstractions.ForeignKey;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.Storage.PostgreSQL.Creation;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

public sealed record PostgreSqlMigratedSchema : ISchema
{
    private readonly ISchema _schema;

    public PostgreSqlMigratedSchema(
        ISchema source,
        ISchema target,
        IDbConnection connection
    )
        : this(source, target, [], connection)
    { }

    public PostgreSqlMigratedSchema(
        ISchema source,
        ISchema target,
        IEnumerable<ITableMapping> tables,
        IDbConnection connection
    )
        : this(
            new PostgreSqlCreatedSchema(
                new PostgreSqlReshapedSchema(source, target, tables, connection),
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
