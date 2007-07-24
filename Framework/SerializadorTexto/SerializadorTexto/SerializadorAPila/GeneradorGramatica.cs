using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using SerializadorTexto;
using SerializadorTexto.SerializadorAPila.LenguajeIncontextual;
using SerializadorTexto.SerializadorAPila.Reglas;
using SerializadorTexto.Atributos.Incontextual;

namespace SerializadorTexto.SerializadorAPila
{
    internal static class GeneradorGramatica
    {

        public static Gramatica GenerarGramatica(Type type)
        {
            Gramatica gr = new Gramatica();
            gr.EsTerminal = new EsTerminal(delegate(string token) { return (token.Length > 0 && token[0] == '[') ||token == Gramatica.End; });
            gr.SimboloInicial = GeneradorNombres.GenerarNombre(type.Name, false, false, false);           
            ExplorarTipo(type, gr);
            return gr; 
        }



        private static void ExplorarTipo(Type type, Gramatica gr)
        {
            string nombreClase = GeneradorNombres.GenerarNombre(type.Name, false, false, false);

            if (!gr.Simbolos.Contains(nombreClase))
                gr.Simbolos.Add(nombreClase); 

            ReglaClase regla = new ReglaClase(type, nombreClase);
            gr.AddRegla(regla);
            
            List<string> cadena = new List<string>();

            LineListInfoCache llic = ReflectorBloques.GetLineListInfoCache(type);

            foreach (LineInfoCache fa in llic.Fields)
            {
                Type ft = fa.FieldInfo.FieldType;
                Type listType = null; 
                bool esList = false; 
                if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(List<>))
                {
                    esList = true;
                    listType = ft; 
                    ft = ft.GetGenericArguments()[0];
                }

                bool esTermial = Reflector.DameAtributoUnico<LineaTextoIncontextualAttribute>(ft) != null;


                cadena.Add(GeneradorNombres.GenerarNombre(ft.Name, fa.Optional, esList, esTermial)); 

                if (fa.Optional)
                {
                    string nombreOpcional = GeneradorNombres.GenerarNombre(ft.Name, true, esList, esTermial);
                    string nombreNoOpcional = GeneradorNombres.GenerarNombre(ft.Name, false, esList, esTermial);

                    gr.AddRegla(new ReglaPrimerCampo(nombreOpcional, nombreNoOpcional));
                    gr.AddRegla(new Regla(nombreOpcional));

                    if (!gr.Simbolos.Contains(nombreOpcional))
                        gr.Simbolos.Add(nombreOpcional);
                }

                if (esList)
                {
                    string nombreLista = GeneradorNombres.GenerarNombre(ft.Name, false, true, esTermial);
                    string nombreNoLista = GeneradorNombres.GenerarNombre(ft.Name, false, false, esTermial);

                    gr.AddRegla(new ReglaLista(listType, nombreLista, nombreNoLista, nombreLista));
                    gr.AddRegla(new Regla(nombreLista));

                    if (!gr.Simbolos.Contains(nombreLista))
                        gr.Simbolos.Add(nombreLista);

                }

                if (esTermial)
                {
                    string nombreLista = GeneradorNombres.GenerarNombre(ft.Name, false, false, true);
                    if (!gr.Terminales.Contains(nombreLista))
                        gr.Terminales.Add(nombreLista); 
                }
                else
                {
                    ExplorarTipo(ft, gr); 
                }
             
            }
            regla.Cola = cadena.ToArray(); 
        }
    }
}
