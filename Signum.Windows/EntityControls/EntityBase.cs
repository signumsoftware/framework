using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.ComponentModel;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Reflection;
using System.Windows.Input;
using System.Windows.Automation;
using Signum.Utilities.DataStructures;

namespace Signum.Windows
{
    public class EntityBase : LineBase
    {
        public static RoutedCommand CreateCommand =
          new RoutedCommand("Create", typeof(EntityLine), new InputGestureCollection(new InputGesture[] { new KeyGesture(Key.N, ModifierKeys.Control, "Create") }));
        public static RoutedCommand ViewCommand =
            new RoutedCommand("View", typeof(EntityLine), new InputGestureCollection(new InputGesture[] { new KeyGesture(Key.G, ModifierKeys.Control, "View") }));
        public static RoutedCommand RemoveCommand =
            new RoutedCommand("Remove", typeof(EntityLine), new InputGestureCollection(new InputGesture[] { new KeyGesture(Key.R, ModifierKeys.Control, "Remove") }));
        public static RoutedCommand FindCommand =
            new RoutedCommand("Find", typeof(EntityLine), new InputGestureCollection(new InputGesture[] { new KeyGesture(Key.F, ModifierKeys.Control, "Find") }));

        public static readonly DependencyProperty EntityProperty =
            DependencyProperty.Register("Entity", typeof(object), typeof(EntityBase), new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((EntityBase)d).OnEntityChanged(e.OldValue, e.NewValue)));
        public object Entity
        {
            get { return (object)GetValue(EntityProperty); }
            set
            {
                SetValue(EntityProperty, null);  //entities have equals overriden
                SetValue(EntityProperty, value);
            }
        }

        protected Implementations? safeImplementations;
        public static readonly DependencyProperty ImplementationsProperty =
            DependencyProperty.Register("Implementations", typeof(Implementations?), typeof(EntityBase), new UIPropertyMetadata(null, (d, e) => ((EntityBase)d).safeImplementations = (Implementations)e.NewValue));
        public Implementations? Implementations
        {
            get { return (Implementations?)GetValue(ImplementationsProperty); }
            set { SetValue(ImplementationsProperty, value); }
        }

        public static readonly DependencyProperty EntityTemplateProperty =
           DependencyProperty.Register("EntityTemplate", typeof(DataTemplate), typeof(EntityBase), new UIPropertyMetadata(null));
        public DataTemplate EntityTemplate
        {
            get { return (DataTemplate)GetValue(EntityTemplateProperty); }
            set { SetValue(EntityTemplateProperty, value); }
        }

