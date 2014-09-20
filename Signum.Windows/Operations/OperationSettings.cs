using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.Windows;
using System.Windows.Media;
using Signum.Entities.Basics;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using Signum.Utilities;
using Signum.Services;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;

namespace Signum.Windows.Operations
{
    public abstract class OperationSettings
    {
        public OperationSymbol OperationSymbol { get; private set; }
        public string Text { get; set; }
        public ImageSource Icon { get; set; }
        public Color? Color { get; set; }

        public abstract Type OverridenType { get; }

        public OperationSettings(IOperationSymbolContainer symbol)
        {
            this.OperationSymbol = symbol.Symbol;
        }

        public OperationSettings(OperationSymbol operationSymbol)
        {
            this.OperationSymbol = operationSymbol;
        }

        public override string ToString()
        {
            return "{0}({1})".Formato(this.GetType().TypeName(), OperationSymbol.Key);
        }
    }

    #region ConstructorOperation
    public abstract class ConstructorOperationSettingsBase : OperationSettings
    {
        public abstract bool HasIsVisible { get; }
        public abstract bool OnIsVisible(IConstructorOperationContext ctx);

        public abstract bool HasConstructor { get; }
        public abstract IdentifiableEntity OnConstructor(IConstructorOperationContext ctx);

        protected ConstructorOperationSettingsBase(IOperationSymbolContainer constructOperation)
            : base(constructOperation)
        {
            
        }
    }

    public class ConstructorOperationSettings<T> : ConstructorOperationSettingsBase where T : class, IIdentifiable
    {
        public Func<ConstructorOperationContext<T>, bool> IsVisible { get; set; }
        public Func<ConstructorOperationContext<T>, T> Constructor { get; set; }

        public ConstructorOperationSettings(ConstructSymbol<T>.Simple constructOperation)
            : base(constructOperation)
        {
        }

        public override bool HasIsVisible { get { return IsVisible != null; } }

        public override bool OnIsVisible(IConstructorOperationContext ctx)
        {
            return IsVisible((ConstructorOperationContext<T>)ctx);
        }

        public override bool HasConstructor { get { return Constructor != null; } }

        public override IdentifiableEntity OnConstructor(IConstructorOperationContext ctx)
        {
            return (IdentifiableEntity)(IIdentifiable)Constructor((ConstructorOperationContext<T>)ctx);
        }

        public override Type OverridenType
        {
            get { return typeof(T); }
        }
    }

    public interface IConstructorOperationContext
    {
        OperationInfo OperationInfo { get; }
        ConstructorContext ConstructorContext { get; }
        ConstructorOperationSettingsBase Settings { get; }
    }

    public class ConstructorOperationContext<T> : IConstructorOperationContext where T : class, IIdentifiable
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

    #region ContextualOperation
    public abstract class ContextualOperationSettingsBase : OperationSettings
    {
        public double Order { get; set; }

        public abstract bool HasClick { get; }
        public abstract void OnClick(IContextualOperationContext ctx);

        public abstract bool HasIsVisible { get; }
        public abstract bool OnIsVisible(IContextualOperationContext ctx);

        protected ContextualOperationSettingsBase(IOperationSymbolContainer constructOperation)
            : base(constructOperation)
        {
        }
    }

    public class ContextualOperationSettings<T> : ContextualOperationSettingsBase where T : class, IIdentifiable
    {
        public Func<ContextualOperationContext<T>, string> ConfirmMessage { get; set; }
        public Action<ContextualOperationContext<T>> Click { get; set; }
        public Func<ContextualOperationContext<T>, bool> IsVisible { get; set; }

        public ContextualOperationSettings(IConstructFromManySymbolContainer<T> symbolContainer)
            : base(symbolContainer)
        {
        }


        internal ContextualOperationSettings(IEntityOperationSymbolContainer<T> symbolContainer)
            : base(symbolContainer)
        {
        }

        public override bool HasClick
        {
            get { return Click != null; }
        }

        public override void OnClick(IContextualOperationContext ctx)
        {
            Click((ContextualOperationContext<T>)ctx);
        }

