//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as UserAssets from '../Signum.UserAssets/Signum.UserAssets'


export interface IToolbarEntity extends Entities.Entity {
  elements: Entities.MList<ToolbarElementEmbedded>;
}

export const ShowCount: EnumType<ShowCount> = new EnumType<ShowCount>("ShowCount");
export type ShowCount =
  "MoreThan0" |
  "Always";

export const ToolbarElementEmbedded: Type<ToolbarElementEmbedded> = new Type<ToolbarElementEmbedded>("ToolbarElementEmbedded");
export interface ToolbarElementEmbedded extends Entities.EmbeddedEntity {
  Type: "ToolbarElementEmbedded";
  type: ToolbarElementType;
  label: string | null;
  iconName: string | null;
  showCount: ShowCount | null;
  iconColor: string | null;
  content: Entities.Lite<Entities.Entity> | null;
  url: string | null;
  openInPopup: boolean;
  autoRefreshPeriod: number | null;
}

export const ToolbarElementType: EnumType<ToolbarElementType> = new EnumType<ToolbarElementType>("ToolbarElementType");
export type ToolbarElementType =
  "Header" |
  "Divider" |
  "Item" |
  "ExtraIcon";

export const ToolbarEntity: Type<ToolbarEntity> = new Type<ToolbarEntity>("Toolbar");
export interface ToolbarEntity extends Entities.Entity, UserAssets.IUserAssetEntity, IToolbarEntity {
  Type: "Toolbar";
  owner: Entities.Lite<Entities.Entity> | null;
  name: string;
  location: ToolbarLocation;
  priority: number | null;
  elements: Entities.MList<ToolbarElementEmbedded>;
  guid: string /*Guid*/;
}

export const ToolbarLocation: EnumType<ToolbarLocation> = new EnumType<ToolbarLocation>("ToolbarLocation");
export type ToolbarLocation =
  "Side" |
  "Top" |
  "Main";

export const ToolbarMenuEntity: Type<ToolbarMenuEntity> = new Type<ToolbarMenuEntity>("ToolbarMenu");
export interface ToolbarMenuEntity extends Entities.Entity, UserAssets.IUserAssetEntity, IToolbarEntity {
  Type: "ToolbarMenu";
  owner: Entities.Lite<Entities.Entity> | null;
  guid: string /*Guid*/;
  name: string;
  elements: Entities.MList<ToolbarElementEmbedded>;
}

export module ToolbarMenuOperation {
  export const Save : Operations.ExecuteSymbol<ToolbarMenuEntity> = registerSymbol("Operation", "ToolbarMenuOperation.Save");
  export const Delete : Operations.DeleteSymbol<ToolbarMenuEntity> = registerSymbol("Operation", "ToolbarMenuOperation.Delete");
}

export module ToolbarMessage {
  export const RecursionDetected: MessageKey = new MessageKey("ToolbarMessage", "RecursionDetected");
  export const _0CyclesHaveBeenFoundInTheToolbarDueToTheRelationships: MessageKey = new MessageKey("ToolbarMessage", "_0CyclesHaveBeenFoundInTheToolbarDueToTheRelationships");
  export const FirstElementCanNotBeExtraIcon: MessageKey = new MessageKey("ToolbarMessage", "FirstElementCanNotBeExtraIcon");
  export const ExtraIconCanNotComeAfterDivider: MessageKey = new MessageKey("ToolbarMessage", "ExtraIconCanNotComeAfterDivider");
}

export module ToolbarOperation {
  export const Save : Operations.ExecuteSymbol<ToolbarEntity> = registerSymbol("Operation", "ToolbarOperation.Save");
  export const Delete : Operations.DeleteSymbol<ToolbarEntity> = registerSymbol("Operation", "ToolbarOperation.Delete");
}

