using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades
{
    class Upgrade_20201230_TensorFlow : CodeUpgradeBase
    {
        public override string Description => "replace CNTK for TensorFlow";

        public override void Execute(UpgradeContext uctx)
        {
            uctx.ChangeCodeFile(@".gitignore", file =>
            {
                file.InsertAfterLastLine(
                    a=>a.Contains("ts_out"),
                    @"/Southwind.React/TensorFlowModels/**");
            });

            uctx.ChangeCodeFile(@"Southwind.Logic/Southwind.Logic.csproj", file =>
            {
                file.Replace(
                    @"Signum.Engine.MachineLearning.CNTK\Signum.Engine.MachineLearning.CNTK.csproj",
                    @"Signum.Engine.MachineLearning.TensorFlow\Signum.Engine.MachineLearning.TensorFlow.csproj");

            }, WarningLevel.Warning);

            uctx.ChangeCodeFile(@"Southwind.React/Southwind.React.csproj", file =>
            {
                file.ReplaceLine(a => a.Contains("CNTK.CPUOnly"),
                    @"<PackageReference Include=""SciSharp.TensorFlow.Redist"" Version=""2.3.1"" />"
                    );
            }, WarningLevel.Warning);

            uctx.ChangeCodeFile(@"Southwind.Logic/Starter.cs", file =>
            {
                file.Replace(
                    @"using Signum.Engine.MachineLearning.CNTK;",
                    @"using Signum.Engine.MachineLearning.TensorFlow;");

                file.Replace(
          @"CNTKPredictorAlgorithm.NeuralNetwork, new CNTKNeuralNetworkPredictorAlgorithm()",
          @"TensorFlowPredictorAlgorithm.NeuralNetworkGraph, new TensorFlowNeuralNetworkPredictor()");

            }, WarningLevel.Warning);

            uctx.ChangeCodeFile(@"Southwind.Terminal/SouthwindMigrations.cs", file =>
            {
                file.Replace(
                    @"CNTKPredictorAlgorithm.NeuralNetwork",
                    @"TensorFlowPredictorAlgorithm.NeuralNetworkGraph");

                file.Replace(
                    @"Learner = NeuralNetworkLearner.MomentumSGD",
                    @"Optimizer = TensorFlowOptimizer.GradientDescentOptimizer");
               
                file.Replace(
                    @"LossFunction = NeuralNetworkEvalFunction.SquaredError",
                    @"LossFunction = NeuralNetworkEvalFunction.MeanSquaredError");

                file.ReplaceBetweenIncluded(
                    a => a.Contains("LearningRate = 0.1,"),
                    a => a.Contains("LearningUnitGain = false,"),
                    @"LearningRate = 0.0001,");
            }, WarningLevel.Warning);
        }
    }
}
