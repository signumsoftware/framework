using Drawing = DocumentFormat.OpenXml.Drawing;
using Presentation = DocumentFormat.OpenXml.Presentation;
using WPDrawing = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using Wordprocessing = DocumentFormat.OpenXml.Wordprocessing;
using Charts = DocumentFormat.OpenXml.Drawing.Charts;
using Data = System.Data;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using System.Globalization;
using System.Text.RegularExpressions;
using Signum.Utilities.Reflection;
using System.Data;
using Signum.Templating;
using Signum.Excel;
using Signum.UserQueries;
using Signum.UserAssets;
using Signum.Chart.UserChart;
using Signum.Chart;
using Signum.Chart.ColorPalette;
using Signum.Entities;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.InkML;

namespace Signum.Word;

public static class TableBinder
{
    internal static void ValidateTables(OpenXmlPart part, WordTemplateEntity template, List<TemplateError> errors)
    {
        //Powerpoint container
        var graphicFrames = part.RootElement!.Descendants<Presentation.GraphicFrame>().ToList();
        foreach (var item in graphicFrames)
        {
            var title = item.GetTitle();

            if (title != null)
            {
                ValidateTitle(template, errors, title);
            }
        }

        //Word container
        var drawings = part.RootElement!.Descendants<Wordprocessing.Drawing>().ToList();
        foreach (var item in drawings)
        {
            var title = item.GetTitle();

            if (title != null)
            {
                ValidateTitle(template, errors, title);
            }
        }
    }

    private static void ValidateTitle(WordTemplateEntity template, List<TemplateError> errors, string title)
    {
        var titleFirstLine = title.Lines().FirstOrDefault();
        var prefix = titleFirstLine?.TryBefore(":");
        if (prefix != null)
        {
            var provider = WordTemplateLogic.ToDataTableProviders.TryGetC(prefix);
            if (provider != null)   
            {
                var error = provider.Validate(titleFirstLine!.After(":"), template);
                if (error != null)
                    errors.Add(new TemplateError(false, error));
              

                var pivotStr = title.TryAfter('\n')?.Trim();
                if (pivotStr.HasText())
                {
                    var pivot = UserChartDataTableProvider.ParsePivot(pivotStr);
                    if (pivot == null)
                        errors.Add(new TemplateError(false, "Unexpected Alternative Text '" + title + "'\nDid you wanted to use 'Pivot(colX, colY, colValue)'?"));
                }
            }
        }
    }

    internal static void ProcessTables(OpenXmlPart part, WordTemplateParameters parameters)
    {
        var graphicFrames = part.RootElement!.Descendants<Presentation.GraphicFrame>().ToList();
        foreach (var item in graphicFrames)
        {
            var title = item.GetTitle();
            Dictionary<string, string>? overridenColors = null;
            Data.DataTable? dataTable = title != null ? GetDataTable(parameters, title, out overridenColors) : null;
            if (dataTable != null)
            {
                ReplaceChartOrTable(part, item, dataTable, title!, overridenColors);
            }
        }

        var drawings = part.RootElement!.Descendants<Wordprocessing.Drawing>().ToList();
        foreach (var item in drawings)
        {
            var title = item.GetTitle();

            Dictionary<string, string>? overridenColors = null;
            Data.DataTable? dataTable = title != null ? GetDataTable(parameters, title, out overridenColors) : null;
            if (dataTable != null)
            {
                ReplaceChartOrTable(part, item, dataTable, title!, overridenColors);
            }
        }
    }

    private static void ReplaceChartOrTable(OpenXmlPart part, OpenXmlElement item, Data.DataTable dataTable, string titleForError, Dictionary<string, string>? overridenColors)
    {
        var chartRef = item.Descendants<Charts.ChartReference>().SingleOrDefaultEx();
        if (chartRef != null)
        {
            OpenXmlPart chartPart = part.GetPartById(chartRef.Id!.Value!);
            var chart = chartPart.RootElement!.Descendants<Charts.Chart>().SingleEx();
            ReplaceChart(chart, dataTable, titleForError, overridenColors);
        }
        else
        {
            var table = item.Descendants<Drawing.Table>().SingleOrDefaultEx();
            if (table != null)
            {
                ReplaceTable(table, dataTable);
            }
        }
    }

