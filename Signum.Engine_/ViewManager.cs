using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Utilidades.EstructurasDatos;
using Utilidades;
using System.Data;
using MotorDB.Motor; 

namespace MotorDB
{
    public static class ViewManager
    {
        static DirectedGraph<Table> directedGraph = new DirectedGraph<Table>();
        static bool cerrado; 

        public static void CargarDesdeRecurso()
        {
            DataTable dt = Ejecutor.EjecutarDataTable(SqlBuilder.GetAllViews().ToSimple());
            dt.Rows.Cast<DataRow>().ForEach(dr => Add(new View( (string)dr[0])));

            DataTable dt2 = Ejecutor.EjecutarDataTable(SqlBuilder.GetAllTableNames().ToSimple());
            dt2.Rows.Cast<DataRow>().ForEach(dr => Add(new Table((string)dr[0])));
        }

        public static  void Add(Table table)
        {
            if (cerrado)
                throw new ApplicationException("El view manager ya está cerrado");

            directedGraph.Add(table); 
        }

        public static void CalcularDependencias()
        {
            var dicView = directedGraph.ToDictionary(v => v.Name);

            foreach (var from in dicView.Values.OfType<View>())
            {
                from.Tokens.Select(t => dicView.TryGetC(t)).NotNull().ForEach(to => directedGraph.Add(from, to));
            }

            cerrado = true; 
        }

        public static View[] ViewsEnOrden()
        {
            return directedGraph.TopologicalSort().OfType<View>().ToArray();
        }

        public class Table
        {
            public string Name;

            public Table(string name)
            {
                this.Name = name;
            }

            protected Table() { }

            public override string ToString()
            {
                return "{0}: {1}".Formato(GetType().Name, Name);
            }
        }

        public class View : Table
        {
            static Regex regex = new Regex(@"\s*(?i:CREATE)\s+(?i:VIEW)\s*(\[?dbo\]?\.)?\[?(?<name>[A-Z][A-Z0-9_]*)\]?\s*(AS)\s*((?<token>[A-Z][A-Z0-9_]*)|.|\s)*",
                RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

            public readonly string FullSql;

            public readonly string[] Tokens;

            public View(string fullSql)
            {
                this.FullSql = fullSql;

                Match match = regex.Match(fullSql);

                if (!match.Success)
                    throw new ApplicationException("La query {0} no tiene una estructura de vista reconocible");

                Name = match.Groups["name"].Value;

                Tokens = match.Groups["token"].Captures.Cast<Capture>().Select(c => c.Value).Distinct().Order().ToArray();
            }
        }
    }

  
}


