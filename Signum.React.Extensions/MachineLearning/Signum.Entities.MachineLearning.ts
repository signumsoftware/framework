//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as Files from '../Files/Signum.Entities.Files'
import * as UserQueries from '../UserQueries/Signum.Entities.UserQueries'
import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets'


export module AccordPredictorAlgorithm {
    export const DiscreteNaiveBayes : PredictorAlgorithmSymbol = registerSymbol("PredictorAlgorithm", "AccordPredictorAlgorithm.DiscreteNaiveBayes");
}

export module CNTKPredictorAlgorithm {
    export const NeuralNetwork : PredictorAlgorithmSymbol = registerSymbol("PredictorAlgorithm", "CNTKPredictorAlgorithm.NeuralNetwork");
}

export interface IPredictorAlgorithmSettings extends Entities.Entity {
}

export const NaiveBayesSettingsEntity = new Type<NaiveBayesSettingsEntity>("NaiveBayesSettings");
export interface NaiveBayesSettingsEntity extends Entities.Entity, IPredictorAlgorithmSettings {
    Type: "NaiveBayesSettings";
    empirical?: boolean;
}

export const NeuralNetworkActivation = new EnumType<NeuralNetworkActivation>("NeuralNetworkActivation");
export type NeuralNetworkActivation =
    "None" |
    "ReLU" |
    "Sigmoid" |
    "Tanh";

