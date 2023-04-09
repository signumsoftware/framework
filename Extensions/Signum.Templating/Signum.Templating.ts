//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Eval from '../Signum.Eval/Signum.Eval'


export interface IContainsQuery extends Entities.Entity {
  query: Basics.QueryEntity;
}

export const ModelConverterSymbol = new Type<ModelConverterSymbol>("ModelConverter");
export interface ModelConverterSymbol extends Basics.Symbol {
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
export interface TemplateApplicableEval extends Eval.EvalEmbedded<ITemplateApplicable> {
  Type: "TemplateApplicableEval";
}

export module TemplateTokenMessage {
  export const Insert = new MessageKey("TemplateTokenMessage", "Insert");
  export const NoColumnSelected = new MessageKey("TemplateTokenMessage", "NoColumnSelected");
  export const YouCannotAddIfBlocksOnCollectionFields = new MessageKey("TemplateTokenMessage", "YouCannotAddIfBlocksOnCollectionFields");
  export const YouHaveToAddTheElementTokenToUseForeachOnCollectionFields = new MessageKey("TemplateTokenMessage", "YouHaveToAddTheElementTokenToUseForeachOnCollectionFields");
  export const YouCanOnlyAddForeachBlocksWithCollectionFields = new MessageKey("TemplateTokenMessage", "YouCanOnlyAddForeachBlocksWithCollectionFields");
  export const YouCannotAddBlocksWithAllOrAny = new MessageKey("TemplateTokenMessage", "YouCannotAddBlocksWithAllOrAny");
  export const ImpossibleToAccess0BecauseTheTemplateHAsNo1 = new MessageKey("TemplateTokenMessage", "ImpossibleToAccess0BecauseTheTemplateHAsNo1");
}

