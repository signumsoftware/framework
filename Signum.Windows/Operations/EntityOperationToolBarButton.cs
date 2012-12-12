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

namespace Signum.Windows.Operations
{
    public static class EntityOperationToolBarButton
    {
        public static FrameworkElement CreateButton(EntityOperationContext eoc)
        {
            if (eoc.OperationInfo.OperationType == OperationType.ConstructorFrom && (eoc.OperationSettings == null || !eoc.OperationSettings.AvoidMoveToSearchControl))
            {
                var controls = eoc.EntityControl.Children<SearchControl>()
                    .Where(sc => eoc.OperationInfo.Key.Equals(OperationClient.GetConstructFromOperationKey(sc)) ||
                    sc.NotSet(OperationClient.ConstructFromOperationKeyProperty) && sc.EntityType == eoc.OperationInfo.ReturnType).ToList();

                if (controls.Any())
                {
                    foreach (var sc in controls)
                    {
                        if (sc.NotSet(OperationClient.ConstructFromOperationKeyProperty))
                        {
                            OperationClient.SetConstructFromOperationKey(sc, eoc.OperationInfo.Key);
                        }

                        sc.Create = false;

                        var menu = sc.Child<Menu>(b => b.Name == "menu");

                        var panel = (StackPanel)menu.Parent;

                        var oldButton = panel.Children<ToolBarButton>(tb => tb.Tag is OperationInfo && ((OperationInfo)tb.Tag).Key.Equals(eoc.OperationInfo.Key)).FirstOrDefault();
                        if (oldButton != null)
                            panel.Children.Remove(oldButton);

                        var index = panel.Children.IndexOf(menu);
                        panel.Children.Insert(index, NewButton(eoc));
                    }

                    return null;
                }
            }

            ToolBarButton result = NewButton(eoc);

            return result;
        }

        static ToolBarButton NewButton(EntityOperationContext eoc)
        {
            var man = OperationClient.Manager;

            ToolBarButton button = new ToolBarButton
            {
                Content = man.GetText(eoc.OperationInfo.Key, eoc.OperationSettings),
                Image = man.GetImage(eoc.OperationInfo.Key, eoc.OperationSettings),
                Background = man.GetBackground(eoc.OperationInfo, eoc.OperationSettings),
                Tag = eoc.OperationInfo,
            };

            AutomationProperties.SetItemStatus(button, OperationDN.UniqueKey(eoc.OperationInfo.Key));

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
                button.Click += (_, __) =>
                {
                    if (eoc.CanExecute != null)
                        throw new ApplicationException("Operation {0} is disabled: {1}".Formato(eoc.OperationInfo.Key, eoc.CanExecute));

                    if (eoc.OperationSettings != null && eoc.OperationSettings.Click != null)
                    {
                        IIdentifiable newIdent = eoc.OperationSettings.Click(eoc);
                        if (newIdent != null)
                            eoc.EntityControl.RaiseEvent(new ChangeDataContextEventArgs(newIdent));
                    }
                    else
                    {
                        DefaultOperationExecute(eoc);
                    }
                };
            }
            return button;
        }

        static void DefaultOperationExecute(EntityOperationContext eoc)
        {
            IdentifiableEntity ident = (IdentifiableEntity)(IIdentifiable)eoc.Entity;
            if (eoc.OperationInfo.OperationType == OperationType.Execute)
            {
                if (eoc.OperationInfo.Lite.Value)
                {
                    if (eoc.EntityControl.LooseChangesIfAny())
                    {
                        Lite<IdentifiableEntity> lite = ident.ToLite();
                        IIdentifiable newIdent = Server.Return((IOperationServer s) => s.ExecuteOperationLite(lite, eoc.OperationInfo.Key, null));
                        if (eoc.OperationInfo.Returns)
                            eoc.EntityControl.RaiseEvent(new ChangeDataContextEventArgs(newIdent));
                    }
                }
                else
                {
                    IIdentifiable newIdent = Server.Return((IOperationServer s) => s.ExecuteOperation(ident, eoc.OperationInfo.Key, null));
                    if (eoc.OperationInfo.Returns)
                        eoc.EntityControl.RaiseEvent(new ChangeDataContextEventArgs(newIdent));
                }
            }
            else if (eoc.OperationInfo.OperationType == OperationType.ConstructorFrom)
            {
                if (eoc.OperationInfo.Lite.Value)
                {
                    if (eoc.EntityControl.LooseChangesIfAny())
                    {
                        Lite<IdentifiableEntity> lite = ident.ToLite();
                        IIdentifiable newIdent = Server.Return((IOperationServer s) => s.ConstructFromLite(lite, eoc.OperationInfo.Key, null));
                        if (eoc.OperationInfo.Returns)
                            Navigator.Navigate(newIdent);
                    }
                }
                else
                {
                    IIdentifiable newIdent = Server.Return((IOperationServer s) => s.ConstructFrom(ident, eoc.OperationInfo.Key, null));
                    if (eoc.OperationInfo.Returns)
                        Navigator.Navigate(newIdent);
                }
            }
            else if (eoc.OperationInfo.OperationType == OperationType.Delete)
            {
                if (MessageBox.Show(Window.GetWindow(eoc.EntityControl), "Are you sure of deleting the entity?", "Delete?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Lite<IdentifiableEntity> lite = ident.ToLite();
                    Server.Return((IOperationServer s) => s.Delete(lite, eoc.OperationInfo.Key, null));
                }
            }
        }
    }
}
