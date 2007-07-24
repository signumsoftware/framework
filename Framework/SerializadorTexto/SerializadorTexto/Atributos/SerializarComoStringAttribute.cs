using System;
using System.Collections.Generic;
using System.Text;

namespace SerializadorTexto.Atributos
{
    [AttributeUsage(AttributeTargets.Enum, Inherited = false)]
    public class SerializarComoStringAttribute : Attribute
    {
    }
}
