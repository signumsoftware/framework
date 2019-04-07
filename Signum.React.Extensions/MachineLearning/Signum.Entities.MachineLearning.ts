//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as Processes from '../Processes/Signum.Entities.Processes'
import * as Files from '../Files/Signum.Entities.Files'
import * as UserQueries from '../UserQueries/Signum.Entities.UserQueries'
import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets'


export const AutoconfigureNeuralNetworkEntity = new Type<AutoconfigureNeuralNetworkEntity>("AutoconfigureNeuralNetwork");
export interface AutoconfigureNeuralNetworkEntity extends Entities.Entity, Processes.IProcessDataEntity {
  Type: "AutoconfigureNeuralNetwork";
  initialPredictor: Entities.Lite<PredictorEntity>;
  exploreLearner: boolean;
  exploreLearningValues: boolean;
  exploreHiddenLayers: boolean;
  exploreOutputLayer: boolean;
  maxLayers: number;
  minNeuronsPerLayer: number;
  maxNeuronsPerLayer: number;
  oneTrainingDuration: number | null;
  generations: number;
  population: number;
  survivalRate: number;
  initialMutationProbability: number;
  seed: number | null;
}

export module CNTKPredictorAlgorithm {
  export const NeuralNetwork : PredictorAlgorithmSymbol = registerSymbol("PredictorAlgorithm", "CNTKPredictorAlgorithm.NeuralNetwork");
}

export module DefaultColumnEncodings {
  export const None : PredictorColumnEncodingSymbol = registerSymbol("PredictorColumnEncoding", "DefaultColumnEncodings.None");
  export const OneHot : PredictorColumnEncodingSymbol = registerSymbol("PredictorColumnEncoding", "DefaultColumnEncodings.OneHot");
  export const NormalizeZScore : PredictorColumnEncodingSymbol = registerSymbol("PredictorColumnEncoding", "DefaultColumnEncodings.NormalizeZScore");
  export const NormalizeMinMax : PredictorColumnEncodingSymbol = registerSymbol("PredictorColumnEncoding", "DefaultColumnEncodings.NormalizeMinMax");
  export const NormalizeLog : PredictorColumnEncodingSymbol = registerSymbol("PredictorColumnEncoding", "DefaultColumnEncodings.NormalizeLog");
  export const SplitWords : PredictorColumnEncodingSymbol = registerSymbol("PredictorColumnEncoding", "DefaultColumnEncodings.SplitWords");
}

export interface IPredictorAlgorithmSettings extends Entities.Entity {
}

export const NeuralNetworkActivation = new EnumType<NeuralNetworkActivation>("NeuralNetworkActivation");
export type NeuralNetworkActivation =
  "None" |
  "ReLU" |
  "Sigmoid" |
  "Tanh";

export const NeuralNetworkEvalFunction = new EnumType<NeuralNetworkEvalFunction>("NeuralNetworkEvalFunction");
export type NeuralNetworkEvalFunction =
  "CrossEntropyWithSoftmax" |
  "ClassificationError" |
  "SquaredError" |
  "MeanAbsoluteError" |
  "MeanAbsolutePercentageError";

export const NeuralNetworkHidenLayerEmbedded = new Type<NeuralNetworkHidenLayerEmbedded>("NeuralNetworkHidenLayerEmbedded");
export interface NeuralNetworkHidenLayerEmbedded extends Entities.EmbeddedEntity {
  Type: "NeuralNetworkHidenLayerEmbedded";
  size: number;
  activation: NeuralNetworkActivation;
  initializer: NeuralNetworkInitializer;
}

export const NeuralNetworkInitializer = new EnumType<NeuralNetworkInitializer>("NeuralNetworkInitializer");
export type NeuralNetworkInitializer =
  "Zero" |
  "GlorotNormal" |
  "GlorotUniform" |
  "HeNormal" |
  "HeUniform" |
  "Normal" |
  "TruncateNormal" |
  "Uniform" |
  "Xavier";

export const NeuralNetworkLearner = new EnumType<NeuralNetworkLearner>("NeuralNetworkLearner");
export type NeuralNetworkLearner =
  "Adam" |
  "AdaDelta" |
  "AdaGrad" |
  "FSAdaGrad" |
  "RMSProp" |
  "MomentumSGD" |
  "SGD";

