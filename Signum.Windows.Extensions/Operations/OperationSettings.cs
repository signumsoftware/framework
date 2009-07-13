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
        public static readonly OperationSettings Hidden = new EntityOperationSettings();

        public string Text { get; set; }
        public ImageSource Image { get; set; }
        public Color? Color { get; set; }
    }


    public class EntityOperationSettings : OperationSettings
    {
        public EntityOperationHandler Click { get; set; }
    }

    public delegate IdentifiableEntity EntityOperationHandler(EntityOperationEventArgs args);

    public class EntityOperationEventArgs : EventArgs
    {
        public IdentifiableEntity Entity { get; internal set; }
        public FrameworkElement EntityControl { get; internal set; }
        public FrameworkElement SenderButton { get; internal set; }
        public OperationInfo OperationInfo { get; internal set; }
    }


    public class ConstructorSettings : OperationSettings
    {
        public ConstructorHandler Constructor { get; internal set; }
    }

    public delegate IdentifiableEntity ConstructorHandler(Type type, OperationInfo operationInfo);



    public class ConstructorFromManySettings : OperationSettings
    {
        public ConstructorFromManyHandler Constructor { get; internal set; }
    }

    public delegate IdentifiableEntity ConstructorFromManyHandler( List<Lazy> lazies, Type type, OperationInfo operationInfo); 
}
