using S = DocumentFormat.OpenXml.Spreadsheet;
using System.Text;
using System.Text.RegularExpressions;
using Signum.Excel;

namespace Signum.Word.Spreedsheet;

/// <summary>
/// Parsing and rewriting of A1 cell references inside Excel formula strings. Pure and template-agnostic:
/// it only knows how to find references (skipping string literals and function names) and how to convert
/// between column letters and indexes.
/// </summary>
internal static class FormulaRewriter
{
    public struct A1Ref
    {
        public bool ColAbs;
        public string Col;
        public bool RowAbs;
        public int Row;

        public override string ToString() => (ColAbs ? "$" : "") + Col + (RowAbs ? "$" : "") + Row;
    }

    // Anchored (\G) A1 reference: optional $, 1-3 column letters, optional $, row digits.
    static readonly Regex RefRegex = new Regex(@"\G(\$?)([A-Za-z]{1,3})(\$?)([0-9]+)", RegexOptions.Compiled);

    /// <summary>
    /// Rewrites every A1 cell reference in a formula (skipping quoted string literals and identifiers
    /// such as function names), applying <paramref name="transform"/> to each reference.
    /// </summary>
    public static string RewriteRefs(string formula, Func<A1Ref, A1Ref> transform)
    {
        var sb = new StringBuilder(formula.Length + 8);
        int i = 0;
        char prev = '\0';

        while (i < formula.Length)
        {
            char c = formula[i];

            if (c == '"') //copy a string literal verbatim, honoring "" escapes
            {
                sb.Append(c);
                i++;
                while (i < formula.Length)
                {
                    sb.Append(formula[i]);
                    if (formula[i] == '"')
                    {
                        if (i + 1 < formula.Length && formula[i + 1] == '"') { sb.Append('"'); i += 2; continue; }
                        i++;
                        break;
                    }
                    i++;
                }
                prev = '"';
                continue;
            }

            //only start a reference at a boundary, so we don't match inside function/defined names
            bool boundary = !(char.IsLetterOrDigit(prev) || prev == '_' || prev == '.');
            if (boundary && (char.IsLetter(c) || c == '$'))
            {
                var m = RefRegex.Match(formula, i);
                if (m.Success && m.Index == i)
                {
                    int after = i + m.Length;
                    char next = after < formula.Length ? formula[after] : '\0';
                    if (!(next == '(' || char.IsLetter(next))) //not a function call / longer identifier
                    {
                        var r = new A1Ref
                        {
                            ColAbs = m.Groups[1].Value == "$",
                            Col = m.Groups[2].Value,
                            RowAbs = m.Groups[3].Value == "$",
                            Row = int.Parse(m.Groups[4].Value),
                        };
                        var text = transform(r).ToString();
                        sb.Append(text);
                        i = after;
                        prev = text[text.Length - 1];
                        continue;
                    }
                }
            }

            sb.Append(c);
            prev = c;
            i++;
        }

        return sb.ToString();
    }

    public static int ColumnIndex(string col) => ExcelExtensions.GetExcelColumnIndex(col + "1");
    public static string ColumnName(int index) => ExcelExtensions.GetExcelColumnName((uint)index);
    public static int RowDigits(string a1) => int.Parse(new string(a1.Where(char.IsDigit).ToArray()));
    public static string ColumnLetters(string a1) => new string(a1.Where(char.IsLetter).ToArray());
    public static int RowOf(S.Cell c) => RowDigits(c.CellReference!.Value!);
    public static int ColumnOf(S.Cell c) => ColumnIndex(ColumnLetters(c.CellReference!.Value!));
}
