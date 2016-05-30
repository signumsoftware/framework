using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Entities;
using System.Web;
using Signum.Utilities;
using Signum.Entities.Basics;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using Signum.Engine.Operations;

namespace Signum.Web.Operations
{
    public abstract class OperationSettings
    {
        protected OperationSettings(OperationSymbol symbol)
        {
            this.OperationSymbol = symbol;
        }

        public OperationSymbol OperationSymbol { get; private set; }

        public abstract Type OverridenType { get; }

        public string Text { get; set; }

        public override string ToString()
        {
            return "{0}({1})".FormatWith(this.GetType().TypeName(), OperationSymbol.Key);
        }
    }

    #region ConstructorOperation
    public abstract class ConstructorOperationSettingsBase : OperationSettings
    {
        public abstract bool HasIsVisible { get; }
        public abstract bool OnIsVisible(IClientConstructorOperationContext ctx);

        public abstract bool HasConstructor { get; }
        public abstract Entity OnConstructor(IConstructorOperationContext ctx);

        public abstract bool HasClientConstructor { get; }
        public abstract JsFunction OnClientConstructor(IClientConstructorOperationContext ctx);

        protected ConstructorOperationSettingsBase(OperationSymbol symbol)
            : base(symbol)
        {

        }

        static GenericInvoker<Func<OperationSymbol, ConstructorOperationSettingsBase>> giCreate =
           new GenericInvoker<Func<OperationSymbol, ConstructorOperationSettingsBase>>(symbol => new ConstructorOperationSettings<Entity>(symbol));
        public static ConstructorOperationSettingsBase Create(Type type, OperationSymbol symbol)
        {
            return giCreate.GetInvoker(type)(symbol);
        }
    }

    public class ConstructorOperationSettings<T> : ConstructorOperationSettingsBase where T : class, IEntity
    {
        public Func<ClientConstructorOperationContext<T>, bool> IsVisible { get; set; }
        public Func<ClientConstructorOperationContext<T>, JsFunction> ClientConstructor { get; set; }

        public Func<ConstructorOperationContext<T>, T> Constructor { get; set; }

        public ConstructorOperationSettings(ConstructSymbol<T>.Simple constructOperation)
            : base(constructOperation.Symbol)
        {
        }

        internal ConstructorOperationSettings(OperationSymbol symbol)
            : base(symbol)
        {
        }

        public override bool HasIsVisible { get { return IsVisible != null; } }

        public override bool OnIsVisible(IClientConstructorOperationContext ctx)
        {
            return IsVisible((ClientConstructorOperationContext<T>)ctx);
        }

        public override bool HasClientConstructor { get { return ClientConstructor != null; } }

        public override JsFunction OnClientConstructor(IClientConstructorOperationContext ctx)
        {
            return ClientConstructor((ClientConstructorOperationContext<T>)ctx);
        }

        public override bool HasConstructor { get { return Constructor != null; } }

        public override Entity OnConstructor(IConstructorOperationContext ctx)
        {
            return (Entity)(IEntity)Constructor((ConstructorOperationContext<T>)ctx);
        }

        public override Type OverridenType
        {
            get { return typeof(T); }
        }
    }

    public interface IClientConstructorOperationContext
    {
        OperationInfo OperationInfo { get; }
        ClientConstructorContext ClientConstructorContext { get; }
        ConstructorOperationSettingsBase Settings { get; }
    }

    public class ClientConstructorOperationContext<T> : IClientConstructorOperationContext where T : class, IEntity
    {
        public OperationInfo OperationInfo { get; private set; }
        public ClientConstructorContext ClientConstructorContext { get; private set; }
        public ConstructorOperationSettings<T> Settings { get; private set; }

        public ClientConstructorOperationContext(OperationInfo info, ClientConstructorContext clientContext, ConstructorOperationSettings<T> settings)
        {
            this.OperationInfo = info;
            this.ClientConstructorContext = clientContext;
            this.Settings = settings;
        }

        ConstructorOperationSettingsBase IClientConstructorOperationContext.Settings
        {
            get { return Settings; }
        }
    }

    public interface IConstructorOperationContext
    {
        OperationInfo OperationInfo { get; }
        ConstructorContext ConstructorContext { get; }
        ConstructorOperationSettingsBase Settings { get; }
    }

