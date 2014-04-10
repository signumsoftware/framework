using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Signum.Utilities;

namespace Signum.Entities.Basics
{
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master)]
    public abstract class SemiSymbol : IdentifiableEntity
    {
        public SemiSymbol() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frame">Inheritors should use new StackFrame(1, false) and add [MethodImpl(MethodImplOptions.NoInlining)]</param>
        /// <param name="fieldName">Inheritors should use [CallerMemberName]</param>
        public void MakeSymbol(StackFrame frame, string fieldName)
        {
            var mi = frame.GetMethod();

            if (!mi.IsStatic || !mi.IsConstructor)
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


        [SqlDbType(Size = 200), UniqueIndex(AllowMultipleNulls=true)]
        string key;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 200)]
        public string Key
        {
            get { return key; }
            set { SetToStr(ref key, value); }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SemiSymbolManager.SymbolDeserialized.Invoke(this);
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


        internal string NiceToString()
        {
            return this.FieldInfo.NiceName();
        }

        static SemiSymbol()
        {
            DescriptionManager.DefaultDescriptionOptions += DescriptionManager_IsSymbolContainer;
        }

        static DescriptionOptions? DescriptionManager_IsSymbolContainer(Type t)
        {
            return t.IsAbstract && t.IsSealed &&
                t.GetFields(BindingFlags.Static | BindingFlags.Public)
                .Any(a => typeof(Symbol).IsAssignableFrom(a.FieldType)) ? DescriptionOptions.Members : (DescriptionOptions?)null;
        }

        [NotNullable, SqlDbType(Size = 100)]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { Set(ref name, value); }
        }

        static Expression<Func<SemiSymbol, string>> ToStringExpression = e => e.Name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public class SemiSymbolManager
    {
        public static Polymorphic<Action<SemiSymbol>> SymbolDeserialized = new Polymorphic<Action<SemiSymbol>>();

        static SemiSymbolManager()
        {
            SymbolDeserialized.Register((SemiSymbol s) =>
            {
                throw new InvalidOperationException(@"Symbols require that the id are set before accesing the database. 
If your curent AppDomain won't access the database (like a Windows application), call SymbolManager.AvoidSetIdOnDeserialized()");
            });
        }

        public static void AvoidSetIdOnDeserialized()
        {
            SymbolDeserialized.Register((SemiSymbol s) =>
            {
            });
        }
    }
}
