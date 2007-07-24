using System;
using System.Collections.Generic;
using System.Text;

namespace SerializadorTexto.SerializadorAPila.LenguajeIncontextual
{
    public delegate bool EsTerminal(string s);

    internal class Gramatica
    {
        public const string Lambda = ""; // cadena vacia 
        public const string End = "$"; 

        public List<string> Simbolos = new List<string>();
        public List<string> Terminales = new List<string>();
        public string SimboloInicial;
        public MultiDictionary<string, Regla> Reglas = new MultiDictionary<string, Regla>();

        public EsTerminal EsTerminal;

        public MultiDictionary<string,string> Primeros;
        public MultiDictionary<string, string> Siguientes;

        public TablaPrediccion TablaPrediccion; 

        public void AddRegla(Regla regla)
        {
            Reglas.Add(regla.Cabeza, regla);
        }

        private void CalcularPrimeros()
        {
            Primeros = new MultiDictionary<string, string>(); 
            foreach (string s in Simbolos)
            {
                CalcularPrimero(s);
            }
        }

        private List<string> CalcularPrimero(string simbolo)
        {
            List<string> primerosSimbolo;
            if (!Primeros.TryGetValue(simbolo, out primerosSimbolo))
            {
                primerosSimbolo = new List<string>();
                foreach (Regla regla in Reglas.GetList(simbolo))
                {
                    List<string> otros = CalcularPrimero(regla.Cola);
                    
                    foreach (string s in otros)
                        if (!primerosSimbolo.Contains(s))
                            primerosSimbolo.Add(s); 
                }

                Primeros.AddRange(simbolo, primerosSimbolo);
            }

            return primerosSimbolo;
        }

        private List<string> CalcularPrimero(string[] cadena)
        {
            if (cadena.Length == 0 || cadena.Length == 1 && cadena[0] == Lambda)
            {
                List<string> lista = new List<string>();
                lista.Add(Lambda);
                return lista;
            }
            else
            {
                List<string> result = new List<string>();
                foreach (string elem in cadena)
                {
                    if (EsTerminal(elem))
                    {
                        result.Add(elem);
                        return result;
                    }
                    else // es simbolo
                    {
                        List<string> otros = CalcularPrimero(elem);

                        foreach (string s in otros)
                            if (!result.Contains(s))
                                result.Add(s); 

                        if (!result.Remove(Lambda))
                            return result;
                    }
                }

                result.Add(Lambda);
                return result;
            }
        }

        private void CalcularSiguientes()
        {
            Siguientes = new MultiDictionary<string, string>();
            Siguientes.Add(this.SimboloInicial, End); 

            bool algoAñadido;
            do
            {
                algoAñadido = false;
                foreach (Regla r in Reglas)
                {
                    string[] cola = r.Cola;
                    for (int i = 0; i < cola.Length; i++)
                    {
                        string elem = cola[i];
                        if (!EsTerminal(elem))
                        {
                            string[] cadena = SubArray(cola, i);

                            List<string> prims = CalcularPrimero(cadena);
                            bool hasLambda = prims.Remove(Lambda);

                            algoAñadido |= Siguientes.AddRangeNoRepeat(elem, prims);
                            
                            List<string> prims2;
                            if (hasLambda && Siguientes.TryGetValue(r.Cabeza,out prims2))
                            {
                                algoAñadido |= Siguientes.AddRangeNoRepeat(elem, prims2);
                            }
                        }
                    }
                }
            } 
            while (algoAñadido); 

        }

        private static string[] SubArray(string[] cola, int i)
        {
            string[] cadena = new string[cola.Length - i - 1];
            for (int j = 0; j < cadena.Length; j++)
                cadena[j] = cola[j + 1 + i];
            return cadena;
        }            

        public void CalculaTablaPrediccion()
        {
            this.CalcularPrimeros();
            this.CalcularSiguientes();
            
            TablaPrediccion = new TablaPrediccion();         
            
            foreach (Regla r in Reglas)
            {
                List<string> primeros = CalcularPrimero(r.Cola);
                bool hasLambda = primeros.Remove(Lambda); 
                foreach (string term in primeros)
                {
                    AddATabla(new ParStrings(r.Cabeza, term), r); 
                }

                if (hasLambda)
                {
                    List<string> siguientes = Siguientes.GetList(r.Cabeza);
                    foreach (string term in siguientes)
                    {
                        AddATabla(new ParStrings(r.Cabeza, term), null);
                    }
                }
            }

        }

        public void AddATabla(ParStrings par, Regla regla)
        {
            Regla regla2; 
            if(TablaPrediccion.TryGetValue(par, out regla2))
            {
                if(regla != regla2)
                    throw new ArgumentException("Existe una ambiguedad LL(1), para el par " + par.ToString() + " la regla " + regla2 + " y la regla " + regla + " son validas");
                return; 
            }

            TablaPrediccion.Add(par, regla);
        }
    }
}
