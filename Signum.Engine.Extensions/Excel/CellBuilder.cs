using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using DocumentFormat.OpenXml.Drawing.Diagrams;

namespace Signum.Engine.Excel;

public enum DefaultStyle
{
    Title,
    Header,
    Date,
    DateTime,
    Text,
    General,
    Boolean,
    Enum,
    Number,
    Decimal,
    Percentage,
    Time
}

public class CellBuilder
{
    public Dictionary<TypeCode, DefaultStyle> DefaultTemplateCells = new Dictionary<TypeCode, DefaultStyle> 
    {
        {TypeCode.Boolean, DefaultStyle.Boolean},
        {TypeCode.Byte, DefaultStyle.Number},
        {TypeCode.Char, DefaultStyle.Text},
        {TypeCode.DateTime, DefaultStyle.DateTime},
        {TypeCode.DBNull, DefaultStyle.General},
        {TypeCode.Decimal, DefaultStyle.Decimal},
        {TypeCode.Double, DefaultStyle.Decimal},
        {TypeCode.Empty, DefaultStyle.General},
        {TypeCode.Int16, DefaultStyle.Number},
        {TypeCode.Int32, DefaultStyle.Number},
        {TypeCode.Int64, DefaultStyle.Number},
        {TypeCode.Object, DefaultStyle.General},
        {TypeCode.SByte, DefaultStyle.Number},
        {TypeCode.Single, DefaultStyle.Number},
        {TypeCode.String, DefaultStyle.Text},
        {TypeCode.UInt16, DefaultStyle.Number},
        {TypeCode.UInt32, DefaultStyle.Number},
        {TypeCode.UInt64, DefaultStyle.Number}
    };

    public DefaultStyle GetDefaultStyle(Type type)
    {
        var uType = type.UnNullify();
        if (uType.IsEnum)
            return DefaultStyle.Enum;

        if (uType == typeof(DateOnly))
            return DefaultStyle.Date;

        if (uType == typeof(TimeOnly))
            return DefaultStyle.Time;

        TypeCode tc = Type.GetTypeCode(uType);
        return DefaultTemplateCells.TryGetS(tc) ?? DefaultStyle.General;
    }

    public Dictionary<DefaultStyle, UInt32Value> DefaultStyles = null!;

    public Cell Cell<T>(T value, bool forImport = false)
    {
        DefaultStyle template = GetDefaultStyle(typeof(T));
        return Cell(value, template, forImport);
    }

    public Cell Cell<T>(T value, UInt32Value styleIndex, bool forImport = false)
    {
        DefaultStyle template = GetDefaultStyle(typeof(T));
        return Cell(value, template, styleIndex, forImport);
    }

    public Cell Cell(object? value, Type type, bool forImport = false)
    {
        DefaultStyle template = GetDefaultStyle(type);
        return Cell(value, template, forImport);
    }

    public Cell Cell(object? value, Type type, UInt32Value styleIndex, bool forImport = false)
    {
        DefaultStyle template = GetDefaultStyle(type);
        return Cell(value, template, styleIndex, forImport);
    }

    public Cell Cell(object? value, DefaultStyle template, bool forImport = false)
    {
        return Cell(value, template, DefaultStyles[template], forImport);
    }

#pragma warning disable CA1822 // Mark members as static
    public Cell Cell(object? value, DefaultStyle template, UInt32Value styleIndex, bool forImport = false)
    {
        string excelValue = value == null ? "" :
            template == DefaultStyle.DateTime ? ExcelExtensions.ToExcelDate(((DateTime)value)) :
            template == DefaultStyle.Date ? value is DateTime dt ? ExcelExtensions.ToExcelDate(dt) : ExcelExtensions.ToExcelDate(((DateOnly)value).ToDateTime()) :
            template == DefaultStyle.Time ? ExcelExtensions.ToExcelTime((TimeOnly)value) :
            template == DefaultStyle.Decimal ? ExcelExtensions.ToExcelNumber(Convert.ToDecimal(value)) :
            template == DefaultStyle.Boolean ? ExcelExtensions.ToExcelNumber((bool)value ? 1 : 0) :
            forImport && template == DefaultStyle.Enum ? value.ToString()! :
            template == DefaultStyle.Enum ? ((Enum)value).NiceToString() :
            forImport && value is Lite<Entity> lite ? lite.KeyLong() :
            value.ToString()!;

        Cell cell = 
            IsInlineString(template) ? new Cell(new InlineString(new Text { Text = excelValue })) { DataType = CellValues.InlineString } :
            new Cell { CellValue = new CellValue(excelValue), DataType = template == DefaultStyle.Boolean ? CellValues.Boolean : null  };

        cell.StyleIndex = styleIndex;

        return cell;
    }
#pragma warning restore CA1822 // Mark members as static

