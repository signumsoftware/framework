using System;
using Interop = Microsoft.Office.Interop.Excel;
using Signum.Utilities;
using System.Collections;
using System.Linq;
using System.IO;
using Signum.Entities.DynamicQuery;
using System.Collections.Generic;
using Signum.Entities;
using Microsoft.Win32;
using Signum.Entities.Extensions;
using Microsoft.Office.Interop.Excel;
using Signum.Windows.Extensions.Properties;

namespace Signum.Windows.Reports
{
    
    public static class ExcelReportGenerator
    {
       
        public static void GenerarInforme(string filename, ResultTable vista)
        {
            if (vista == null || vista.Rows.Length == 0 || vista.Columns.Length == 0)
                throw new InvalidOperationException(Resources.NoRows);

            ApplicationClass appExcel = null;
            Application app = null;
            Interop.Workbook wb = null;
            Interop.Worksheet wsDatos = null;
            Interop.Worksheet wsResultados = null;

            try
            {
                appExcel = new ApplicationClass();
                app = appExcel.Application;

                wb = app.Workbooks.Open(filename, Type.Missing, Type.Missing,Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                                        Type.Missing, Type.Missing, Type.Missing, Type.Missing,Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                                        
               //Buscar hoja con los datos origen. Lanzo excepción si no la encuentro
                wsDatos = ((IEnumerable)wb.Worksheets).Cast<Interop.Worksheet>()
                        .Where(ws => ws.Name == "Datos")
                        .Single(" El fichero Excel usado como template debe tener una Hoja de Cálculo con el nombre: Datos");
                    
                //Compruebo que las columnas de la vista sean las mismas que las cabeceras de la hoja de datos origen en el excel
                List<Column> colsVisibles = vista.VisibleColumns.ToList();
                for (int numCol = 0; numCol < colsVisibles.Count; numCol++)
                {
                    string columna = DameColumnaExcel(numCol); //((char)('A' + (char)numCol)).ToString();
                    Range rangeHeader = wsDatos.get_Range(columna + "1", columna + "1");
                    if (rangeHeader.Value2.ToString() != colsVisibles[numCol].DisplayName)
                        throw new ApplicationException("Las cabeceras de las columnas de la vista de datos y las del template excel no coinciden en la posición " + (numCol + 1).ToString());
                }

                //Borrar datos origen antiguos si los hubiera
                string columnasVista = DameColumnaExcel(colsVisibles.Count - 1); //((char)('A' + (char)vista.Columns.Count)).ToString();
                Range rangeTotal = wsDatos.get_Range("A2", columnasVista + wsDatos.Rows.Count.ToString());
                rangeTotal.Clear();

                //Copiar nuevos datos origen
                for (int j = 0; j < vista.Rows.Length; j++)
                {
                    int numFilaBase1 = j + 2;

                    var fila = vista.Rows[j];

                    int i = 0;
                    foreach (var col in vista.VisibleColumns)
                    {
                        string columna = DameColumnaExcel(i); //((char)('A' + (char)numCol)).ToString();
                        Range range = wsDatos.get_Range(columna + numFilaBase1, columna + numFilaBase1);
                        if (range != null)
                            range.Value2 = fila[col.Index].TryCC(a => a.ToString());

                        i++;
                    }
                }

                //Recorrer todas las hojas de cálculo, y para cada una de ellas buscar todas sus Tablas y Gráficos Dinámicos (ambos son PivotTables) y
                //  actualizar su SourceData al área de los nuevos datos origen
                for (int i = 1; i <= wb.Worksheets.Count; i++)
                {
                    wsResultados = (Interop.Worksheet)wb.Worksheets[i];
                    Interop.PivotTables pivotTables = (Interop.PivotTables)wsResultados.PivotTables(Type.Missing);
                    for (int j = 1; j <= pivotTables.Count; j++)
                    {
                        Interop.PivotTable pt = pivotTables.Item(j);
                        if (pt.SourceData.ToString().StartsWith("Datos!"))
                        {
                            pt.SourceData = "Datos!F1C1:F" + (int)(vista.Rows.Length + 1) + "C" + colsVisibles.Count;
                            pt.PivotCache().Refresh();
                        }
                    }
                }
            }
            finally
            {
                if (wb != null)
                {
                    wb.Save();
                    wb.Close(true, filename, Type.Missing);
                    wsDatos = null;
                    wsResultados = null;
                    wb = null;
                }
                if (app != null)
                {
                    app.Quit();
                    app = null;
                }
                if (appExcel != null)
                {
                    appExcel.Quit();
                    appExcel = null;
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private static string DameColumnaExcel(int numColumnaBase0)
        {
            string resultado = "";
            int numLetrasAlfabeto = 26;
            int numVecesAlfabeto;
            int numLetraAlfabeto;
            numVecesAlfabeto = Math.DivRem(numColumnaBase0, numLetrasAlfabeto, out numLetraAlfabeto);

            if(numVecesAlfabeto > 0)
                resultado = ((char)('A' + (char)(numVecesAlfabeto-1))).ToString();

            resultado = resultado + ((char)('A' + (char)numLetraAlfabeto)).ToString();
            return resultado;
        }

    }
}

