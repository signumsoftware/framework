using System.IO;
using System.Text;
using Xunit;

namespace Signum.Test.Environment;

/// <summary>
/// A <see cref="Connector.CurrentLogger"/> sink (drop-in for <c>DebugTextWriter</c>) that
/// writes each test's generated SQL to its own file, for cross-checking against the altea
/// TypeScript port. xUnit v3 constructs the test class (and therefore this writer) inside
/// the test's execution context, so <see cref="TestContext.Current"/> identifies the test.
///
/// Files are named <c>&lt;TestClass&gt;.&lt;TestMethod&gt;.&lt;pg|ss&gt;.sql</c> — the dialect
/// suffix comes from ASPNETCORE_ENVIRONMENT. Output dir: the SQL_DUMP_DIR env var, else a
/// default under the altea repo. Set SQL_DUMP=1 to activate (else it no-ops, so a normal
/// test run is unaffected).
/// </summary>
public sealed class SqlDumpTextWriter : TextWriter
{
    public static bool Enabled => System.Environment.GetEnvironmentVariable("SQL_DUMP") == "1";

    readonly StringBuilder sb = new();
    readonly string? path;

    public SqlDumpTextWriter()
    {
        var tc = TestContext.Current;
        var fullClass = tc.TestClass?.TestClassName;
        var cls = fullClass == null ? null : fullClass.Substring(fullClass.LastIndexOf('.') + 1);
        var method = tc.TestMethod?.MethodName;

        var env = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var suffix = env == "Postgres" ? "pg" : env == "SqlServer" ? "ss" : "unknown";

        var dir = System.Environment.GetEnvironmentVariable("SQL_DUMP_DIR")
            ?? @"D:\Altea\eastwind\sqlcmp\cs";

        if (cls != null && method != null)
        {
            Directory.CreateDirectory(dir);
            path = Path.Combine(dir, $"{cls}.{method}.{suffix}.sql");
        }
    }

    public override Encoding Encoding => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public override void Write(string? value)
    {
        sb.Append(value);
        Flush();
    }

    public override void Write(char[] buffer, int index, int count)
    {
        sb.Append(buffer, index, count);
        Flush();
    }

    // Each test instance owns one unique file; rewriting it on every append keeps the file
    // complete without needing an explicit end-of-test hook. Volume is tiny (a few small
    // writes per test), so the repeated overwrite is cheap.
    public override void Flush()
    {
        if (path != null)
            File.WriteAllText(path, sb.ToString());
    }
}
