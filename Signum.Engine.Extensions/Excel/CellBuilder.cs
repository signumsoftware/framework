using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using spreadsheet = DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Signum.Entities.DynamicQuery;
using System.IO;
using Signum.Utilities.DataStructures;
using Signum.Utilities;
using System.Globalization;

namespace Signum.Engine.Excel
{
    public enum TemplateCells
    {
        Header,

        Date,
        DateTime,
        Text,
        General,
        Number,
        Decimal
    }

    public class CellBuilder
    {
        public Dictionary<TypeCode, TemplateCells> DefaultTemplateCells = new Dictionary<TypeCode, TemplateCells> 
        {
            {TypeCode.Boolean, TemplateCells.General},
            {TypeCode.Byte, TemplateCells.Number},
            {TypeCode.Char, TemplateCells.Text},
            {TypeCode.DateTime, TemplateCells.DateTime},
            {TypeCode.DBNull, TemplateCells.General},
            {TypeCode.Decimal, TemplateCells.Decimal},
            {TypeCode.Double, TemplateCells.Decimal},
            {TypeCode.Empty, TemplateCells.General},
            {TypeCode.Int16, TemplateCells.Number},
            {TypeCode.Int32, TemplateCells.Number},
            {TypeCode.Int64, TemplateCells.Number},
            {TypeCode.Object, TemplateCells.General},
            {TypeCode.SByte, TemplateCells.Number},
            {TypeCode.Single, TemplateCells.Number},
            {TypeCode.String, TemplateCells.Text},
            {TypeCode.UInt16, TemplateCells.Number},
            {TypeCode.UInt32, TemplateCells.Number},
            {TypeCode.UInt64, TemplateCells.Number}
        };

        public TemplateCells GetTemplateCell(Type type)
        {
            TypeCode tc = type.UnNullify().Let(a => a.IsEnum ? TypeCode.Object : Type.GetTypeCode(a));
            return DefaultTemplateCells.TryGetS(tc) ?? TemplateCells.General;
        }

        public Dictionary<TemplateCells, CellValues?> DefaultCellValues = new Dictionary<TemplateCells, CellValues?> 
        {
            {TemplateCells.Date, null},
            {TemplateCells.DateTime, null},
            {TemplateCells.Text, CellValues.InlineString},
            {TemplateCells.General, CellValues.InlineString},
            {TemplateCells.Number, null},
            {TemplateCells.Decimal, null}
        };

        public Dictionary<TemplateCells, UInt32Value> DefaultStyles;

        public Cell Cell<T>(T value)
        {
            TemplateCells template = GetTemplateCell(typeof(T));
            return Cell(value, template);
        }

        public Cell Cell<T>(T value, UInt32Value styleIndex)
        {
            TemplateCells template = GetTemplateCell(typeof(T));
            return Cell(value, template, styleIndex);
        }

        public Cell Cell(object value, Type type)
        {
            TemplateCells template = GetTemplateCell(type);
            return Cell(value, template);
        }

        public Cell Cell(object value, TemplateCells template)
        {
            return Cell(value, template, DefaultStyles[template]);
        }

        public Cell Cell(object value, TemplateCells template, UInt32Value styleIndex)
        {
            string excelValue = value == null ? "" :
                        (template == TemplateCells.Date || template == TemplateCells.DateTime) ? ExcelExtensions.ToExcelDate(((DateTime)value)) :
                        (template == TemplateCells.Decimal) ? ExcelExtensions.ToExcelNumber(Convert.ToDecimal(value)) :
                        value.ToString();

            Cell cell = (template == TemplateCells.General || template == TemplateCells.Text || template == TemplateCells.Header) ? 
                new Cell(new InlineString(new Text { Text = excelValue })) { DataType = CellValues.InlineString } : 
                new Cell { CellValue = new CellValue(excelValue), DataType = DefaultCellValues[template] };

            cell.StyleIndex = styleIndex;

            return cell;
        }

        public Cell Cell(Type type, object value, UInt32Value styleIndex)
        {
            TemplateCells template = GetTemplateCell(type);
            return Cell(value, template, styleIndex);
        }
    }
}