export const NeuralNetworkSettingsEntity = new Type<NeuralNetworkSettingsEntity>("NeuralNetworkSettings");
export interface NeuralNetworkSettingsEntity extends Entities.Entity, IPredictorAlgorithmSettings {
  Type: "NeuralNetworkSettings";
  device: string | null;
  predictionType: PredictionType;
  hiddenLayers: Entities.MList<NeuralNetworkHidenLayerEmbedded>;
  outputActivation: NeuralNetworkActivation;
  outputInitializer: NeuralNetworkInitializer;
  learner: NeuralNetworkLearner;
  lossFunction: NeuralNetworkEvalFunction;
  evalErrorFunction: NeuralNetworkEvalFunction;
  learningRate: number;
  learningMomentum: number | null;
  learningUnitGain: boolean | null;
  learningVarianceMomentum: number | null;
  minibatchSize: number;
  numMinibatches: number;
  bestResultFromLast: number;
  saveProgressEvery: number;
  saveValidationProgressEvery: number;
}

export const PredictionSet = new EnumType<PredictionSet>("PredictionSet");
export type PredictionSet =
  "Validation" |
  "Training";

export const PredictionType = new EnumType<PredictionType>("PredictionType");
export type PredictionType =
  "Regression" |
  "MultiRegression" |
  "Classification" |
  "MultiClassification";

export const PredictorAlgorithmSymbol = new Type<PredictorAlgorithmSymbol>("PredictorAlgorithm");
export interface PredictorAlgorithmSymbol extends Entities.Symbol {
  Type: "PredictorAlgorithm";
}

export const PredictorClassificationMetricsEmbedded = new Type<PredictorClassificationMetricsEmbedded>("PredictorClassificationMetricsEmbedded");
export interface PredictorClassificationMetricsEmbedded extends Entities.EmbeddedEntity {
  Type: "PredictorClassificationMetricsEmbedded";
  totalCount: number;
  missCount: number;
  missRate: number | null;
}

export const PredictorCodificationEntity = new Type<PredictorCodificationEntity>("PredictorCodification");
export interface PredictorCodificationEntity extends Entities.Entity {
  Type: "PredictorCodification";
  predictor: Entities.Lite<PredictorEntity>;
  usage: PredictorColumnUsage;
  index: number;
  subQueryIndex: number | null;
  originalColumnIndex: number;
  splitKey0: string | null;
  splitKey1: string | null;
  splitKey2: string | null;
  isValue: string | null;
  average: number | null;
  stdDev: number | null;
  min: number | null;
  max: number | null;
}

export const PredictorColumnEmbedded = new Type<PredictorColumnEmbedded>("PredictorColumnEmbedded");
export interface PredictorColumnEmbedded extends Entities.EmbeddedEntity {
  Type: "PredictorColumnEmbedded";
  usage: PredictorColumnUsage;
  token: UserAssets.QueryTokenEmbedded;
  encoding: PredictorColumnEncodingSymbol;
  nullHandling: PredictorColumnNullHandling;
}

export const PredictorColumnEncodingSymbol = new Type<PredictorColumnEncodingSymbol>("PredictorColumnEncoding");
export interface PredictorColumnEncodingSymbol extends Entities.Symbol {
  Type: "PredictorColumnEncoding";
}

export const PredictorColumnNullHandling = new EnumType<PredictorColumnNullHandling>("PredictorColumnNullHandling");
export type PredictorColumnNullHandling =
  "Zero" |
  "Error" |
  "Average" |
  "Min" |
  "Max";

export const PredictorColumnUsage = new EnumType<PredictorColumnUsage>("PredictorColumnUsage");
export type PredictorColumnUsage =
  "Input" |
  "Output";

export const PredictorEntity = new Type<PredictorEntity>("Predictor");
export interface PredictorEntity extends Entities.Entity, Processes.IProcessDataEntity {
  Type: "Predictor";
  name: string | null;
  settings: PredictorSettingsEmbedded;
  algorithm: PredictorAlgorithmSymbol;
  resultSaver: PredictorResultSaverSymbol | null;
  publication: PredictorPublicationSymbol | null;
  trainingException: Entities.Lite<Basics.ExceptionEntity> | null;
  user: Entities.Lite<Basics.IUserEntity> | null;
  algorithmSettings: IPredictorAlgorithmSettings;
  state: PredictorState;
  mainQuery: PredictorMainQueryEmbedded;
  subQueries: Entities.MList<PredictorSubQueryEntity>;
  files: Entities.MList<Files.FilePathEmbedded>;
  resultTraining: PredictorMetricsEmbedded | null;
  resultValidation: PredictorMetricsEmbedded | null;
  classificationTraining: PredictorClassificationMetricsEmbedded | null;
  classificationValidation: PredictorClassificationMetricsEmbedded | null;
  regressionTraining: PredictorRegressionMetricsEmbedded | null;
  regressionValidation: PredictorRegressionMetricsEmbedded | null;
}

