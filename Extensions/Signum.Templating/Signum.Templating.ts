//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Eval from '../Signum.Eval/Signum.Eval'

import { FilterOptionParsed, OrderOptionParsed, FilterRequest, OrderRequest, Pagination } from '@framework/FindOptions'

//Partial
export interface QueryModel {
    queryKey: string;

    filters: FilterRequest[];
    orders: OrderRequest[];
    pagination: Pagination;
}

export interface ITemplateApplicable {}

export interface IContainsQuery extends Entities.Entity {
  query: Basics.QueryEntity | null;
}

export const ModelConverterSymbol: Type<ModelConverterSymbol> = new Type<ModelConverterSymbol>("ModelConverter");
export interface ModelConverterSymbol extends Basics.Symbol {
  Type: "ModelConverter";
}

export const MultiEntityModel: Type<MultiEntityModel> = new Type<MultiEntityModel>("MultiEntityModel");
export interface MultiEntityModel extends Entities.ModelEntity {
  Type: "MultiEntityModel";
  entities: Entities.MList<Entities.Lite<Entities.Entity>>;
}

export const QueryModel: Type<QueryModel> = new Type<QueryModel>("QueryModel");
export interface QueryModel extends Entities.ModelEntity {
  Type: "QueryModel";
}

export namespace QueryModelMessage {
  export const ConfigureYourQueryAndPressSearchBeforeOk: MessageKey = new MessageKey("QueryModelMessage", "ConfigureYourQueryAndPressSearchBeforeOk");
}

export const TemplateApplicableEval: Type<TemplateApplicableEval> = new Type<TemplateApplicableEval>("TemplateApplicableEval");
export interface TemplateApplicableEval extends Eval.EvalEmbedded<ITemplateApplicable> {
  Type: "TemplateApplicableEval";
}

export namespace TemplateMessage {
  export const Template: MessageKey = new MessageKey("TemplateMessage", "Template");
  export const CopyToClipboard: MessageKey = new MessageKey("TemplateMessage", "CopyToClipboard");
}

export namespace TemplateTokenMessage {
  export const Insert: MessageKey = new MessageKey("TemplateTokenMessage", "Insert");
  export const NoColumnSelected: MessageKey = new MessageKey("TemplateTokenMessage", "NoColumnSelected");
  export const YouCannotAddIfBlocksOnCollectionFields: MessageKey = new MessageKey("TemplateTokenMessage", "YouCannotAddIfBlocksOnCollectionFields");
  export const YouHaveToAddTheElementTokenToUseForeachOnCollectionFields: MessageKey = new MessageKey("TemplateTokenMessage", "YouHaveToAddTheElementTokenToUseForeachOnCollectionFields");
  export const YouCanOnlyAddForeachBlocksWithCollectionFields: MessageKey = new MessageKey("TemplateTokenMessage", "YouCanOnlyAddForeachBlocksWithCollectionFields");
  export const YouCannotAddBlocksWithAllOrAny: MessageKey = new MessageKey("TemplateTokenMessage", "YouCannotAddBlocksWithAllOrAny");
  export const ImpossibleToAccess0BecauseTheTemplateHAsNo1: MessageKey = new MessageKey("TemplateTokenMessage", "ImpossibleToAccess0BecauseTheTemplateHAsNo1");
}

