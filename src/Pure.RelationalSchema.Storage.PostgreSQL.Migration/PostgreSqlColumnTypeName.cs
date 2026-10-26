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

internal sealed record PostgreSqlColumnTypeName : IString
{
    private readonly IColumnType _columnType;

    public PostgreSqlColumnTypeName(IColumnType columnType)
    {
        _columnType = columnType;
    }

    public string TextValue =>
        new StringSwitch<IColumnType>(
            _columnType,
            [
                new KeyValuePair<IColumnType, IString>(
                    new IntColumnType(),
                    new String("integer")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new BoolColumnType(),
                    new String("boolean")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new LongColumnType(),
                    new String("bigint")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new UShortColumnType(),
                    new String("integer")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new UIntColumnType(),
                    new String("bigint")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new ULongColumnType(),
                    new String("numeric")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new StringColumnType(),
                    new String("text")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new DateColumnType(),
                    new String("date")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new DateTimeColumnType(),
                    new String("timestamp")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new TimeColumnType(),
                    new String("time")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new DeterminedHashColumnType(),
                    new String("bytea")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new DoubleColumnType(),
                    new String("double precision")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new FloatColumnType(),
                    new String("real")
                ),
                new KeyValuePair<IColumnType, IString>(
                    new UuidColumnType(),
                    new String("uuid")
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
