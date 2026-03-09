//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as UserAssets from '../Signum.UserAssets/Signum.UserAssets'


export const CssStepEmbedded: Type<CssStepEmbedded> = new Type<CssStepEmbedded>("CssStepEmbedded");
export interface CssStepEmbedded extends Entities.EmbeddedEntity {
  Type: "CssStepEmbedded";
  type: CssStepType;
  cssSelector: string | null;
  property: Basics.PropertyRouteEntity | null;
  toolbarContent: Entities.Lite<Entities.Entity> | null;
}

export const CssStepType: EnumType<CssStepType> = new EnumType<CssStepType>("CssStepType");
export type CssStepType =
  "CSSSelector" |
  "Property" |
  "ToolbarContent";

export const PopoverAlign: EnumType<PopoverAlign> = new EnumType<PopoverAlign>("PopoverAlign");
export type PopoverAlign =
  "Start" |
  "Center" |
  "End";

export const PopoverSide: EnumType<PopoverSide> = new EnumType<PopoverSide>("PopoverSide");
export type PopoverSide =
  "Top" |
  "Right" |
  "Bottom" |
  "Left";

export const TourEntity: Type<TourEntity> = new Type<TourEntity>("Tour");
export interface TourEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "Tour";
  trigger: Entities.Lite<Entities.Entity>;
  steps: Entities.MList<TourStepEntity>;
  showProgress: boolean;
  animate: boolean;
  showCloseButton: boolean;
  guid: string /*Guid*/;
}

export namespace TourMessage {
  export const Next: MessageKey = new MessageKey("TourMessage", "Next");
  export const Previous: MessageKey = new MessageKey("TourMessage", "Previous");
  export const Close: MessageKey = new MessageKey("TourMessage", "Close");
  export const Done: MessageKey = new MessageKey("TourMessage", "Done");
}

export namespace TourOperation {
  export const Save : Operations.ExecuteSymbol<TourEntity> = registerSymbol("Operation", "TourOperation.Save");
  export const Delete : Operations.DeleteSymbol<TourEntity> = registerSymbol("Operation", "TourOperation.Delete");
}

export const TourStepEntity: Type<TourStepEntity> = new Type<TourStepEntity>("TourStep");
export interface TourStepEntity extends Entities.Entity {
  Type: "TourStep";
  tour: Entities.Lite<TourEntity>;
  title: string;
  cssSteps: Entities.MList<CssStepEmbedded>;
  description: string;
  side: PopoverSide | null;
  align: PopoverAlign | null;
}

export const TourTriggerSymbol: Type<TourTriggerSymbol> = new Type<TourTriggerSymbol>("TourTrigger");
export interface TourTriggerSymbol extends Basics.Symbol {
  Type: "TourTrigger";
}

