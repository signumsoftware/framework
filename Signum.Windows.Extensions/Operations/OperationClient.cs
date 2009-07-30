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

        protected internal virtual List<FrameworkElement> ButtonBar_GetButtonBarElement(object entity, Control entityControl)
        {
            IdentifiableEntity ident = entity as IdentifiableEntity;

            if (ident == null)
                return null;

            var list = Server.Service<IOperationServer>().GetEntityOperationInfos(ident);

            var result = list.Select(oi => GenerateButton(oi, ident, entityControl)).NotNull().ToList();

            return result;
        } 

        protected internal virtual Win.FrameworkElement GenerateButton(OperationInfo operationInfo, IdentifiableEntity entity, Win.FrameworkElement entityControl)
        {
            EntityOperationSettings os = (EntityOperationSettings)Settings.TryGetC(operationInfo.Key);

            if (os != null && os.IsVisible != null && !os.IsVisible(entity))
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

        private static void ButtonClick(ToolBarButton sender, OperationInfo operationInfo, Win.FrameworkElement entityControl, Func<EntityOperationEventArgs, IdentifiableEntity> handler)
        {
            if (!operationInfo.CanExecute)
                throw new ApplicationException("Action {0} is disabled".Formato(operationInfo.Key));

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
                        Lazy lazy = Lazy.Create(ident.GetType(), ident);
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
