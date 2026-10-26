using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.String;
using Pure.Primitives.String.Operations;
using Char = Pure.Primitives.Char.Char;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

internal sealed record TableRenameStatement : IString
{
    private readonly IString _schemaName;

    private readonly IString _sourceTableName;

    private readonly IString _targetTableName;

    public TableRenameStatement(
        IString schemaName,
        IString sourceTableName,
        IString targetTableName
    )
    {
        _schemaName = schemaName;
        _sourceTableName = sourceTableName;
        _targetTableName = targetTableName;
    }

    public string TextValue =>
        new ConcatenatedString(
            new WhitespaceJoinedString(
                new String("ALTER TABLE"),
                new QualifiedName(_schemaName, _sourceTableName),
                new String("RENAME TO"),
                new WrappedString(new DoubleQuoteString(), _targetTableName)
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
