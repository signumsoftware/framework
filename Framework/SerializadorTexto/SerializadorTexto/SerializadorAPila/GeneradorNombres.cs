using System;
using System.Collections.Generic;
using System.Text;

namespace SerializadorTexto.SerializadorAPila
{
    internal static class GeneradorNombres
    {
        public static string GenerarNombre(string nombreClase, bool opcional, bool esLista, bool esTerminal)
        {
            return (opcional ? "?" : "") + (esLista ? "*" : "") + (esTerminal ? "[" : "") + nombreClase + (esTerminal ? "]" : ""); 
        }

        public static string ParsearNombre(string nombre, out bool opcional, out bool esLista, out bool esTerminal)
        {
            if(opcional = nombre.Contains("?"))
            {
                nombre = nombre.Replace("?", "");           
            }

            if (esLista = nombre.Contains("*"))
            {
                nombre = nombre.Replace("*", "");
            }

            if (esTerminal = nombre.Contains("["))
            {
                nombre = nombre.Replace("[", "");
                nombre = nombre.Replace("]", "");
            }

            return nombre;
        }
        public static string ParsearNombre(string nombre)
        {
            return nombre
                .Replace("?", "")
                .Replace("*", "")
                .Replace("[", "")
                .Replace("]", "");
        }
    }
}
