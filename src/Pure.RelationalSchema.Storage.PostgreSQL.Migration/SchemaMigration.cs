using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.Migration.Abstractions.SchemaMigration;
using Pure.RelationalSchema.Migration.Abstractions.TableMapping;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

public sealed record SchemaMigration : ISchemaMigration
{
    public SchemaMigration(ISchema source, ISchema target)
        : this(source, target, [])
    { }

    public SchemaMigration(
        ISchema source,
        ISchema target,
        IEnumerable<ITableMapping> tables
    )
    {
        Source = source;
        Target = target;
        Tables = tables;
    }

    public ISchema Source { get; }

    public ISchema Target { get; }

    public IEnumerable<ITableMapping> Tables { get; }
}
