using S = DocumentFormat.OpenXml.Spreadsheet;

namespace Signum.Word.Spreedsheet;

/// <summary>
/// A row-level @foreach block captured while parsing a spreadsheet, tied to the concrete worksheet it
/// lives in. Recorded before the generic engine collapses the block, so the finalizer can later renumber
/// rows and fix formula ranges.
/// </summary>
public class SpreadsheetForeachBlock(S.Worksheet worksheet, int foreachRow, int endForeachRow)
{
    public S.Worksheet Worksheet { get; } = worksheet;
    public int ForeachRow { get; } = foreachRow;
    public int EndForeachRow { get; } = endForeachRow;
}

/// <summary>
/// Given the rendered rows and the captured @foreach blocks, works out where every row moves after block
/// expansion: the new number of each rendered row (clones get consecutive numbers, rows below the block
/// shift) and how any original row number remaps when it appears inside a formula (<see cref="MapRow"/>).
/// </summary>
internal class SpreadsheetBlockPlan
{
    class Plan
    {
        public int Rf;           //original @foreach marker row
        public int Re;           //original @endforeach marker row
        public int F;            //first rendered data row
        public int L;            //last rendered data row
        public int Contribution; //net row-count change this block introduces below itself
    }

    readonly List<Plan> plans;
    readonly Dictionary<S.Row, (int newRow, bool inBlock)> rows;

    SpreadsheetBlockPlan(List<Plan> plans, Dictionary<S.Row, (int, bool)> rows)
    {
        this.plans = plans;
        this.rows = rows;
    }

    /// <summary>New number and in-block flag for each rendered row (by row element).</summary>
    public IReadOnlyDictionary<S.Row, (int newRow, bool inBlock)> Rows => rows;

    public static SpreadsheetBlockPlan Compute(List<S.Row> rows, List<SpreadsheetForeachBlock> blocks)
    {
        var plans = new List<Plan>();
        int cumulative = 0;
        foreach (var b in blocks.OrderBy(b => b.ForeachRow))
        {
            int produced = rows.Count(r => r.RowIndex?.Value is uint ri && b.ForeachRow < ri && ri < b.EndForeachRow);
            var p = new Plan { Rf = b.ForeachRow, Re = b.EndForeachRow, F = b.ForeachRow + cumulative };
            p.L = p.F + produced - 1;
            p.Contribution = produced - (b.EndForeachRow - b.ForeachRow + 1);
            cumulative += p.Contribution;
            plans.Add(p);
        }

        var map = new Dictionary<S.Row, (int, bool)>();
        var counters = plans.ToDictionary(p => p, p => p.F); //next data-row number to assign per block
        foreach (var row in rows)
        {
            if (row.RowIndex?.Value is not uint riv)
                continue;

            int r0 = (int)riv;
            var block = plans.FirstOrDefault(p => p.Rf < r0 && r0 < p.Re);
            int newRow = block != null ? counters[block]++ : MapRow(plans, r0);
            map[row] = (newRow, block != null);
        }

        return new SpreadsheetBlockPlan(plans, map);
    }

    /// <summary>Maps an original row number to its new position after all block expansions.</summary>
    public int MapRow(int originalRow) => MapRow(plans, originalRow);

    public bool IsMarkerRow(int row) => plans.Any(p => p.Rf == row || p.Re == row);

    static int MapRow(List<Plan> plans, int rho)
    {
        foreach (var b in plans)
        {
            if (rho == b.Rf) return b.F;              //@foreach anchor -> first data row
            if (rho == b.Re) return b.L;              //@endforeach anchor -> last data row
            if (b.Rf < rho && rho < b.Re) return b.F; //interior body row
        }

        int d = 0;
        foreach (var b in plans)
            if (b.Re < rho)
                d += b.Contribution;
        return rho + d;
    }
}
