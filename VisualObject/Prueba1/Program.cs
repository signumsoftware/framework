using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.DebuggerVisualizers;
using VisualObject;
using System.Drawing;
using System.Diagnostics;

namespace Prueba1
{
    [Serializable()]
    public class Animal
    {
        object o;
        Ojo ojo1;
        Ojo ojo2;
        Pata[] patas;
        Oreja oreja1;
        Oreja oreja2;
        public Animal(object o, Ojo ojo1, Ojo ojo2, Oreja oreja1, Oreja oreja2, Pata[] patas)
        {
            this.o = o;
            this.ojo1 = ojo1;
            this.ojo2 = ojo2;
            this.oreja1 = oreja1;
            this.oreja2 = oreja2;
            this.patas = patas;
        }
    }
    [Serializable()]
    public class Oreja
    {
        bool conPelo;
        public Oreja(bool conPelo)
        {
            this.conPelo = conPelo;
        }
    }
    [Serializable()]
    public class Pata
    {
        int longitud;
        Animal a;

        public Animal A
        {
            get { return a; }
            set { a = value; }
        }
        public Pata(int longitud)
        {
            this.longitud = longitud;
        }
    }
    [Serializable()]
    public class Ojo
    {
        Color col;
        public Ojo(Color col)
        {
            this.col = col;
        }
    }

    [Serializable()]
    public class NodoLista
    {
        NodoLista siguiente;

        public NodoLista Siguiente
        {
            get { return siguiente; }
            set { siguiente = value; }
        }
        int valor;
        public NodoLista(int valor)
        {
            this.valor = valor;
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            string a = "hola";
            string b = a;

            Ojo ojo1 = new Ojo(Color.Black);
            Ojo ojo2 = new Ojo(Color.Wheat);
            Oreja unica = new Oreja(true);
            Pata alante = new Pata(2);
            Pata atras = new Pata(3);
            Animal an = new Animal(true, ojo1, ojo2, unica, unica, new Pata[] { alante, alante, atras, atras });
            alante.A = an;
            atras.A = an;



            /*
                        NodoLista primero = new NodoLista(0);
                        NodoLista anterior = primero;
                        for (int i = 1; i < 15; i++)
                        {
                            anterior.Siguiente = new NodoLista(i);
                            anterior = anterior.Siguiente;                   
                        }
                        */

            VisualizerDevelopmentHost host = new VisualizerDevelopmentHost(
                an, typeof(VisualObjectPlugin.VisualObjectVisualizer));
            host.ShowVisualizer();


            Process.Start("VisualObject.exe"); 
        }

        private static void Guenas(string a, string b)
        {
            Console.WriteLine(a);
            Console.WriteLine(b);
        }
    }

}
