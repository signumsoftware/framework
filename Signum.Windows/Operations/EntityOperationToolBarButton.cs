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
using Signum.Entities.Reflection;

namespace Signum.Windows.Operations
{
    public static class EntityOperationToolBarButton
    {
        public static bool MoveToSearchControls(IEntityOperationContext eoc)
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

        public static ToolBarButton NewToolbarButton(IEntityOperationContext eoc)
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


        internal static MenuItem NewMenuItem(IEntityOperationContext eoc, EntityOperationGroup group)
        {
            var man = OperationClient.Manager;

            MenuItem menuItem = new MenuItem
            {
                Header = eoc.OperationSettings?.Text ?? 
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

        static void OperationExecute(IEntityOperationContext eoc)
        {
            if (eoc.CanExecute != null)
                throw new ApplicationException("Operation {0} is disabled: {1}".FormatWith(eoc.OperationInfo.OperationSymbol, eoc.CanExecute));

            if (eoc.OperationSettings != null && eoc.OperationSettings.HasClick)
            {
                IEntity newIdent = eoc.OperationSettings.OnClick(eoc);
                if (newIdent != null)
                    eoc.EntityControl.RaiseEvent(new ChangeDataContextEventArgs(newIdent));
            }
            else
            {
                Entity ident = (Entity)(IEntity)eoc.Entity;
                if (eoc.OperationInfo.OperationType == OperationType.Execute)
                {
                    if (!eoc.OperationInfo.CanBeModified.Value)
                    {
                        if (eoc.EntityControl.LooseChangesIfAny())
                        {
                            if (eoc.ConfirmMessage())
                            {
                                Lite<Entity> lite = ident.ToLite();
                                IEntity newIdent = Server.Return((IOperationServer s) => s.ExecuteOperationLite(lite, eoc.OperationInfo.OperationSymbol, null));
                                if (eoc.OperationInfo.Returns)
                                    eoc.EntityControl.RaiseEvent(new ChangeDataContextEventArgs(newIdent));
                                if (eoc.OperationSettings != null && eoc.OperationSettings.AutoClose)
                                     eoc.EntityControl.RaiseEvent(new CloseFormEventArgs());
                            }
                        }
                    }
                    else
                    {
                        if (eoc.ConfirmMessage())
                        {
                            try
                            {
                                IEntity newIdent = Server.Return((IOperationServer s) => s.ExecuteOperation(ident, eoc.OperationInfo.OperationSymbol, null));
                                if (eoc.OperationInfo.Returns)
                                    eoc.EntityControl.RaiseEvent(new ChangeDataContextEventArgs(newIdent));
                                if (eoc.OperationSettings != null && eoc.OperationSettings.AutoClose)
                                    eoc.EntityControl.RaiseEvent(new CloseFormEventArgs());

                            }
                            catch (IntegrityCheckException e)
                            {
                                GraphExplorer.SetValidationErrors(GraphExplorer.FromRoot(ident), e);
                                throw e;
                            }
                        }
                    }
                }
                else if (eoc.OperationInfo.OperationType == OperationType.ConstructorFrom)
                {
                    if (!eoc.OperationInfo.CanBeModified.Value && !eoc.EntityControl.LooseChangesIfAny())
                        return;

                    if (!eoc.ConfirmMessage())
                        return;

                    IEntity result = (Entity)new ConstructorContext(eoc.EntityControl, eoc.OperationInfo).SurroundConstructUntyped(eoc.OperationInfo.ReturnType, ctx =>
                    {
                        Entity r;

                        if (!eoc.OperationInfo.CanBeModified.Value)
                        {
                            r = Server.Return((IOperationServer s) => s.ConstructFromLite(ident.ToLite(), eoc.OperationInfo.OperationSymbol, null));
                        }
                        else
                        {
                            try
                            {
                                r = Server.Return((IOperationServer s) => s.ConstructFrom(ident, eoc.OperationInfo.OperationSymbol, null));
                            }
                            catch (IntegrityCheckException e)
                            {
                                GraphExplorer.SetValidationErrors(GraphExplorer.FromRoot(ident), e);
                                throw e;
                            }
                        }

                        if (r == null)
                            MessageBox.Show(Window.GetWindow(eoc.EntityControl), OperationMessage.TheOperation0DidNotReturnAnEntity.NiceToString().FormatWith(eoc.OperationInfo.OperationSymbol.NiceToString()));

                        return r;
                    });

                    if (result != null)
                        Navigator.Navigate(result);
                       
                }
                else if (eoc.OperationInfo.OperationType == OperationType.Delete)
                {
                    if (eoc.ConfirmMessage())
                    {
                        Lite<Entity> lite = ident.ToLite();
                        Server.Execute((IOperationServer s) => s.DeleteLite(lite, eoc.OperationInfo.OperationSymbol, null));
                        eoc.EntityControl.RaiseEvent(new CloseFormEventArgs());
                    }
                }
            }
        }

        public static ToolBarButton CreateGroupContainer(EntityOperationGroup group)
        {
            ToolBarButton groupButton = new ToolBarButton
            {
                Content = group.Text(),
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
