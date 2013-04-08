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

    public class EntityOperationGroup
    {
        public static readonly EntityOperationGroup None = new EntityOperationGroup();

        public static EntityOperationGroup Create = new EntityOperationGroup
        {
            Description = () => Signum.Entities.Properties.Resources.Create,
            SimplifyName = cs => Regex.Replace(cs, Signum.Entities.Properties.Resources.CreateFromRegex, m => m.Groups[1].Value, RegexOptions.IgnoreCase),
            Background = Brushes.Green,
            AutomationName = "Create"
        }; 

        public Func<string> Description;
        public Func<string, string> SimplifyName;
        public Brush Background;
        public string AutomationName; 
    }

    public class EntityOperationSettings : OperationSettings
    {
        public Func<EntityOperationContext, IdentifiableEntity> Click { get; set; }
        public Func<EntityOperationContext, bool> IsVisible { get; set; }

        public ContextualOperationSettings ContextualFromMany { get; private set; }
        public ContextualOperationSettings Contextual { get; private set; }

        public bool AvoidMoveToSearchControl { get; set; }

        public EntityOperationGroup Group { get; set; }

        public EntityOperationSettings(Enum key)
            : base(key)
        {
            Contextual = new ContextualOperationSettings(key);
            ContextualFromMany = new ContextualOperationSettings(key); 
        }
    }

    public class EntityOperationContext
    {
        public FrameworkElement EntityControl { get; set; }
        public Control SenderButton { get; set; }
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
