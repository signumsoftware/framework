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
        public string Text { get; set; }
        public ImageSource Icon { get; set; }
        public Color? Color { get; set; }
    }

    //Execute & ConstructorFrom
    public class EntityOperationSettings : OperationSettings
    {
        public Func<EntityOperationEventArgs, IdentifiableEntity> Click { get; set; }
        public Func<IdentifiableEntity, bool> IsVisible { get;  set; }
    }

    public class EntityOperationEventArgs : EventArgs
    {
        public IdentifiableEntity Entity { get; internal set; }
        public FrameworkElement EntityControl { get; internal set; }
        public FrameworkElement SenderButton { get; internal set; }
        public OperationInfo OperationInfo { get; internal set; } 
    }

    //Constructor
    public class ConstructorSettings : OperationSettings
    {
        public Func<OperationInfo, Window, IdentifiableEntity> Constructor { get; set; }
        public Func<OperationInfo, bool> IsVisible { get; set; } 
    }

    //ConsturctorFromMany
    public class ConstructorFromManySettings : OperationSettings
    {
        public Func<ConstructorFromManyEventArgs, IdentifiableEntity> Constructor { get; set; }
        public Func<object, OperationInfo, bool> IsVisible { get; set; } 
    }

    public class ConstructorFromManyEventArgs : EventArgs
    {
        public object QueryName { get; internal set;  }
        public List<Lite> Entities { get; internal set; }
        public Window Window { get; internal set; }
        public OperationInfo OperationInfo { get; internal set; }

    }
}
