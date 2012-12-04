using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.DataStructures;
using Signum.Utilities;
using Signum.Entities.Basics;

namespace Signum.Entities.Disconnected
{
    public static class DisconnectedExportRanges
    {
        static Dictionary<Type, int> maxIdsInRange;

        static Interval<int>? machineIdRange;

        public static void Initialize(DisconnectedExportDN lastExport, DisconnectedMachineDN currentMachine, Dictionary<Lite<TypeDN>, Type> typeDictionary)
        {
            if (!lastExport.Machine.RefersTo(currentMachine))
                throw new InvalidOperationException("The machine of the lastExport doesn't match the current machine");

            machineIdRange = new Interval<int>(currentMachine.SeedMin, currentMachine.SeedMax);

            maxIdsInRange = lastExport.Copies.Where(a => a.MaxIdInRange != null).ToDictionary(a => typeDictionary[a.Type], a => a.MaxIdInRange.Value);
        }

        public static bool InModifiableRange(Type type, int id)
        {
            if (machineIdRange == null)
                throw new InvalidOperationException("DisconnectedExportRanges has not been initialized");

            if (!machineIdRange.Value.Contains(id))
                return false;
            
            int? max = maxIdsInRange.TryGetS(type);

            return max == null || id > max;
        }
    }
}
