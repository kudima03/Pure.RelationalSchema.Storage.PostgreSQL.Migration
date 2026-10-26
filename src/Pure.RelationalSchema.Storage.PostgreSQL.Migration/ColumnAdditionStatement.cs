using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.String;
using Pure.Primitives.String.Operations;
using Char = Pure.Primitives.Char.Char;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

internal sealed record ColumnAdditionStatement : IString
{
    private readonly IString _schemaName;

    private readonly IString _tableName;

    private readonly IString _columnName;

    private readonly IString _typeName;

    private readonly IString _defaultValue;

    public ColumnAdditionStatement(
        IString schemaName,
        IString tableName,
        IString columnName,
        IString typeName,
        IString defaultValue
    )
    {
        _schemaName = schemaName;
        _tableName = tableName;
        _columnName = columnName;
        _typeName = typeName;
        _defaultValue = defaultValue;
    }

    public string TextValue =>
        new NewLineJoinedString(
            new ConcatenatedString(
                new WhitespaceJoinedString(
                    new String("ALTER TABLE"),
                    new QualifiedName(_schemaName, _tableName),
                    new String("ADD COLUMN"),
                    new WrappedString(new DoubleQuoteString(), _columnName),
                    _typeName,
                    new String("NOT NULL DEFAULT"),
                    new WrappedString(new SingleQuoteString(), _defaultValue)
                ),
                new SemicolonString()
            ),
            new ConcatenatedString(
                new WhitespaceJoinedString(
                    new String("ALTER TABLE"),
                    new QualifiedName(_schemaName, _tableName),
                    new String("ALTER COLUMN"),
                    new WrappedString(new DoubleQuoteString(), _columnName),
                    new String("DROP DEFAULT")
                ),
                new SemicolonString()
            )
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
