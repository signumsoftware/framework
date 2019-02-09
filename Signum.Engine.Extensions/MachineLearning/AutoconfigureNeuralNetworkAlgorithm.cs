using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Engine.Processes;
using Signum.Entities;
using Signum.Entities.MachineLearning;
using Signum.Utilities;
using Signum.Engine.Operations;

namespace Signum.Engine.MachineLearning
{
    public class AutoconfigureNeuralNetworkAlgorithm : Processes.IProcessAlgorithm
    {
        public void Execute(ExecutingProcess ep)
        {   
            var conf = (AutoconfigureNeuralNetworkEntity)ep.Data!;

            var initial = conf.InitialPredictor.RetrieveAndForget();
            Random r = conf.Seed == null ? 
                new Random(): 
                new Random(conf.Seed.Value);

            var mutationProbability = conf.InitialMutationProbability;

            List<PredictorEntity> population = 0.To(conf.Population).Select(p => initial.ConstructFrom(PredictorOperation.Clone)).ToList();

            population.ForEach(p => Mutate(p, conf, mutationProbability, r));

            Dictionary<PredictorEntity, double> evaluatedPopulation = EvaluatePopulation(ep, conf, population, 0);

            for (int gen = 1; gen < conf.Generations + 1; gen++)
            {
                population = CrossOverPopulation(evaluatedPopulation, initial, conf, r);

                population.ForEach(p => Mutate(p, conf, mutationProbability, r));

                evaluatedPopulation = EvaluatePopulation(ep, conf, population, gen);
            }
        }
        
        public List<PredictorEntity> CrossOverPopulation(Dictionary<PredictorEntity, double> evaluatedPopulation, PredictorEntity initial, AutoconfigureNeuralNetworkEntity opt, Random r)
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

        private PredictorEntity CrossOver(PredictorEntity child, PredictorEntity father, PredictorEntity mother, Random r, AutoconfigureNeuralNetworkEntity conf)
        {
            var nnChild = (NeuralNetworkSettingsEntity)child.AlgorithmSettings;
            var nnFather = (NeuralNetworkSettingsEntity)father.AlgorithmSettings;
            var nnMother = (NeuralNetworkSettingsEntity)mother.AlgorithmSettings;

            if (conf.ExploreLearner)
            {
                nnChild.Learner = r.NextBool() ? nnFather.Learner : nnMother.Learner;
            }

            if (conf.ExploreLearningValues)
            {
                nnChild.LearningRate = r.NextBool() ? nnFather.LearningRate : nnMother.LearningRate;
                nnChild.LearningMomentum = r.NextBool() ? nnFather.LearningMomentum : nnMother.LearningMomentum;
                nnChild.LearningVarianceMomentum = r.NextBool() ? nnFather.LearningVarianceMomentum : nnMother.LearningVarianceMomentum;
            }

            if (conf.ExploreHiddenLayers)
            {
                nnChild.HiddenLayers = nnFather.HiddenLayers.ZipOrDefault(nnMother.HiddenLayers, (h1, h2) => r.NextBool() ? h1 : h2).NotNull().ToMList();
            }

            if (conf.ExploreOutputLayer)
            {
                nnChild.OutputActivation = r.NextBool() ? nnFather.OutputActivation : nnMother.OutputActivation;
                nnChild.OutputInitializer = r.NextBool() ? nnFather.OutputInitializer : nnMother.OutputInitializer;
            }

            return child;
        }

        public Dictionary<PredictorEntity, double> EvaluatePopulation(ExecutingProcess ep, AutoconfigureNeuralNetworkEntity conf, List<PredictorEntity> population,  int gen)
        {
            var total = conf.Population * (conf.Generations + 1);
            var evaluatedPopulation = new Dictionary<PredictorEntity, double>();
            for (int i = 0; i < population.Count; i++)
            {
                ep.CancellationToken.ThrowIfCancellationRequested();

                var current = gen * population.Count + i;
                ep.ProgressChanged(current / (decimal)total);
                var p = population[i];

                double lastValidation = Evaluate(ep, p, onProgress: val => ep.ProgressChanged((current + val ?? 0) / (decimal)total));

                evaluatedPopulation.Add(p, lastValidation);
            }
            return evaluatedPopulation;
        }

        private static double Evaluate(ExecutingProcess ep, PredictorEntity p, Action<decimal?> onProgress)
        {
            PredictorLogic.TrainSync(p, onReportProgres: (str, val) => onProgress(val));

            return p.ResultValidation!.Loss.Value;
        }

        //private static double EvaluateMock(ExecutingProcess ep, PredictorEntity p, Action<decimal?> onProgress)
        //{
        //    var nns = (NeuralNetworkSettingsEntity)p.AlgorithmSettings; 

        //    var ctx = Lite.Create<PredictorEntity>(1835).GetPredictContext();
        //    var mq = ctx.Predictor.MainQuery;
        //    var inputs = new PredictDictionary(ctx.Predictor)
        //    {
        //        MainQueryValues = 
        //        {
        //            { mq.FindColumn(nameof(nns.Learner)), nns.Learner },
        //            { mq.FindColumn(nameof(nns.LearningRate)), nns.LearningRate },
        //            { mq.FindColumn(nameof(nns.LearningMomentum)), nns.LearningMomentum },
        //            { mq.FindColumn(nameof(nns.LearningVarianceMomentum)), nns.LearningVarianceMomentum },
        //            { mq.FindColumn(nameof(nns.LearningUnitGain)), nns.LearningUnitGain },
        //        }
        //    };

        //    var outputs = inputs.PredictBasic();

        //    var outValue = outputs.MainQueryValues.GetOrThrow(mq.FindColumn(nameof(ctx.Predictor.ResultValidation)));

        //    return Convert.ToDouble(outValue);
        //}

        private void Mutate(PredictorEntity predictor, AutoconfigureNeuralNetworkEntity conf, double mutationProbability, Random r)
        {
            var nns = (NeuralNetworkSettingsEntity)predictor.AlgorithmSettings;
            if (conf.ExploreLearner)
            {
                if (r.NextDouble() < mutationProbability)
                    nns.Learner = r.NextElement(EnumExtensions.GetValues<NeuralNetworkLearner>());
            }

            if (conf.ExploreLearningValues)
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

            if (conf.ExploreHiddenLayers)
            {
                if (r.NextDouble() < mutationProbability)
                {
                    var shouldHidden = Math.Min(0, Math.Max(nns.HiddenLayers.Count + (r.NextBool() ? 1 : -1), conf.MaxLayers));

                    if (shouldHidden > nns.HiddenLayers.Count)
                    {
                        nns.HiddenLayers.Add(new NeuralNetworkHidenLayerEmbedded
                        {
                            Size = r.Next(conf.MaxNeuronsPerLayer),
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
                        hl.Size = (r.Next(conf.MinNeuronsPerLayer, conf.MaxNeuronsPerLayer) + hl.Size) / 2;

                    if (r.NextDouble() < mutationProbability)
                        hl.Activation = r.NextElement(EnumExtensions.GetValues<NeuralNetworkActivation>());

                    if (r.NextDouble() < mutationProbability)
                        hl.Initializer = r.NextElement(EnumExtensions.GetValues<NeuralNetworkInitializer>());
                }
            }

            if (conf.ExploreOutputLayer)
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
