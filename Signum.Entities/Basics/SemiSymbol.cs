using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Basics
{
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false)]
    public abstract class SemiSymbol : Entity
    {
        static Dictionary<Type, Dictionary<string, SemiSymbol>> Symbols = new Dictionary<Type, Dictionary<string, SemiSymbol>>();
        static Dictionary<Type, Dictionary<string, Tuple<PrimaryKey, string>>> Ids = new Dictionary<Type, Dictionary<string, Tuple<PrimaryKey, string>>>();

        public SemiSymbol() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frame">Inheritors should use new StackFrame(1, false) and add [MethodImpl(MethodImplOptions.NoInlining)]</param>
        /// <param name="fieldName">Inheritors should use [CallerMemberName]</param>
        protected void MakeSymbol(StackFrame frame, string fieldName)
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

            var dic = Ids.TryGetC(this.GetType());
            if (dic != null)
            {
                var tup = dic.TryGetC(this.Key);
                if (tup != null)
                    this.SetIdAndName(tup);
            }
            Symbols.GetOrCreate(this.GetType()).Add(this.Key, this);
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
            internal set { fieldInfo = value; }
        }


        [SqlDbType(Size = 200), UniqueIndex(AllowMultipleNulls = true)]
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 200)]
        public string Key { get; set; }

        internal string NiceToString()
        {
            return this.FieldInfo.NiceName();
        }

        static SemiSymbol()
        {
            DescriptionManager.DefaultDescriptionOptions += DescriptionManager_IsSymbolContainer;
            DescriptionManager.Invalidate();
        }

        static DescriptionOptions? DescriptionManager_IsSymbolContainer(Type t)
        {
            return t.IsAbstract && t.IsSealed &&
                t.GetFields(BindingFlags.Static | BindingFlags.Public)
                .Any(a => typeof(SemiSymbol).IsAssignableFrom(a.FieldType)) ? DescriptionOptions.Members : (DescriptionOptions?)null;
        }

        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        static Expression<Func<SemiSymbol, string>> ToStringExpression = e => e.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public static void SetSemiSymbolIdsAndNames<S>(Dictionary<string, Tuple<PrimaryKey, string>> symbolIds)
            where S : SemiSymbol
        {
            SemiSymbol.Ids[typeof(S)] = symbolIds;

            var symbols = SemiSymbol.Symbols.TryGetC(typeof(S));

            if (symbols != null)
            {
                foreach (var kvp in symbolIds)
                {
                    var s = symbols.TryGetC(kvp.Key);
                    if (s != null)
                        s.SetIdAndName(kvp.Value);
                }
            }
        }

        private void SetIdAndName(Tuple<PrimaryKey, string> idAndName)
        {
            this.id = idAndName.Item1;
            this.Name = idAndName.Item2;
            this.IsNew = false;
            this.toStr = this.Key;
            if (this.Modified != ModifiedState.Sealed)
                this.Modified = ModifiedState.Sealed;
        }

        internal static Dictionary<string, Tuple<PrimaryKey, string>> GetSemiSymbolIdsAndNames(Type type)
        {
            return SemiSymbol.Ids.GetOrThrow(type);
        }
    }
}
