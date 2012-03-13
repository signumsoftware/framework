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

    //Execute & ConstructorFrom
    public class EntityOperationSettings<T> : OperationSettings
        where T : class, IIdentifiable
    {
        public Func<EntityOperationEventArgs<T>, T> Click { get; set; }
        public Func<EntityOperationEventArgs<T>, bool> IsVisible { get; set; }
        public bool VisibleOnOk { get; set; }

        public EntityOperationSettings(Enum key): base(key)
        {
        }

        public bool AvoidMoveToSearchControl { get; set; }
    }

    public class EntityOperationEventArgs<T> : EventArgs
        where T : class, IIdentifiable
    {
        public T Entity { get; internal set; }
        public FrameworkElement EntityControl { get; internal set; }
        public ToolBarButton SenderButton { get; internal set; }
        public OperationInfo OperationInfo { get; internal set; }
    }

    //Constructor
    public class ConstructorSettings : OperationSettings
    {
        public Func<OperationInfo, Window, IdentifiableEntity> Constructor { get; set; }
        public Func<OperationInfo, bool> IsVisible { get; set; } 

        public ConstructorSettings(Enum key)
            : base(key)
        {
        }
    }

    //ConsturctorFromMany
    public class ConstructorFromManySettings : OperationSettings
    {
        public Func<ConstructorFromManyEventArgs, IdentifiableEntity> Constructor { get; set; }
        public Func<object, OperationInfo, bool> IsVisible { get; set; }

        public ConstructorFromManySettings(Enum key)
            : base(key)
        {
        }
    }

    public class ConstructorFromManyEventArgs : EventArgs
    {
        public object QueryName { get; internal set;  }
        public List<Lite> Entities { get; internal set; }
        public Window Window { get; internal set; }
        public OperationInfo OperationInfo { get; internal set; }

    }
}
