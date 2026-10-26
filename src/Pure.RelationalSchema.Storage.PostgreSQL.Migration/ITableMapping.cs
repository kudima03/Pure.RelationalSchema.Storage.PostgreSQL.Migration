using Pure.RelationalSchema.Abstractions.Table;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

public interface ITableMapping
{
    public ITable Source { get; }

    public ITable Target { get; }

    public IEnumerable<IColumnMapping> Columns { get; }
}
