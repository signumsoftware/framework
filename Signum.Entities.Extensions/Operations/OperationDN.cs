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
    }

    [Serializable]
    public class OperationInfo
    {
        public Enum Key { get; set; }
        public OperationType OperationType { get; set; }
        public string CanExecute { get; set; }
        public bool? Lite { get; set; }
        public bool Returns { get; set; }
        public Type ReturnType { get; set; }

        public override string ToString()
        {
            return "{0} ({1}) CanExecute = {2}, Lite = {3}, Returns {4}".Formato(Key, OperationType, CanExecute, Lite, Returns);
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

        public bool HasStates { get; set; }
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
