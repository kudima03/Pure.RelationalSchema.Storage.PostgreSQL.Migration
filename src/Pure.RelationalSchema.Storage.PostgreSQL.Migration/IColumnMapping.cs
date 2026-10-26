using Pure.RelationalSchema.Abstractions.Column;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

public interface IColumnMapping
{
    public IColumn Source { get; }

    public IColumn Target { get; }
}
