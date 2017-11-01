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
using Signum.Entities;

namespace Signum.Windows.Authorization
{
    public static class OperationAuthClient
    {
        static Dictionary<(OperationSymbol operation, Type type), OperationAllowed> authorizedOperations;

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

        public static bool GetAllowed(OperationSymbol operationSymbol, Type type, bool inUserInterface)
        {
            var allowed = authorizedOperations.GetOrThrow((operationSymbol, type));

            return allowed == OperationAllowed.Allow || allowed == OperationAllowed.DBOnly && !inUserInterface;
        }
    }


    [MarkupExtensionReturnType(typeof(bool))]
    public class OperationAllowedExtension : MarkupExtension
    {
        public bool InUserInterface { get; set; }

        OperationSymbol operationSymbol;
        Type type;

        public OperationAllowedExtension(object value, Type type)
        {
            this.operationSymbol = (value is IOperationSymbolContainer) ? ((IOperationSymbolContainer)value).Symbol : (OperationSymbol)value;
            this.type = type;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return OperationAuthClient.GetAllowed(operationSymbol, type, InUserInterface);
        }
    }

    [MarkupExtensionReturnType(typeof(Visibility))]
    public class OperationVisiblityExtension : MarkupExtension
    {
        public bool InUserInterface { get; set; }

        OperationSymbol operationSymbol;
        Type type;

        public OperationVisiblityExtension(object value, Type type)
        {
            this.operationSymbol = (value is IOperationSymbolContainer) ? ((IOperationSymbolContainer)value).Symbol : (OperationSymbol)value;

        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return OperationAuthClient.GetAllowed(operationSymbol, type, InUserInterface) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}

