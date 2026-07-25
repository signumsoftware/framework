using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using S = DocumentFormat.OpenXml.Spreadsheet;
using Signum.Templating;

namespace Signum.Word.Spreedsheet;

/// <summary>
/// Spreadsheet-specific steps of the Word/OpenXml template pipeline. Callers must have already
/// established that the package is a <see cref="SpreadsheetDocument"/> and pass its <see cref="WorkbookPart"/>.
/// <para>Prep (before parsing): <see cref="DeshareFormulas"/> and <see cref="InlineTokens"/>.
/// Cleanup (after the generic engine cloned the @foreach body rows): <see cref="Finalize"/>.</para>
/// Row math is delegated to <see cref="SpreadsheetBlockPlan"/>; reference surgery to <see cref="FormulaRewriter"/>.
/// </summary>
public static class SpreadsheetUtils
{
    // ================================================================= Prep

    /// <summary>
    /// Expands shared formulas (<c>&lt;f t="shared" .../&gt;</c>) into standalone formulas so that cloning
    /// a template row can't duplicate a shared-formula index/range (which corrupts the workbook).
    /// </summary>
    public static void DeshareFormulas(WorkbookPart wb)
    {
        foreach (var wsPart in wb.WorksheetParts)
        {
            if (wsPart.Worksheet == null)
                continue;

            var cells = wsPart.Worksheet.Descendants<S.Cell>()
                .Where(c => c.CellFormula?.FormulaType?.Value == S.CellFormulaValues.Shared)
                .ToList();

            if (cells.IsEmpty())
                continue;

            //the master of each shared group holds the actual formula text
            var masters = new Dictionary<uint, (int row, int col, string text)>();
            foreach (var c in cells)
            {
                var f = c.CellFormula!;
                if (f.SharedIndex?.Value is uint si && f.Text.HasText() && !masters.ContainsKey(si))
                    masters[si] = (FormulaRewriter.RowOf(c), FormulaRewriter.ColumnOf(c), f.Text);
            }

            foreach (var c in cells)
            {
                var f = c.CellFormula!;
                if (!f.Text.HasText() && f.SharedIndex?.Value is uint si && masters.TryGetValue(si, out var m))
                {
                    int dRow = FormulaRewriter.RowOf(c) - m.row;
                    int dCol = FormulaRewriter.ColumnOf(c) - m.col;
                    f.Text = FormulaRewriter.RewriteRefs(m.text, r =>
                    {
                        if (!r.RowAbs) r.Row += dRow;
                        if (!r.ColAbs) r.Col = FormulaRewriter.ColumnName(FormulaRewriter.ColumnIndex(r.Col) + dCol);
                        return r;
                    });
                }

                f.FormulaType = null;
                f.SharedIndex = null;
                f.Reference = null;
            }
        }
    }

    /// <summary>
    /// Converts every shared-string cell whose text contains a template keyword into an inline string
    /// holding a private copy of the runs. This attaches the token text to the cell (so its ancestor
    /// chain becomes cell -> row -> sheetData) and sidesteps shared-string deduplication, where one
    /// physical &lt;si&gt; is referenced by many cells.
    /// </summary>
    public static void InlineTokens(WorkbookPart wb)
    {
        var sst = wb.SharedStringTablePart?.SharedStringTable;
        if (sst == null)
            return;

        var sharedItems = sst.Elements<S.SharedStringItem>().ToList();

        foreach (var wsPart in wb.WorksheetParts)
        {
            if (wsPart.Worksheet == null)
                continue;

            foreach (var cell in wsPart.Worksheet.Descendants<S.Cell>().ToList())
            {
                if (cell.DataType?.Value != S.CellValues.SharedString || cell.CellValue == null)
                    continue;

                if (!int.TryParse(cell.CellValue.InnerText, out int idx) || idx < 0 || idx >= sharedItems.Count)
                    continue;

                var si = sharedItems[idx];
                if (!TemplateUtils.KeywordsRegex.IsMatch(si.InnerText))
                    continue;

                var inline = new S.InlineString();
                foreach (var child in si.ChildElements)
                    inline.AppendChild(child.CloneNode(true));

                cell.RemoveAllChildren(); //also drops the <v> shared-string index
                cell.DataType = S.CellValues.InlineString;
                cell.AppendChild(inline);
            }
        }
    }

