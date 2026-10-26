using Pure.RelationalSchema.Abstractions.Column;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

public sealed record ColumnMapping : IColumnMapping
{
    public ColumnMapping(IColumn source, IColumn target)
    {
        Source = source;
        Target = target;
    }

    public IColumn Source { get; }

    public IColumn Target { get; }
}
