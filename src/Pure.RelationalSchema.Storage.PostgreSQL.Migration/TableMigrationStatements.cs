using System.Collections;
using Pure.Primitives.Abstractions.String;
using Pure.RelationalSchema.Migration.Abstractions.ColumnMapping;
using Pure.RelationalSchema.Migration.Abstractions.TableMapping;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

internal sealed record TableMigrationStatements : IEnumerable<IString>
{
    private readonly IString _schemaName;

    private readonly ITableMapping _mapping;

    public TableMigrationStatements(IString schemaName, ITableMapping mapping)
    {
        _schemaName = schemaName;
        _mapping = mapping;
    }

    public IEnumerator<IString> GetEnumerator()
    {
        IString sourceName = new TableName(_mapping.Source);
        IString targetName = new TableName(_mapping.Target);
        IReadOnlyCollection<IColumnMapping> columns = [.. _mapping.Columns];
        IReadOnlyCollection<string> mappedSources =
        [
            .. columns.Select(x => new ColumnName(x.Source).TextValue),
        ];
        IReadOnlyCollection<string> mappedTargets =
        [
            .. columns.Select(x => new ColumnName(x.Target).TextValue),
        ];
        IEnumerable<IString> statements =
        [
            .. _mapping
                .Source.Columns.Where(x =>
                    !mappedSources.Contains(new ColumnName(x).TextValue)
                )
                .Select(x => new ColumnDropStatement(
                    _schemaName,
                    sourceName,
                    new ColumnName(x)
                )),
            .. columns
                .Where(x =>
                    new ColumnName(x.Source).TextValue
                    != new ColumnName(x.Target).TextValue
                )
                .Select(x => new ColumnRenameStatement(
                    _schemaName,
                    sourceName,
                    new ColumnName(x.Source),
                    new ColumnName(x.Target)
                )),
            .. columns
                .Where(x =>
                    new PostgreSqlColumnTypeName(x.Source.Type).TextValue
                    != new PostgreSqlColumnTypeName(x.Target.Type).TextValue
                )
                .Select(x => new ColumnTypeChangeStatement(
                    _schemaName,
                    sourceName,
                    new ColumnName(x.Target),
                    new PostgreSqlColumnTypeName(x.Target.Type)
                )),
            .. _mapping
                .Target.Columns.Where(x =>
                    !mappedTargets.Contains(new ColumnName(x).TextValue)
                )
                .Select(x => new ColumnAdditionStatement(
                    _schemaName,
                    sourceName,
                    new ColumnName(x),
                    new PostgreSqlColumnTypeName(x.Type),
                    new PostgreSqlDefaultValue(x.Type)
                )),
            .. new[] { new TableRenameStatement(_schemaName, sourceName, targetName) }
                .Where(_ => sourceName.TextValue != targetName.TextValue),
        ];
        return statements.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
