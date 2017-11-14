//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as UserQueries from '../UserQueries/Signum.Entities.UserQueries'
import * as Files from '../Files/Signum.Entities.Files'
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

export const NeuralNetworkSettingsEntity = new Type<NeuralNetworkSettingsEntity>("NeuralNetworkSettings");
export interface NeuralNetworkSettingsEntity extends Entities.Entity, IPredictorAlgorithmSettings {
    Type: "NeuralNetworkSettings";
    minibatchSize?: number;
}

export const PredictorAlgorithmSymbol = new Type<PredictorAlgorithmSymbol>("PredictorAlgorithm");
export interface PredictorAlgorithmSymbol extends Entities.Symbol {
    Type: "PredictorAlgorithm";
}

export const PredictorCodificationEntity = new Type<PredictorCodificationEntity>("PredictorCodification");
export interface PredictorCodificationEntity extends Entities.Entity {
    Type: "PredictorCodification";
    predictor?: Entities.Lite<PredictorEntity> | null;
    columnIndex?: number;
    originalMultiColumnIndex?: number | null;
    originalColumnIndex?: number;
    groupKey0?: string | null;
    groupKey1?: string | null;
    groupKey2?: string | null;
    isValue?: string | null;
    codedValues: Entities.MList<string>;
}

export const PredictorColumnEmbedded = new Type<PredictorColumnEmbedded>("PredictorColumnEmbedded");
export interface PredictorColumnEmbedded extends Entities.EmbeddedEntity {
    Type: "PredictorColumnEmbedded";
    usage?: PredictorColumnUsage;
    token?: UserAssets.QueryTokenEmbedded | null;
    encoding?: PredictorColumnEncoding;
}

export const PredictorColumnEncoding = new EnumType<PredictorColumnEncoding>("PredictorColumnEncoding");
export type PredictorColumnEncoding =
    "None" |
    "OneHot" |
    "Codified";

export const PredictorColumnUsage = new EnumType<PredictorColumnUsage>("PredictorColumnUsage");
export type PredictorColumnUsage =
    "Input" |
    "Output";

export const PredictorEntity = new Type<PredictorEntity>("Predictor");
export interface PredictorEntity extends Entities.Entity {
    Type: "Predictor";
    query?: Basics.QueryEntity | null;
    name?: string | null;
    settings?: PredictorSettingsEmbedded | null;
    algorithm?: PredictorAlgorithmSymbol | null;
    trainingException?: Entities.Lite<Basics.ExceptionEntity> | null;
    user?: Entities.Lite<Basics.IUserEntity> | null;
    algorithmSettings?: IPredictorAlgorithmSettings | null;
    state?: PredictorState;
    filters: Entities.MList<UserQueries.QueryFilterEmbedded>;
    simpleColumns: Entities.MList<PredictorColumnEmbedded>;
    multiColumns: Entities.MList<PredictorMultiColumnEntity>;
    trainingStats?: PredictorStatsEmbedded | null;
    testStats?: PredictorStatsEmbedded | null;
    files: Entities.MList<Files.FilePathEmbedded>;
}

export module PredictorFileType {
    export const PredictorFile : Files.FileTypeSymbol = registerSymbol("FileType", "PredictorFileType.PredictorFile");
}

export const PredictorGroupKeyEmbedded = new Type<PredictorGroupKeyEmbedded>("PredictorGroupKeyEmbedded");
export interface PredictorGroupKeyEmbedded extends Entities.EmbeddedEntity {
    Type: "PredictorGroupKeyEmbedded";
    token?: UserAssets.QueryTokenEmbedded | null;
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
}

export const PredictorMultiColumnEntity = new Type<PredictorMultiColumnEntity>("PredictorMultiColumn");
export interface PredictorMultiColumnEntity extends Entities.Entity {
    Type: "PredictorMultiColumn";
    predictor?: Entities.Lite<PredictorEntity> | null;
    name?: string | null;
    query?: Basics.QueryEntity | null;
    additionalFilters: Entities.MList<UserQueries.QueryFilterEmbedded>;
    groupKeys: Entities.MList<PredictorGroupKeyEmbedded>;
    aggregates: Entities.MList<PredictorColumnEmbedded>;
}

export module PredictorOperation {
    export const Save : Entities.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Save");
    export const Train : Entities.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Train");
    export const CancelTraining : Entities.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.CancelTraining");
    export const Untrain : Entities.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Untrain");
    export const Delete : Entities.DeleteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Delete");
    export const Clone : Entities.ConstructSymbol_From<PredictorEntity, PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Clone");
}

export const PredictorProgressEntity = new Type<PredictorProgressEntity>("PredictorProgress");
export interface PredictorProgressEntity extends Entities.Entity {
    Type: "PredictorProgress";
    predictor?: Entities.Lite<PredictorEntity> | null;
    creationDate?: string;
    miniBatchIndex?: number;
    trainingSet?: number;
    trainingMisses?: number | null;
    trainingError?: number;
    testSet?: number;
    testMisses?: number | null;
    testError?: number;
}

export const PredictorSettingsEmbedded = new Type<PredictorSettingsEmbedded>("PredictorSettingsEmbedded");
export interface PredictorSettingsEmbedded extends Entities.EmbeddedEntity {
    Type: "PredictorSettingsEmbedded";
    testPercentage?: number;
}

export const PredictorState = new EnumType<PredictorState>("PredictorState");
export type PredictorState =
    "Draft" |
    "Training" |
    "Trained" |
    "Error";

export const PredictorStatsEmbedded = new Type<PredictorStatsEmbedded>("PredictorStatsEmbedded");
export interface PredictorStatsEmbedded extends Entities.EmbeddedEntity {
    Type: "PredictorStatsEmbedded";
    totalCount?: number;
    errorCount?: number | null;
    mean?: number | null;
    variance?: number | null;
    standartDeviation?: number | null;
}


