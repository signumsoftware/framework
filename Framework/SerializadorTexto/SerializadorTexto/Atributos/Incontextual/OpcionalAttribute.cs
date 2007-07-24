using System;
using System.Collections.Generic;
using System.Text;

namespace SerializadorTexto.Atributos.Incontextual
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public sealed class OpcionalAttribute : Attribute
    {  
    }
}
