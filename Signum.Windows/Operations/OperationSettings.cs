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
        public FrameworkElement EntityControl { get; set; }
        public ToolBarButton SenderButton { get; set; }
        public OperationInfo OperationInfo { get; set; }
        public ViewMode ViewButtons { get; set; }
        public bool ShowOperations { get; set; }
        public string CanExecute { get; set; }

        public IdentifiableEntity Entity { get; set; }
        public EntityOperationSettings OperationSettings { get; set; }
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
        public Lite<IdentifiableEntity>[] Entities { get; set; }
        public SearchControl SearchControl { get; set; }
        public OperationInfo OperationInfo { get; set; }
        public string CanExecute { get; set; }
        public ContextualOperationSettings OperationSettings { get; set; }
    }
}
