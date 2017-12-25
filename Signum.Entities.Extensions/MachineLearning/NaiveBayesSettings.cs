using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.MachineLearning
{
    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class NaiveBayesSettingsEntity : Entity, IPredictorAlgorithmSettings
    {
        public bool Empirical { get; set; }

        public IPredictorAlgorithmSettings Clone() => new NaiveBayesSettingsEntity
        {
            Empirical = Empirical
        };
    }

}
