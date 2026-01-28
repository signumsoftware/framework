//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'


export const MatchTranslatedInstances: EnumType<MatchTranslatedInstances> = new EnumType<MatchTranslatedInstances>("MatchTranslatedInstances");
export type MatchTranslatedInstances =
  "ByInstanceID" |
  "ByOriginalText";

export const TranslatedInstanceEntity: Type<TranslatedInstanceEntity> = new Type<TranslatedInstanceEntity>("TranslatedInstance");
export interface TranslatedInstanceEntity extends Entities.Entity {
  Type: "TranslatedInstance";
  culture: Basics.CultureInfoEntity;
  instance: Entities.Lite<Entities.Entity>;
  propertyRoute: Basics.PropertyRouteEntity;
  rowId: string | null;
  translatedText: string;
  originalText: string;
}

export namespace TranslatedInstanceOperation {
  export const Delete : Operations.DeleteSymbol<TranslatedInstanceEntity> = registerSymbol("Operation", "TranslatedInstanceOperation.Delete");
}

export const TranslatedSummaryState: EnumType<TranslatedSummaryState> = new EnumType<TranslatedSummaryState>("TranslatedSummaryState");
export type TranslatedSummaryState =
  "Completed" |
  "Pending" |
  "None";