    public class ConstructorOperationContext<T> : IConstructorOperationContext where T : class, IEntity
    {
        public OperationInfo OperationInfo { get; private set; }
        public ConstructorContext ConstructorContext { get; private set; }
        public ConstructorOperationSettings<T> Settings { get; private set; }

        public ConstructorOperationContext(OperationInfo info, ConstructorContext context, ConstructorOperationSettings<T> settings)
        {
            this.OperationInfo = info;
            this.ConstructorContext = context;
            this.Settings = settings;
        }

        ConstructorOperationSettingsBase IConstructorOperationContext.Settings
        {
            get { return Settings; }
        }
    }
    #endregion

    public abstract class ContextualOperationSettingsBase : OperationSettings
    {
        public double Order { get; set; }

        public abstract bool HasClick { get; }
        public abstract JsFunction OnClick(IContextualOperationContext ctx);

        public abstract bool HasIsVisible { get; }
        public abstract bool OnIsVisible(IContextualOperationContext ctx);

        public bool? HideOnCanExecute { get; set; }

        protected ContextualOperationSettingsBase(OperationSymbol symbol)
            : base(symbol)
        {
        }

        static GenericInvoker<Func<OperationSymbol, ContextualOperationSettingsBase>> giCreate =
            new GenericInvoker<Func<OperationSymbol, ContextualOperationSettingsBase>>(symbol => new ContextualOperationSettings<Entity>(symbol));
        public static ContextualOperationSettingsBase Create(Type type, OperationSymbol symbol)
        {
            return giCreate.GetInvoker(type)(symbol);
        }

        public BootstrapStyle? Style { get; set; }
    }

    public class ContextualOperationSettings<T> : ContextualOperationSettingsBase where T : class, IEntity
    {
        public ContextualOperationSettings(IConstructFromManySymbolContainer<T> symbolContainer)
            : base(symbolContainer.Symbol)
        {
        }


        internal ContextualOperationSettings(IEntityOperationSymbolContainer<T> symbolContainer)
            : base(symbolContainer.Symbol)
        {
        }

        internal ContextualOperationSettings(OperationSymbol symbol)
            : base(symbol)
        {
        }

        public Func<ContextualOperationContext<T>, bool> IsVisible { get; set; }
        public Func<ContextualOperationContext<T>, string> ConfirmMessage { get; set; }
        public Func<ContextualOperationContext<T>, JsFunction> Click { get; set; }

        public override bool HasIsVisible
        {
            get { return IsVisible != null; }
        }

        public override bool OnIsVisible(IContextualOperationContext ctx)
        {
            return IsVisible((ContextualOperationContext<T>)ctx);
        }

        public override bool HasClick
        {
            get { return Click != null; }
        }

        public override JsFunction OnClick(IContextualOperationContext ctx)
        {
            return Click((ContextualOperationContext<T>)ctx);
        }

        public override Type OverridenType
        {
            get { return typeof(T); }
        }
    }

    public interface IContextualOperationContext
    {
        OperationInfo OperationInfo { get; }
        string CanExecute { get; set; }
        ContextualOperationSettingsBase OperationSettings { get; }
        EntityOperationSettingsBase EntityOperationSettings { get; }
        SelectedItemsMenuContext Context { get; }

        Type Type { get; }

        JsOperationOptions Options();

        bool HideOnCanExecute { get; }

        IEnumerable<Lite<IEntity>> UntypedEntites { get; }
    }

