using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.String;
using Pure.Primitives.String.Operations;
using Char = Pure.Primitives.Char.Char;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

internal sealed record ColumnDropStatement : IString
{
    private readonly IString _schemaName;

    private readonly IString _tableName;

    private readonly IString _columnName;

    public ColumnDropStatement(IString schemaName, IString tableName, IString columnName)
    {
        _schemaName = schemaName;
        _tableName = tableName;
        _columnName = columnName;
    }

    public string TextValue =>
        new ConcatenatedString(
            new WhitespaceJoinedString(
                new String("ALTER TABLE"),
                new QualifiedName(_schemaName, _tableName),
                new String("DROP COLUMN IF EXISTS"),
                new WrappedString(new DoubleQuoteString(), _columnName)
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
