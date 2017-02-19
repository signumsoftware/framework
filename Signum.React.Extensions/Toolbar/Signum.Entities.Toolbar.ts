//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets'


export const ToolbarElementEntity = new Type<ToolbarElementEntity>("ToolbarElementEntity");
export interface ToolbarElementEntity extends Entities.EmbeddedEntity {
    Type: "ToolbarElementEntity";
    type?: ToolbarElementType;
    label?: string | null;
    iconName?: string | null;
    iconColor?: string | null;
    content?: Entities.Lite<Entities.Entity> | null;
    openInPopup?: boolean;
    autoRefreshPeriod?: number | null;
}

export const ToolbarElementType = new EnumType<ToolbarElementType>("ToolbarElementType");
export type ToolbarElementType =
    "Link" |
    "Menu" |
    "Header" |
    "Divider";

export const ToolbarEntity = new Type<ToolbarEntity>("Toolbar");
export interface ToolbarEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
    Type: "Toolbar";
    owner?: Entities.Lite<Entities.Entity> | null;
    name?: string | null;
    priority?: number | null;
    elements: Entities.MList<ToolbarElementEntity>;
    guid?: string;
}

export const ToolbarMenuEntity = new Type<ToolbarMenuEntity>("ToolbarMenu");
export interface ToolbarMenuEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
    Type: "ToolbarMenu";
    guid?: string;
    name?: string | null;
    elements: Entities.MList<ToolbarElementEntity>;
}

export module ToolbarMenuOperation {
    export const Save : Entities.ExecuteSymbol<ToolbarMenuEntity> = registerSymbol("Operation", "ToolbarMenuOperation.Save");
    export const Delete : Entities.DeleteSymbol<ToolbarMenuEntity> = registerSymbol("Operation", "ToolbarMenuOperation.Delete");
}

export module ToolbarOperation {
    export const Save : Entities.ExecuteSymbol<ToolbarEntity> = registerSymbol("Operation", "ToolbarOperation.Save");
    export const Delete : Entities.DeleteSymbol<ToolbarEntity> = registerSymbol("Operation", "ToolbarOperation.Delete");
}