    public static string? GetTitle(this Presentation.GraphicFrame frame)
    {
        var nvdp = frame.Descendants<Presentation.NonVisualDrawingProperties>().FirstOrDefault();

        if(nvdp != null)
            return nvdp.Description?.Value ?? nvdp.Title?.Value;

        throw new NotImplementedException("Imposible to get the Title from " + frame?.GetType().FullName);
       
    }

    public static string? GetTitle(this Wordprocessing.Drawing drawing)
    {
        var prop = drawing.Descendants<WPDrawing.DocProperties>().FirstOrDefault();

        if (prop != null)
            return prop.Description?.Value ?? prop.Title?.Value;

        throw new NotImplementedException("Imposible to get the Title from " + drawing?.GetType().FullName);
    }

    static void SynchronizeNodes<N, T>(List<N> nodes, List<T> data, Action<N, T, int, bool> apply)
        where N : OpenXmlElement
    {
        var last = nodes.Last();
        for (int i = 0; i < data.Count; i++)
        {
            if (i < nodes.Count)
            {
                apply(nodes[i], data[i], i, false);
            }
            else
            {
                var clone = (N)last.CloneNode(true);
                last.Parent!.InsertAfter(clone, last);
                apply(clone, data[i], i, true);
                last = clone;
            }
        }

        for (int i = data.Count; i < nodes.Count; i++)
        {
            nodes[i].Remove();
        }
    }

    private static void ReplaceTable(Drawing.Table table, Data.DataTable dataTable)
    {
        var tableGrid = table.Descendants<Drawing.TableGrid>().SingleEx();
        SynchronizeNodes(
            tableGrid.Descendants<Drawing.GridColumn>().ToList(),
            dataTable.Columns.Cast<Data.DataColumn>().ToList(),
            (gc, dc, i, isCloned) => { });

        var rows = table.Descendants<Drawing.TableRow>().ToList();
        SynchronizeNodes(
           rows.FirstEx().Descendants<Drawing.TableCell>().ToList(),
           dataTable.Columns.Cast<Data.DataColumn>().ToList(),
           (gc, dc, i, isCloned) =>
           {
               var text = gc.Descendants<Drawing.Text>().SingleOrDefaultEx();

               if (text != null)
                   text.Text = dc.Caption ?? dc.ColumnName;
           });           
        
        SynchronizeNodes(
            rows.Skip(1).ToList(),
            dataTable.Rows.Cast<Data.DataRow>().ToList(),
            (tr, dr, j, isCloned) => 
            {
                SynchronizeNodes(
                    tr.Descendants<Drawing.TableCell>().ToList(),
                    dataTable.Columns.Cast<Data.DataColumn>().Select(dc => dr[dc]).ToList(),
                    (gc, val, i, isCloned2) => { gc.Descendants<Drawing.Text>().SingleEx().Text = ToExcelString(val)!; });
            });
    }

    public static void ReplaceChart(Charts.Chart chart, Data.DataTable table, string titleForError, Dictionary<string, string>? overridenColors)
    {
        var plotArea = chart.Descendants<Charts.PlotArea>().SingleEx();
        var series = plotArea.Descendants<OpenXmlCompositeElement>().Where(a => a.LocalName == "ser").ToList();

        var rows = table.Rows.Cast<Data.DataRow>().ToList();

        SynchronizeNodes(series, table.Columns.Cast<Data.DataColumn>().Skip(1).ToList(),
            (ser, col, i, isCloned) =>
            {
                if (!ReflectionTools.IsNumber(col.DataType) && !ReflectionTools.IsDate(col.DataType))
                    throw new InvalidOperationException($"Unable to bind the chart serie with the column '{col.ColumnName}' of type '{col.DataType.TypeName()}'. Consider using 'Pivot(colY, colX, colValue)' in the Alternative Text of your chart like this:\n" + titleForError.Lines().FirstEx() + "\nPivot(0,1,2)");

                if (isCloned)
                    ser.Descendants<Drawing.SchemeColor>().ToList().ForEach(f => f.Remove());

                BindSerie(ser, rows, col, i, overridenColors);
            });
    }


