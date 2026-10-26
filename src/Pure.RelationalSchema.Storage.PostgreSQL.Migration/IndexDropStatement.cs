using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.String;
using Pure.Primitives.String.Operations;
using Char = Pure.Primitives.Char.Char;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

internal sealed record IndexDropStatement : IString
{
    private readonly IString _schemaName;

    private readonly IString _indexName;

    public IndexDropStatement(IString schemaName, IString indexName)
    {
        _schemaName = schemaName;
        _indexName = indexName;
    }

    public string TextValue =>
        new ConcatenatedString(
            new WhitespaceJoinedString(
                new String("DROP INDEX IF EXISTS"),
                new QualifiedName(_schemaName, _indexName)
            ),
            new SemicolonString()
        ).TextValue;

    public IEnumerator<IChar> GetEnumerator()
    {
        return TextValue.Select(x => new Char(x)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
