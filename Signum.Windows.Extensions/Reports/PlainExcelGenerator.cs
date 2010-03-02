using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Excel;

namespace Signum.Windows.Reports
{
    
    public static class PlainExcelGenerator
    {
        static DataType Integer = (DataType)923; 

        static Dictionary<TypeCode, DataType> TypesConverter = new Dictionary<TypeCode, DataType> 
        {
            {TypeCode.Boolean, DataType.String},
            {TypeCode.Byte,DataType.String},
            {TypeCode.Char,DataType.String},
            {TypeCode.DateTime,DataType.DateTime},
            {TypeCode.DBNull,DataType.String},
            {TypeCode.Decimal,DataType.Number},
            {TypeCode.Double,DataType.Number},
            {TypeCode.Empty,DataType.String},
            {TypeCode.Int16,Integer},
            {TypeCode.Int32,Integer},
            {TypeCode.Int64,Integer},
            {TypeCode.Object,DataType.String},
            {TypeCode.SByte,DataType.String},
            {TypeCode.Single,DataType.Number},
            {TypeCode.String,DataType.String},
            {TypeCode.UInt16,Integer},
            {TypeCode.UInt32,Integer},
            {TypeCode.UInt64,Integer}
        };

        static Dictionary<DataType, string> TypeStyle = new Dictionary<DataType, string> 
        {
            {DataType.Boolean,styleText},
            {DataType.String,styleText},
            {DataType.DateTime,styleDateTime},
            {DataType.Number,styleDecimal},
            {Integer,styleInteger}
        };

        public static void GenerateReport(string filename, ResultTable view)
        {
            Workbook book = new Workbook
            {
                Properties = new DocumentProperties
                {
                    Author = "Signum Software",
                    LastAuthor = "Signum Software",
                    Created = new System.DateTime(2008, 7, 28, 11, 49, 19, 0),
                    LastSaved = new System.DateTime(2008, 7, 31, 12, 20, 28, 0),
                    Version = "12.00"
                },
                ExcelWorkbook = new ExcelWorkbook
                {
                    WindowHeight = 8670,
                    WindowWidth = 10455,
                    WindowTopX = 120,
                    WindowTopY = 150
                }
            };

            book.Styles = GenerateStyles();

            book.Worksheets.Add(GenerateWorksheetSheet(view));

            book.Save(filename);

            //Open the generated document
            System.Diagnostics.Process.Start(filename);
        }

        #region Styles
        private const string Default = "Default";
        private const string styleTitulo = "styleTitulo";
        private const string styleColumna = "styleColumna";
        private const string styleDecimal = "styleDecimal";
        private const string styleText = "styleText";
        private const string styleDateTime = "styleDateTime";
        private const string styleInteger = "styleInteger";


        static StyleCollection GenerateStyles()
        {
            return new StyleCollection 
            {
                new Style(Default) 
                {
                    Name = "Normal",
                    Font = new Font { FontName = "Arial", Size = 11F, Color = "#000000" },
                    Interior = new Interior(),
                    Alignment = new Alignment { Vertical = VerticalAlignment.Bottom },
                    Borders = new BorderCollection()
                },
                new Style(styleTitulo) 
                {
                    Font = new Font { Bold = true, Italic = true, FontName = "Arial", Size = 11F, Color = "#000000" },
                    Interior = new Interior 
                    {
                        Color = "#C5D9F1",
                        Pattern = InteriorPattern.Solid
                    },
                    Alignment = new Alignment { Horizontal = HorizontalAlignment.Center, Vertical = VerticalAlignment.Bottom },
                    Borders = new BorderCollection 
                    {
                        new Border 
                        {
                            Position = Position.Bottom,
                            Weight = 1,
                            LineStyle = LineStyleOption.Continuous
                        },
                        new Border 
                        {
                            Position = Position.Top,
                            Weight = 1,
                            LineStyle = LineStyleOption.Continuous
                        }
                    }
                },
                new Style(styleColumna)
                {
                    Alignment = new Alignment { Vertical = VerticalAlignment.Bottom, WrapText = true, Indent=1 }
                },
                new Style(styleDecimal) 
                {
                    NumberFormat = "Fixed"
                },
                new Style(styleText) 
                {
                    Font = new Font { FontName = "Arial", Size = 11F },
                    NumberFormat = "@"
                },
                new Style(styleDateTime) 
                {
                    NumberFormat = "dd/mm/yyyy\\ h:mm:ss"
                },
                new Style(styleInteger) 
                {
                    NumberFormat = "0"
                }
            };
        }
        #endregion

        static Worksheet GenerateWorksheetSheet(ResultTable vista)
        {
            return new Worksheet("Datos")
            {
                Table = new WorksheetTable
                {
                    DefaultRowHeight = 15F,
                    DefaultColumnWidth = 60F,
                    FullColumns = 1,
                    FullRows = 1,
                    Columns = new ColumnCollection 
                    {
                        vista.VisibleColumns.Select(c => 
                            new Signum.Excel.Column(styleColumna) 
                            {
                                  AutoFitWidth = (c.Type == typeof(string)) ? false : true,
                                  Width = (c.Type == typeof(string)) ? 150 : 65
                            }
                        )
                    },
                    Rows = new RowCollection 
                    {
                        new Row
                        {
                            AutoFitHeight = false,
                            Cells = new CellCollection()
                            {
                                vista.VisibleColumns.Select((c,i) =>
                                new Cell
                                {
                                    StyleID = styleTitulo,
                                    Data = new CellData(DataType.String, c.DisplayName),
                                    Offset = 0
                                })
                            }
                        },
                        vista.Rows.Select(row =>
                            new Row
                            {
                                AutoFitHeight = false,
                                Cells = new CellCollection()
                                {
                                    vista.VisibleColumns.Select(c=>
                                    {
                                        TypeCode tc = c.Type.UnNullify().Map(a=>a.IsEnum ? TypeCode.Object : Type.GetTypeCode(a));
                                        DataType dt = TypesConverter[tc];
                                   
                                        return new Cell
                                        {
                                            StyleID = TypeStyle[dt],
                                            Data = row[c].TryCC(o=>new CellData((dt!=Integer) ? dt : DataType.Number,
                                                      (dt == DataType.DateTime) ? ((DateTime)o).ToStringExcel() :
                                                      (dt==DataType.Number) ? Convert.ToDecimal(o).ToStringExcel() : 
                                                      o.ToString())),
                                            Offset = 0
                                        };

                                    })
                                }
                            }
                        )
                    }
                },
                Options = new Options
                {
                    Selected = true,
                    PageSetup = new PageSetup
                    {
                        Header = new PageHeader { Margin = 0.3F },
                        Footer = new PageFooter { Margin = 0.3F },
                        PageMargins = new PageMargins { Bottom = 0.75F, Left = 0.7F, Right = 0.7F, Top = 0.75F }
                    },
                    Print = new PrintOptions
                    {
                        PaperSizeIndex = 9,
                        ValidPrinterInfo = true
                    }
                }
            };
        }

    }
}

