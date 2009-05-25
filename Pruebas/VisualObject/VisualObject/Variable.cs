using System;
using System.Collections.Generic;
using System.Text;

namespace VisualObject
{
    public struct Variable: IComparable<Variable>
    {
        public static Dictionary<string, string> aliases;

        static Variable(){
            aliases = new Dictionary<string, string>();
            aliases.Add("Byte",     "byte");
            aliases.Add("SByte",    "sbyte");
            aliases.Add("Int32",    "int");
            aliases.Add("UInt32",   "uint");
            aliases.Add("Int16",    "short");
            aliases.Add("UInt16",   "ushort");
            aliases.Add("Int64",    "long");
            aliases.Add("UInt64",   "ulong");
            aliases.Add("Single",   "float");
            aliases.Add("Double",   "double");
            aliases.Add("Char",     "char");
            aliases.Add("Boolean",  "bool");
            aliases.Add("Object",   "object");
            aliases.Add("String",   "string");
            aliases.Add("Decimal",  "decimal");
         }

        public static string MontarGenerico(Type genericType)
        {
            string[] result = genericType.Name.Split('`');
            Type[] tipos = genericType.GetGenericArguments();
            StringBuilder sb = new StringBuilder(result[0]);
            sb.Append('<');
            for (int i = 0; i < tipos.Length; i++)
            {
                string conv = TypeName(tipos[i]);
                sb.Append(conv);
                sb.Append((i== tipos.Length-1)?'>': ',');
            }
            return sb.ToString();
        }

        public static string TypeName(Type t)
        {
            if (t.IsGenericType)
            {
                return MontarGenerico(t);
            }        

            string name = t.Name;
            if (aliases.ContainsKey(name))
                return aliases[name];
            else
                return name; 
        }
        
        Type tipo;
        string nombre;

        public Variable(Type tipo, string nombre)
        {
            this.tipo = tipo;
            this.nombre = nombre; 
        }

        #region Propiedades
        public Type Tipo
        {
            get { return tipo; }
            set { tipo = value; }
        }

        public string Nombre
        {
            get { return nombre; }
            set { nombre = value; }
        } 
        #endregion

        #region De Object
        public override bool Equals(object obj)
        {
            if (obj.GetType() == this.GetType())
            {
                Variable var = (Variable)obj;
                return var.nombre == this.nombre &&
                       var.tipo == this.tipo;

            }
            return false;
        }

        public override string ToString()
        {
            return TypeName(tipo) +" " + this.nombre;
        }

        public override int GetHashCode()
        {
            return this.nombre.GetHashCode() ^ this.tipo.GetHashCode();
        } 
        #endregion

        #region IComparable<Variable> Members

        public int CompareTo(Variable other)
        {
            int com = nombre.CompareTo(other.nombre);
            if (com == 0) return TypeName(tipo).CompareTo(TypeName(other.tipo));
            return com; 
        }

        #endregion
}
}