    private static void BindSerie(OpenXmlCompositeElement serie, List<Data.DataRow> rows, Data.DataColumn dataColumn, int index, Dictionary<string, string>? overridenColors)
    {
        serie.Descendants<Charts.Formula>().ToList().ForEach(f => f.Remove());

        serie.GetFirstChild<Charts.Index>()!.Val = new UInt32Value((uint)index);
        serie.GetFirstChild<Charts.Order>()!.Val = new UInt32Value((uint)index);

        var setTxt = serie.Descendants<Charts.SeriesText>().SingleEx();
        setTxt.StringReference!.Descendants<Charts.NumericValue>().SingleEx().Text = dataColumn.ColumnName;

        var colorSerie = dataColumn.ColumnName == null ? null : overridenColors?.TryGetC((string)dataColumn.ColumnName);
        if (colorSerie != null)
        {
            var sppr = serie.Descendants<Charts.ChartShapeProperties>().SingleEx();
            sppr.Elements<SolidFill>().ToList().ForEach(a => a.Remove());
            sppr.InsertAt(new SolidFill { RgbColorModelHex = new RgbColorModelHex() { Val = colorSerie.After("#") } }, 0);
        }

        {
            var cat = serie.Descendants<Charts.CategoryAxisData>().SingleEx();
            cat.Descendants<Charts.PointCount>().SingleEx().Val = new UInt32Value((uint)rows.Count);
            if (cat.Descendants<Charts.StringPoint>().ToList() is { Count: > 0 } strPoints)
            {
                SynchronizeNodes(strPoints, rows,
                  (sp, row, i, isCloned) =>
                  {
                      sp.Index = new UInt32Value((uint)i);
                      sp.Descendants<Charts.NumericValue>().Single().Text = ToExcelString(row[0])!;
                  });
            }
            else if (cat.Descendants<Charts.NumericPoint>().ToList() is { Count: > 0 } numPoints)
            {
                SynchronizeNodes(numPoints, rows,
                 (sp, row, i, isCloned) =>
                 {
                     sp.Index = new UInt32Value((uint)i);
                     sp.Descendants<Charts.NumericValue>().Single().Text = ToExcelString(row[0])!;
                 });
            }
            else
                throw new NotImplementedException("Neither StringPoint or NumericPoint found in CategoryAxisData");
        }

        if (overridenColors != null && serie.Descendants<Charts.DataPoint>().ToList() is { Count: > 0 } dataPoints)
        {
            SynchronizeNodes(dataPoints, rows,
              (sp, row, i, isCloned) =>
              {
                  sp.Index = new Charts.Index { Val = new UInt32Value((uint)i) };
                  var rowText = row[0];

                  var color = rowText == null ? null : overridenColors?.TryGetC((string)rowText);
                  if(color != null)
                  {
                      var sppr = sp.Descendants<Charts.ChartShapeProperties>().SingleEx();
                      sppr.Elements<SolidFill>().ToList().ForEach(a => a.Remove());
                      sppr.InsertAt(new SolidFill { RgbColorModelHex = new RgbColorModelHex() { Val = color.After("#") } }, 0);
                  }
              });
        }

        {
            var vals = serie.Descendants<Charts.Values>().SingleEx();
            vals.Descendants<Charts.PointCount>().SingleEx().Val = new UInt32Value((uint)rows.Count - 1);
            var valsValues = vals.Descendants<Charts.NumericPoint>().ToList();
            SynchronizeNodes(valsValues, rows,
              (sp, row, i, isCloned) =>
              {
                  sp.Index = new UInt32Value((uint)i);
                  sp.Descendants<Charts.NumericValue>().Single().Text = ToExcelString(row[dataColumn])!;
              });
        }
    }

    private static string? ToExcelString(object? val)
    {
        return val == null ? null :
            (val is DateTime dt) ? ExcelExtensions.ToExcelDate(dt) :
            (val is DateOnly d) ? ExcelExtensions.ToExcelDate(d.ToDateTime()) :
            (val is IFormattable fmt) ? fmt.ToString(null, CultureInfo.InvariantCulture) :
            val.ToString();
    }
    
