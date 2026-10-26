using Pure.RelationalSchema.Abstractions.Table;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

public sealed record TableMapping : ITableMapping
{
    public TableMapping(ITable source, ITable target)
        : this(source, target, [])
    { }

    public TableMapping(ITable source, ITable target, IEnumerable<IColumnMapping> columns)
    {
        Source = source;
        Target = target;
        Columns = columns;
    }

    public ITable Source { get; }

    public ITable Target { get; }

    public IEnumerable<IColumnMapping> Columns { get; }
}
