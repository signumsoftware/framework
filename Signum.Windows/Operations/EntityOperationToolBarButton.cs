using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Services;
using Signum.Utilities;
using Signum.Windows.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Signum.Windows.Operations
{
    public static class EntityOperationToolBarButton
    {
        public static bool MoveToSearchControls(EntityOperationContext eoc)
        {
            var controls = eoc.EntityControl.Children<SearchControl>()
                .Where(sc => eoc.OperationInfo.OperationSymbol.Equals(OperationClient.GetConstructFromOperationKey(sc)) ||
                sc.NotSet(OperationClient.ConstructFromOperationKeyProperty) && sc.EntityType == eoc.OperationInfo.ReturnType).ToList();

            if (!controls.Any())
                return false;

            foreach (var sc in controls)
            {
                if (sc.NotSet(OperationClient.ConstructFromOperationKeyProperty))
                    OperationClient.SetConstructFromOperationKey(sc, eoc.OperationInfo.OperationSymbol);

                sc.Create = false;

                var menu = sc.Child<Menu>(b => b.Name == "menu");

                var panel = (StackPanel)menu.Parent;

                var oldButton = panel.Children<ToolBarButton>(tb => tb.Tag is OperationInfo && ((OperationInfo)tb.Tag).OperationSymbol.Equals(eoc.OperationInfo.OperationSymbol)).FirstOrDefault();
                if (oldButton != null)
                    panel.Children.Remove(oldButton);

                var index = panel.Children.IndexOf(menu);
                panel.Children.Insert(index, NewToolbarButton(eoc));
            }
            return true;
        }

        public static ToolBarButton NewToolbarButton(EntityOperationContext eoc)
        {
            var man = OperationClient.Manager;

            ToolBarButton button = new ToolBarButton
            {
                Content = man.GetText(eoc.OperationInfo.OperationSymbol, eoc.OperationSettings),
                Image = man.GetImage(eoc.OperationInfo.OperationSymbol, eoc.OperationSettings),
                Tag = eoc.OperationInfo,
                Background = man.GetBackground(eoc.OperationInfo, eoc.OperationSettings)
            };

            if (eoc.OperationSettings != null && eoc.OperationSettings.Order != 0)
                Common.SetOrder(button, eoc.OperationSettings.Order);

            AutomationProperties.SetName(button, eoc.OperationInfo.OperationSymbol.Key);

            eoc.SenderButton = button;

            if (eoc.CanExecute != null)
            {
                button.ToolTip = eoc.CanExecute;
                button.IsEnabled = false;
                ToolTipService.SetShowOnDisabled(button, true);
                AutomationProperties.SetHelpText(button, eoc.CanExecute);
            }
            else
            {
                button.Click += (_, __) => OperationExecute(eoc);
            }
            return button;
        }


        internal static MenuItem NewMenuItem(EntityOperationContext eoc, EntityOperationGroup group)
        {
            var man = OperationClient.Manager;

            MenuItem menuItem = new MenuItem
            {
                Header = eoc.OperationSettings.Try(os => os.Text) ?? 
                (group == null || group.SimplifyName == null ? eoc.OperationInfo.OperationSymbol.NiceToString() :
                 group.SimplifyName(eoc.OperationInfo.OperationSymbol.NiceToString())),
                Icon = man.GetImage(eoc.OperationInfo.OperationSymbol, eoc.OperationSettings).ToSmallImage(),
                Tag = eoc.OperationInfo,
                Background = man.GetBackground(eoc.OperationInfo, eoc.OperationSettings)
            };

            if (eoc.OperationSettings != null && eoc.OperationSettings.Order != 0)
                Common.SetOrder(menuItem, eoc.OperationSettings.Order);

            AutomationProperties.SetName(menuItem, eoc.OperationInfo.OperationSymbol.Key);

            eoc.SenderButton = menuItem;

            if (eoc.CanExecute != null)
            {
                menuItem.ToolTip = eoc.CanExecute;
                menuItem.IsEnabled = false;
                ToolTipService.SetShowOnDisabled(menuItem, true);
                AutomationProperties.SetHelpText(menuItem, eoc.CanExecute);
            }
            else
            {
                menuItem.Click += (_, __) => OperationExecute(eoc);
            }
            return menuItem;
        }

        static void OperationExecute(EntityOperationContext eoc)
        {
            if (eoc.CanExecute != null)
                throw new ApplicationException("Operation {0} is disabled: {1}".Formato(eoc.OperationInfo.OperationSymbol, eoc.CanExecute));

            if (eoc.OperationSettings != null && eoc.OperationSettings.Click != null)
            {
                IIdentifiable newIdent = eoc.OperationSettings.Click(eoc);
                if (newIdent != null)
                    eoc.EntityControl.RaiseEvent(new ChangeDataContextEventArgs(newIdent));
            }
            else
            {
                IdentifiableEntity ident = (IdentifiableEntity)(IIdentifiable)eoc.Entity;
                if (eoc.OperationInfo.OperationType == OperationType.Execute)
                {
                    if (eoc.OperationInfo.Lite.Value)
                    {
                        if (eoc.EntityControl.LooseChangesIfAny())
                        {
                            if (eoc.ConfirmMessage())
                            {
                                Lite<IdentifiableEntity> lite = ident.ToLite();
                                IIdentifiable newIdent = Server.Return((IOperationServer s) => s.ExecuteOperationLite(lite, eoc.OperationInfo.OperationSymbol, null));
                                if (eoc.OperationInfo.Returns)
                                    eoc.EntityControl.RaiseEvent(new ChangeDataContextEventArgs(newIdent));
                            }
                        }
                    }
                    else
                    {
                        if (eoc.ConfirmMessage())
                        {
                            IIdentifiable newIdent = Server.Return((IOperationServer s) => s.ExecuteOperation(ident, eoc.OperationInfo.OperationSymbol, null));
                            if (eoc.OperationInfo.Returns)
                                eoc.EntityControl.RaiseEvent(new ChangeDataContextEventArgs(newIdent));
                        }
                    }
                }
                else if (eoc.OperationInfo.OperationType == OperationType.ConstructorFrom)
                {
                    IIdentifiable result = null;
                    if (eoc.OperationInfo.Lite.Value)
                    {
                        if (!eoc.EntityControl.LooseChangesIfAny())
                            return;

                        if (eoc.ConfirmMessage())
                        {
                            Lite<IdentifiableEntity> lite = ident.ToLite();
                            result = (IdentifiableEntity)eoc.EntityControl.SurroundConstruct(eoc.OperationInfo.ReturnType, null, ctx => Server.Return((IOperationServer s) => s.ConstructFromLite(lite, eoc.OperationInfo.OperationSymbol, null)));
                        }
                    }
                    else
                    {
                        if (eoc.ConfirmMessage())
                        {
                            result = (IdentifiableEntity)eoc.EntityControl.SurroundConstruct(eoc.OperationInfo.ReturnType, null, ctx => Server.Return((IOperationServer s) => s.ConstructFrom(ident, eoc.OperationInfo.OperationSymbol, null)));
                        }
                    }

                    if (result != null)
                        Navigator.Navigate(result);
                    else
                        MessageBox.Show(Window.GetWindow(eoc.EntityControl), OperationMessage.TheOperation0DidNotReturnAnEntity.NiceToString().Formato(eoc.OperationInfo.OperationSymbol.NiceToString()));

                }
                else if (eoc.OperationInfo.OperationType == OperationType.Delete)
                {
                    if (eoc.ConfirmMessage())
                    {
                        Lite<IdentifiableEntity> lite = ident.ToLite();
                        Server.Execute((IOperationServer s) => s.Delete(lite, eoc.OperationInfo.OperationSymbol, null));
                        Window.GetWindow(eoc.EntityControl).Close();
                    }
                }
            }
        }

        public static ToolBarButton CreateGroupContainer(EntityOperationGroup group)
        {
            ToolBarButton groupButton = new ToolBarButton
            {
                Content = group.Description(),
                ContextMenu = new ContextMenu(),
                Background = group.Background,
            };

            Common.SetOrder(groupButton, group.Order);

            AutomationProperties.SetItemStatus(groupButton, "Group");

            if (group.AutomationName.HasText())
                AutomationProperties.SetName(groupButton, group.AutomationName);

            groupButton.ContextMenu = new ContextMenu
            {
                PlacementTarget = groupButton,
                Placement = PlacementMode.Bottom,
            };

            ContextMenuService.SetIsEnabled(groupButton, false);

            groupButton.Click += (object sender, RoutedEventArgs e) =>
            {
                ToolBarButton tbb = (ToolBarButton)sender;
                tbb.ContextMenu.IsEnabled = true;
                tbb.ContextMenu.IsOpen = true;
            };

            return groupButton;
        }
    }
}