    public class ContextualOperationContext<T> : IContextualOperationContext 
        where T : class, IEntity
    {
        public List<Lite<T>> Entities { get; private set; }
        public Type SingleType { get { return Entities.Select(a => a.EntityType).Distinct().Only(); } }

        public OperationInfo OperationInfo { get; private set; }
        public ContextualOperationSettings<T> OperationSettings { get; set; }
        public EntityOperationSettings<T> EntityOperationSettings { get; set; }

        public SelectedItemsMenuContext Context { get; private set; }
        public string Prefix { get { return Context.Prefix; } }    
        public UrlHelper Url { get { return Context.Url; } }
        public object QueryName { get { return Context.QueryName; } }

        public string CanExecute { get; set; }

        public ContextualOperationContext(SelectedItemsMenuContext ctx, OperationInfo info, ContextualOperationSettings<T> settings, EntityOperationSettings<T> entityOperationSettings)
        {
            this.Context = ctx;
            this.OperationInfo = info;
            this.OperationSettings = settings;
            this.Entities = Context.Lites.Cast<Lite<T>>().ToList();
            this.EntityOperationSettings = entityOperationSettings;
        }

        public JsOperationOptions Options()
        {
            var result = new JsOperationOptions(OperationInfo.OperationSymbol, this.Prefix) { isLite = OperationInfo.Lite , isContextual=true};

            result.confirmMessage = OperationSettings != null && OperationSettings.ConfirmMessage != null ? OperationSettings.ConfirmMessage(this) :
                OperationInfo.OperationType == OperationType.Delete ? OperationMessage.PleaseConfirmYouDLikeToDeleteTheSelectedEntitiesFromTheSystem.NiceToString() : null;

            return result;
        }

        ContextualOperationSettingsBase IContextualOperationContext.OperationSettings
        {
            get { return OperationSettings; }
        }

        EntityOperationSettingsBase IContextualOperationContext.EntityOperationSettings
        {
            get { return EntityOperationSettings; }
        }

        public Type Type
        {
            get { return typeof(T); }
        }

        public bool HideOnCanExecute
        {
            get
            {
                if (this.OperationSettings != null && this.OperationSettings.HideOnCanExecute.HasValue)
                    return this.OperationSettings.HideOnCanExecute.Value;

                if (this.EntityOperationSettings != null)
                    return this.EntityOperationSettings.HideOnCanExecute;

                return false;
            }
        }

        public IEnumerable<Lite<IEntity>> UntypedEntites
        {
            get { return Entities; }
        }

        public string ComposePrefix(string prefixPart)
        {
            return TypeContextUtilities.Compose(this.Prefix, prefixPart);
        }
    }

    public class EntityOperationGroup
    {
        public static readonly EntityOperationGroup None = new EntityOperationGroup();

        public static EntityOperationGroup Create = new EntityOperationGroup
        {
            Description = () => OperationMessage.Create.NiceToString(),
            SimplifyName = cs => Regex.Replace(cs, OperationMessage.CreateFromRegex.NiceToString(), m => m.Groups[1].Value.FirstUpper(), RegexOptions.IgnoreCase),
            CssClass = "sf-operation"
        };

        public Func<string> Description;
        public Func<string, string> SimplifyName;
        public string CssClass;
        public double Order = 100;
    }

    public abstract class EntityOperationSettingsBase : OperationSettings
    {
        public BootstrapStyle? Style { get; set; } 
        public double Order { get; set; }

        public abstract ContextualOperationSettingsBase ContextualUntyped { get; }
        public abstract ContextualOperationSettingsBase ContextualFromManyUntyped { get; }
       
        public EntityOperationGroup Group { get; set; }

        public abstract bool HasClick { get; }
        public abstract JsFunction OnClick(IEntityOperationContext ctx);

        public abstract bool HasIsVisible { get; }
        public abstract bool OnIsVisible(IEntityOperationContext ctx);

        public bool HideOnCanExecute { get; set; }

        public EntityOperationSettingsBase(OperationSymbol symbol)
            : base(symbol)
        {
            this.HideOnCanExecute = false;
        }

        static GenericInvoker<Func<OperationSymbol, EntityOperationSettingsBase>> giCreate =
            new GenericInvoker<Func<OperationSymbol, EntityOperationSettingsBase>>(symbol => new EntityOperationSettings<Entity>(symbol));
        public static EntityOperationSettingsBase Create(Type type, OperationSymbol symbol)
        {
            return giCreate.GetInvoker(type)(symbol);
        }

        public static Func<OperationInfo, BootstrapStyle> AutoStyleFunction { get; set; }
    }

    public class EntityOperationSettings<T> : EntityOperationSettingsBase where T : class, IEntity
    {
        public ContextualOperationSettings<T> ContextualFromMany { get; private set; }
        public ContextualOperationSettings<T> Contextual { get; private set; }

