using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using Signum.Entities;
using Signum.Entities.Isolation;
using Signum.Utilities;
using Signum.Utilities.Reflection;

namespace Signum.Windows.Isolation
{
    public class IsolationClient
    {
        public static Func<Window, Lite<IsolationDN>> SelectIsolationInteractively;

        public static void AsserIsStarted()
        {
            Navigator.Manager.AssertDefined(ReflectionTools.GetMethodInfo(() => IsolationClient.Start()));
        }

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Constructor.Manager.PreConstructors += Constructor_PreConstructors;

                List<Lite<IsolationDN>> isolations = null; 

                SelectIsolationInteractively = owner =>
                {
                    if(isolations == null)
                        isolations = Server.RetrieveAllLite<IsolationDN>(); 
                    
                    Lite<IsolationDN> result;
                    if (SelectorWindow.ShowDialog(isolations, out result,
                        title: IsolationMessage.SelectAnIsolation.NiceToString(),
                        message: IsolationMessage.SelectAnIsolation.NiceToString(), 
                        owner: owner))
                        return result;

                    return null;
                }; 
            }
        }

        static bool Constructor_PreConstructors(Type type, FrameworkElement element, List<object> args)
        {
            if (MixinDeclarations.IsDeclared(type, typeof(IsolationMixin)))
            {
                Lite<IsolationDN> isolation = GetIsolation(element);

                if (isolation == null)
                    return false;

                args.Add(isolation);
            }

            return true;
        }

        public static Lite<IsolationDN> GetIsolation(FrameworkElement element)
        {
            var result = IsolationDN.Current;
            if (result != null)
                return result;

            var entity = element.DataContext as IdentifiableEntity;

            result = entity.TryIsolation();
            if (result != null)
                return result;

            return SelectIsolationInteractively(Window.GetWindow(element));
        }
    }
}
