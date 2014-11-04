using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Windows;
using Signum.Entities;
using Signum.Entities.Basics;
using System.Reflection;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Windows.Operations;

namespace Signum.Windows
{
    public static class Constructor
    {
        public static ConstructorManager Manager; 

        public static void Start(ConstructorManager manager)
        {
            Manager = manager;
        }

        public static T Construct<T>(this ConstructorContext ctx)
            where T : ModifiableEntity
        {
            return (T)ctx.SurroundConstructUntyped(typeof(T), Manager.ConstructCore);
        }

        public static ModifiableEntity ConstructUntyped(this ConstructorContext ctx, Type type)
        {
            return ctx.SurroundConstructUntyped(type, Manager.ConstructCore);
        }

        public static T SurroundConstruct<T>(this ConstructorContext ctx, Func<ConstructorContext, T> constructor)
            where T : ModifiableEntity
        {
            ctx.Type = typeof(T);

            return (T)Manager.SurroundConstruct(ctx, constructor);
        }

        public static ModifiableEntity SurroundConstructUntyped(this ConstructorContext ctx, Type type, Func<ConstructorContext, ModifiableEntity> constructor)
        {
            ctx.Type = type;

            return Manager.SurroundConstruct(ctx, constructor);
        }

        public static void Register<T>(Func<ConstructorContext, T> constructor)
            where T : ModifiableEntity
        {
            Manager.Constructors.Add(typeof(T), constructor);
        }
    }

    public class ConstructorContext
    {
        public Type Type { get; internal set; }
        public FrameworkElement Element { get; private set; }
        public OperationInfo OperationInfo { get; private set; }
        public List<object> Args { get; private set; }
        public bool CancelConstruction { get; set; }

        public ConstructorContext(FrameworkElement element = null, OperationInfo operationInfo = null, List<object> args = null)
        {
            this.Element = element;
            this.Args = args ?? new List<object>();
            this.OperationInfo = operationInfo;
        }
    }

    public class ConstructorManager
    {
        public event Func<ConstructorContext, IDisposable> PreConstructors;

        public Dictionary<Type, Func<ConstructorContext, ModifiableEntity>> Constructors = new Dictionary<Type, Func<ConstructorContext, ModifiableEntity>>();

        public event Action<ConstructorContext, ModifiableEntity> PostConstructors;

        public ConstructorManager()
        {
            PostConstructors += PostConstructors_AddFilterProperties;
        }

        public virtual ModifiableEntity ConstructCore(ConstructorContext ctx)
        {
            Func<ConstructorContext, ModifiableEntity> c = Constructors.TryGetC(ctx.Type);
            if (c != null)
            {
                ModifiableEntity result = c(ctx);
                return result;
            }

            if (ctx.Type.IsIdentifiableEntity() && OperationClient.Manager.HasConstructOperations(ctx.Type))
                return OperationClient.Manager.Construct(ctx);

            return (ModifiableEntity)Activator.CreateInstance(ctx.Type);
        }

        public virtual ModifiableEntity SurroundConstruct(ConstructorContext ctx, Func<ConstructorContext, ModifiableEntity> constructor)
        {
            IDisposable disposable = null;
            try
            {
                foreach (var pre in PreConstructors.GetInvocationListTyped())
                {
                    disposable = Disposable.Combine(disposable, pre(ctx));

                    if (ctx.CancelConstruction)
                        return null;
                }

                var entity = constructor(ctx);

                if (entity == null || ctx.CancelConstruction)
                    return null;

                foreach (Action<ConstructorContext, ModifiableEntity> post in PostConstructors.GetInvocationListTyped())
                {
                    post(ctx, entity);

                    if (ctx.CancelConstruction)
                        return null;
                }

                return entity;
            }
            finally
            {
                if (disposable != null)
                    disposable.Dispose();
            }
        }


        public static void PostConstructors_AddFilterProperties(ConstructorContext ctx, ModifiableEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException("result");

            if (ctx.Element is SearchControl)
            {
                var filters = ((SearchControl)ctx.Element).FilterOptions.Where(fo => fo.Operation == FilterOperation.EqualTo && fo.Token is ColumnToken);

                var pairs = from pi in ctx.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            join fo in filters on pi.Name equals fo.Token.Key
                            where Server.CanConvert(fo.Value, pi.PropertyType) && fo.Value != null
                            select new { pi, fo };

                foreach (var p in pairs)
                {
                    p.pi.SetValue(entity, Server.Convert(p.fo.Value, p.pi.PropertyType), null);
                }
            }
        }

    }
}