        public static readonly DependencyProperty EntityTemplateSelectorProperty =
            DependencyProperty.Register("EntityTemplateSelector", typeof(DataTemplateSelector), typeof(EntityBase), new UIPropertyMetadata(null));
        public DataTemplateSelector EntityTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(EntityTemplateSelectorProperty); }
            set { SetValue(EntityTemplateSelectorProperty, value); }
        }

        public static readonly DependencyProperty CreateProperty =
            DependencyProperty.Register("Create", typeof(bool), typeof(EntityBase), new FrameworkPropertyMetadata(true, (d, e) => ((EntityBase)d).UpdateVisibility()));
        public bool Create
        {
            get { return (bool)GetValue(CreateProperty); }
            set { SetValue(CreateProperty, value); }
        }

        public static readonly DependencyProperty ViewProperty =
            DependencyProperty.Register("View", typeof(bool), typeof(EntityBase), new FrameworkPropertyMetadata(true, (d, e) => ((EntityBase)d).UpdateVisibility()));
        public bool View
        {
            get { return (bool)GetValue(ViewProperty); }
            set { SetValue(ViewProperty, value); }
        }

        public static readonly DependencyProperty NavigateProperty =
            DependencyProperty.Register("Navigate", typeof(bool), typeof(EntityBase), new UIPropertyMetadata(true));
        public bool Navigate
        {
            get { return (bool)GetValue(NavigateProperty); }
            set { SetValue(NavigateProperty, value); }
        }

        public static readonly DependencyProperty FindProperty =
            DependencyProperty.Register("Find", typeof(bool), typeof(EntityBase), new FrameworkPropertyMetadata(true, (d, e) => ((EntityBase)d).UpdateVisibility()));
        public bool Find
        {
            get { return (bool)GetValue(FindProperty); }
            set { SetValue(FindProperty, value); }
        }

        public static readonly DependencyProperty RemoveProperty =
            DependencyProperty.Register("Remove", typeof(bool), typeof(EntityBase), new FrameworkPropertyMetadata(true, (d, e) => ((EntityBase)d).UpdateVisibility()));
        public bool Remove
        {
            get { return (bool)GetValue(RemoveProperty); }
            set { SetValue(RemoveProperty, value); }
        }

        public static readonly DependencyProperty ViewOnCreateProperty =
            DependencyProperty.Register("ViewOnCreate", typeof(bool), typeof(EntityBase), new UIPropertyMetadata(true));
        public bool ViewOnCreate
        {
            get { return (bool)GetValue(ViewOnCreateProperty); }
            set { SetValue(ViewOnCreateProperty, value); }
        }

        public static readonly DependencyProperty ReadonlyEntityProperty =
            DependencyProperty.Register("ReadonlyEntity", typeof(bool?), typeof(EntityBase), new PropertyMetadata(null));
        public bool? ReadonlyEntity
        {
            get { return (bool?)GetValue(ReadonlyEntityProperty); }
            set { SetValue(ReadonlyEntityProperty, value); }
        }

        public event Func<object> Creating;
        public event Func<object> Finding;
        public event Func<object, object> Viewing;
        public event Action<object> Navigating; 
        public event Func<object, bool> Removing;

        public event EntityChangedEventHandler EntityChanged;

        static EntityBase()
        {
            LineBase.TypeProperty.OverrideMetadata(typeof(EntityBase), 
                new UIPropertyMetadata((d, e) => ((EntityBase)d).SetType((Type)e.NewValue)));

            Common.ValuePropertySelector.SetDefinition(typeof(EntityBase), EntityProperty);

            Common.IsReadOnlyProperty.OverrideMetadata(typeof(EntityBase),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, 
                    (d, e) => ((EntityBase)d).UpdateVisibility()));
        }

        public EntityBase()
        {
            this.CommandBindings.Add(new CommandBinding(CreateCommand, btCreate_Click));
            this.CommandBindings.Add(new CommandBinding(FindCommand, btFind_Click));
            this.CommandBindings.Add(new CommandBinding(RemoveCommand, btRemove_Click));
            this.CommandBindings.Add(new CommandBinding(ViewCommand, btView_Click));
        }

        void IsReadOnlyChanged(object sender, EventArgs e)
        {
            UpdateVisibility();
        }

        private void SetType(Type type)
        {
            if (type.IsLite())
            {
                CleanLite = true;
                CleanType = Lite.Extract(type);
            }
            else
            {
                CleanLite = false;
                CleanType = type;
            }
        }

        protected internal Type CleanType { get; private set; }
        protected internal bool CleanLite { get; private set; }

        protected bool isUserInteraction = false;

        protected void SetEntityUserInteraction(object entity)
        {
            try
            {
                isUserInteraction = true;
                Entity = Server.Convert(entity, Type);
            }
            finally
            {
                isUserInteraction = false;
            }
        }
        

        public override void OnLoad(object sender, RoutedEventArgs e)
        {
            base.OnLoad(sender, e);

            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            if (this.NotSet(EntityBase.EntityTemplateProperty) && this.NotSet(EntityBase.EntityTemplateSelectorProperty))
            {
                var type = Type;

                if (this is EntityCombo && !type.IsLite()) //Allways going to be lite
                    type = Lite.Generate(type);

                EntityTemplate = Navigator.FindDataTemplate(this, type);
            }

            if (this.NotSet(EntityBase.ImplementationsProperty) && CleanType.IsEntity() && !CleanType.IsAbstract)
                Implementations = Signum.Entities.Implementations.By(CleanType);

            if (this.NotSet(EntityBase.CreateProperty) && Create)
                Create =
                    CleanType.IsEmbeddedEntity() ? Navigator.IsCreable(CleanType ) : 
                    Implementations.Value.IsByAll ? false:
                    Implementations.Value.Types.Any(t => Navigator.IsCreable(t, isSearch: false));

            if (this.NotSet(EntityBase.ViewProperty) && View)
                View = CleanType.IsEmbeddedEntity() ? Navigator.IsViewable(CleanType) :
                    Implementations.Value.IsByAll ? true :
                    Implementations.Value.Types.Any(t => Navigator.IsViewable(t));

            if (this.NotSet(EntityBase.NavigateProperty) && Navigate)            
                Navigate = CleanType.IsEmbeddedEntity() ? Navigator.IsNavigable(CleanType, isSearch: false) :
                    Implementations.Value.IsByAll ? true :
                    Implementations.Value.Types.Any(t => Navigator.IsNavigable(t, isSearch: false));

            if (this.NotSet(EntityBase.FindProperty) && Find)
                Find = CleanType.IsEmbeddedEntity() ? false:
                    Implementations.Value.IsByAll ? false :
                    Implementations.Value.Types.Any(t => Finder.IsFindable(t));

            if (this.NotSet(EntityBase.ViewOnCreateProperty) && ViewOnCreate && !View)
                ViewOnCreate = false;

            UpdateVisibility();
        }


        protected virtual void UpdateVisibility()
        {
        }


        protected virtual bool CanRemove()
        {
            return Entity != null && Remove && !Common.GetIsReadOnly(this);
        }

        protected bool CanViewOrNavigate()
        {
            return CanViewOrNavigate(Entity);
        }

        protected virtual bool CanViewOrNavigate(object entity)
        {
            if (entity == null)
                return false;

            return _CanView(entity) || _CanNavigate(entity);
        }

        protected bool _CanView(object entity)
        {
            if (View && this.NotSet(ViewProperty))
            {
                Type entityType = CleanLite ? ((Lite<Entity>)entity).EntityType : entity.GetType();

                return Navigator.IsViewable(entityType);
            }
            else
                return View;
        }

        protected bool _CanNavigate(object entity)
        {
            if (Navigate && this.NotSet(NavigateProperty))
            {
                Type entityType = CleanLite ? ((Lite<Entity>)entity).EntityType : entity.GetType();

                return Navigator.IsNavigable(entityType, isSearch: false);
            }
            else
                return Navigate;
        }

        protected virtual bool CanFind()
        {
            return Entity == null && Find && !Common.GetIsReadOnly(this);
        }

        protected virtual bool CanCreate()
        {
            return Entity == null && Create && !Common.GetIsReadOnly(this);
        }

        protected virtual void btCreate_Click(object sender, RoutedEventArgs e)
        {
            object entity = OnCreate();

            if (entity != null)
                SetEntityUserInteraction(entity);
        }

        protected virtual void btFind_Click(object sender, RoutedEventArgs e)
        {
            object entity = OnFinding();

            if (entity != null)
                SetEntityUserInteraction(entity);
        }

        protected virtual void btView_Click(object sender, RoutedEventArgs e)
        {
            object entity = OnViewingOrNavigating(Entity, creating: false);

            if (entity != null)
                SetEntityUserInteraction(entity);
        }

        protected virtual void btRemove_Click(object sender, RoutedEventArgs e)
        {
            if (OnRemoving(Entity))
                SetEntityUserInteraction(null);
        }

        protected virtual void OnEntityChanged(object oldValue, object newValue)
        {
            EntityChanged?.Invoke(this, isUserInteraction, oldValue, newValue);

            AutomationProperties.SetItemStatus(this, Common.GetEntityStringAndHashCode(newValue));

            UpdateVisibility();
        }


        public Type SelectType(Func<Type, bool> filterType)
        {
            if (CleanType.IsEmbeddedEntity())
                return CleanType;

            if (Implementations.Value.IsByAll)
                throw new InvalidOperationException("ImplementedByAll is not supported for this operation, override the event");

            return Navigator.SelectType(Window.GetWindow(this), Implementations.Value.Types, filterType);
        }

        protected object OnCreate()
        {
            if (!CanCreate())
                return null;

            object value;
            if (Creating == null)
            {
                Type type = SelectType(t => Navigator.IsCreable(t, isSearch: false));
                if (type == null)
                    return null;

                object entity = new ConstructorContext(this).ConstructUntyped(type);

                value = entity;
            }
            else
                value = Creating();

            if (value == null)
                return null;

            if (ViewOnCreate)
            {
                value = OnViewingOrNavigating(value, creating: true);
            }

            return value;
        }

        protected object OnFinding()
        {
            if (!CanFind())
                return null;

            object value;
            if (Finding == null)
            {
                Type type = SelectType(Finder.IsFindable);
                if (type == null)
                    return null;

                value = Finder.Find(new FindOptions { QueryName = type });
            }
            else
                value = Finding();

            if (value == null)
                return null;

            return value;
        }

        public virtual PropertyRoute GetEntityPropertyRoute()
        {
            return Common.GetPropertyRoute(this);
        }

        protected object OnViewingOrNavigating(object entity, bool creating)
        {
            if (!CanViewOrNavigate(entity))
                return null;

            bool navigatePreferred = _CanNavigate(entity) && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) || Mouse.MiddleButton == MouseButtonState.Pressed);

            if (navigatePreferred)
            {
                _OnNavigating(entity);
                return null;
            }
            else
            {
                if (!_CanView(entity))
                    return null;

                return _OnViewing(entity, creating);
            }
        }

        private object _OnViewing(object entity, bool creating)
        {
            if (Viewing != null)
                return Viewing(entity);

            var options = new ViewOptions
            {
                PropertyRoute = CleanType.IsEmbeddedEntity() ? GetEntityPropertyRoute() : null,
            };

            bool isReadOnly = (ReadonlyEntity ?? Common.GetIsReadOnly(this)) && !creating;
            if (isReadOnly)
                options.ReadOnly = isReadOnly;

            return Navigator.ViewUntyped(entity, options);
        }

        protected void _OnNavigating(object entity)
        {
            if (Navigating != null)
                Navigating(entity);
            else
            {
                var options = new NavigateOptions();

                bool isReadOnly = (ReadonlyEntity ?? Common.GetIsReadOnly(this));
                if (isReadOnly)
                    options.ReadOnly = isReadOnly;

                Navigator.NavigateUntyped(entity, options);
            }
        }

        protected bool OnRemoving(object entity)
        {
            if (!CanRemove())
                return false;

            return Removing == null ? true : Removing(entity);
        }
    }

    public delegate void EntityChangedEventHandler(object sender, bool userInteraction, object oldValue, object newValue);
}
