using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.String;
using Pure.Primitives.String.Operations;
using Char = Pure.Primitives.Char.Char;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Migration;

internal sealed record MigrationBlockStatement : IString
{
    private readonly IString _schemaName;

    private readonly IEnumerable<IString> _statements;

    public MigrationBlockStatement(IString schemaName, IEnumerable<IString> statements)
    {
        _schemaName = schemaName;
        _statements = statements;
    }

    public string TextValue =>
        new NewLineJoinedString(
            [
                new String("DO $$"),
                new String("BEGIN"),
                new WhitespaceJoinedString(
                    new String("IF EXISTS"),
                    new WrappedString(
                        new LeftRoundBracketString(),
                        new WhitespaceJoinedString(
                            new String("SELECT 1 FROM pg_catalog.pg_namespace"),
                            new String("WHERE nspname ="),
                            new WrappedString(new SingleQuoteString(), _schemaName)
                        ),
                        new RightRoundBracketString()
                    ),
                    new String("THEN")
                ),
                .. _statements,
                new String("END IF;"),
                new String("END$$;"),
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