        public EntityOperationSettings(IEntityOperationSymbolContainer<T> symbolContainer)
            : base(symbolContainer.Symbol)
        {
            this.Contextual = new ContextualOperationSettings<T>(symbolContainer);
            this.ContextualFromMany = new ContextualOperationSettings<T>(symbolContainer); 
        }

        internal EntityOperationSettings(OperationSymbol symbol)
            : base(symbol)
        {
            this.Contextual = new ContextualOperationSettings<T>(symbol);
            this.ContextualFromMany = new ContextualOperationSettings<T>(symbol);
        }

        static EntityOperationSettings()
        {
            AutoStyleFunction = oi => oi.OperationType == OperationType.Delete ? BootstrapStyle.Danger :
                oi.OperationType == OperationType.Execute && oi.OperationSymbol.Key.EndsWith(".Save") ? BootstrapStyle.Primary :
                BootstrapStyle.Default;
        }

        public Func<EntityOperationContext<T>, bool> IsVisible { get; set; }
        public Func<EntityOperationContext<T>, JsFunction> Click { get; set; }
        public Func<EntityOperationContext<T>, string> ConfirmMessage { get; set; }

        public override bool HasIsVisible
        {
            get { return IsVisible != null; }
        }

        public override bool OnIsVisible(IEntityOperationContext ctx)
        {
            return IsVisible((EntityOperationContext<T>)ctx);
        }

        public override bool HasClick
        {
            get { return Click != null; }
        }

        public override JsFunction OnClick(IEntityOperationContext ctx)
        {
            return Click((EntityOperationContext<T>)ctx);
        }

        public override Type OverridenType
        {
            get { return typeof(T); }
        }

        public override ContextualOperationSettingsBase ContextualUntyped
        {
            get { return Contextual; }
        }

        public override ContextualOperationSettingsBase ContextualFromManyUntyped
        {
            get { return ContextualFromMany; }
        }
    }

    public interface IEntityOperationContext
    {
        EntityButtonContext Context { get; }

        OperationInfo OperationInfo { get; }
        IEntity Entity { get; }
        EntityOperationSettingsBase OperationSettings { get; }
        string CanExecute { get; set; }
        JsOperationOptions Options();
    }

    public class EntityOperationContext<T> : IEntityOperationContext where T : class, IEntity
    {
        public EntityButtonContext Context { get; private set; }
        public UrlHelper Url { get { return Context.Url; } }
        public string PartialViewName { get { return Context.PartialViewName; } }
        public string Prefix { get { return Context.Prefix; } }
        public ViewMode ViewMode { get { return Context.ViewMode; } }
        public bool ShowOperations { get { return Context.ShowOperations; } }

        public OperationInfo OperationInfo { get; private set; }
        public EntityOperationSettings<T> OperationSettings { get; private set; }

        public T Entity { get; private set; }
        public string CanExecute { get; set; }

        public EntityOperationContext(T entity, OperationInfo operationInfo, EntityButtonContext context, EntityOperationSettings<T> settings)
        {
            Entity = entity;
            OperationInfo = operationInfo;
            Context = context;
            OperationSettings = settings;
        }

        public JsOperationOptions Options()
        {
            var result = new JsOperationOptions(OperationInfo.OperationSymbol, this.Prefix) { isLite = OperationInfo.Lite };

            result.confirmMessage = OperationSettings != null && OperationSettings.ConfirmMessage != null ? OperationSettings.ConfirmMessage(this) :
                OperationInfo.OperationType == OperationType.Delete ? OperationMessage.PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem.NiceToString() : null;

            return result;
        }

        public override string ToString()
        {
            return OperationInfo.ToString();
        }


        public string ComposePrefix(string prefixPart)
        {
            return TypeContextUtilities.Compose(this.Prefix, prefixPart); 
        }

        IEntity IEntityOperationContext.Entity
        {
            get { return this.Entity; }
        }

        EntityOperationSettingsBase IEntityOperationContext.OperationSettings
        {
            get { return this.OperationSettings; }
        }
    }

   

    public class JsOperationOptions
    {
        public JsOperationOptions(OperationSymbol operation, string prefix)
        {
            this.operationKey = operation.Key;
            this.prefix = prefix;
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string operationKey;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string prefix;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? isLite;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string confirmMessage;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string controllerUrl;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? isContextual;
    }
}
