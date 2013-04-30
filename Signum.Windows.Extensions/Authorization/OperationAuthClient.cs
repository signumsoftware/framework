using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using Signum.Services;
using System.Windows.Markup;
using System.Windows;
using Signum.Windows.Operations;
using Signum.Utilities;

namespace Signum.Windows.Authorization
{
    public static class OperationAuthClient
    {
        static Dictionary<Enum, OperationAllowed> authorizedOperations;

        public static bool Started { get; private set; }

        internal static void Start()
        {
            Started = true;

            AuthClient.UpdateCacheEvent += new Action(AuthClient_UpdateCacheEvent);
        }

        static void AuthClient_UpdateCacheEvent()
        {
            authorizedOperations = Server.Return((IOperationAuthServer s) => s.AllowedOperations());
        }

        public static bool GetAllowed(Enum operationKey, bool inUserInterface)
        {
            var allowed = authorizedOperations.GetOrThrow(operationKey);

            return allowed == OperationAllowed.Allow || allowed == OperationAllowed.DBOnly && !inUserInterface;
        }
    }


    [MarkupExtensionReturnType(typeof(bool))]
    public class OperationAllowedExtension : MarkupExtension
    {
        public bool InUserInterface { get; set; }

        Enum operationKey;
        public OperationAllowedExtension(object value)
        {
            this.operationKey = (Enum)value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return OperationAuthClient.GetAllowed(operationKey, InUserInterface);
        }
    }

    [MarkupExtensionReturnType(typeof(Visibility))]
    public class OperationVisiblityExtension : MarkupExtension
    {
        public bool InUserInterface { get; set; }

        Enum operationKey;
        public OperationVisiblityExtension(object value)
        {
            this.operationKey = (Enum)value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return OperationAuthClient.GetAllowed(operationKey, InUserInterface) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}

