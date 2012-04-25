using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using Signum.Services;
using System.Windows.Markup;
using System.Windows;

namespace Signum.Windows.Authorization
{
    public static class OperationAuthClient
    {
        static DefaultDictionary<Enum, bool> authorizedOperations;

        public static bool Started { get; private set; }

        internal static void Start()
        {
            Started = true;

            AuthClient.UpdateCacheEvent += new Action(AuthClient_UpdateCacheEvent);
        }

        static void AuthClient_UpdateCacheEvent()
        {
            authorizedOperations = Server.Return((IOperationAuthServer s) => s.OperationRules());
        }

        public static bool GetAllowed(Enum operationKey)
        {
            return authorizedOperations.GetAllowed(operationKey);
        }
    }


    [MarkupExtensionReturnType(typeof(bool))]
    public class OperationAllowedExtension : MarkupExtension
    {
        Enum operationKey;
        public OperationAllowedExtension(object value)
        {
            this.operationKey = (Enum)value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return OperationAuthClient.GetAllowed(operationKey);
        }
    }

    [MarkupExtensionReturnType(typeof(Visibility))]
    public class OperationVisiblityExtension : MarkupExtension
    {
        Enum operationKey;
        public OperationVisiblityExtension(object value)
        {
            this.operationKey = (Enum)value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
             return OperationAuthClient.GetAllowed(operationKey) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}

