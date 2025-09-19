//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as UserAssets from '../Signum.UserAssets/Signum.UserAssets'


export interface IToolbarEntity extends Entities.Entity {
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

export const ToolbarMenuElementEmbedded: Type<ToolbarMenuElementEmbedded> = new Type<ToolbarMenuElementEmbedded>("ToolbarMenuElementEmbedded");
export interface ToolbarMenuElementEmbedded extends ToolbarElementEmbedded {
  withEntity: boolean;
  autoSelect: boolean;
}

export const ToolbarMenuEntity: Type<ToolbarMenuEntity> = new Type<ToolbarMenuEntity>("ToolbarMenu");
export interface ToolbarMenuEntity extends Entities.Entity, UserAssets.IUserAssetEntity, IToolbarEntity {
  Type: "ToolbarMenu";
  owner: Entities.Lite<Entities.Entity> | null;
  guid: string /*Guid*/;
  name: string;
  elements: Entities.MList<ToolbarMenuElementEmbedded>;
  entityType: Entities.Lite<Basics.TypeEntity> | null;
}

export namespace ToolbarMenuOperation {
  export const Save : Operations.ExecuteSymbol<ToolbarMenuEntity> = registerSymbol("Operation", "ToolbarMenuOperation.Save");
  export const Delete : Operations.DeleteSymbol<ToolbarMenuEntity> = registerSymbol("Operation", "ToolbarMenuOperation.Delete");
}

export namespace ToolbarMessage {
  export const RecursionDetected: MessageKey = new MessageKey("ToolbarMessage", "RecursionDetected");
  export const _0CyclesHaveBeenFoundInTheToolbarDueToTheRelationships: MessageKey = new MessageKey("ToolbarMessage", "_0CyclesHaveBeenFoundInTheToolbarDueToTheRelationships");
  export const FirstElementCanNotBeExtraIcon: MessageKey = new MessageKey("ToolbarMessage", "FirstElementCanNotBeExtraIcon");
  export const ExtraIconCanNotComeAfterDivider: MessageKey = new MessageKey("ToolbarMessage", "ExtraIconCanNotComeAfterDivider");
  export const If0Selected: MessageKey = new MessageKey("ToolbarMessage", "If0Selected");
  export const No0Selected: MessageKey = new MessageKey("ToolbarMessage", "No0Selected");
  export const ShowTogether: MessageKey = new MessageKey("ToolbarMessage", "ShowTogether");
}

export namespace ToolbarOperation {
  export const Save : Operations.ExecuteSymbol<ToolbarEntity> = registerSymbol("Operation", "ToolbarOperation.Save");
  export const Delete : Operations.DeleteSymbol<ToolbarEntity> = registerSymbol("Operation", "ToolbarOperation.Delete");
}

export const ToolbarSwitcherEntity: Type<ToolbarSwitcherEntity> = new Type<ToolbarSwitcherEntity>("ToolbarSwitcher");
export interface ToolbarSwitcherEntity extends Entities.Entity, IToolbarEntity, UserAssets.IUserAssetEntity {
  Type: "ToolbarSwitcher";
  name: string;
  owner: Entities.Lite<Entities.Entity> | null;
  options: Entities.MList<ToolbarSwitcherOptionEmbedded>;
  guid: string /*Guid*/;
}

export namespace ToolbarSwitcherOperation {
  export const Save : Operations.ExecuteSymbol<ToolbarSwitcherEntity> = registerSymbol("Operation", "ToolbarSwitcherOperation.Save");
  export const Delete : Operations.DeleteSymbol<ToolbarSwitcherEntity> = registerSymbol("Operation", "ToolbarSwitcherOperation.Delete");
}

export const ToolbarSwitcherOptionEmbedded: Type<ToolbarSwitcherOptionEmbedded> = new Type<ToolbarSwitcherOptionEmbedded>("ToolbarSwitcherOptionEmbedded");
export interface ToolbarSwitcherOptionEmbedded extends Entities.EmbeddedEntity {
  Type: "ToolbarSwitcherOptionEmbedded";
  toolbarMenu: Entities.Lite<ToolbarMenuEntity>;
  iconName: string | null;
  iconColor: string | null;
}

