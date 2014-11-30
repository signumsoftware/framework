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

        private static void WaitFinished(this NormalWindowProxy<ProcessEntity> pe, Func<string> actionDescription, int? timeout = null)
        {
            pe.Element.Wait(() => pe.ValueLineValue(pe2 => pe2.State) == ProcessState.Finished,
                            () => "{0}, result state is {1}".FormatWith((actionDescription != null ? actionDescription() : "Waiting for process to finish"), pe.ValueLineValue(pe2 => pe2.State)),
                            timeout ?? DefaultTimeout);
        }

        public static void ConstructProcessPlayAndWait<T>(this NormalWindowProxy<T> normalWindow, ConstructSymbol<ProcessEntity>.From<T> symbol, int? timeout = null) where T : Entity
        {
            using (var pe = normalWindow.ConstructFrom(symbol))
            {
                pe.Execute(ProcessOperation.Execute);
                pe.WaitFinished(() => "Waiting for process after {0} to finish".FormatWith(symbol.Symbol), timeout);
            }
        }

        public static void ConstructProcessWait<T>(this NormalWindowProxy<T> normalWindow, ConstructSymbol<ProcessEntity>.From<T> symbol, int? timeout = null) where T : Entity
        {
            using (var pe = normalWindow.ConstructFrom(symbol))
            {
                pe.WaitFinished(() => "Waiting for process after {0} to finish".FormatWith(symbol.Symbol), timeout);
            }
        }
    }
}
