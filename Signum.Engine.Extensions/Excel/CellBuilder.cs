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
using Signum.Entities;

namespace Signum.Engine.Excel
{
    public enum TemplateCells
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
        DecimalEuro,
        DecimalDollar,
        DecimalPound,
        DecimalYuan,
    }

    public class CellBuilder
    {
        public Dictionary<TypeCode, TemplateCells> DefaultTemplateCells = new Dictionary<TypeCode, TemplateCells> 
        {
            {TypeCode.Boolean, TemplateCells.Boolean},
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
            var uType = type.UnNullify();
            if (uType.IsEnum)
                return TemplateCells.Enum;

            TypeCode tc = Type.GetTypeCode(uType);
            return DefaultTemplateCells.TryGetS(tc) ?? TemplateCells.General;
        }

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
                        (template.ToString().StartsWith("Decimal")) ? ExcelExtensions.ToExcelNumber(Convert.ToDecimal(value)) :
                        (template == TemplateCells.Boolean) ? ToYesNo((bool)value) :
                        (template == TemplateCells.Enum) ? ((Enum)value)?.NiceToString() :
                        value.ToString();

            Cell cell = IsInlineString(template)? 
                new Cell(new InlineString(new Text { Text = excelValue })) { DataType = CellValues.InlineString } : 
                new Cell { CellValue = new CellValue(excelValue), DataType = null };

            cell.StyleIndex = styleIndex;

            return cell;
        }


        private bool IsInlineString(TemplateCells template)
        {
            switch (template)
            {
                case TemplateCells.Title: 
                case TemplateCells.Header:
                case TemplateCells.Text: 
                case TemplateCells.General: 
                case TemplateCells.Boolean: 
                case TemplateCells.Enum:
                    return true;

                case TemplateCells.Date: 
                case TemplateCells.DateTime: 
                case TemplateCells.Number: 
                case TemplateCells.Decimal:
                case TemplateCells.DecimalDollar:
                case TemplateCells.DecimalEuro:
                case TemplateCells.DecimalPound:
                case TemplateCells.DecimalYuan:
                    return false;

                default:
                    throw new InvalidOperationException("Unexpected"); 
            }
        }

        internal TemplateCells GetTemplateCell(ResultColumn c)
        {
            if (c.Column.Type.UnNullify() == typeof(DateTime) && c.Column.Format == "d")
                return TemplateCells.Date;

            if (c.Column.Type.UnNullify() == typeof(decimal))
            {
                switch (c.Column.Unit)
                {
                    case "€": return TemplateCells.DecimalEuro;
                    case "$": return TemplateCells.DecimalDollar;
                    case "£": return TemplateCells.DecimalPound;
                    case "¥": return TemplateCells.DecimalYuan;
                }
            }

            return GetTemplateCell(c.Column.Type);
        }

        private string ToYesNo(bool value)
        {
            return value ? BooleanEnum.True.NiceToString() : BooleanEnum.False.NiceToString();
        }

        public Cell Cell(Type type, object value, UInt32Value styleIndex)
        {
            TemplateCells template = GetTemplateCell(type);
            return Cell(value, template, styleIndex);
        }
    }
}
