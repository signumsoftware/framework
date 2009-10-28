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
    public class OperationDN : EnumDN
    {
    }

    [Serializable]
    public class OperationInfo
    {
        public Enum Key { get; set; }
        public OperationType OperationType { get; set; }
        public bool CanExecute { get; set; }
        public bool Lite { get; set; }
        public bool Returns { get; set; }

        public override string ToString()
        {
            return "{0} ({1}) CanExecute = {2}, Lite = {3}, Returns {4}".Formato(Key, OperationType, CanExecute, Lite, Returns);
        }
    }


    [Flags]
    public enum OperationType
    {
        Execute, 
        Constructor, 
        ConstructorFrom,
        ConstructorFromMany
    }
}
