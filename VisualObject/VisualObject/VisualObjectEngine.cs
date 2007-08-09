using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Drawing;
using System.Collections;
using System.Drawing.Drawing2D;
using Sharp3D.Math.Core;
using System.Security.Permissions;

namespace VisualObject
{
    internal class ComparadorObjectosPorReferencia : IEqualityComparer<object>
    {
        bool IEqualityComparer<object>.Equals(object a, object b)
        {
            return object.ReferenceEquals(a, b);
        }

        int IEqualityComparer<object>.GetHashCode(object obj)
        {
            return obj.GetHashCode();
        }
    }


    [ReflectionPermission(SecurityAction.Demand, TypeInformation = true)]
    public class VisualObjectEngine
    {
        Size size;
        internal static Random r = new Random(); 
        TipoRefDibujable tipoPrincipal;
        
        ConjuntoDibujables cjtDibujables = new ConjuntoDibujables(10);
        Dictionary<object, TipoRefDibujable> objsPorReferencia = new Dictionary<object, TipoRefDibujable>(new ComparadorObjectosPorReferencia());

        int limiteNumeroNiveles;
        int limiteNumeroObjetos;
        
       


        public VisualObjectEngine(object o, int limiteNumeroNiveles,int limiteNumeroObjetos, Size size)
        {
            this.size = size;
            this.limiteNumeroNiveles = limiteNumeroNiveles;
            this.limiteNumeroObjetos = limiteNumeroObjetos;
            tipoPrincipal = (TipoRefDibujable)ToTipoDibujable(o, true,0);
            tipoPrincipal.PosicionInicio(size.Height); 
            tipoPrincipal.ProcesarMovimiento = false;             
        }

        private TipoDibujable ToTipoDibujable(object o, bool refTypeVar, int numNiveles)
        {
            
            
            if (o == null) return new NullDibujable();

            Type t = o.GetType();
            if (refTypeVar && (t.IsValueType)) // caso especial para el boxing
            {
                TipoDibujable td = ToTipoDibujable(o, false,numNiveles);
                BoxedValueType result =  new BoxedValueType(t, o.ToString(), td);
                result.GenerarSize(size);
                AñadirDibujable(o, result);
                return result; 
            }
            if (t.IsPrimitive)
            {
                string value = o.ToString();
                return new PrimitivoDibujable(t, value);
            }
            if (t == typeof(string))
            {
                string value = o.ToString();
                return new PrimitivoDibujable(t, "\"" + value + "\""); 
            }    
            else if (t.IsValueType) // solo structs
            {
                
                StructDibujable sd = new StructDibujable(t, o.ToString());
                foreach (FieldInfo f in t.GetFields(BindingFlags.NonPublic|
                                                    BindingFlags.Public|
                                                    BindingFlags.Instance))
                {
                    if (!f.IsStatic)
                    {
                        string nombre = f.Name;

                        object value = f.GetValue(o);
                        TipoDibujable td = ToTipoDibujable(value, !f.FieldType.IsValueType, numNiveles);
                        sd.Elementos.Add(new Variable(f.FieldType, nombre), td);
                    }
                }
                return sd; 
            }
            else if (t.IsClass)
            {
                if (objsPorReferencia.ContainsKey(o))
                {
                    return objsPorReferencia[o];
                }
                else if (numNiveles >= limiteNumeroNiveles || cjtDibujables.Objetos.Count>=limiteNumeroObjetos)
                {
                    return new NullForcedDibujable(); 
                }
                else
                {
                    if (o is ICollection)
                    {
                        ICollection col = (ICollection)o;

                        Type tipoElement = TipoDeColeccion(t);
                        bool refTypeArray = !tipoElement.IsValueType;
                        CollectionDibujable ald = new CollectionDibujable(col.Count, tipoElement, t, o.ToString());
                        AñadirDibujable(o, ald);
                        int i = 0;
                        foreach (object obj in col)
                        {
                            TipoDibujable td = ToTipoDibujable(obj, refTypeArray, numNiveles + 1);
                            ald.Elementos[i++] = td;
                        }
                        ald.GenerarSize(size);
                        return ald;
                    }
                    else
                    {
                        ClassDibujable cd = new ClassDibujable(t, o.ToString());
                        AñadirDibujable(o, cd);
                        foreach (FieldInfo f in t.GetFields(BindingFlags.NonPublic |
                                                            BindingFlags.Public |
                                                            BindingFlags.Instance))
                        {
                            if (!f.IsStatic)
                            {
                                string nombre = f.Name;
                                object value = f.GetValue(o);
                                TipoDibujable td = ToTipoDibujable(value, !f.FieldType.IsValueType, numNiveles + 1);
                                cd.Elementos.Add(new Variable(f.FieldType, nombre), td);
                            }
                        }
                        cd.GenerarSize(size);
                        return cd;
                    }
                }
            }
           
            else  return null; 
        }

        public void MoverTodos(){
            cjtDibujables.MoverTodas();
        }

        private void AñadirDibujable(object o, TipoRefDibujable result)
        {
            objsPorReferencia.Add(o, result);
            cjtDibujables.Objetos.Add(result);
        }

        private Type TipoDeColeccion(Type t)
        {
            if (t.IsArray)
            {
                return t.GetElementType();
            }
            else
            {
                Type[] interfaces = t.GetInterfaces();
                Type res = Array.Find<Type>(interfaces, delegate(Type tipo) { return tipo.Name == "IEnumerable`1"; });
                if (res != null)
                {
                    return res.GetGenericArguments()[0]; 
                }
            }
            return typeof(object); 
        }


        public void Dibujar(System.Drawing.Graphics gr, Size s)
        {
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            for (int i = 0; i < cjtDibujables.Objetos.Count; i++)
            {
                cjtDibujables.Objetos[i].Dibujar(gr);
            }
            using(Font f = new Font("MS Sans Serif",8)){
                gr.DrawString(cjtDibujables.FPS.ToString(), f, Brushes.Black, new PointF(10, 10));
            } 
        }


        public TipoRefDibujable TipoEn(int x, int y)
        {
            Point p = new Point(x,y);
            for (int i = cjtDibujables.Objetos.Count - 1; i >= 0; i--)
            {
                TipoRefDibujable trd = cjtDibujables.Objetos[i];
                if (trd.Rectangulo.Contains(p)) 
                    return trd;
            }
            return null; 
        }
    }
}
