using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Operations;
using Signum.Entities;
using Signum.Services;
using Signum.Windows.Operations;
using System.Windows.Media;
using Signum.Utilities;
using System.Reflection;
using Win = System.Windows;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using System.Windows;

namespace Signum.Windows.Operations
{
    public static class OperationClient
    {
        public static OperationManager Manager{get;private set;}

        public static void Start(OperationManager operationManager)
        {
            Manager = operationManager;

            ButtonBar.GetButtonBarElement +=
                (obj, mainControl) => obj is IdentifiableEntity ?
                Server.Service<IOperationServer>().GetEntityOperationInfos((IdentifiableEntity)obj)
                .Select(oi => Manager.GenerateButton(oi, mainControl)).NotNull().ToList() : null;

            Constructor.ConstructorManager.GeneralConstructor += Manager.ConstructorManager_GeneralConstructor;
        } 

        public static Brush GetBackground(Enum key)
        {
            return Manager.GetBackground(key, Manager.Settings.TryGetC(key));
        }

        public static ImageSource GetImage(Enum key)
        {
            return Manager.GetImage(key, Manager.Settings.TryGetC(key));
        }

        public static string GetText(Enum key)
        {
            return Manager.GetText(key, Manager.Settings.TryGetC(key));
        }
    }

    public class OperationManager
    {
        public Dictionary<Enum, OperationSettings> Settings = new Dictionary<Enum, OperationSettings>();

        protected internal virtual Win.FrameworkElement GenerateButton(OperationInfo operationInfo, Win.FrameworkElement entityControl)
        {
            OperationSettings os = Settings.TryGetC(operationInfo.Key);

            if (os == OperationSettings.Hidden)
                return null;

            ToolBarButton button = new ToolBarButton
            {
                Content = GetText(operationInfo.Key, os),
                Image = GetImage(operationInfo.Key, os), 
                Background = GetBackground(operationInfo.Key, os),
                IsEnabled = operationInfo.CanExecute,
                Tag = operationInfo
            };

            button.Click += (sender, args) => ButtonClick((ToolBarButton)sender, operationInfo, entityControl, os.TryCC(o => o.Click));

            return button;
        }

        protected internal virtual Brush GetBackground(Enum p, OperationSettings os)
        {
            if (os != null && os.Color != null)
                return new SolidColorBrush(os.Color.Value);

            return null; 
        }

        protected internal virtual ImageSource GetImage(Enum key, OperationSettings os)
        {
            if (os != null && os.Image != null)
                return os.Image;

            return null; 
        }

        protected internal virtual string GetText(Enum key, OperationSettings os)
        {
            if (os != null && os.Text != null)
                return os.Text;

            return EnumExtensions.NiceToString(key); 
        }

        private static void ButtonClick(ToolBarButton sender, OperationInfo operationInfo, Win.FrameworkElement entityControl, OperationHandlerMethod handler)
        {
            if (!operationInfo.CanExecute)
                throw new ApplicationException("Action {0} is disabled".Formato(operationInfo.Key));

            IdentifiableEntity ident = (IdentifiableEntity)entityControl.DataContext;

            if (handler != null)
            {
                OperationEventArgs oea = new OperationEventArgs
                {
                    Entity = ident,
                    EntityControl = entityControl,
                    OperationInfo = operationInfo,
                    SenderButton = sender
                };

                IdentifiableEntity newIdent = handler(oea);
                if (newIdent != null)
                    entityControl.RaiseEvent(new ChangeDataContextEventArgs(newIdent));
            }
            else if(operationInfo.OperationType == OperationType.Execute)
            {
                 if (operationInfo.Lazy)
                 {
                     if (entityControl.LooseChangesIfAny())
                     {
                         Lazy<IdentifiableEntity> lazy = ident.ToLazy();
                         IdentifiableEntity newIdent = Server.Service<IOperationServer>().ExecuteOperationLazy(lazy, operationInfo.Key, null);
                         if (operationInfo.Returns)
                             entityControl.RaiseEvent(new ChangeDataContextEventArgs(newIdent));
                     }
                 }
                 else
                 {
                     IdentifiableEntity newIdent = Server.Service<IOperationServer>().ExecuteOperation(ident, operationInfo.Key, null);
                     if (operationInfo.Returns)
                         entityControl.RaiseEvent(new ChangeDataContextEventArgs(newIdent));
                 }
            }
            else if (operationInfo.OperationType == OperationType.ConstructorFrom)
            {
                if (operationInfo.Lazy)
                {
                    if (entityControl.LooseChangesIfAny())
                    {
                        Lazy<IdentifiableEntity> lazy = ident.ToLazy();
                        IdentifiableEntity newIdent = Server.Service<IOperationServer>().ConstructFromLazy(lazy, operationInfo.Key, null);
                        if (operationInfo.Returns)
                            Navigator.View(newIdent);
                    }
                }
                else
                {
                    IdentifiableEntity newIdent = Server.Service<IOperationServer>().ConstructFrom(ident, operationInfo.Key, null);
                    if (operationInfo.Returns)
                        Navigator.View(newIdent);
                }
            }
        }

        internal object ConstructorManager_GeneralConstructor(Type type, Window win)
        {
            var list = Server.Service<IOperationServer>().GetConstructorOperationInfos(type);

            if (list == null || list.Count == 0)
                return null;

            Enum selected = null;
            if (list.Count == 1)
            {
                selected = list[0].Key;
            }
            else
            {
                ConstructorSelectorWindow sel = new ConstructorSelectorWindow();
                sel.ConstructorKeys = list.Select(a => a.Key).ToArray();
                if (sel.ShowDialog() != true)
                    return null;

                selected = sel.SelectedKey;
            }

            return Server.Service<IOperationServer>().Construct(type, selected);
        }
    }

    public delegate IdentifiableEntity OperationHandlerMethod(OperationEventArgs args);

   

    public class OperationEventArgs : EventArgs
    {
        public IdentifiableEntity Entity { get; internal set; }
        public FrameworkElement EntityControl { get; internal set; }
        public FrameworkElement SenderButton { get; internal set; }
        public OperationInfo OperationInfo { get; internal set; }
    }

    public class OperationSettings
    {
        public static readonly OperationSettings Hidden; 

        public string Text { get; set; }
        public ImageSource Image { get; set; }
        public Color? Color { get; set; }
        public OperationHandlerMethod Click { get; set; }
    }
}
