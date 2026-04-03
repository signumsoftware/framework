//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Security from '../../Signum/React/Signum.Security'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Processes from '../Signum.Processes/Signum.Processes'
import * as Files from '../Signum.Files/Signum.Files'
import * as Queries from '../Signum.UserAssets/Signum.UserAssets.Queries'


export const AutoconfigureNeuralNetworkEntity: Type<AutoconfigureNeuralNetworkEntity> = new Type<AutoconfigureNeuralNetworkEntity>("AutoconfigureNeuralNetwork");
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

export namespace DefaultColumnEncodings {
  export const None : PredictorColumnEncodingSymbol = registerSymbol("PredictorColumnEncoding", "DefaultColumnEncodings.None");
  export const OneHot : PredictorColumnEncodingSymbol = registerSymbol("PredictorColumnEncoding", "DefaultColumnEncodings.OneHot");
  export const NormalizeZScore : PredictorColumnEncodingSymbol = registerSymbol("PredictorColumnEncoding", "DefaultColumnEncodings.NormalizeZScore");
  export const NormalizeMinMax : PredictorColumnEncodingSymbol = registerSymbol("PredictorColumnEncoding", "DefaultColumnEncodings.NormalizeMinMax");
  export const NormalizeLog : PredictorColumnEncodingSymbol = registerSymbol("PredictorColumnEncoding", "DefaultColumnEncodings.NormalizeLog");
  export const SplitWords : PredictorColumnEncodingSymbol = registerSymbol("PredictorColumnEncoding", "DefaultColumnEncodings.SplitWords");
}

export interface IPredictorAlgorithmSettings extends Entities.Entity {
}

export const NeuralNetworkActivation: EnumType<NeuralNetworkActivation> = new EnumType<NeuralNetworkActivation>("NeuralNetworkActivation");
export type NeuralNetworkActivation =
  "None" |
  "ReLU" |
  "Sigmoid" |
  "Tanh";

export const NeuralNetworkEvalFunction: EnumType<NeuralNetworkEvalFunction> = new EnumType<NeuralNetworkEvalFunction>("NeuralNetworkEvalFunction");
export type NeuralNetworkEvalFunction =
  "softmax_cross_entropy_with_logits_v2" |
  "softmax_cross_entropy_with_logits" |
  "sigmoid_cross_entropy_with_logits" |
  "ClassificationError" |
  "MeanSquaredError" |
  "MeanAbsoluteError" |
  "MeanAbsolutePercentageError";

export const NeuralNetworkHidenLayerEmbedded: Type<NeuralNetworkHidenLayerEmbedded> = new Type<NeuralNetworkHidenLayerEmbedded>("NeuralNetworkHidenLayerEmbedded");
export interface NeuralNetworkHidenLayerEmbedded extends Entities.EmbeddedEntity {
  Type: "NeuralNetworkHidenLayerEmbedded";
  size: number;
  activation: NeuralNetworkActivation;
  initializer: NeuralNetworkInitializer;
}

export const NeuralNetworkInitializer: EnumType<NeuralNetworkInitializer> = new EnumType<NeuralNetworkInitializer>("NeuralNetworkInitializer");
export type NeuralNetworkInitializer =
  "glorot_uniform_initializer" |
  "ones_initializer" |
  "zeros_initializer" |
  "random_uniform_initializer" |
  "orthogonal_initializer" |
  "random_normal_initializer" |
  "truncated_normal_initializer" |
  "variance_scaling_initializer";

export const NeuralNetworkSettingsEntity: Type<NeuralNetworkSettingsEntity> = new Type<NeuralNetworkSettingsEntity>("NeuralNetworkSettings");
export interface NeuralNetworkSettingsEntity extends Entities.Entity, IPredictorAlgorithmSettings {
  Type: "NeuralNetworkSettings";
  device: string | null;
  predictionType: PredictionType;
  hiddenLayers: Entities.MList<NeuralNetworkHidenLayerEmbedded>;
  outputActivation: NeuralNetworkActivation;
  outputInitializer: NeuralNetworkInitializer;
  optimizer: TensorFlowOptimizer;
  lossFunction: NeuralNetworkEvalFunction;
  evalErrorFunction: NeuralNetworkEvalFunction;
  learningRate: number;
  learningEpsilon: number;
  minibatchSize: number;
  numMinibatches: number;
  bestResultFromLast: number;
  saveProgressEvery: number;
  saveValidationProgressEvery: number;
}

