using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.Windows;
using System.Windows.Media;
using Signum.Entities.Basics;

namespace Signum.Windows.Operations
{
    public abstract class OperationSettings
    {
        public Enum Key { get; set; }
        public string Text { get; set; }
        public ImageSource Icon { get; set; }
        public Color? Color { get; set; }

        public OperationSettings(Enum key)
        {
            this.Key = key;
        }

    }

    public abstract class EntityOperationSettingsBase : OperationSettings
    {
        public ContextualOperationSettings ContextualFromMany { get; set; }
        public ContextualOperationSettings Contextual { get; set; }

        public bool AvoidMoveToSearchControl { get; set; }


        public EntityOperationSettingsBase(Enum key)
            : base(key)
        {
        }

        public abstract bool ClickOverriden { get; }
    }

    //Execute & ConstructorFrom
    public class EntityOperationSettings<T> : EntityOperationSettingsBase 
        where T : class, IIdentifiable
    {
        public override bool ClickOverriden { get { return Click != null; } }

        public Func<EntityOperationEventArgs<T>, T> Click { get; set; }
        public Func<EntityOperationEventArgs<T>, bool> IsVisible { get; set; }

        public EntityOperationSettings(Enum key): base(key)
        {
        }
    }

    public class EntityOperationEventArgs<T> : EventArgs
        where T : class, IIdentifiable
    {
        public T Entity { get; internal set; }
        public FrameworkElement EntityControl { get; internal set; }
        public ToolBarButton SenderButton { get; internal set; }
        public OperationInfo OperationInfo { get; internal set; }
        public ViewButtons ViewButtons { get; internal set; }
        public bool SaveProtected { get; internal set; }
    }


    public class ConstructorSettings : OperationSettings
    {
        public Func<OperationInfo, Window, IdentifiableEntity> Constructor { get; set; }
        public Func<OperationInfo, bool> IsVisible { get; set; } 

        public ConstructorSettings(Enum key)
            : base(key)
        {
        }
    }

    public class ContextualOperationSettings : OperationSettings
    {
        public Action<ContextualOperationEventArgs> Click { get; set; }
        public Func<ContextualOperationEventArgs, bool> IsVisible { get; set; }


        public bool OnVisible(SearchControl sc, OperationInfo oi)
        {
            if (IsVisible == null)
                return true;

            return IsVisible(new ContextualOperationEventArgs
            {
                Entities = sc.SelectedItems,
                SearchControl = sc,
                OperationInfo = oi,
            });
        }

        public ContextualOperationSettings(Enum key)
            : base(key)
        {
        }
    }

    public class ContextualOperationEventArgs : EventArgs
    {
        public Lite[] Entities { get; set; }
        public SearchControl SearchControl { get; set; }
        public OperationInfo OperationInfo { get; set; }
    }
}