    /// <summary>
    /// Uses cell notes (legacy comments) as token carriers: when a note contains a template keyword, its
    /// text becomes the anchored cell's content (as an inline string) and the note is removed. This lets a
    /// token live on a cell that couldn't hold it directly (e.g. a cell with date data validation). The
    /// author-name prefix Excel prepends (up to the first line break) is dropped.
    /// </summary>
    public static void InlineNoteTokens(WorkbookPart wb)
    {
        foreach (var wsPart in wb.WorksheetParts)
        {
            var commentsPart = wsPart.WorksheetCommentsPart;
            var commentList = commentsPart?.Comments?.CommentList;
            if (commentList == null || wsPart.Worksheet == null)
                continue;

            var cellsByRef = wsPart.Worksheet.Descendants<S.Cell>()
                .Where(c => c.CellReference?.Value != null)
                .GroupBy(c => c.CellReference!.Value!)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var comment in commentList.Elements<S.Comment>().ToList())
            {
                if (!TemplateUtils.KeywordsRegex.IsMatch(comment.InnerText))
                    continue;

                if (comment.Reference?.Value is not string reference || !cellsByRef.TryGetValue(reference, out var cell))
                    continue;

                cell.RemoveAllChildren();
                cell.DataType = S.CellValues.InlineString;
                cell.AppendChild(new S.InlineString(new S.Text(NoteBody(comment.InnerText)) { Space = SpaceProcessingModeValues.Preserve }));

                comment.Remove();
            }

            //if every note was a token binding, drop the now-empty note infrastructure
            if (!commentList.Elements<S.Comment>().Any())
                RemoveNotes(wsPart, commentsPart!);
        }
    }

    //Excel prepends "Author:\n" to a note; the meaningful body is what follows the first line break.
    static string NoteBody(string commentText)
    {
        int nl = commentText.IndexOf('\n');
        return (nl >= 0 ? commentText.Substring(nl + 1) : commentText).Trim();
    }

    static void RemoveNotes(WorksheetPart wsPart, WorksheetCommentsPart commentsPart)
    {
        wsPart.DeletePart(commentsPart);

        //legacy notes also carry a VML drawing (the yellow box) referenced by a <legacyDrawing> element
        wsPart.Worksheet!.GetFirstChild<S.LegacyDrawing>()?.Remove();
        foreach (var vml in wsPart.VmlDrawingParts.ToList())
            wsPart.DeletePart(vml);
    }

    // ================================================================= Cleanup

    /// <summary>
    /// After the generic renderer cloned the body row(s) of each row-level @foreach, this renumbers all
    /// rows/cells and rewrites formula references so absolute anchors on the marker rows map to the real
    /// first/last data rows, relative references follow each clone, and formulas below the block shift.
    /// Also drops data validations, remaps merges, fixes the dimension, clears the calc chain and forces
    /// a full recalculation on load.
    /// </summary>
    public static void Finalize(WorkbookPart wb, List<SpreadsheetForeachBlock> blocks)
    {
        foreach (var wsPart in wb.WorksheetParts)
        {
            var sheetData = wsPart.Worksheet?.GetFirstChild<S.SheetData>();
            if (sheetData == null)
                continue;

            var sheetBlocks = blocks.Where(b => b.Worksheet == wsPart.Worksheet).ToList();
            if (sheetBlocks.Any())
                FinalizeSheet(wsPart, sheetData, sheetBlocks);
        }

        RemoveCalcChain(wb);
        ForceFullCalcOnLoad(wb);
    }

    static void FinalizeSheet(WorksheetPart wsPart, S.SheetData sheetData, List<SpreadsheetForeachBlock> blocks)
    {
        var rows = sheetData.Elements<S.Row>().ToList();
        var plan = SpreadsheetBlockPlan.Compute(rows, blocks);

        foreach (var row in rows)
            if (plan.Rows.TryGetValue(row, out var m))
                RenumberRow(row, m.newRow, m.inBlock, plan);

        RemapMerges(wsPart, plan);
        DropDataValidations(wsPart);
        FixDimension(wsPart, sheetData);
    }

    static void RenumberRow(S.Row row, int newRow, bool inBlock, SpreadsheetBlockPlan plan)
    {
        int shift = newRow - (int)row.RowIndex!.Value;
        foreach (var cell in row.Elements<S.Cell>())
        {
            FixCellFormula(cell, inBlock, shift, plan);
            FixCellReference(cell, newRow);
        }
        row.RowIndex = (uint)newRow;
    }

    static void FixCellFormula(S.Cell cell, bool inBlock, int shift, SpreadsheetBlockPlan plan)
    {
        if (cell.CellFormula == null || !cell.CellFormula.Text.HasText())
            return;

        //inside a cloned body row, relative refs follow the clone; everything else remaps by position
        cell.CellFormula.Text = FormulaRewriter.RewriteRefs(cell.CellFormula.Text, r =>
        {
            r.Row = inBlock && !r.RowAbs ? r.Row + shift : plan.MapRow(r.Row);
            return r;
        });

        cell.CellValue?.Remove(); //stale cached result; recalculated on load
    }

    static void FixCellReference(S.Cell cell, int newRow)
    {
        if (cell.CellReference?.Value is string cr)
            cell.CellReference = FormulaRewriter.ColumnLetters(cr) + newRow;
    }

    static void RemapMerges(WorksheetPart wsPart, SpreadsheetBlockPlan plan)
    {
        var merges = wsPart.Worksheet!.GetFirstChild<S.MergeCells>();
        if (merges == null)
            return;

        foreach (var mc in merges.Elements<S.MergeCell>().ToList())
        {
            if (mc.Reference?.Value is not string reference)
                continue;

            var parts = reference.Split(':');
            if (parts.Length != 2)
                continue;

            int r1 = FormulaRewriter.RowDigits(parts[0]);
            int r2 = FormulaRewriter.RowDigits(parts[1]);

            //merges on the removed @foreach/@endforeach marker rows no longer have a home
            if (plan.IsMarkerRow(r1) || plan.IsMarkerRow(r2))
            {
                mc.Remove();
                continue;
            }

            mc.Reference = $"{FormulaRewriter.ColumnLetters(parts[0])}{plan.MapRow(r1)}:{FormulaRewriter.ColumnLetters(parts[1])}{plan.MapRow(r2)}";
        }

        if (!merges.Elements<S.MergeCell>().Any())
            merges.Remove();
        else
            merges.Count = (uint)merges.Elements<S.MergeCell>().Count();
    }

    static void DropDataValidations(WorksheetPart wsPart)
    {
        //A generated report is not a fill-in form; validations anchored to template rows would be stale.
        foreach (var dv in wsPart.Worksheet!.Elements<S.DataValidations>().ToList())
            dv.Remove();
    }

    static void FixDimension(WorksheetPart wsPart, S.SheetData sheetData)
    {
        var dim = wsPart.Worksheet!.GetFirstChild<S.SheetDimension>();
        if (dim == null)
            return;

        var rowIdxs = sheetData.Elements<S.Row>().Select(r => (int?)r.RowIndex?.Value).NotNull().ToList();
        if (rowIdxs.IsEmpty())
            return;

        string lastCol = dim.Reference?.Value?.TryAfter(":")?.Let(FormulaRewriter.ColumnLetters) ?? "A";
        dim.Reference = $"A{rowIdxs.Min()}:{lastCol}{rowIdxs.Max()}";
    }

    static void RemoveCalcChain(WorkbookPart wb)
    {
        if (wb.CalculationChainPart is { } calcChain)
            wb.DeletePart(calcChain);
    }

    static void ForceFullCalcOnLoad(WorkbookPart wb)
    {
        var workbook = wb.Workbook;
        if (workbook == null)
            return;

        var calcPr = workbook.GetFirstChild<S.CalculationProperties>();
        if (calcPr == null)
        {
            calcPr = new S.CalculationProperties();
            if (workbook.GetFirstChild<S.Sheets>() is { } sheets)
                workbook.InsertAfter(calcPr, sheets);
            else
                workbook.AppendChild(calcPr);
        }
        calcPr.FullCalculationOnLoad = true;
    }
}
