using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.String.Operations;
using Pure.RelationalSchema.Abstractions.ForeignKey;
using Pure.RelationalSchema.HashCodes;
using Char = Pure.Primitives.Char.Char;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

internal sealed record ForeignKeyName : IString
{
    private readonly IForeignKey _foreignKey;

    public ForeignKeyName(IForeignKey foreignKey)
    {
        _foreignKey = foreignKey;
    }

    public string TextValue =>
        new TrimmedHash(new HexString(new ForeignKeyHash(_foreignKey))).TextValue;

    public IEnumerator<IChar> GetEnumerator()
    {
        return TextValue.Select(x => new Char(x)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