export const PredictorEpochProgressEntity = new Type<PredictorEpochProgressEntity>("PredictorEpochProgress");
export interface PredictorEpochProgressEntity extends Entities.Entity {
  Type: "PredictorEpochProgress";
  predictor: Entities.Lite<PredictorEntity>;
  creationDate: string;
  ellapsed: number;
  trainingExamples: number;
  epoch: number;
  lossTraining: number | null;
  evaluationTraining: number | null;
  lossValidation: number | null;
  evaluationValidation: number | null;
}

export module PredictorFileType {
  export const PredictorFile : Files.FileTypeSymbol = registerSymbol("FileType", "PredictorFileType.PredictorFile");
}

export const PredictorMainQueryEmbedded = new Type<PredictorMainQueryEmbedded>("PredictorMainQueryEmbedded");
export interface PredictorMainQueryEmbedded extends Entities.EmbeddedEntity {
  Type: "PredictorMainQueryEmbedded";
  query: Basics.QueryEntity;
  groupResults: boolean;
  filters: Entities.MList<UserQueries.QueryFilterEmbedded>;
  columns: Entities.MList<PredictorColumnEmbedded>;
}

export module PredictorMessage {
  export const Csv = new MessageKey("PredictorMessage", "Csv");
  export const Tsv = new MessageKey("PredictorMessage", "Tsv");
  export const TsvMetadata = new MessageKey("PredictorMessage", "TsvMetadata");
  export const TensorflowProjector = new MessageKey("PredictorMessage", "TensorflowProjector");
  export const DownloadCsv = new MessageKey("PredictorMessage", "DownloadCsv");
  export const DownloadTsv = new MessageKey("PredictorMessage", "DownloadTsv");
  export const DownloadTsvMetadata = new MessageKey("PredictorMessage", "DownloadTsvMetadata");
  export const OpenTensorflowProjector = new MessageKey("PredictorMessage", "OpenTensorflowProjector");
  export const _0IsAlreadyBeingTrained = new MessageKey("PredictorMessage", "_0IsAlreadyBeingTrained");
  export const StartingTraining = new MessageKey("PredictorMessage", "StartingTraining");
  export const Preview = new MessageKey("PredictorMessage", "Preview");
  export const Codifications = new MessageKey("PredictorMessage", "Codifications");
  export const Progress = new MessageKey("PredictorMessage", "Progress");
  export const Results = new MessageKey("PredictorMessage", "Results");
  export const _0NotSuportedFor1 = new MessageKey("PredictorMessage", "_0NotSuportedFor1");
  export const _0IsRequiredFor1 = new MessageKey("PredictorMessage", "_0IsRequiredFor1");
  export const _0ShouldBeDivisibleBy12 = new MessageKey("PredictorMessage", "_0ShouldBeDivisibleBy12");
  export const TheTypeOf01DoesNotMatch23 = new MessageKey("PredictorMessage", "TheTypeOf01DoesNotMatch23");
  export const Predict = new MessageKey("PredictorMessage", "Predict");
  export const ThereShouldBe0ColumnsWith12Currently3 = new MessageKey("PredictorMessage", "ThereShouldBe0ColumnsWith12Currently3");
  export const ShouldBeOfType0 = new MessageKey("PredictorMessage", "ShouldBeOfType0");
  export const TooManyParentKeys = new MessageKey("PredictorMessage", "TooManyParentKeys");
  export const _0CanNotBe1Because2Use3 = new MessageKey("PredictorMessage", "_0CanNotBe1Because2Use3");
  export const _0IsNotCompatibleWith12 = new MessageKey("PredictorMessage", "_0IsNotCompatibleWith12");
  export const NoPublicationsForQuery0Registered = new MessageKey("PredictorMessage", "NoPublicationsForQuery0Registered");
  export const NoPublicationsProcessRegisteredFor0 = new MessageKey("PredictorMessage", "NoPublicationsProcessRegisteredFor0");
  export const PredictorIsPublishedUntrainAnyway = new MessageKey("PredictorMessage", "PredictorIsPublishedUntrainAnyway");
}

export const PredictorMetricsEmbedded = new Type<PredictorMetricsEmbedded>("PredictorMetricsEmbedded");
export interface PredictorMetricsEmbedded extends Entities.EmbeddedEntity {
  Type: "PredictorMetricsEmbedded";
  loss: number | null;
  evaluation: number | null;
}