    private static Data.DataTable? GetDataTable(WordTemplateParameters parameters, string title, out Dictionary<string, string>? overridenColors)
    {
        overridenColors = null;

        var titleFirsLine = title.Lines().FirstOrDefault();
        var key = titleFirsLine.TryBefore(":");

        if (key == null)
            return null;

        var provider = WordTemplateLogic.ToDataTableProviders.TryGetC(key);
        if (provider == null)
            return null;

        var ctx = new WordTemplateLogic.WordContext(parameters.Template, (Entity?)parameters.Entity, parameters.Model);

        var table = provider.GetDataTable(titleFirsLine!.After(":"), ctx, out overridenColors);

        var pivotStr = title.TryAfter('\n')?.Trim();

        if (pivotStr.HasText())
        {
            var pivot = UserChartDataTableProvider.ParsePivot(pivotStr)!.Value;
            return table.ToDataTablePivot(pivot.colY, pivot.colX, pivot.colValue);
        }

        return table;
    }

    public static DataTable ToDataTablePivot(this DataTable dt, int rowColumnIndex, int columnColumnIndex, int valueIndex)
    {
        Dictionary<object, Dictionary<object, object>> dictionary =
            dt.Rows.Cast<DataRow>()
            .AgGroupToDictionary(
                row => row[rowColumnIndex],
                gr => gr.ToDictionaryEx(
                    row => row[columnColumnIndex],
                    row => row[valueIndex])
            );

        var allColumns = dictionary.Values.SelectMany(d => d.Keys).Distinct();

        var rowColumn = dt.Columns[rowColumnIndex];
        var valueColumn = dt.Columns[valueIndex];

        var result = new DataTable();
        result.Columns.Add(new DataColumn(rowColumn.ColumnName, rowColumn.DataType));
        foreach (var item in allColumns)
            result.Columns.Add(new DataColumn(item.ToString(), valueColumn.DataType));

        foreach (var kvp in dictionary)
        {
            var rowValues = allColumns.Select(key => kvp.Value.TryGetC(key))
                .PreAnd(kvp.Key)
                .ToArray();
            result.Rows.Add(rowValues);
        }

        return result;
    }
}

public class ModelDataTableProvider : IWordDataTableProvider
{
    public Data.DataTable GetDataTable(string suffix, WordTemplateLogic.WordContext ctx, out Dictionary<string, string>? overridenColors)
    {
        overridenColors = null;

        MethodInfo mi = GetMethod(ctx.Template, suffix);

        object? result;
        try
        {
            object[] args = new object[1];
            result = mi.Invoke(ctx.Model, args);
            overridenColors = args[0] as Dictionary<string, string>;
        }
        catch (TargetInvocationException e)
        {
            e.InnerException!.PreserveStackTrace();

            throw e.InnerException!;
        }

        if (!(result is Data.DataTable dt))
            throw new InvalidOperationException($"Method '{suffix}' on '{ctx.Model!.GetType().Name}' did not return a DataTable");

        return dt;
    }

    private static MethodInfo GetMethod(WordTemplateEntity template, string method)
    {
        if (template.Model == null)
            throw new InvalidOperationException($"No WordModel found in template '{template}' to call '{method}'");

        var type = template.Model.ToType();
        var mi = type.GetMethod(method);
        if (mi == null)
            throw new InvalidOperationException($"No Method with name '{method}' found in type '{type.Name}'");


        return mi;
    }

    public string? Validate(string suffix, WordTemplateEntity template)
    {
        try
        {
            GetMethod(template, suffix);
            return null;
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }
}

public class UserQueryDataTableProvider : IWordDataTableProvider
{
    public Data.DataTable GetDataTable(string suffix, WordTemplateLogic.WordContext context, out Dictionary<string, string>? overridenColors)
    {
        var userQuery = Database.Query<UserQueryEntity>().SingleEx(a => a.Guid == Guid.Parse((suffix.TryBefore("\n") ?? suffix).Trim()));

        using (CurrentEntityConverter.SetCurrentEntity(context.GetEntity()))
        {
            overridenColors = null;
            var request = UserQueryLogic.ToQueryRequest(userQuery);
            ResultTable result = QueryLogic.Queries.ExecuteQuery(request);
            return result.ToDataTable(new UnambiguousNiceDataTableValueConverter(result));
        }
    }

