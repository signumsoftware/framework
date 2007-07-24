using System;
using System.Collections.Generic;
using System.Text;
using SerializadorTexto.SerializadorAPila.Reglas;

namespace SerializadorTexto.SerializadorAPila.LenguajeIncontextual
{
    internal abstract class Automata
    {
        protected abstract void NextToken();
        protected abstract string PeekToken(ref object token);

        protected Gramatica _gramatica;
        TablaPrediccion _prediccion;
        object _result; 

        string _errorMessage = null;

        public string ErrorMessage
        {
            get { return _errorMessage; }
        }

        public object Resultado
        {
            get { return _result; }
        } 

        public Automata(Gramatica gramatica)
        {
            _gramatica = gramatica;
            _prediccion = gramatica.TablaPrediccion; 
        }

        public bool Procesar()
        {
            Regla primera =  new ReglaPrimerCampo("Inicio",_gramatica.SimboloInicial, Gramatica.End);
            NextToken();
            _result = null;
            return ProcesarRegla(primera, null, ref _result);
        }

        protected bool ProcesarRegla(Regla regla, object param, ref object result)
        {
            regla.ComienzoRegla(param, ref result);
            string[] cadena = regla.Cola;

            for (int i = 0; i < cadena.Length; i++)
            {
                object memberTerminal=null; 
                string token = PeekToken(ref memberTerminal);

                if (_gramatica.EsTerminal(cadena[i]))
                {                    
                    if (token != cadena[i])
                    {
                        _errorMessage = "Aplicando la regla " + regla + " en el terminal " + i + " (" + cadena[i] + ") se encontro un terminal " + token;
                        return false; 
                    }
                    regla.ProcesarTerminal(i,ref result, memberTerminal); 

                    NextToken();
                }
                else
                {
                    ParStrings clave = new ParStrings(cadena[i], token); 
                    Regla nueva;
                    if (_prediccion.TryGetValue(clave, out nueva))
                    {
                        if (nueva == null)
                        {
                            // no hacer nada 
                        }
                        else
                        {
                            object memberSimbolo = null;
                            if (!ProcesarRegla(nueva, result, ref memberSimbolo))
                                return false;

                            regla.ProcesarSimbolo(i, ref result, memberSimbolo); 
                        }
                    }
                    else
                    {
                        _errorMessage  = "Aplicando la regla " + regla + " en el símbolo " + i + " (" + cadena[i] + ") se encontro un terminal " + token + " para el que no seconoce una prediccion posible";
                        return false; 
                    }
                }
            }
            return true; 
        }     
    }
}
