using System.Collections;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.Migration.Abstractions.TableMapping;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

internal sealed record CompleteTableMappings : IEnumerable<ITableMapping>
{
    private readonly ISchema _source;

    private readonly ISchema _target;

    private readonly IEnumerable<ITableMapping> _mappings;

    public CompleteTableMappings(
        ISchema source,
        ISchema target,
        IEnumerable<ITableMapping> mappings
    )
    {
        _source = source;
        _target = target;
        _mappings = mappings;
    }

    public IEnumerator<ITableMapping> GetEnumerator()
    {
        IReadOnlyCollection<ITableMapping> mappings = [.. _mappings];
        IReadOnlyCollection<string> sources =
        [
            .. _source.Tables.Select(x => new TableName(x).TextValue),
        ];
        IReadOnlyCollection<string> targets =
        [
            .. _target.Tables.Select(x => new TableName(x).TextValue),
        ];
        IReadOnlyCollection<string> mappedSources =
        [
            .. mappings.Select(x => new TableName(x.Source).TextValue),
        ];
        IReadOnlyCollection<string> mappedTargets =
        [
            .. mappings.Select(x => new TableName(x.Target).TextValue),
        ];

        if (mappedSources.Except(sources).Any() || mappedTargets.Except(targets).Any())
        {
            throw new ArgumentException("Mapped table is missing from its schema.");
        }

        if (
            mappedSources.Distinct().Count() != mappedSources.Count
            || mappedTargets.Distinct().Count() != mappedTargets.Count
        )
        {
            throw new ArgumentException("Table is mapped more than once.");
        }

        IEnumerable<ITableMapping> completed =
        [
            .. mappings,
            .. _source
                .Tables.Where(x =>
                    !mappedSources.Contains(new TableName(x).TextValue)
                    && !mappedTargets.Contains(new TableName(x).TextValue)
                    && targets.Contains(new TableName(x).TextValue)
                )
                .Select(x => new TableMapping(x, x)),
        ];

        return completed
            .Select(x => new CompleteTableMapping(x))
            .Cast<ITableMapping>()
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
