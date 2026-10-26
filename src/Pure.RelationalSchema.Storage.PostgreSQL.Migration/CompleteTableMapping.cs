using Pure.RelationalSchema.Abstractions.Table;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

internal sealed record CompleteTableMapping : ITableMapping
{
    private readonly ITableMapping _mapping;

    public CompleteTableMapping(ITableMapping mapping)
    {
        _mapping = mapping;
    }

    public ITable Source => _mapping.Source;

    public ITable Target => _mapping.Target;

    public IEnumerable<IColumnMapping> Columns => CompleteColumns();

    private IEnumerable<IColumnMapping> CompleteColumns()
    {
        IReadOnlyCollection<IColumnMapping> mappings = [.. _mapping.Columns];
        IReadOnlyCollection<string> sources =
        [
            .. _mapping.Source.Columns.Select(x => new ColumnName(x).TextValue),
        ];
        IReadOnlyCollection<string> targets =
        [
            .. _mapping.Target.Columns.Select(x => new ColumnName(x).TextValue),
        ];
        IReadOnlyCollection<string> mappedSources =
        [
            .. mappings.Select(x => new ColumnName(x.Source).TextValue),
        ];
        IReadOnlyCollection<string> mappedTargets =
        [
            .. mappings.Select(x => new ColumnName(x.Target).TextValue),
        ];

        if (mappedSources.Except(sources).Any() || mappedTargets.Except(targets).Any())
        {
            throw new ArgumentException("Mapped column is missing from its table.");
        }

        if (
            mappedSources.Distinct().Count() != mappedSources.Count
            || mappedTargets.Distinct().Count() != mappedTargets.Count
        )
        {
            throw new ArgumentException("Column is mapped more than once.");
        }

        IEnumerable<IColumnMapping> completed =
        [
            .. mappings,
            .. _mapping
                .Source.Columns.Where(x =>
                    !mappedSources.Contains(new ColumnName(x).TextValue)
                    && !mappedTargets.Contains(new ColumnName(x).TextValue)
                    && targets.Contains(new ColumnName(x).TextValue)
                )
                .Select(x => new ColumnMapping(x, x)),
        ];

        return completed;
    }
}
