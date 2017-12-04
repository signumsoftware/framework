using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Signum.Engine.Processes;
using Signum.Entities;
using Signum.Entities.MachineLearning;
using Signum.Utilities;
using Signum.Engine.Operations;

namespace Signum.Engine.MachineLearning
{
    public class PredictorNeuralGeneticAlgorithm : Processes.IProcessAlgorithm
    {
        public void Execute(ExecutingProcess ep)
        {   
            var opt = (NeuralNetworkSettingsGeneticOptimizerEntity)ep.Data;

            var initial = opt.StartingFrom.Retrieve();
            Random r = opt.Seed == null ? null : new Random(opt.Seed.Value);

            var mutationProbability = opt.InitialMutationProbability;

            List<PredictorEntity> population = 0.To(opt.Population).Select(p => initial.ConstructFrom(PredictorOperation.Clone)).ToList();

            population.ForEach(p => Mutate(p, opt, mutationProbability, r));

            Dictionary<PredictorEntity, double> evaluatedPopulation = EvaluatePopulation(ep, opt, population, 0);

            for (int gen = 1; gen < opt.Generations + 1; gen++)
            {
                population = CrossOverPopulation(evaluatedPopulation, initial, opt, r);

                EvaluatePopulation(ep, opt, population, gen);
            }
        }
        
        public List<PredictorEntity> CrossOverPopulation(Dictionary<PredictorEntity, double> evaluatedPopulation, PredictorEntity initial, NeuralNetworkSettingsGeneticOptimizerEntity opt, Random r)
        {
            var positiveSurvivors = evaluatedPopulation.ToDictionary(kvp => kvp.Key, kvp => 1 / (kvp.Value + 0.01));

            var total = positiveSurvivors.Values.Sum();

            PredictorEntity SelectRandomly()
            {
                var point = r.NextDouble() * total;

                double acum = 0;
                foreach (var kvp in positiveSurvivors)
                {
                    acum += kvp.Value;
                    if (point < acum)
                        return kvp.Key;                    
                }

                throw new InvalidOperationException("Out of range");
            }
            
            return 0.To(opt.Population).Select(i => CrossOver(initial.ConstructFrom(PredictorOperation.Clone), SelectRandomly(), SelectRandomly(), r, opt)).ToList();
        }

        private PredictorEntity CrossOver(PredictorEntity child, PredictorEntity father, PredictorEntity mother, Random r, NeuralNetworkSettingsGeneticOptimizerEntity opt)
        {
            var nnChild = (NeuralNetworkSettingsEntity)child.AlgorithmSettings;
            var nnFather = (NeuralNetworkSettingsEntity)father.AlgorithmSettings;
            var nnMother = (NeuralNetworkSettingsEntity)mother.AlgorithmSettings;

            if (opt.ExploreLearner)
            {
                nnChild.Learner = r.NextBool() ? nnFather.Learner : nnMother.Learner;
            }

            if (opt.ExploreLearningValues)
            {
                nnChild.LearningRate = r.NextBool() ? nnFather.LearningRate : nnMother.LearningRate;
                nnChild.LearningMomentum = r.NextBool() ? nnFather.LearningMomentum : nnMother.LearningMomentum;
                nnChild.LearningVarianceMomentum = r.NextBool() ? nnFather.LearningVarianceMomentum : nnMother.LearningVarianceMomentum;
            }

            if (opt.ExploreHiddenLayers)
            {
                nnChild.HiddenLayers = nnFather.HiddenLayers.ZipOrDefault(nnMother.HiddenLayers, (h1, h2) => r.NextBool() ? h1 : h2).NotNull().ToMList();
            }

            if (opt.ExploreOutputLayer)
            {
                nnChild.OutputActivation = r.NextBool() ? nnFather.OutputActivation : nnMother.OutputActivation;
                nnChild.OutputInitializer = r.NextBool() ? nnFather.OutputInitializer : nnMother.OutputInitializer;
            }

            return child;
        }

