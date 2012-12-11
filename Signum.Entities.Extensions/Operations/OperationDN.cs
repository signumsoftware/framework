using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics; 

namespace Signum.Entities.Operations
{
    [Serializable]
    public class OperationDN : MultiEnumDN
    {
        public static string NotDefinedFor(Enum operation, IEnumerable<Type> notDefined)
        {
            if (notDefined.Any())
                return "{0} is not defined for {1}".Formato(operation.NiceToString(), notDefined.CommaAnd(a => a.NiceName()));

            return null;
        }
    }

    [Serializable]
    public class OperationInfo
    {
        public Enum Key { get; internal set; }
        public OperationType OperationType { get; internal set; }

        public bool? Lite { get; internal set; }
        public bool? AllowsNew { get; internal set; }
        public bool? HasStates { get; internal set; }
        public bool? HasCanExecute { get; internal set; }

        public bool Returns { get; internal set; }
        public Type ReturnType { get; internal set; }

        public override string ToString()
        {
            return "{0} ({1}) Lite = {2}, Returns {3}".Formato(Key, OperationType, Lite, Returns);
        }

        public bool IsEntityOperation
        {
            get
            {
                return OperationType == Operations.OperationType.Execute ||
                    OperationType == Operations.OperationType.ConstructorFrom ||
                    OperationType == Operations.OperationType.Delete;
            }
        }

    }


    [Flags]
    public enum OperationType
    {
        Execute, 
        Delete,
        Constructor, 
        ConstructorFrom,
        ConstructorFromMany
    }
}
