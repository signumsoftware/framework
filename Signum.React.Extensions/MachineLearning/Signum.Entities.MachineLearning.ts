//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as UserQueries from '../UserQueries/Signum.Entities.UserQueries'
import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets'


export const ActivationFunction = new EnumType<ActivationFunction>("ActivationFunction");
export type ActivationFunction =
    "ReLU" |
    "Tanh" |
    "Sigmoid" |
    "Linear";

export const NeuronalNetworkSettingsEntity = new Type<NeuronalNetworkSettingsEntity>("NeuronalNetworkSettingsEntity");
export interface NeuronalNetworkSettingsEntity extends Entities.EmbeddedEntity {
    Type: "NeuronalNetworkSettingsEntity";
    learningRate?: number;
    activationFunction?: ActivationFunction;
    regularization?: Regularization;
    regularizationRate?: number;
    trainingRatio?: number;
    backSize?: number;
    neuronalNetworkDescription?: string | null;
}

export const PredictorColumnEmbedded = new Type<PredictorColumnEmbedded>("PredictorColumnEmbedded");
export interface PredictorColumnEmbedded extends Entities.EmbeddedEntity {
    Type: "PredictorColumnEmbedded";
    type?: PredictorColumnType;
    token?: UserAssets.QueryTokenEmbedded | null;
}

export const PredictorColumnType = new EnumType<PredictorColumnType>("PredictorColumnType");
export type PredictorColumnType =
    "Input" |
    "Output";

export const PredictorEntity = new Type<PredictorEntity>("Predictor");
export interface PredictorEntity extends Entities.Entity {
    Type: "Predictor";
    query?: Basics.QueryEntity | null;
    name?: string | null;
    filters: Entities.MList<UserQueries.QueryFilterEmbedded>;
    columns: Entities.MList<PredictorColumnEmbedded>;
}

export module PredictorOperation {
    export const Save : Entities.ExecuteSymbol<PredictorEntity> = registerSymbol("Operation", "PredictorOperation.Save");
}

export const Regularization = new EnumType<Regularization>("Regularization");
export type Regularization =
    "None" |
    "L1" |
    "L2";


