using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.RelationalSchema.Abstractions.Schema;
using Char = Pure.Primitives.Char.Char;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

internal sealed record SchemaMigrationStatement : IString
{
    private readonly ISchema _source;

    private readonly ISchema _target;

    private readonly IEnumerable<ITableMapping> _mappings;

    public SchemaMigrationStatement(
        ISchema source,
        ISchema target,
        IEnumerable<ITableMapping> mappings
    )
    {
        _source = source;
        _target = target;
        _mappings = mappings;
    }

    public string TextValue =>
        new MigrationBlockStatement(new SchemaName(_source), Statements()).TextValue;

    public IEnumerator<IChar> GetEnumerator()
    {
        return TextValue.Select(x => new Char(x)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private IEnumerable<IString> Statements()
    {
        IString sourceName = new SchemaName(_source);
        IString targetName = new SchemaName(_target);
        IReadOnlyCollection<ITableMapping> mappings = [.. _mappings];
        IReadOnlyCollection<string> mappedTables =
        [
            .. mappings.Select(x => new TableName(x.Source).TextValue),
        ];
        IReadOnlyCollection<string> targetForeignKeys =
        [
            .. _target.ForeignKeys.Select(x => new ForeignKeyName(x).TextValue),
        ];
        IReadOnlyCollection<string> targetIndexes =
        [
            .. _target.Tables.SelectMany(table =>
                table.Indexes.Select(index => new IndexName(table, index).TextValue)
            ),
        ];
        return
        [
            .. _source
                .ForeignKeys.Where(x =>
                    !targetForeignKeys.Contains(new ForeignKeyName(x).TextValue)
                )
                .Select(x => new ForeignKeyDropStatement(
                    sourceName,
                    new TableName(x.ReferencingTable),
                    new ForeignKeyName(x)
                )),
            .. _source
                .Tables.SelectMany(table =>
                    table.Indexes.Select(index => new IndexName(table, index))
                )
                .Where(x => !targetIndexes.Contains(x.TextValue))
                .Select(x => new IndexDropStatement(sourceName, x)),
            .. _source
                .Tables.Where(x => !mappedTables.Contains(new TableName(x).TextValue))
                .Select(x => new TableDropStatement(sourceName, new TableName(x))),
            .. mappings.SelectMany(x => new TableMigrationStatements(sourceName, x)),
            .. new[] { new SchemaRenameStatement(sourceName, targetName) }.Where(_ =>
                sourceName.TextValue != targetName.TextValue
            ),
        ];
    }
}
