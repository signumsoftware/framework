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

namespace Signum.Windows.Operations
{
    public static class OperationClient
    {
        public static OperationManager Manager;

        public static Win.FrameworkElement GenerateButton(OperationInfo operationInfo, Win.FrameworkElement control)
        {
            return Manager.GenerateButton(operationInfo, control);
        }
    }

    public class OperationManager
    {
        public Dictionary<Enum, OperationSettings> Settings = new Dictionary<Enum, OperationSettings>();

        protected internal Win.FrameworkElement GenerateButton(OperationInfo operationInfo, Win.FrameworkElement control)
        {
            ToolBarButton button = new ToolBarButton
            {
                Content = operationInfo.OperationKey,
                IsEnabled = operationInfo.CanExecute,
                Tag = operationInfo
            };

            OperationSettings os = Settings.TryGetC(operationInfo.OperationKey);

            if (os != null)
            {
                if (os.Text != null)
                    button.Content = os.Text;

                if (os.Image != null)
                    button.Image = os.Image;

                if (os.Color != null)
                    button.Background = new SolidColorBrush(os.Color.Value);
            }

            button.Click += (sender, args) => ButtonClick((ToolBarButton)sender, operationInfo, control, os.TryCC(o => o.Click));

            return button;
        }

        private static void ButtonClick(ToolBarButton sender, OperationInfo operationInfo, Win.FrameworkElement control, OperationHandlerMethod handler)
        {
            if (!operationInfo.CanExecute)
                throw new ApplicationException("Action {0} is disabled".Formato(operationInfo.OperationKey));

            IdentifiableEntity ident = (IdentifiableEntity)control.DataContext;

            IdentifiableEntity newIdent = null;
            if (handler != null)
            {
                newIdent = handler(control, operationInfo, sender);
            }
            else
            {
                if ((operationInfo.Flags & OperationFlags.Lazy) == OperationFlags.Lazy)
                {
                    if (control.LooseChangesIfAny())
                    {
                        Lazy<IdentifiableEntity> lazy = ident.ToLazy();
                        newIdent = Server.Service<IOperationServer>().ExecuteOperationLazy(lazy, operationInfo.OperationKey, null);
                    }
                }
                else
                    newIdent = Server.Service<IOperationServer>().ExecuteOperation(ident, operationInfo.OperationKey, null);
            }

            if (newIdent != null)
                control.RaiseEvent(new ChangeDataContextEventArgs(newIdent));

        }
    }

    public delegate IdentifiableEntity OperationHandlerMethod(Win.FrameworkElement control, OperationInfo operationInfo, ToolBarButton sender);

    public class OperationSettings
    {
        public string Text { get; set; }
        public ImageSource Image { get; set; }
        public Color? Color { get; set; }
        public OperationHandlerMethod Click { get; set; }
    }
}
