using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.Number;
using Pure.Primitives.String;
using Pure.Primitives.String.Operations;
using Pure.RelationalSchema.Abstractions.Index;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.HashCodes;
using Char = Pure.Primitives.Char.Char;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

internal sealed record IndexName : IString
{
    private readonly ITable _table;

    private readonly IIndex _index;

    public IndexName(ITable table, IIndex index)
    {
        _table = table;
        _index = index;
    }

    public string TextValue =>
        new JoinedString(
            new DotString(),
            [
                new Substring(new HexString(new TableHash(_table)), new UShort(31)),
                new Substring(new HexString(new IndexHash(_index)), new UShort(31)),
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
