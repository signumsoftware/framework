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
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;

namespace Signum.Entities
{
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false), InTypeScript(Undefined = false)]
    public abstract class Symbol : Entity
    {
        static Dictionary<Type, Dictionary<string, Symbol>> Symbols = new Dictionary<Type, Dictionary<string, Symbol>>();
        static Dictionary<Type, Dictionary<string, PrimaryKey>> Ids = new Dictionary<Type, Dictionary<string, PrimaryKey>>();

        public Symbol() { }

        /// <summary>
        /// Similar methods of inheritors will be automatically called by Signum.MSBuildTask using AutoInitiAttribute
        /// </summary>
        public Symbol(Type declaringType, string fieldName)
        {
            this.fieldInfo = declaringType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (this.fieldInfo == null)
                throw new InvalidOperationException(string.Format("No field with name {0} found in {1}", fieldName, declaringType.Name));

            this.Key = declaringType.Name + "." + fieldName;
            
            try
            {
                Symbols.GetOrCreate(this.GetType()).Add(this.Key, this);
            }
            catch (Exception e) when (StartParameters.IgnoredCodeErrors != null)
            {
                //Could happend if Dynamic code has a duplicated name
                this.fieldInfo = null;
                this.Key = null;
                StartParameters.IgnoredCodeErrors.Add(e);
                return;
            }

            var dic = Ids.TryGetC(this.GetType());
            if (dic != null)
            {
                PrimaryKey? id = dic.TryGetS(this.Key);
                if (id != null)
                    this.SetId(id.Value);
            }
        }

        private static bool IsStaticClass(Type type)
        {
            return type.IsAbstract && type.IsSealed;
        }

        [Ignore]
        FieldInfo fieldInfo;
        [HiddenProperty]
        public FieldInfo FieldInfo
        {
            get { return fieldInfo; }
            internal set { fieldInfo = value; }
        }


        [UniqueIndex]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
        public string Key { get; set; }

        static Expression<Func<Symbol, string>> ToStringExpression = e => e.Key;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public bool BaseEquals(object obj)
        {
            return base.Equals(obj);
        }

        public override bool Equals(object obj)
        {
            return obj is Symbol &&
                obj.GetType() == this.GetType() &&
                ((Symbol)obj).Key == Key;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        public string NiceToString()
        {
            return this.FieldInfo.NiceName();
        }

        public static void SetSymbolIds<S>(Dictionary<string, PrimaryKey> symbolIds)
            where S : Symbol
        {
            Symbol.Ids[typeof(S)] = symbolIds;

            var symbols = Symbol.Symbols.TryGetC(typeof(S));

            if (symbols != null)
            {
                foreach (var kvp in symbolIds)
                {
                    var s = symbols.TryGetC(kvp.Key);
                    if (s != null)
                        s.SetId(kvp.Value);
                }
            }
        }

        private void SetId(PrimaryKey id)
        {
            this.id = id;
            this.IsNew = false;
            this.toStr = this.Key;
            if (this.Modified != ModifiedState.Sealed)
                this.Modified = ModifiedState.Sealed;
        }

        internal static Dictionary<string, PrimaryKey> GetSymbolIds(Type type)
        {
            return Symbol.Ids.GetOrThrow(type);
        }
    }
}
