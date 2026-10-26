using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.String;
using Pure.Primitives.String.Operations;
using Char = Pure.Primitives.Char.Char;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

internal sealed record SchemaRenameStatement : IString
{
    private readonly IString _sourceSchemaName;

    private readonly IString _targetSchemaName;

    public SchemaRenameStatement(IString sourceSchemaName, IString targetSchemaName)
    {
        _sourceSchemaName = sourceSchemaName;
        _targetSchemaName = targetSchemaName;
    }

    public string TextValue =>
        new ConcatenatedString(
            new WhitespaceJoinedString(
                new String("ALTER SCHEMA"),
                new WrappedString(new DoubleQuoteString(), _sourceSchemaName),
                new String("RENAME TO"),
                new WrappedString(new DoubleQuoteString(), _targetSchemaName)
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
