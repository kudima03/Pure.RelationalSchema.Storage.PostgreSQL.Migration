using System.Data;
using Pure.Primitives.Abstractions.Bool;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.Bool;
using Pure.RelationalSchema.Abstractions.ForeignKey;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.Migration.Abstractions.SchemaMigration;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

internal sealed record PostgreSqlReshapedSchema : ISchema
{
    private readonly ISchemaMigration _migration;

    private readonly Lazy<IBool> _migrated;

    public PostgreSqlReshapedSchema(ISchemaMigration migration, IDbConnection connection)
    {
        _migration = migration;
        _migrated = new Lazy<IBool>(() =>
        {
            using IDbCommand command = connection.CreateCommand();
            IString migrationScript = new SchemaMigrationStatement(
                migration.Source,
                migration.Target,
                new CompleteTableMappings(
                    migration.Source,
                    migration.Target,
                    migration.Tables
                )
            );
            command.CommandText = migrationScript.TextValue;
            _ = command.ExecuteNonQuery();
            return new True();
        });
    }

    public IString Name =>
        _migrated.Value.BoolValue
            ? _migration.Target.Name
            : throw new ArgumentException();

    public IEnumerable<ITable> Tables =>
        _migrated.Value.BoolValue
            ? _migration.Target.Tables
            : throw new ArgumentException();

    public IEnumerable<IForeignKey> ForeignKeys =>
        _migrated.Value.BoolValue
            ? _migration.Target.ForeignKeys
            : throw new ArgumentException();
}
