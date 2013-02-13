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
            IHasChangesHandler hch = element as IHasChangesHandler;
            if (hch != null)
                return hch.HasChanges();

            var graph = GraphExplorer.FromRoot((Modifiable)element.DataContext);
            return graph.Any(a => a.Modified == ModifiableState.SelfModified);
        }

        public static bool AssertErrors(this FrameworkElement element)
        {
            IAssertErrorsHandler aeh = element as IAssertErrorsHandler;
            if (aeh != null)
                return aeh.AssertErrors();

            string error = GetErrors(element);

            if (error.HasText())
            {
                MessageBox.Show(Window.GetWindow(element), Properties.Resources.ImpossibleToSaveIntegrityCheckFailed + error, Properties.Resources.ThereAreErrors, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        public static bool AssertErrors(Modifiable mod, Window window)
        {
            string error = GetErrors(mod); 

            if (error.HasText())
            {
                MessageBox.Show(window, Properties.Resources.ImpossibleToSaveIntegrityCheckFailed + error, Properties.Resources.ThereAreErrors, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        public static string GetErrors(this FrameworkElement element)
        {
            IGetErrorsHandler geh = element as IGetErrorsHandler;
            if (geh != null)
                return geh.GetErrors();

            var visualErrors = VisualErrors(element).DefaultText(null);

            var entityErrors = GetErrors((Modifiable)element.DataContext).DefaultText(null);

            return "\r\n".Combine(visualErrors, entityErrors).DefaultText(null);
        }

        private static string VisualErrors(FrameworkElement element)
        {
            var visualErrors = (from c in element.Children<DependencyObject>(Validation.GetHasError, WhereFlags.NonRecursive | WhereFlags.BreathFirst | WhereFlags.VisualTree)
                                from e in Validation.GetErrors(c)
                                where !(e.RuleInError is DataErrorValidationRule)
                                select DoubleListConverter.CleanErrorMessage(e)).ToString("\r\n");
            return visualErrors;
        }

        public static string GetErrors(Modifiable mod)
        {
            var graph = GraphExplorer.PreSaving(() => GraphExplorer.FromRoot(mod));
            string error = GraphExplorer.FullIntegrityCheck(graph, withIndependentEmbeddedEntities: !(mod is IdentifiableEntity));
            return error;
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
    
    public interface IAssertErrorsHandler
    {
        bool AssertErrors();
    }

    public interface IGetErrorsHandler
    {
        string GetErrors();
    }

    public interface IHasChangesHandler
    {
        bool HasChanges();
    }
}
