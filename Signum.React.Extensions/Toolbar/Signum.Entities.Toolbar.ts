//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Scripts/Reflection'
import * as Entities from '../../Signum.React/Scripts/Signum.Entities'
import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets'


export interface IToolbarEntity extends Entities.Entity {
  elements: Entities.MList<ToolbarElementEmbedded>;
}

export const ShowCount = new EnumType<ShowCount>("ShowCount");
export type ShowCount =
  "MoreThan0" |
  "Always";

export const ToolbarElementEmbedded = new Type<ToolbarElementEmbedded>("ToolbarElementEmbedded");
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

export const ToolbarElementType = new EnumType<ToolbarElementType>("ToolbarElementType");
export type ToolbarElementType =
  "Header" |
  "Divider" |
  "Item";

export const ToolbarEntity = new Type<ToolbarEntity>("Toolbar");
export interface ToolbarEntity extends Entities.Entity, UserAssets.IUserAssetEntity, IToolbarEntity {
  Type: "Toolbar";
  owner: Entities.Lite<Entities.Entity> | null;
  name: string;
  location: ToolbarLocation;
  priority: number | null;
  elements: Entities.MList<ToolbarElementEmbedded>;
  guid: string /*Guid*/;
}

export const ToolbarLocation = new EnumType<ToolbarLocation>("ToolbarLocation");
export type ToolbarLocation =
  "Side" |
  "Top" |
  "Main";

export const ToolbarMenuEntity = new Type<ToolbarMenuEntity>("ToolbarMenu");
export interface ToolbarMenuEntity extends Entities.Entity, UserAssets.IUserAssetEntity, IToolbarEntity {
  Type: "ToolbarMenu";
  owner: Entities.Lite<Entities.Entity> | null;
  guid: string /*Guid*/;
  name: string;
  elements: Entities.MList<ToolbarElementEmbedded>;
}

export module ToolbarMenuOperation {
  export const Save : Entities.ExecuteSymbol<ToolbarMenuEntity> = registerSymbol("Operation", "ToolbarMenuOperation.Save");
  export const Delete : Entities.DeleteSymbol<ToolbarMenuEntity> = registerSymbol("Operation", "ToolbarMenuOperation.Delete");
}

export module ToolbarMessage {
  export const RecursionDetected = new MessageKey("ToolbarMessage", "RecursionDetected");
  export const _0CyclesHaveBeenFoundInTheToolbarDueToTheRelationships = new MessageKey("ToolbarMessage", "_0CyclesHaveBeenFoundInTheToolbarDueToTheRelationships");
}

export module ToolbarOperation {
  export const Save : Entities.ExecuteSymbol<ToolbarEntity> = registerSymbol("Operation", "ToolbarOperation.Save");
  export const Delete : Entities.DeleteSymbol<ToolbarEntity> = registerSymbol("Operation", "ToolbarOperation.Delete");
}