export const PredictionSet: EnumType<PredictionSet> = new EnumType<PredictionSet>("PredictionSet");
export type PredictionSet =
  "Validation" |
  "Training";

export const PredictionType: EnumType<PredictionType> = new EnumType<PredictionType>("PredictionType");
export type PredictionType =
  "Regression" |
  "MultiRegression" |
  "Classification" |
  "MultiClassification";

export const PredictorAlgorithmSymbol: Type<PredictorAlgorithmSymbol> = new Type<PredictorAlgorithmSymbol>("PredictorAlgorithm");
export interface PredictorAlgorithmSymbol extends Basics.Symbol {
  Type: "PredictorAlgorithm";
}

export const PredictorClassificationMetricsEmbedded: Type<PredictorClassificationMetricsEmbedded> = new Type<PredictorClassificationMetricsEmbedded>("PredictorClassificationMetricsEmbedded");
export interface PredictorClassificationMetricsEmbedded extends Entities.EmbeddedEntity {
  Type: "PredictorClassificationMetricsEmbedded";
  totalCount: number;
  missCount: number;
  missRate: number | null;
}

export const PredictorCodificationEntity: Type<PredictorCodificationEntity> = new Type<PredictorCodificationEntity>("PredictorCodification");
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

export const PredictorColumnEmbedded: Type<PredictorColumnEmbedded> = new Type<PredictorColumnEmbedded>("PredictorColumnEmbedded");
export interface PredictorColumnEmbedded extends Entities.EmbeddedEntity {
  Type: "PredictorColumnEmbedded";
  usage: PredictorColumnUsage;
  token: Queries.QueryTokenEmbedded;
  encoding: PredictorColumnEncodingSymbol;
  nullHandling: PredictorColumnNullHandling;
}

export const PredictorColumnEncodingSymbol: Type<PredictorColumnEncodingSymbol> = new Type<PredictorColumnEncodingSymbol>("PredictorColumnEncoding");
export interface PredictorColumnEncodingSymbol extends Basics.Symbol {
  Type: "PredictorColumnEncoding";
}

export const PredictorColumnNullHandling: EnumType<PredictorColumnNullHandling> = new EnumType<PredictorColumnNullHandling>("PredictorColumnNullHandling");
export type PredictorColumnNullHandling =
  "Zero" |
  "Error" |
  "Average" |
  "Min" |
  "Max";

export const PredictorColumnUsage: EnumType<PredictorColumnUsage> = new EnumType<PredictorColumnUsage>("PredictorColumnUsage");
export type PredictorColumnUsage =
  "Input" |
  "Output";

