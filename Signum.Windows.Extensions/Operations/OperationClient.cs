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
using System.Windows.Controls;
using Signum.Entities.Reflection;

namespace Signum.Windows.Operations
{
    public static class OperationClient
    {
        public static OperationManager Manager{get;private set;}

        public static void Start(OperationManager operationManager)
        {
            Manager = operationManager;

            ButtonBar.GetButtonBarElement += Manager.ButtonBar_GetButtonBarElement;

            Constructor.ConstructorManager.GeneralConstructor += Manager.ConstructorManager_GeneralConstructor;

            SearchControl.GetCustomMenuItems += (qn, type) =>
            {
                if (type == null)
                    return null;

                var list = Server.Service<IOperationServer>().GetQueryOperationInfos(type).Where(oi =>
                {
                    ConstructorFromManySettings set = (ConstructorFromManySettings)Manager.Settings.TryGetC(oi.Key);
                    return set == null || set.IsVisible == null || set.IsVisible(qn, oi);
                }).ToList();

                if (list.Count == 0)
                    return null;

                return new ConstructFromMenuItem { OperationInfos = list };
            };
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

        protected internal virtual List<FrameworkElement> ButtonBar_GetButtonBarElement(object entity, Control entityControl, ViewButtons viewButtons)
        {
            IdentifiableEntity ident = entity as IdentifiableEntity;

            if (ident == null)
                return null;

            var list = Server.Service<IOperationServer>().GetEntityOperationInfos(ident);

            var result = list.Select(oi => GenerateButton(oi, ident, entityControl, viewButtons)).NotNull().ToList();

            return result;
        }

        protected internal virtual Win.FrameworkElement GenerateButton(OperationInfo operationInfo, IdentifiableEntity entity, Win.FrameworkElement entityControl, ViewButtons viewButtons)
        {
            EntityOperationSettings os = (EntityOperationSettings)Settings.TryGetC(operationInfo.Key);

            if (os != null && os.IsVisible != null && !os.IsVisible(entity))
                return null;

            if (viewButtons == ViewButtons.Ok && (os == null || !os.VisibleOnOk))
                return null;

            ToolBarButton button = new ToolBarButton
            {
                Content = GetText(operationInfo.Key, os),
                Image = GetImage(operationInfo.Key, os),
                Background = GetBackground(operationInfo.Key, os),
                Tag = operationInfo,
                ToolTip = operationInfo.CanExecute,
            };

            if (operationInfo.CanExecute != null)
            {
                button.ToolTip = operationInfo.CanExecute;
                button.IsEnabled = false;
                ToolTipService.SetShowOnDisabled(button, true);
            }

            button.Click += (sender, args) => ButtonClick((ToolBarButton)sender, operationInfo, entityControl, os.TryCC(o => o.Click));

            return button;
        }

        protected internal virtual Brush GetBackground(Enum key, OperationSettings os)
        {
            if (os != null && os.Color != null)
                return new SolidColorBrush(os.Color.Value);

            //if (oi.OperationType == OperationType.Delete) TODO Olmo: Pasar OperationInfo
            //    return new SolidColorBrush(Colors.Red);

            return null; 
        }

        protected internal virtual ImageSource GetImage(Enum key, OperationSettings os)
        {
            if (os != null && os.Icon != null)
                return os.Icon;

            return null; 
        }

        protected internal virtual string GetText(Enum key, OperationSettings os)
        {
            if (os != null && os.Text != null)
                return os.Text;

            return EnumExtensions.NiceToString(key);
        }

        private static void ButtonClick(ToolBarButton sender, OperationInfo operationInfo, Win.FrameworkElement entityControl, Func<EntityOperationEventArgs, IIdentifiable> handler)
        {
            if (operationInfo.CanExecute != null)
                throw new ApplicationException("Action {0} is disabled: {1}".Formato(operationInfo.Key, operationInfo.CanExecute));

            IdentifiableEntity ident = (IdentifiableEntity)entityControl.DataContext;

            if (handler != null)
            {
                EntityOperationEventArgs oea = new EntityOperationEventArgs
                {
                    Entity = ident,
                    EntityControl = entityControl,
                    OperationInfo = operationInfo,
                    SenderButton = sender
                };

                IIdentifiable newIdent = handler(oea);
                if (newIdent != null)
                    entityControl.RaiseEvent(new ChangeDataContextEventArgs(newIdent));
            }
            else if(operationInfo.OperationType == OperationType.Execute)
            {
                 if (operationInfo.Lite.Value)
                 {
                     if (entityControl.LooseChangesIfAny())
                     {
                         Lite<IdentifiableEntity> lite = ident.ToLite();
                         IIdentifiable newIdent = Server.Service<IOperationServer>().ExecuteOperationLite(lite, operationInfo.Key, null);
                         if (operationInfo.Returns)
                             entityControl.RaiseEvent(new ChangeDataContextEventArgs(newIdent));
                     }
                 }
                 else
                 {
                     IIdentifiable newIdent = Server.Service<IOperationServer>().ExecuteOperation(ident, operationInfo.Key, null);
                     if (operationInfo.Returns)
                         entityControl.RaiseEvent(new ChangeDataContextEventArgs(newIdent));
                 }
            }
            else if (operationInfo.OperationType == OperationType.ConstructorFrom)
            {
                if (operationInfo.Lite.Value)
                {
                    if (entityControl.LooseChangesIfAny())
                    {
                        Lite lite = Lite.Create(ident.GetType(), ident);
                        IIdentifiable newIdent = Server.Service<IOperationServer>().ConstructFromLite(lite, operationInfo.Key, null);
                        if (operationInfo.Returns)
                            Navigator.View(newIdent, ViewButtons.Save);
                    }
                }
                else
                {
                    IIdentifiable newIdent = Server.Service<IOperationServer>().ConstructFrom(ident, operationInfo.Key, null);
                    if (operationInfo.Returns)
                        Navigator.View(newIdent, ViewButtons.Save);
                }
            }
            else if (operationInfo.OperationType == OperationType.Delete)
            {
                if (MessageBox.Show("Are you sure of deleting the entity?", "Delete?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Lite lite = Lite.Create(ident.GetType(), ident);
                    Server.Service<IOperationServer>().Delete(lite, operationInfo.Key, null);
                }
            }
        }

        internal object ConstructorManager_GeneralConstructor(Type type, Window win)
        {
            if (!typeof(IIdentifiable).IsAssignableFrom(type))
                return null; 

            var list = Server.Service<IOperationServer>().GetConstructorOperationInfos(type);

            var dic = (from oi in list
                       let os = (ConstructorSettings)Settings.TryGetC(oi.Key)
                       where os == null || os.IsVisible == null || os.IsVisible(oi)
                       select new { OperationInfo = oi, OperationSettings = os }).ToDictionary(a => a.OperationInfo.Key);


            if (dic.Count == 0)
                return null;

            Enum selected = null;
            if (list.Count == 1)
            {
                selected = dic.Keys.Single();
            }
            else
            {
                ConstructorSelectorWindow sel = new ConstructorSelectorWindow();
                sel.ConstructorKeys = dic.Keys.ToArray();
                if (sel.ShowDialog() != true)
                    return null;

                selected = sel.SelectedKey;
            }

            var pair = dic[selected];

            if (pair.OperationSettings != null && pair.OperationSettings.Constructor != null)
                return pair.OperationSettings.Constructor(pair.OperationInfo, win);
            else
                return Server.Service<IOperationServer>().Construct(type, selected);
        }
    }
}
