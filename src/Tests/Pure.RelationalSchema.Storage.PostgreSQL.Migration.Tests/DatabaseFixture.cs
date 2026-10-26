using System.Data;
using Npgsql;
using Pure.HashCodes.Abstractions;
using Pure.Primitives.String.Operations;
using Pure.RelationalSchema.Abstractions.Column;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.HashCodes;
using Pure.RelationalSchema.Storage.PostgreSQL.Creation;
using Testcontainers.PostgreSql;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration.Tests;

public sealed record DatabaseFixture : IDisposable
{
    private const int MaxIdentifierLength = 63;

    private readonly PostgreSqlContainer _postgres;

    public DatabaseFixture()
    {
        _postgres = new PostgreSqlBuilder("postgres")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _postgres.StartAsync().GetAwaiter().GetResult();

        Connection = new NpgsqlConnection(_postgres.GetConnectionString());

        Connection.Open();
    }

    public IDbConnection Connection { get; }

    public void Store(ISchema schema)
    {
        _ = new PostgreSqlCreatedSchema(schema, Connection).Name;
    }

    public void Insert(ISchema schema, ITable table, params string[] values)
    {
        Execute(
            $"INSERT INTO {QualifiedName(schema, table)} VALUES ("
                + string.Join(", ", values.Select(x => $"'{x}'"))
                + ");"
        );
    }

    public IReadOnlyList<string> Values(ISchema schema, ITable table, IColumn column)
    {
        using IDbCommand command = Connection.CreateCommand();
        command.CommandText =
            $"SELECT \"{Name(new ColumnHash(column))}\"::text "
            + $"FROM {QualifiedName(schema, table)} ORDER BY 1;";
        using IDataReader reader = command.ExecuteReader();
        List<string> values = [];
        while (reader.Read())
        {
            values.Add(reader.GetString(0));
        }
        return values;
    }

    public bool SchemaExists(ISchema schema)
    {
        return Exists(
            $"SELECT 1 FROM pg_namespace WHERE nspname = '{Name(new SchemaHash(schema))}'"
        );
    }

    public bool TableExists(ISchema schema, ITable table)
    {
        return Exists(
            "SELECT 1 FROM information_schema.tables "
                + $"WHERE table_schema = '{Name(new SchemaHash(schema))}' "
                + $"AND table_name = '{Name(new TableHash(table))}'"
        );
    }

    public bool ColumnExists(ISchema schema, ITable table, IColumn column)
    {
        return Exists(ColumnQuery("1", schema, table, column));
    }

    public string ColumnDescription(ISchema schema, ITable table, IColumn column)
    {
        using IDbCommand command = Connection.CreateCommand();
        command.CommandText = ColumnQuery(
            "data_type || ' ' || is_nullable || ' ' || coalesce(column_default, '-')",
            schema,
            table,
            column
        );
        return (string)command.ExecuteScalar()!;
    }

    public long IndexCount(ISchema schema)
    {
        return Count(
            "SELECT count(*) FROM pg_indexes "
                + $"WHERE schemaname = '{Name(new SchemaHash(schema))}'"
        );
    }

    public long ForeignKeyCount(ISchema schema)
    {
        return Count(
            "SELECT count(*) FROM information_schema.table_constraints "
                + $"WHERE constraint_schema = '{Name(new SchemaHash(schema))}' "
                + "AND constraint_type = 'FOREIGN KEY'"
        );
    }

    public void Dispose()
    {
        Connection.Dispose();
        _postgres.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private static string Name(IDeterminedHash hash)
    {
        return new HexString(hash).TextValue[..MaxIdentifierLength];
    }

    private static string QualifiedName(ISchema schema, ITable table)
    {
        return $"\"{Name(new SchemaHash(schema))}\".\"{Name(new TableHash(table))}\"";
    }

    private static string ColumnQuery(
        string selection,
        ISchema schema,
        ITable table,
        IColumn column
    )
    {
        return $"SELECT {selection} FROM information_schema.columns "
            + $"WHERE table_schema = '{Name(new SchemaHash(schema))}' "
            + $"AND table_name = '{Name(new TableHash(table))}' "
            + $"AND column_name = '{Name(new ColumnHash(column))}'";
    }

    private bool Exists(string query)
    {
        using IDbCommand command = Connection.CreateCommand();
        command.CommandText = query;
        return command.ExecuteScalar() is not null;
    }

    private long Count(string query)
    {
        using IDbCommand command = Connection.CreateCommand();
        command.CommandText = query;
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private void Execute(string sql)
    {
        using IDbCommand command = Connection.CreateCommand();
        command.CommandText = sql;
        _ = command.ExecuteNonQuery();
    }
}
