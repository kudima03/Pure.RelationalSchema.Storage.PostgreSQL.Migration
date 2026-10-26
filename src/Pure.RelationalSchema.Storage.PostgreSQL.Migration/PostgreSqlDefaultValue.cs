using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.Switches.String;
using Pure.RelationalSchema.Abstractions.ColumnType;
using Pure.RelationalSchema.ColumnType;
using Pure.RelationalSchema.HashCodes;
using Char = Pure.Primitives.Char.Char;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

internal sealed record PostgreSqlDefaultValue : IString
{
    private readonly IColumnType _columnType;

    public PostgreSqlDefaultValue(IColumnType columnType)
    {
        _columnType = columnType;
    }

    public string TextValue =>
        new StringSwitch<IColumnType>(
            _columnType,
            [
                new KeyValuePair<IColumnType, IString>(
                    new IntColumnType(),
                    new String("0")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new BoolColumnType(),
                    new String("false")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new LongColumnType(),
                    new String("0")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new UShortColumnType(),
                    new String("0")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new UIntColumnType(),
                    new String("0")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new ULongColumnType(),
                    new String("0")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new StringColumnType(),
                    new String("")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new DateColumnType(),
                    new String("0001-01-01")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new DateTimeColumnType(),
                    new String("0001-01-01 00:00:00")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new TimeColumnType(),
                    new String("00:00:00")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new DeterminedHashColumnType(),
                    new String("")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new DoubleColumnType(),
                    new String("0")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new FloatColumnType(),
                    new String("0")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new UuidColumnType(),
                    new String("00000000-0000-0000-0000-000000000000")
                ),
            ],
            x => new ColumnTypeHash(x)
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
