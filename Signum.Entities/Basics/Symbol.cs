using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using Signum.Entities.Basics;
using Signum.Utilities;
using Signum.Utilities.Reflection;

namespace Signum.Entities
{
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master)]
    public abstract class Symbol : IdentifiableEntity
    {
        public Symbol() { }
      
        /// <summary>
        /// 
        /// </summary>
        /// <param name="frame">Inheritors should use new StackFrame(1, false) and add [MethodImpl(MethodImplOptions.NoInlining)]</param>
        /// <param name="fieldName">Inheritors should use [CallerMemberName]</param>
        public Symbol(StackFrame frame, string fieldName)
        {
            var mi = frame.GetMethod();

            if (mi != mi.DeclaringType.TypeInitializer)
                throw new InvalidOperationException(string.Format("{0} {1} can only be created in static field initializers", GetType().Name, fieldName));

            if (!IsStaticClass(mi.DeclaringType))
                throw new InvalidOperationException(string.Format("{0} {1} is declared in {2}, but {2} is not static", GetType().Name, fieldName, mi.DeclaringType.Name));

            this.fieldInfo = mi.DeclaringType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (this.fieldInfo == null)
                throw new InvalidOperationException(string.Format("No field with name {0} found in {1}", fieldName, mi.DeclaringType.Name));

            this.Key = mi.DeclaringType.Name + "." + fieldName;
        }

        private static bool IsStaticClass(Type type)
        {
            return type.IsAbstract && type.IsSealed;
        }

        [Ignore]
        FieldInfo fieldInfo;
        public FieldInfo FieldInfo
        {
            get { return fieldInfo; }
        }


        [NotNullable, SqlDbType(Size = 200), UniqueIndex]
        string key;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
        public string Key
        {
            get { return key; }
            set { SetToStr(ref key, value); }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SymbolManager.SymbolDeserialized.Invoke(this);
        }


        static Expression<Func<Symbol, string>> ToStringExpression = e => e.Key;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public override bool Equals(object obj)
        {
            return obj is Symbol &&
                obj.GetType() == this.GetType() &&
                ((Symbol)obj).Key == Key;
        }

        public override int GetHashCode()
        {
            return key.GetHashCode();
        }


        public string NiceToString()
        {
            return this.FieldInfo.NiceName();
        }
    }

    public class SymbolManager
    {
        public static Polymorphic<Action<Symbol>> SymbolDeserialized = new Polymorphic<Action<Symbol>>();

        static SymbolManager()
        {
            SymbolDeserialized.Register((Symbol s) =>
            {
                throw new InvalidOperationException(@"Symbols require that the id are set before accesing the database. 
If your curent AppDomain won't access the database (like a Windows application), call SymbolManager.AvoidSetIdOnDeserialized()");
            });
        }

        public static void AvoidSetIdOnDeserialized()
        {
            SymbolDeserialized.Register((Symbol s) =>
            {
            });
        }
    }
}
