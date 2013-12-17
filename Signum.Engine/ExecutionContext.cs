using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;

namespace Signum.Engine
{
    public static class ExecutionMode
    {
        static readonly ThreadVariable<bool> inGlobalMode = Statics.ThreadVariable<bool>("inGlobalMode");
        public static bool InGlobal
        {
            get { return inGlobalMode.Value; }
        }

        public static IDisposable Global()
        {
            var oldValue = inGlobalMode.Value;
            inGlobalMode.Value = true;
            return new Disposable(() => inGlobalMode.Value = oldValue);
        }

        static readonly ThreadVariable<bool> inUserInterfaceMode = Statics.ThreadVariable<bool>("inUserInterfaceMode");
        public static bool InUserInterface
        {
            get { return inUserInterfaceMode.Value; }
        }

        public static IDisposable UserInterface()
        {
            var oldValue = inUserInterfaceMode.Value;
            inUserInterfaceMode.Value = true;
            return new Disposable(() => inUserInterfaceMode.Value = oldValue);
        }


        public static bool IsCacheDisabled
        {
            get { return cacheTempDisabled.Value; }
        }

        static readonly ThreadVariable<bool> cacheTempDisabled = Statics.ThreadVariable<bool>("cacheTempDisabled");
        public static IDisposable DisableCache()
        {
            if (cacheTempDisabled.Value) return null;
            cacheTempDisabled.Value = true;
            return new Disposable(() => cacheTempDisabled.Value = false);
        }


        public static bool IsSynchronizeSchemaOnly
        {
            get { return synchronizeSchemaOnly.Value; }
        }

        static readonly ThreadVariable<bool> synchronizeSchemaOnly = Statics.ThreadVariable<bool>("synchronizeSchemaOnly");
        public static IDisposable SynchronizeSchemaOnly()
        {
            if (synchronizeSchemaOnly.Value) return null;
            synchronizeSchemaOnly.Value = true;
            return new Disposable(() => synchronizeSchemaOnly.Value = false);
        }

    }
}
