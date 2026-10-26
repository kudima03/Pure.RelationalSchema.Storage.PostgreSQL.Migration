using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.String.Operations;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.HashCodes;
using Char = Pure.Primitives.Char.Char;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

internal sealed record SchemaName : IString
{
    private readonly ISchema _schema;

    public SchemaName(ISchema schema)
    {
        _schema = schema;
    }

    public string TextValue =>
        new TrimmedHash(new HexString(new SchemaHash(_schema))).TextValue;

    public IEnumerator<IChar> GetEnumerator()
    {
        return TextValue.Select(x => new Char(x)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