        public Dictionary<PredictorEntity, double> EvaluatePopulation(ExecutingProcess ep, NeuralNetworkSettingsGeneticOptimizerEntity opt, List<PredictorEntity> population,  int gen)
        {
            var total = opt.Population * (opt.Generations + 1);
            var evaluatedPopulation = new Dictionary<PredictorEntity, double>();
            for (int i = 0; i < population.Count; i++)
            {
                ep.CancellationToken.ThrowIfCancellationRequested();

                var current = gen * population.Count + i;                
                ep.ProgressChanged(current / (decimal)total);
                var p = population[i];

                PredictorLogic.TrainSync(p, onReportProgres: (str, val) => ep.ProgressChanged((current + val ?? 0) / (decimal)total));

                var lastValidation = p.EpochProgresses().OrderByDescending(a => a.Epoch).SingleOrDefaultEx().EvaluationValidation ?? double.MaxValue;

                evaluatedPopulation.Add(p, lastValidation);
            }
            return evaluatedPopulation;
        }

        private void Mutate(PredictorEntity predictor, NeuralNetworkSettingsGeneticOptimizerEntity opt, double mutationProbability, Random r)
        {
            var nns = (NeuralNetworkSettingsEntity)predictor.AlgorithmSettings;
            if (opt.ExploreLearner)
            {
                if (r.NextDouble() < mutationProbability)
                    nns.Learner = r.NextElement(EnumExtensions.GetValues<NeuralNetworkLearner>());
            }

            if (opt.ExploreLearningValues)
            {
                double IncrementOrDecrement(double value)
                {
                    var ratio = 1.1 + r.NextDouble() * 0.9;

                    return r.NextBool() ? value / ratio : value * ratio;
                }

                if (r.NextDouble() < mutationProbability)
                    nns.LearningRate = IncrementOrDecrement(nns.LearningRate);

                if (r.NextDouble() < mutationProbability)
                    nns.LearningMomentum = IncrementOrDecrement(nns.LearningMomentum ?? 0.01);

                if (r.Next() < mutationProbability)
                    nns.LearningVarianceMomentum = IncrementOrDecrement(nns.LearningVarianceMomentum ?? 0.01);
            }

            if (opt.ExploreHiddenLayers)
            {
                if (r.NextDouble() < mutationProbability)
                {
                    var shouldHidden = Math.Min(0, Math.Max(nns.HiddenLayers.Count + (r.NextBool() ? 1 : -1), opt.MaxLayers));

                    if (shouldHidden > nns.HiddenLayers.Count)
                    {
                        nns.HiddenLayers.Add(new NeuralNetworkHidenLayerEmbedded
                        {
                            Size = r.Next(opt.MaxNeuronsPerLayer),
                            Activation = r.NextElement(EnumExtensions.GetValues<NeuralNetworkActivation>()),
                            Initializer = r.NextElement(EnumExtensions.GetValues<NeuralNetworkInitializer>()),
                        });
                    }
                    else if (shouldHidden < nns.HiddenLayers.Count)
                    {
                        nns.HiddenLayers.RemoveAt(r.Next(nns.HiddenLayers.Count));
                    }
                }

                foreach (var hl in nns.HiddenLayers)
                {
                    if (r.NextDouble() < mutationProbability)
                        hl.Size = (r.Next(opt.MinNeuronsPerLayer, opt.MaxNeuronsPerLayer) + hl.Size) / 2;

                    if (r.NextDouble() < mutationProbability)
                        hl.Activation = r.NextElement(EnumExtensions.GetValues<NeuralNetworkActivation>());

                    if (r.NextDouble() < mutationProbability)
                        hl.Initializer = r.NextElement(EnumExtensions.GetValues<NeuralNetworkInitializer>());
                }
            }

            if (opt.ExploreOutputLayer)
            {
                if (r.NextDouble() < mutationProbability)
                    nns.OutputActivation = r.NextElement(EnumExtensions.GetValues<NeuralNetworkActivation>());

                if (r.NextDouble() < mutationProbability)
                    nns.OutputInitializer = r.NextElement(EnumExtensions.GetValues<NeuralNetworkInitializer>());
            }

            nns.LearningUnitGain = false; //better to deverge than to stay flat

        }
    }
}
