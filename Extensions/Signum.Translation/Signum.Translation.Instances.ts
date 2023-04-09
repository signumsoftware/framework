//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'


export const TranslatedInstanceEntity = new Type<TranslatedInstanceEntity>("TranslatedInstance");
export interface TranslatedInstanceEntity extends Entities.Entity {
  Type: "TranslatedInstance";
  culture: Basics.CultureInfoEntity;
  instance: Entities.Lite<Entities.Entity>;
  propertyRoute: Basics.PropertyRouteEntity;
  rowId: string | null;
  translatedText: string;
  originalText: string;
}

export const TranslatedSummaryState = new EnumType<TranslatedSummaryState>("TranslatedSummaryState");
export type TranslatedSummaryState =
  "Completed" |
  "Pending" |
  "None";