    public string? Validate(string suffix, WordTemplateEntity template)
    {
        if (!Guid.TryParse((suffix.TryBefore("\n") ?? suffix).Trim(), out Guid guid))
            return "Impossible to convert '{0}' in a GUID for a UserQuery".FormatWith(suffix);

        var uc = Database.Query<UserQueryEntity>().Where(a => a.Guid == guid).Select(a => new { UQ = a.ToLite(), a.EntityType }).SingleOrDefaultEx();

        if (uc == null)
            return "No UserQuery with GUID={0} found".FormatWith(guid);

        if (uc.EntityType != null)
        {
            var imp = QueryLogic.Queries.GetEntityImplementations(template.Query.ToQueryName());

            var type = TypeLogic.GetType(uc.EntityType.Retrieve().CleanName);

            if (imp.Types.Contains(type))
                return "No UserQuery {0} (GUID={1}) is not compatible with ".FormatWith(uc.UQ, guid, imp);
        }

        return null;
    }
}

public class UserChartDataTableProvider : IWordDataTableProvider
{
    public Data.DataTable GetDataTable(string suffix, WordTemplateLogic.WordContext context, out Dictionary<string, string>? overridenColors)
    {
        return GetDataTable(suffix, context.GetEntity(), out overridenColors);
    }

    public Data.DataTable GetDataTable(string suffix, Entity? entity, out Dictionary<string, string>? overridenColors)
    {
        var userChart = Database.Query<UserChartEntity>().SingleEx(a => a.Guid == Guid.Parse((suffix.TryBefore("\n") ?? suffix).Trim()));

        using (CurrentEntityConverter.SetCurrentEntity(entity))
        {
            var chartRequest = UserChartLogic.ToChartRequest(userChart);
            ResultTable result = ChartLogic.ExecuteChartAsync(chartRequest, CancellationToken.None).Result;
            var tokens = chartRequest.Columns.Select(a => a.Token).NotNull().ToList();

            var converter = new UnambiguousNiceDataTableValueConverter(result);

            overridenColors = result.Columns.SelectMany(c =>
            {
                if (typeof(Lite<Entity>).IsAssignableFrom(c.Column.Type))
                    return (from lite in c.Values.OfType<Lite<Entity>>().Distinct()
                            let color = ColorPaletteLogic.ColorFor(lite)
                            where color != null
                            select KeyValuePair.Create((string)converter.ConvertValue(lite, c.Column)!, color));

                if (c.Column.Type.UnNullify().IsEnum)
                    return (from e in c.Values.OfType<Enum>().Distinct()
                            let color = ColorPaletteLogic.ColorFor(EnumEntity.FromEnumUntyped(e).ToLite())
                            where color != null
                            select KeyValuePair.Create((string)converter.ConvertValue(e, c.Column)!, color));

                return Enumerable.Empty<KeyValuePair<string, string>>();
            }).DistinctBy(a => a.Key).ToDictionary();

            return result.ToDataTable(converter);
        }
    }

    public string? Validate(string suffix, WordTemplateEntity template)
    {
        if (!Guid.TryParse((suffix.TryBefore("\n") ?? suffix).Trim(), out Guid guid))
            return "Impossible to convert '{0}' in a GUID for a UserChart".FormatWith(suffix);

        var uc = Database.Query<UserChartEntity>().Where(a => a.Guid == guid).Select(a => new { UC = a.ToLite(), a.EntityType }).SingleOrDefaultEx();

        if (uc == null)
            return "No UserChart with GUID={0} found".FormatWith(guid);

        if(uc.EntityType != null)
        {
            var imp = QueryLogic.Queries.GetEntityImplementations(template.Query.ToQueryName());

            var type = TypeLogic.GetType(uc.EntityType.Retrieve().CleanName);

            if (!imp.Types.Contains(type))
                return "No UserChart {0} (GUID={1}) is not compatible with ".FormatWith(uc.UC, guid, imp);
        }

        return null;
    }

 

    internal static (int colY, int colX, int colValue)? ParsePivot(string pivotStr)
    {
        var m = Regex.Match(pivotStr, @"^Pivot\s*\(\s*(?<colY>\d+)\s*,\s*(?<colX>\d+)\s*,\s*(?<colValue>\d+)\s*\)\s*$");
        if (!m.Success)
            return null;

        return (
            int.Parse(m.Groups["colY"].Value),
            int.Parse(m.Groups["colX"].Value),
            int.Parse(m.Groups["colValue"].Value));
    }
}
