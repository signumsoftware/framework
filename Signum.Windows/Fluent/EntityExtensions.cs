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
            if (element is IHasChangesHandler hch)
                return hch.HasChanges();

            return GraphExplorer.HasChanges((Modifiable)element.DataContext);
        }

        public static bool AssertErrors(this FrameworkElement element)
        {
            if (element is IAssertErrorsHandler aeh)
                return aeh.AssertErrors();

            string error = GetErrors(element);

            if (error.HasText())
            {
                MessageBox.Show(Window.GetWindow(element), NormalWindowMessage.ImpossibleToSaveIntegrityCheckFailed.NiceToString() + error, NormalWindowMessage.ThereAreErrors.NiceToString(), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        public static bool AssertErrors(Modifiable mod, Window window)
        {
            var error = GetErrors(mod);

            if (error != null)
            {
                MessageBox.Show(window, NormalWindowMessage.ImpossibleToSaveIntegrityCheckFailed.NiceToString() + error.Values.SelectMany(a=>a.Errors.Values).ToString("\r\n"), NormalWindowMessage.ThereAreErrors.NiceToString(), 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        public static string GetErrors(this FrameworkElement element)
        {
            if (element is IGetErrorsHandler geh)
                return geh.GetErrors();

            var visualErrors = VisualErrors(element).DefaultText(null);

            var entityErrors = GetErrors((Modifiable)element.DataContext)?.Values.ToString(d => d.Errors.Values.ToString("\r\n"), "\r\n");

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

        public static Dictionary<Guid, IntegrityCheck> GetErrors(Modifiable mod)
        {
            var graph = GraphExplorer.PreSaving(() => GraphExplorer.FromRoot(mod));
            var error = GraphExplorer.FullIntegrityCheck(graph);
            return error;
        }

        public static bool LooseChangesIfAny(this FrameworkElement element)
        {
            return !element.HasChanges() ||
                MessageBox.Show(
                NormalWindowMessage.ThereAreChangesContinue.NiceToString(),
                NormalWindowMessage.ThereAreChanges.NiceToString(),
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