        public override bool HasIsVisible
        {
            get { return IsVisible != null; }
        }

        public override bool OnIsVisible(IContextualOperationContext ctx)
        {
            return IsVisible((ContextualOperationContext<T>)ctx);
        }

        public override Type OverridenType
        {
            get { return typeof(T); }
        }
    }

    public interface IContextualOperationContext
    {
        IEnumerable<Lite<IIdentifiable>> Entities { get; }
        SearchControl SearchControl { get; }
        OperationInfo OperationInfo { get; }
        string CanExecute { get; set; }
        ContextualOperationSettingsBase OperationSettings { get; }
        MenuItem SenderMenuItem { get; set; }

        bool ConfirmMessage();

        Type Type { get; }
    }

    public class ContextualOperationContext<T> : IContextualOperationContext where T : class, IIdentifiable
    {
        public List<Lite<T>> Entities { get; private set; }
        public Type SingleType { get { return Entities.Select(a => a.EntityType).Distinct().Only(); } }
        public SearchControl SearchControl { get; private set; }
        public OperationInfo OperationInfo { get; private set; }
        public string CanExecute { get; set; }
        public ContextualOperationSettings<T> OperationSettings { get; private set; }
        public MenuItem SenderMenuItem { get; set; }

        public ContextualOperationContext(SearchControl searchControl, OperationInfo info, ContextualOperationSettings<T> settings)
        {
            this.SearchControl = searchControl;
            this.OperationInfo = info;
            this.OperationSettings = settings;
            this.Entities = searchControl.SelectedItems.Cast<Lite<T>>().ToList();
        }

        public bool ConfirmMessage()
        {
            string message = OperationSettings != null && OperationSettings.ConfirmMessage != null ? OperationSettings.ConfirmMessage(this) :
                OperationInfo.OperationType == OperationType.Delete && Entities.Count > 1 ? OperationMessage.PleaseConfirmYouDLikeToDeleteTheSelectedEntitiesFromTheSystem.NiceToString() :
                OperationInfo.OperationType == OperationType.Delete && Entities.Count == 1 ? OperationMessage.PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem.NiceToString() : null;

            if (message == null)
                return true;

            return MessageBox.Show(Window.GetWindow(SearchControl), message, OperationInfo.OperationSymbol.NiceToString(), MessageBoxButton.OKCancel) == MessageBoxResult.OK;
        }

        IEnumerable<Lite<IIdentifiable>> IContextualOperationContext.Entities
        {
            get { return Entities; }
        }

        ContextualOperationSettingsBase IContextualOperationContext.OperationSettings
        {
            get { return OperationSettings; }
        }

        public Type Type
        {
            get { return Entities.Select(l => l.EntityType).Distinct().Only() ?? typeof(T); }
        }
    } 
    #endregion

    #region EntityOperation
    public class EntityOperationGroup
    {
        public static readonly EntityOperationGroup None = new EntityOperationGroup();

        public static EntityOperationGroup Create = new EntityOperationGroup
        {
            Text = () => OperationMessage.Create.NiceToString(),
            SimplifyName = cs => Regex.Replace(cs, OperationMessage.CreateFromRegex.NiceToString(), m => m.Groups[1].Value.FirstUpper(), RegexOptions.IgnoreCase),
            Background = Brushes.Green,
            AutomationName = "Create"
        };

        public Func<string> Text;
        public Func<string, string> SimplifyName;
        public Brush Background;
        public string AutomationName;
        public double Order = 100;
    }


    public abstract class EntityOperationSettingsBase : OperationSettings
    {
        public bool AvoidMoveToSearchControl { get; set; }
        public double Order { get; set; }

        public EntityOperationGroup Group { get; set; }

        public abstract bool HasClick { get; }
        public abstract IdentifiableEntity OnClick(IEntityOperationContext ctx);

        public abstract bool HasIsVisible { get; }
        public abstract bool OnIsVisible(IEntityOperationContext ctx);


        protected EntityOperationSettingsBase(IOperationSymbolContainer constructOperation)
            : base(constructOperation)
        {
        }

