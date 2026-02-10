//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as UserAssets from '../Signum.UserAssets/Signum.UserAssets'


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
  name: string;
  steps: Entities.MList<TourStepEmbedded>;
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
}

export const TourStepEmbedded: Type<TourStepEmbedded> = new Type<TourStepEmbedded>("TourStepEmbedded");
export interface TourStepEmbedded extends Entities.EmbeddedEntity {
  Type: "TourStepEmbedded";
  element: string | null;
  title: string | null;
  description: string | null;
  side: PopoverSide | null;
  align: PopoverAlign | null;
}

