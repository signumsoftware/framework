//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Dynamic from '../Dynamic/Signum.Entities.Dynamic'

import { FilterOptionParsed, OrderOptionParsed, FilterRequest, OrderRequest, Pagination } from '@framework/FindOptions' 

//Partial
export interface QueryModel {
    queryKey: string;

    filters: FilterRequest[];
    orders: OrderRequest[];
    pagination: Pagination;
}

export interface ITemplateApplicable {}
export const ModelConverterSymbol = new Type<ModelConverterSymbol>("ModelConverter");
export interface ModelConverterSymbol extends Entities.Symbol {
    Type: "ModelConverter";
}

export const MultiEntityModel = new Type<MultiEntityModel>("MultiEntityModel");
export interface MultiEntityModel extends Entities.ModelEntity {
    Type: "MultiEntityModel";
    entities: Entities.MList<Entities.Lite<Entities.Entity>>;
}

export const QueryModel = new Type<QueryModel>("QueryModel");
export interface QueryModel extends Entities.ModelEntity {
    Type: "QueryModel";
}

export module QueryModelMessage {
    export const ConfigureYourQueryAndPressSearchBeforeOk = new MessageKey("QueryModelMessage", "ConfigureYourQueryAndPressSearchBeforeOk");
}

export const TemplateApplicableEval = new Type<TemplateApplicableEval>("TemplateApplicableEval");
export interface TemplateApplicableEval extends Dynamic.EvalEmbedded<ITemplateApplicable> {
    Type: "TemplateApplicableEval";
}

export module TemplateTokenMessage {
    export const Insert = new MessageKey("TemplateTokenMessage", "Insert");
    export const NoColumnSelected = new MessageKey("TemplateTokenMessage", "NoColumnSelected");
    export const YouCannotAddIfBlocksOnCollectionFields = new MessageKey("TemplateTokenMessage", "YouCannotAddIfBlocksOnCollectionFields");
    export const YouHaveToAddTheElementTokenToUseForeachOnCollectionFields = new MessageKey("TemplateTokenMessage", "YouHaveToAddTheElementTokenToUseForeachOnCollectionFields");
    export const YouCanOnlyAddForeachBlocksWithCollectionFields = new MessageKey("TemplateTokenMessage", "YouCanOnlyAddForeachBlocksWithCollectionFields");
    export const YouCannotAddBlocksWithAllOrAny = new MessageKey("TemplateTokenMessage", "YouCannotAddBlocksWithAllOrAny");
}