    private static bool IsInlineString(DefaultStyle template)
    {
        return template switch
        {
            DefaultStyle.Title or 
            DefaultStyle.Header or 
            DefaultStyle.Text or
            DefaultStyle.General or 
            DefaultStyle.Enum => true,

            DefaultStyle.Boolean or 
            DefaultStyle.Date or 
            DefaultStyle.DateTime or 
            DefaultStyle.Time or 
            DefaultStyle.Number or 
            DefaultStyle.Decimal => false,
            _ => throw new InvalidOperationException("Unexpected"),
        };
    }

    public Dictionary<string, UInt32Value> CustomDecimalStyles = new Dictionary<string, UInt32Value>();
    public UInt32Value CellFormatCount = null!;
        

    internal (DefaultStyle defaultStyle, UInt32Value styleIndex) GetDefaultStyleAndIndex(ResultColumn c)
    {

        if (ReflectionTools.IsNumber(c.Column.Type))
        {
            if (c.Column.Unit.HasText() || c.Column.Format != null && c.Column.Format != Reflector.FormatString(c.Column.Type))
            {
                string formatExpression = GetCustomFormatExpression(c.Column.Unit, c.Column.Format);
                var styleIndex = CustomDecimalStyles.GetOrCreate(formatExpression, () => CellFormatCount++);
                return (ReflectionTools.IsIntegerNumber(c.Column.Type) ? DefaultStyle.Number : DefaultStyle.Decimal,
                    styleIndex);

            }
        }

        var defaultStyle = c.Column.Type.UnNullify() == typeof(DateTime) && c.Column.Format == "d" ? DefaultStyle.Date :
            ReflectionTools.IsDecimalNumber(c.Column.Type) && c.Column.Format?.ToLower()== "p"? DefaultStyle.Percentage :
            GetDefaultStyle(c.Column.Type);

        return (defaultStyle, DefaultStyles.GetOrThrow(defaultStyle));
    }

    private static string GetCustomFormatExpression(string? columnUnit, string? columnFormat)
    {
        var excelUnitPrefix = 
            columnUnit == "$" ? "[$$-409]" : 
            columnUnit == "£" ? "[$£-809]" : 
            columnUnit == "¥" ? "[$¥-804]" : "";

        var excelUnitSuffix = excelUnitPrefix == "" && !string.IsNullOrEmpty(columnUnit) ? $"\" {columnUnit}\"" : "";

        var excelFormat = GetExcelFormat(columnFormat);

        return excelUnitPrefix + excelFormat + excelUnitSuffix;

    }

    private static string GetExcelFormat(string? columnFormat)
    {
        if (columnFormat == null)
        {
            return "#,##0.00";
        }
        var f = columnFormat.ToUpper();

        static string DecimalPlaces(int places)
        {
            if (places == 0)
                return "";

            return "." + "0".Replicate(places);
        }

       return f.StartsWith("C") ? "#,##0" + DecimalPlaces(f.After("C").ToInt() ?? 2)
            : f.StartsWith("N") ? "#,##0" + DecimalPlaces(f.After("N").ToInt() ?? 2)
            : f.StartsWith("D") ? "0".Replicate(f.After("D").ToInt() ?? 1)
            : f.StartsWith("F") ? "0" + DecimalPlaces(f.After("F").ToInt() ?? 2)
            : f.StartsWith("E") ? "0" + DecimalPlaces(f.After("E").ToInt() ?? 2)
            : f.StartsWith("P") ? "0" + DecimalPlaces(f.After("P").ToInt() ?? 2) + "%"
            : columnFormat;
    }
}
