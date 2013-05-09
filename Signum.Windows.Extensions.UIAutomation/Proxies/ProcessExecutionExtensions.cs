using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Processes;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Windows.UIAutomation
{
    public static class ProcessExecutionExtensions
    {
        static int DefaultTimeout = 20 * 1000;

        public static void PlayAndWait(this NormalWindowProxy<ProcessExecutionDN> pe, Func<string> actionDescription  = null, int? timeout = null)
        {
            using (pe)
            {
                pe.Execute(ProcessOperation.Execute);
                pe.Element.Wait(() => pe.ValueLineValue(pe2 => pe2.State) == ProcessState.Finished,
                                () => "{0}, result state is {1}".Formato((actionDescription != null ? actionDescription() : "Waiting for process to finish"), pe.ValueLineValue(pe2 => pe2.State)),
                                timeout ?? DefaultTimeout);
            }
        }

        public static void ConstructProcessAndPlay<T>(this NormalWindowProxy<T> normalWnindow, Enum processOperation, int? timeout = null) where T : ModifiableEntity
        {
            var pe = normalWnindow.ConstructFrom<ProcessExecutionDN>(processOperation);
            pe.PlayAndWait(() => "Waiting for process after {0} to finish".Formato(OperationDN.UniqueKey(processOperation)));
        }
    }
}
