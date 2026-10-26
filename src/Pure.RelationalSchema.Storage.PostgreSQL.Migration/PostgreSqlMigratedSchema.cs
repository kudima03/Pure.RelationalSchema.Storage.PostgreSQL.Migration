using System.Data;
using Pure.Primitives.Abstractions.Bool;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.Bool;
using Pure.RelationalSchema.Abstractions.ForeignKey;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.Abstractions.Table;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

public sealed record PostgreSqlMigratedSchema : ISchema
{
    private readonly ISchema _target;

    private readonly Lazy<IBool> _migrated;

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
    {
        _target = target;
        _migrated = new Lazy<IBool>(() =>
        {
            using IDbCommand command = connection.CreateCommand();
            IString migrationScript = new SchemaMigrationStatement(
                source,
                target,
                new CompleteTableMappings(source, target, tables)
            );
            command.CommandText = migrationScript.TextValue;
            _ = command.ExecuteNonQuery();
            return new True();
        });
    }

    public IString Name =>
        _migrated.Value.BoolValue ? _target.Name : throw new ArgumentException();

    public IEnumerable<ITable> Tables =>
        _migrated.Value.BoolValue ? _target.Tables : throw new ArgumentException();

    public IEnumerable<IForeignKey> ForeignKeys =>
        _migrated.Value.BoolValue ? _target.ForeignKeys : throw new ArgumentException();
}
