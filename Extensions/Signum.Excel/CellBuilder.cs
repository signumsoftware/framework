using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using Signum.DynamicQuery.Tokens;
using Signum.Utilities.Reflection;

namespace Signum.Excel;

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
    Time,
    Multiline
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
            value is string s ? s.Replace("\n", "\n").Replace("\n", "\n") :
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
            DefaultStyle.Enum or 
            DefaultStyle.Multiline => true,

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
        

    internal (DefaultStyle defaultStyle, UInt32Value styleIndex) GetDefaultStyleAndIndex(DynamicQuery.Column c)
    {

        if (ReflectionTools.IsNumber(c.Type))
        {
            if (c.Unit.HasText() || c.Format != null && c.Format != Reflector.FormatString(c.Type))
            {
                string formatExpression = GetCustomFormatExpression(c.Unit, c.Format);
                var styleIndex = CustomDecimalStyles.GetOrCreate(formatExpression, () => CellFormatCount++);
                return (ReflectionTools.IsIntegerNumber(c.Type) ? DefaultStyle.Number : DefaultStyle.Decimal,
                    styleIndex);

            }
        }

        var defaultStyle = 
            c.Type == typeof(string) && c.Token is EntityPropertyToken ept && Validator.TryGetPropertyValidator(ept.PropertyRoute)?.Validators.Any(v => v is StringLengthValidatorAttribute slv && slv.MultiLine) == true? DefaultStyle.Multiline: 
            c.Token is CollectionToArrayToken at && at.ToArrayType is CollectionToArrayType.SeparatedByNewLine or  CollectionToArrayType.SeparatedByNewLineDistinct ? DefaultStyle.Text :
            c.Type.UnNullify() == typeof(DateTime) && c.Format == "d" ? DefaultStyle.Date :
            ReflectionTools.IsDecimalNumber(c.Type) && c.Format?.ToLower()== "p"? DefaultStyle.Percentage :
            GetDefaultStyle(c.Type);

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
