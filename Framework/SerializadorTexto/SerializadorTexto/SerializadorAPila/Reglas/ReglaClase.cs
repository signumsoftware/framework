using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Reflection;
using SerializadorTexto;
using SerializadorTexto.SerializadorAPila.LenguajeIncontextual;

namespace SerializadorTexto.SerializadorAPila.Reglas
{
    internal class ReglaClase : Regla
    {
        Type classType;
        List<LineInfoCache> fi;

        public ReglaClase(Type classType, string cabeza, params string[] cola)
            :base(cabeza, cola)
        {
            this.classType = classType;
            fi = ReflectorBloques.GetLineListInfoCache(classType).Fields;
        }

        public override void ComienzoRegla(object param, ref object result)
        {
            result = Activator.CreateInstance(classType);
        }

        public override void ProcesarSimbolo(int i, ref object result, object member)
        {
            fi[i].FieldInfo.SetValue(result, member); 
        }

        public override void ProcesarTerminal(int i, ref object result, object member)
        {
            fi[i].FieldInfo.SetValue(result, member); 
        }
    }
}
