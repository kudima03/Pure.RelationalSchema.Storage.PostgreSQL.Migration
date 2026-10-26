using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.String;
using Pure.Primitives.String.Operations;
using Char = Pure.Primitives.Char.Char;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

internal sealed record QualifiedName : IString
{
    private readonly IString _schemaName;

    private readonly IString _name;

    public QualifiedName(IString schemaName, IString name)
    {
        _schemaName = schemaName;
        _name = name;
    }

    public string TextValue =>
        new JoinedString(
            new DotString(),
            [
                new WrappedString(new DoubleQuoteString(), _schemaName),
                new WrappedString(new DoubleQuoteString(), _name),
            ]
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
