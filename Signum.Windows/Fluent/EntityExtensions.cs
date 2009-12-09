using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using System.Windows.Controls;
using System.Windows.Data;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Windows
{
    public static class EntityExtensions
    {
        public static bool HasChanges(this FrameworkElement element)
        {
            var graph = GraphExplorer.FromRoot((Modifiable)element.DataContext);
            return graph.Any(a => a.SelfModified);
        }

        public static bool AssertErrors(this FrameworkElement element)
        {
            IAsserErrorsHandler aeh = element as IAsserErrorsHandler;
            if (aeh != null)
                return aeh.AssertErrors();

            return AssertErrors((Modifiable)element.DataContext);
        }

        public static bool AssertErrors(Modifiable mod)
        {
            var graph = GraphExplorer.PreSaving(() => GraphExplorer.FromRoot(mod));
            string error = GraphExplorer.Integrity(graph);

            if (error.HasText())
            {
                MessageBox.Show(Properties.Resources.ImpossibleToSaveIntegrityCheckFailed + error, Properties.Resources.ThereAreErrors, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        public static bool LooseChangesIfAny(this FrameworkElement element)
        {
            return !element.HasChanges() ||
                MessageBox.Show(
                Properties.Resources.ThereAreChangesContinue,
                Properties.Resources.ThereAreChanges,
                MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK) == MessageBoxResult.OK;
        }
    }
    
    public interface IAsserErrorsHandler
    {
        bool AssertErrors();
    }
}