export const NeuralNetworkHidenLayerEmbedded = new Type<NeuralNetworkHidenLayerEmbedded>("NeuralNetworkHidenLayerEmbedded");
export interface NeuralNetworkHidenLayerEmbedded extends Entities.EmbeddedEntity {
    Type: "NeuralNetworkHidenLayerEmbedded";
    size?: number;
    activation?: NeuralNetworkActivation;
    initializer?: NeuralNetworkInitializer;
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

export const NeuralNetworkSettingsEntity = new Type<NeuralNetworkSettingsEntity>("NeuralNetworkSettings");
export interface NeuralNetworkSettingsEntity extends Entities.Entity, IPredictorAlgorithmSettings {
    Type: "NeuralNetworkSettings";
    predictionType?: PredictionType;
    hiddenLayers: Entities.MList<NeuralNetworkHidenLayerEmbedded>;
    outputActivation?: NeuralNetworkActivation;
    outputInitializer?: NeuralNetworkInitializer;
    learningRate?: number;
    learningMomentum?: number | null;
    minibatchSize?: number;
    numMinibatches?: number;
    saveProgressEvery?: number;
    saveValidationProgressEvery?: number;
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
    totalCount?: number;
    missCount?: number;
    missRate?: number | null;
}

export const PredictorCodificationEntity = new Type<PredictorCodificationEntity>("PredictorCodification");
export interface PredictorCodificationEntity extends Entities.Entity {
    Type: "PredictorCodification";
    predictor?: Entities.Lite<PredictorEntity> | null;
    usage?: PredictorColumnUsage;
    index?: number;
    subQueryIndex?: number | null;
    originalColumnIndex?: number;
    splitKey0?: string | null;
    splitKey1?: string | null;
    splitKey2?: string | null;
    isValue?: string | null;
    codedValues: Entities.MList<string>;
    mean?: number | null;
    stdDev?: number | null;
}

export const PredictorColumnEmbedded = new Type<PredictorColumnEmbedded>("PredictorColumnEmbedded");
export interface PredictorColumnEmbedded extends Entities.EmbeddedEntity {
    Type: "PredictorColumnEmbedded";
    usage?: PredictorColumnUsage;
    token?: UserAssets.QueryTokenEmbedded | null;
    encoding?: PredictorColumnEncoding;
    nullHandling?: PredictorColumnNullHandling;
}

export const PredictorColumnEncoding = new EnumType<PredictorColumnEncoding>("PredictorColumnEncoding");
export type PredictorColumnEncoding =
    "None" |
    "OneHot" |
    "Codified" |
    "NormalizeZScore";

export const PredictorColumnNullHandling = new EnumType<PredictorColumnNullHandling>("PredictorColumnNullHandling");
export type PredictorColumnNullHandling =
    "Zero" |
    "Error" |
    "Mean";

export const PredictorColumnUsage = new EnumType<PredictorColumnUsage>("PredictorColumnUsage");
export type PredictorColumnUsage =
    "Input" |
    "Output";

export const PredictorEntity = new Type<PredictorEntity>("Predictor");
export interface PredictorEntity extends Entities.Entity {
    Type: "Predictor";
    name?: string | null;
    settings?: PredictorSettingsEmbedded | null;
    algorithm?: PredictorAlgorithmSymbol | null;
    resultSaver?: PredictorResultSaverSymbol | null;
    trainingException?: Entities.Lite<Basics.ExceptionEntity> | null;
    user?: Entities.Lite<Basics.IUserEntity> | null;
    algorithmSettings?: IPredictorAlgorithmSettings | null;
    state?: PredictorState;
    mainQuery: PredictorMainQueryEmbedded;
    subQueries: Entities.MList<PredictorSubQueryEntity>;
    files: Entities.MList<Files.FilePathEmbedded>;
    classificationTraining?: PredictorClassificationMetricsEmbedded | null;
    classificationValidation?: PredictorClassificationMetricsEmbedded | null;
    regressionTraining?: PredictorRegressionMetricsEmbedded | null;
    regressionValidation?: PredictorRegressionMetricsEmbedded | null;
}

export const PredictorEpochProgressEntity = new Type<PredictorEpochProgressEntity>("PredictorEpochProgress");
export interface PredictorEpochProgressEntity extends Entities.Entity {
    Type: "PredictorEpochProgress";
    predictor?: Entities.Lite<PredictorEntity> | null;
    creationDate?: string;
    ellapsed?: number;
    trainingExamples?: number;
    epoch?: number;
    lossTraining?: number | null;
    evaluationTraining?: number | null;
    lossValidation?: number | null;
    evaluationValidation?: number | null;
}

export module PredictorFileType {
    export const PredictorFile : Files.FileTypeSymbol = registerSymbol("FileType", "PredictorFileType.PredictorFile");
}

export const PredictorMainQueryEmbedded = new Type<PredictorMainQueryEmbedded>("PredictorMainQueryEmbedded");
export interface PredictorMainQueryEmbedded extends Entities.EmbeddedEntity {
    Type: "PredictorMainQueryEmbedded";
    query?: Basics.QueryEntity | null;
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
    export const ParentKeyOf0ShouldBeOfType1 = new MessageKey("PredictorMessage", "ParentKeyOf0ShouldBeOfType1");
    export const Predict = new MessageKey("PredictorMessage", "Predict");
}

export module PredictorOperation {
    export const Save : Entities.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Save");
    export const Train : Entities.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Train");
    export const CancelTraining : Entities.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.CancelTraining");
    export const StopTraining : Entities.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.StopTraining");
    export const Untrain : Entities.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Untrain");
    export const Delete : Entities.DeleteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Delete");
    export const Clone : Entities.ConstructSymbol_From<PredictorEntity, PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Clone");
}

export const PredictorRegressionMetricsEmbedded = new Type<PredictorRegressionMetricsEmbedded>("PredictorRegressionMetricsEmbedded");
export interface PredictorRegressionMetricsEmbedded extends Entities.EmbeddedEntity {
    Type: "PredictorRegressionMetricsEmbedded";
    signed?: number | null;
    absolute?: number | null;
    deviation?: number | null;
    percentageSigned?: number | null;
    percentageAbsolute?: number | null;
    percentageDeviation?: number | null;
}

export module PredictorResultSaver {
    export const SimpleRegression : PredictorResultSaverSymbol = registerSymbol("PredictorResultSaver", "PredictorResultSaver.SimpleRegression");
    export const SimpleClassification : PredictorResultSaverSymbol = registerSymbol("PredictorResultSaver", "PredictorResultSaver.SimpleClassification");
}

export const PredictorResultSaverSymbol = new Type<PredictorResultSaverSymbol>("PredictorResultSaver");
export interface PredictorResultSaverSymbol extends Entities.Symbol {
    Type: "PredictorResultSaver";
}

export const PredictorSettingsEmbedded = new Type<PredictorSettingsEmbedded>("PredictorSettingsEmbedded");
export interface PredictorSettingsEmbedded extends Entities.EmbeddedEntity {
    Type: "PredictorSettingsEmbedded";
    testPercentage?: number;
    seed?: number | null;
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
    usage?: PredictorSubQueryColumnUsage;
    token?: UserAssets.QueryTokenEmbedded | null;
    encoding?: PredictorColumnEncoding | null;
    nullHandling?: PredictorColumnNullHandling | null;
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
    predictor?: Entities.Lite<PredictorEntity> | null;
    name?: string | null;
    query?: Basics.QueryEntity | null;
    filters: Entities.MList<UserQueries.QueryFilterEmbedded>;
    columns: Entities.MList<PredictorSubQueryColumnEmbedded>;
}

export const PredictSimpleClassificationEntity = new Type<PredictSimpleClassificationEntity>("PredictSimpleClassification");
export interface PredictSimpleClassificationEntity extends Entities.Entity {
    Type: "PredictSimpleClassification";
    predictor?: Entities.Lite<PredictorEntity> | null;
    target?: Entities.Lite<Entities.Entity> | null;
    type?: PredictionSet;
    predictedValue?: string | null;
}

export const PredictSimpleRegressionEntity = new Type<PredictSimpleRegressionEntity>("PredictSimpleRegression");
export interface PredictSimpleRegressionEntity extends Entities.Entity {
    Type: "PredictSimpleRegression";
    predictor?: Entities.Lite<PredictorEntity> | null;
    target?: Entities.Lite<Entities.Entity> | null;
    type?: PredictionSet;
    predictedValue?: number | null;
}