export module PredictorOperation {
  export const Save : Entities.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Save");
  export const Train : Entities.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Train");
  export const CancelTraining : Entities.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.CancelTraining");
  export const StopTraining : Entities.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.StopTraining");
  export const Untrain : Entities.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Untrain");
  export const Publish : Entities.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Publish");
  export const AfterPublishProcess : Entities.ConstructSymbol_From<Entities.Entity, PredictorEntity> = registerSymbol("Operation", "PredictorOperation.AfterPublishProcess");
  export const Delete : Entities.DeleteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Delete");
  export const Clone : Entities.ConstructSymbol_From<PredictorEntity, PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Clone");
  export const AutoconfigureNetwork : Entities.ConstructSymbol_From<Processes.ProcessEntity, PredictorEntity> = registerSymbol("Operation", "PredictorOperation.AutoconfigureNetwork");
}

export module PredictorProcessAlgorithm {
  export const AutoconfigureNeuralNetwork : Processes.ProcessAlgorithmSymbol = registerSymbol("ProcessAlgorithm", "PredictorProcessAlgorithm.AutoconfigureNeuralNetwork");
}

export const PredictorPublicationSymbol = new Type<PredictorPublicationSymbol>("PredictorPublication");
export interface PredictorPublicationSymbol extends Entities.Symbol {
  Type: "PredictorPublication";
}

export const PredictorRegressionMetricsEmbedded = new Type<PredictorRegressionMetricsEmbedded>("PredictorRegressionMetricsEmbedded");
export interface PredictorRegressionMetricsEmbedded extends Entities.EmbeddedEntity {
  Type: "PredictorRegressionMetricsEmbedded";
  meanError: number | null;
  meanSquaredError: number | null;
  meanAbsoluteError: number | null;
  rootMeanSquareError: number | null;
  meanPercentageError: number | null;
  meanAbsolutePercentageError: number | null;
}

export const PredictorResultSaverSymbol = new Type<PredictorResultSaverSymbol>("PredictorResultSaver");
export interface PredictorResultSaverSymbol extends Entities.Symbol {
  Type: "PredictorResultSaver";
}

export const PredictorSettingsEmbedded = new Type<PredictorSettingsEmbedded>("PredictorSettingsEmbedded");
export interface PredictorSettingsEmbedded extends Entities.EmbeddedEntity {
  Type: "PredictorSettingsEmbedded";
  testPercentage: number;
  seed: number | null;
}

export module PredictorSimpleResultSaver {
  export const StatisticsOnly : PredictorResultSaverSymbol = registerSymbol("PredictorResultSaver", "PredictorSimpleResultSaver.StatisticsOnly");
  export const Full : PredictorResultSaverSymbol = registerSymbol("PredictorResultSaver", "PredictorSimpleResultSaver.Full");
}

export const PredictorState = new EnumType<PredictorState>("PredictorState");
export type PredictorState =
  "Draft" |
  "Training" |
  "Trained" |
  "Error";

export const PredictorSubQueryColumnEmbedded = new Type<PredictorSubQueryColumnEmbedded>("PredictorSubQueryColumnEmbedded");
export interface PredictorSubQueryColumnEmbedded extends Entities.EmbeddedEntity {
  Type: "PredictorSubQueryColumnEmbedded";
  usage: PredictorSubQueryColumnUsage;
  token: UserAssets.QueryTokenEmbedded;
  encoding: PredictorColumnEncodingSymbol;
  nullHandling: PredictorColumnNullHandling | null;
}

export const PredictorSubQueryColumnUsage = new EnumType<PredictorSubQueryColumnUsage>("PredictorSubQueryColumnUsage");
export type PredictorSubQueryColumnUsage =
  "ParentKey" |
  "SplitBy" |
  "Input" |
  "Output";

export const PredictorSubQueryEntity = new Type<PredictorSubQueryEntity>("PredictorSubQuery");
export interface PredictorSubQueryEntity extends Entities.Entity {
  Type: "PredictorSubQuery";
  predictor: Entities.Lite<PredictorEntity>;
  name: string;
  query: Basics.QueryEntity;
  filters: Entities.MList<UserQueries.QueryFilterEmbedded>;
  columns: Entities.MList<PredictorSubQueryColumnEmbedded>;
  order: number;
}

export const PredictSimpleResultEntity = new Type<PredictSimpleResultEntity>("PredictSimpleResult");
export interface PredictSimpleResultEntity extends Entities.Entity {
  Type: "PredictSimpleResult";
  predictor: Entities.Lite<PredictorEntity>;
  target: Entities.Lite<Entities.Entity> | null;
  key0: string | null;
  key1: string | null;
  key2: string | null;
  type: PredictionSet;
  originalCategory: string | null;
  originalValue: number | null;
  predictedCategory: string | null;
  predictedValue: number | null;
}


