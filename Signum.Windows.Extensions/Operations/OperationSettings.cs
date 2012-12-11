using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.Windows;
using Signum.Entities.Operations;
using System.Windows.Media;

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

    public class EntityOperationSettings : OperationSettings
    {
        public Func<EntityOperationContext, IdentifiableEntity> Click { get; set; }
        public Func<EntityOperationContext, bool> IsVisible { get; set; }

        public ContextualOperationSettings ContextualFromMany { get; set; }
        public ContextualOperationSettings Contextual { get; set; }

        public bool AvoidMoveToSearchControl { get; set; }

        public EntityOperationSettings(Enum key)
            : base(key)
        {
        }
    }

    public class EntityOperationContext
    {
        public FrameworkElement EntityControl { get; internal set; }
        public ToolBarButton SenderButton { get; internal set; }
        public OperationInfo OperationInfo { get; internal set; }
        public ViewButtons ViewButtons { get; internal set; }
        public bool SaveProtected { get; internal set; }
        public string CanExecute { get; internal set; }

        public IdentifiableEntity Entity { get; internal set; }
        public EntityOperationSettings OperationSettings { get; internal set; }
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
        public Action<ContextualOperationContext> Click { get; set; }
        public Func<ContextualOperationContext, bool> IsVisible { get; set; }

        public ContextualOperationSettings(Enum key)
            : base(key)
        {
        }
    }

    public class ContextualOperationContext 
    {
        public Lite[] Entities { get; internal set; }
        public SearchControl SearchControl { get; internal set; }
        public OperationInfo OperationInfo { get; internal set; }
        public string CanExecute { get; internal set; }
        public ContextualOperationSettings OperationSettings { get; set; }
    }
}