export const PredictorEntity: Type<PredictorEntity> = new Type<PredictorEntity>("Predictor");
export interface PredictorEntity extends Entities.Entity, Processes.IProcessDataEntity {
  Type: "Predictor";
  name: string | null;
  settings: PredictorSettingsEmbedded;
  algorithm: PredictorAlgorithmSymbol;
  resultSaver: PredictorResultSaverSymbol | null;
  publication: PredictorPublicationSymbol | null;
  trainingException: Entities.Lite<Basics.ExceptionEntity> | null;
  user: Entities.Lite<Security.IUserEntity> | null;
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

export const PredictorEpochProgressEntity: Type<PredictorEpochProgressEntity> = new Type<PredictorEpochProgressEntity>("PredictorEpochProgress");
export interface PredictorEpochProgressEntity extends Entities.Entity {
  Type: "PredictorEpochProgress";
  predictor: Entities.Lite<PredictorEntity>;
  creationDate: string /*DateTime*/;
  ellapsed: number;
  trainingExamples: number;
  epoch: number;
  lossTraining: number | null;
  accuracyTraining: number | null;
  lossValidation: number | null;
  accuracyValidation: number | null;
}

export namespace PredictorFileType {
  export const PredictorFile : Files.FileTypeSymbol = registerSymbol("FileType", "PredictorFileType.PredictorFile");
}

export const PredictorMainQueryEmbedded: Type<PredictorMainQueryEmbedded> = new Type<PredictorMainQueryEmbedded>("PredictorMainQueryEmbedded");
export interface PredictorMainQueryEmbedded extends Entities.EmbeddedEntity {
  Type: "PredictorMainQueryEmbedded";
  query: Basics.QueryEntity;
  groupResults: boolean;
  filters: Entities.MList<Queries.QueryFilterEmbedded>;
  columns: Entities.MList<PredictorColumnEmbedded>;
}

export namespace PredictorMessage {
  export const Csv: MessageKey = new MessageKey("PredictorMessage", "Csv");
  export const Tsv: MessageKey = new MessageKey("PredictorMessage", "Tsv");
  export const TsvMetadata: MessageKey = new MessageKey("PredictorMessage", "TsvMetadata");
  export const TensorflowProjector: MessageKey = new MessageKey("PredictorMessage", "TensorflowProjector");
  export const DownloadCsv: MessageKey = new MessageKey("PredictorMessage", "DownloadCsv");
  export const DownloadTsv: MessageKey = new MessageKey("PredictorMessage", "DownloadTsv");
  export const DownloadTsvMetadata: MessageKey = new MessageKey("PredictorMessage", "DownloadTsvMetadata");
  export const OpenTensorflowProjector: MessageKey = new MessageKey("PredictorMessage", "OpenTensorflowProjector");
  export const _0IsAlreadyBeingTrained: MessageKey = new MessageKey("PredictorMessage", "_0IsAlreadyBeingTrained");
  export const StartingTraining: MessageKey = new MessageKey("PredictorMessage", "StartingTraining");
  export const Preview: MessageKey = new MessageKey("PredictorMessage", "Preview");
  export const Codifications: MessageKey = new MessageKey("PredictorMessage", "Codifications");
  export const Progress: MessageKey = new MessageKey("PredictorMessage", "Progress");
  export const Results: MessageKey = new MessageKey("PredictorMessage", "Results");
  export const _0NotSuportedFor1: MessageKey = new MessageKey("PredictorMessage", "_0NotSuportedFor1");
  export const _0IsRequiredFor1: MessageKey = new MessageKey("PredictorMessage", "_0IsRequiredFor1");
  export const _0ShouldBeDivisibleBy12: MessageKey = new MessageKey("PredictorMessage", "_0ShouldBeDivisibleBy12");
  export const TheTypeOf01DoesNotMatch23: MessageKey = new MessageKey("PredictorMessage", "TheTypeOf01DoesNotMatch23");
  export const Predict: MessageKey = new MessageKey("PredictorMessage", "Predict");
  export const ThereShouldBe0ColumnsWith12Currently3: MessageKey = new MessageKey("PredictorMessage", "ThereShouldBe0ColumnsWith12Currently3");
  export const ShouldBeOfType0: MessageKey = new MessageKey("PredictorMessage", "ShouldBeOfType0");
  export const TooManyParentKeys: MessageKey = new MessageKey("PredictorMessage", "TooManyParentKeys");
  export const _0CanNotBe1Because2Use3: MessageKey = new MessageKey("PredictorMessage", "_0CanNotBe1Because2Use3");
  export const _0IsNotCompatibleWith12: MessageKey = new MessageKey("PredictorMessage", "_0IsNotCompatibleWith12");
  export const NoPublicationsForQuery0Registered: MessageKey = new MessageKey("PredictorMessage", "NoPublicationsForQuery0Registered");
  export const NoPublicationsProcessRegisteredFor0: MessageKey = new MessageKey("PredictorMessage", "NoPublicationsProcessRegisteredFor0");
  export const PredictorIsPublishedUntrainAnyway: MessageKey = new MessageKey("PredictorMessage", "PredictorIsPublishedUntrainAnyway");
}

export const PredictorMetricsEmbedded: Type<PredictorMetricsEmbedded> = new Type<PredictorMetricsEmbedded>("PredictorMetricsEmbedded");
export interface PredictorMetricsEmbedded extends Entities.EmbeddedEntity {
  Type: "PredictorMetricsEmbedded";
  loss: number | null;
  accuracy: number | null;
}

export namespace PredictorOperation {
  export const Save : Operations.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Save");
  export const Train : Operations.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Train");
  export const CancelTraining : Operations.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.CancelTraining");
  export const StopTraining : Operations.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.StopTraining");
  export const Untrain : Operations.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Untrain");
  export const Publish : Operations.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Publish");
  export const AfterPublishProcess : Operations.ConstructSymbol_From<Entities.Entity, PredictorEntity> = registerSymbol("Operation", "PredictorOperation.AfterPublishProcess");
  export const Delete : Operations.DeleteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Delete");
  export const Clone : Operations.ConstructSymbol_From<PredictorEntity, PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Clone");
  export const AutoconfigureNetwork : Operations.ConstructSymbol_From<Processes.ProcessEntity, PredictorEntity> = registerSymbol("Operation", "PredictorOperation.AutoconfigureNetwork");
}

export namespace PredictorProcessAlgorithm {
  export const AutoconfigureNeuralNetwork : Processes.ProcessAlgorithmSymbol = registerSymbol("ProcessAlgorithm", "PredictorProcessAlgorithm.AutoconfigureNeuralNetwork");
}

export const PredictorPublicationSymbol: Type<PredictorPublicationSymbol> = new Type<PredictorPublicationSymbol>("PredictorPublication");
export interface PredictorPublicationSymbol extends Basics.Symbol {
  Type: "PredictorPublication";
}

export const PredictorRegressionMetricsEmbedded: Type<PredictorRegressionMetricsEmbedded> = new Type<PredictorRegressionMetricsEmbedded>("PredictorRegressionMetricsEmbedded");
export interface PredictorRegressionMetricsEmbedded extends Entities.EmbeddedEntity {
  Type: "PredictorRegressionMetricsEmbedded";
  meanError: number | null;
  meanSquaredError: number | null;
  meanAbsoluteError: number | null;
  rootMeanSquareError: number | null;
  meanPercentageError: number | null;
  meanAbsolutePercentageError: number | null;
}

export const PredictorResultSaverSymbol: Type<PredictorResultSaverSymbol> = new Type<PredictorResultSaverSymbol>("PredictorResultSaver");
export interface PredictorResultSaverSymbol extends Basics.Symbol {
  Type: "PredictorResultSaver";
}

export const PredictorSettingsEmbedded: Type<PredictorSettingsEmbedded> = new Type<PredictorSettingsEmbedded>("PredictorSettingsEmbedded");
export interface PredictorSettingsEmbedded extends Entities.EmbeddedEntity {
  Type: "PredictorSettingsEmbedded";
  testPercentage: number;
  seed: number | null;
}

export namespace PredictorSimpleResultSaver {
  export const StatisticsOnly : PredictorResultSaverSymbol = registerSymbol("PredictorResultSaver", "PredictorSimpleResultSaver.StatisticsOnly");
  export const Full : PredictorResultSaverSymbol = registerSymbol("PredictorResultSaver", "PredictorSimpleResultSaver.Full");
}

export const PredictorState: EnumType<PredictorState> = new EnumType<PredictorState>("PredictorState");
export type PredictorState =
  "Draft" |
  "Training" |
  "Trained" |
  "Error";

export const PredictorSubQueryColumnEmbedded: Type<PredictorSubQueryColumnEmbedded> = new Type<PredictorSubQueryColumnEmbedded>("PredictorSubQueryColumnEmbedded");
export interface PredictorSubQueryColumnEmbedded extends Entities.EmbeddedEntity {
  Type: "PredictorSubQueryColumnEmbedded";
  usage: PredictorSubQueryColumnUsage;
  token: Queries.QueryTokenEmbedded;
  encoding: PredictorColumnEncodingSymbol;
  nullHandling: PredictorColumnNullHandling | null;
}

export const PredictorSubQueryColumnUsage: EnumType<PredictorSubQueryColumnUsage> = new EnumType<PredictorSubQueryColumnUsage>("PredictorSubQueryColumnUsage");
export type PredictorSubQueryColumnUsage =
  "ParentKey" |
  "SplitBy" |
  "Input" |
  "Output";

export const PredictorSubQueryEntity: Type<PredictorSubQueryEntity> = new Type<PredictorSubQueryEntity>("PredictorSubQuery");
export interface PredictorSubQueryEntity extends Entities.Entity {
  Type: "PredictorSubQuery";
  predictor: Entities.Lite<PredictorEntity>;
  name: string;
  query: Basics.QueryEntity;
  filters: Entities.MList<Queries.QueryFilterEmbedded>;
  columns: Entities.MList<PredictorSubQueryColumnEmbedded>;
  order: number;
}

export const PredictSimpleResultEntity: Type<PredictSimpleResultEntity> = new Type<PredictSimpleResultEntity>("PredictSimpleResult");
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

export const TensorFlowOptimizer: EnumType<TensorFlowOptimizer> = new EnumType<TensorFlowOptimizer>("TensorFlowOptimizer");
export type TensorFlowOptimizer =
  "Adam" |
  "GradientDescentOptimizer";

export namespace TensorFlowPredictorAlgorithm {
  export const NeuralNetworkGraph : PredictorAlgorithmSymbol = registerSymbol("PredictorAlgorithm", "TensorFlowPredictorAlgorithm.NeuralNetworkGraph");
}

