using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.String;
using Pure.Primitives.String.Operations;
using Char = Pure.Primitives.Char.Char;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

internal sealed record ColumnRenameStatement : IString
{
    private readonly IString _schemaName;

    private readonly IString _tableName;

    private readonly IString _sourceColumnName;

    private readonly IString _targetColumnName;

    public ColumnRenameStatement(
        IString schemaName,
        IString tableName,
        IString sourceColumnName,
        IString targetColumnName
    )
    {
        _schemaName = schemaName;
        _tableName = tableName;
        _sourceColumnName = sourceColumnName;
        _targetColumnName = targetColumnName;
    }

    public string TextValue =>
        new ConcatenatedString(
            new WhitespaceJoinedString(
                new String("ALTER TABLE"),
                new QualifiedName(_schemaName, _tableName),
                new String("RENAME COLUMN"),
                new WrappedString(new DoubleQuoteString(), _sourceColumnName),
                new String("TO"),
                new WrappedString(new DoubleQuoteString(), _targetColumnName)
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