        public abstract ContextualOperationSettingsBase ContextualUntyped { get; }
        public abstract ContextualOperationSettingsBase ContextualFromManyUntyped { get; }
    }

    public class EntityOperationSettings<T> : EntityOperationSettingsBase where T : class, IIdentifiable
    {
        public Func<EntityOperationContext<T>, string> ConfirmMessage { get; set; }
        public Func<EntityOperationContext<T>, IdentifiableEntity> Click { get; set; }
        public Func<EntityOperationContext<T>, bool> IsVisible { get; set; }

        public ContextualOperationSettings<T> ContextualFromMany { get; private set; }
        public ContextualOperationSettings<T> Contextual { get; private set; }

        /// <param name="symbolContainer">A ExecuteSymbol&lt;T&gt;, DeleteSymbol&lt;T&gt; or a Construct&lt;R&gt;.From&lt;T&gt;</param>
        public EntityOperationSettings(IEntityOperationSymbolContainer<T> symbolContainer)
            : base(symbolContainer)
        {
            Contextual = new ContextualOperationSettings<T>(symbolContainer);
            ContextualFromMany = new ContextualOperationSettings<T>(symbolContainer);
        }

        public override ContextualOperationSettingsBase ContextualUntyped
        {
            get { return this.Contextual; }
        }

        public override ContextualOperationSettingsBase ContextualFromManyUntyped
        {
            get { return this.ContextualFromMany; }
        }

        public override bool HasClick
        {
            get { return this.Click != null; }
        }

        public override IdentifiableEntity OnClick(IEntityOperationContext ctx)
        {
            return this.Click((EntityOperationContext<T>)ctx);
        }

        public override bool HasIsVisible
        {
            get { return this.IsVisible != null; }
        }

        public override bool OnIsVisible(IEntityOperationContext ctx)
        {
            return this.IsVisible((EntityOperationContext<T>)ctx); 
        }

        public override Type OverridenType
        {
            get { return typeof(T); }
        }
    }

    public interface IEntityOperationContext
    {
        FrameworkElement EntityControl { get; }
        Control SenderButton { get; set; }
        OperationInfo OperationInfo { get; }
        ViewMode ViewButtons { get; }
        bool ShowOperations { get; }
        string CanExecute { get; set; }

        IIdentifiable Entity { get; }
        EntityOperationSettingsBase OperationSettings { get; }

        bool ConfirmMessage();
    }


    public class EntityOperationContext<T> : IEntityOperationContext where T : class, IIdentifiable
    {
        public EntityButtonContext Context { get; private set; }
        public FrameworkElement EntityControl { get { return Context.MainControl; } }
        public Control SenderButton { get; set; }
        public OperationInfo OperationInfo { get; internal set; }
        public ViewMode ViewButtons { get { return Context.ViewButtons; } }
        public bool ShowOperations { get { return Context.ShowOperations; } }
        public string CanExecute { get; set; }

        public T Entity { get; internal set; }
        public EntityOperationSettings<T> OperationSettings { get; internal set; }

        public EntityOperationContext(T entity, OperationInfo operationInfo, EntityButtonContext context, EntityOperationSettings<T> settings)
        {
            Entity = entity;
            OperationInfo = operationInfo;
            Context = context;
            OperationSettings = settings;
        }

        public bool ConfirmMessage()
        {
            string message = OperationSettings != null && OperationSettings.ConfirmMessage != null ? OperationSettings.ConfirmMessage(this) :
                OperationInfo.OperationType == OperationType.Delete ? OperationMessage.PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem.NiceToString() : null;

            if (message == null)
                return true;

            return MessageBox.Show(Window.GetWindow(EntityControl), message, OperationInfo.OperationSymbol.NiceToString(), MessageBoxButton.OKCancel) == MessageBoxResult.OK;
        }

        IIdentifiable IEntityOperationContext.Entity
        {
            get { return Entity; }
        }

        EntityOperationSettingsBase IEntityOperationContext.OperationSettings
        {
            get { return OperationSettings; }
        }
    } 
    #endregion
}
