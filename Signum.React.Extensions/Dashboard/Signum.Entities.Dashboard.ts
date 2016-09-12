//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets'
import * as UserQueries from '../UserQueries/Signum.Entities.UserQueries'
import * as Chart from '../Chart/Signum.Entities.Chart'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'


export const CountSearchControlPartEntity = new Type<CountSearchControlPartEntity>("CountSearchControlPart");
export interface CountSearchControlPartEntity extends Entities.Entity, IPartEntity {
    Type: "CountSearchControlPart";
    userQueries: Entities.MList<CountUserQueryElementEntity>;
    requiresTitle?: boolean;
}

export const CountUserQueryElementEntity = new Type<CountUserQueryElementEntity>("CountUserQueryElementEntity");
export interface CountUserQueryElementEntity extends Entities.EmbeddedEntity {
    Type: "CountUserQueryElementEntity";
    label?: string | null;
    userQuery?: UserQueries.UserQueryEntity | null;
    href?: string | null;
}

export const DashboardEmbedededInEntity = new EnumType<DashboardEmbedededInEntity>("DashboardEmbedededInEntity");
export type DashboardEmbedededInEntity =
    "None" |
    "Top" |
    "Bottom";

export const DashboardEntity = new Type<DashboardEntity>("Dashboard");
export interface DashboardEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
    Type: "Dashboard";
    entityType?: Entities.Lite<Basics.TypeEntity> | null;
    embeddedInEntity?: DashboardEmbedededInEntity | null;
    owner?: Entities.Lite<Entities.Entity> | null;
    dashboardPriority?: number | null;
    autoRefreshPeriod?: number | null;
    displayName?: string | null;
    parts: Entities.MList<PanelPartEntity>;
    guid?: string;
    forNavbar?: boolean;
    key?: string | null;
}

export module DashboardMessage {
    export const CreateNewPart = new MessageKey("DashboardMessage", "CreateNewPart");
    export const DashboardDN_TitleMustBeSpecifiedFor0 = new MessageKey("DashboardMessage", "DashboardDN_TitleMustBeSpecifiedFor0");
    export const CountSearchControlPartEntity = new MessageKey("DashboardMessage", "CountSearchControlPartEntity");
    export const CountUserQueryElement = new MessageKey("DashboardMessage", "CountUserQueryElement");
    export const Preview = new MessageKey("DashboardMessage", "Preview");
    export const _0Is1InstedOf2In3 = new MessageKey("DashboardMessage", "_0Is1InstedOf2In3");
    export const Part0IsTooLarge = new MessageKey("DashboardMessage", "Part0IsTooLarge");
    export const Part0OverlapsWith1 = new MessageKey("DashboardMessage", "Part0OverlapsWith1");
}

export module DashboardOperation {
    export const Create : Entities.ConstructSymbol_Simple<DashboardEntity> = registerSymbol("Operation", "DashboardOperation.Create");
    export const Save : Entities.ExecuteSymbol<DashboardEntity> = registerSymbol("Operation", "DashboardOperation.Save");
    export const Clone : Entities.ConstructSymbol_From<DashboardEntity, DashboardEntity> = registerSymbol("Operation", "DashboardOperation.Clone");
    export const Delete : Entities.DeleteSymbol<DashboardEntity> = registerSymbol("Operation", "DashboardOperation.Delete");
}

export module DashboardPermission {
    export const ViewDashboard : Authorization.PermissionSymbol = registerSymbol("Permission", "DashboardPermission.ViewDashboard");
}

export interface IPartEntity extends Entities.Entity {
    requiresTitle?: boolean;
}

export const LinkElementEntity = new Type<LinkElementEntity>("LinkElementEntity");
export interface LinkElementEntity extends Entities.EmbeddedEntity {
    Type: "LinkElementEntity";
    label?: string | null;
    link?: string | null;
}

export const LinkListPartEntity = new Type<LinkListPartEntity>("LinkListPart");
export interface LinkListPartEntity extends Entities.Entity, IPartEntity {
    Type: "LinkListPart";
    links: Entities.MList<LinkElementEntity>;
    requiresTitle?: boolean;
}

export const LinkPartEntity = new Type<LinkPartEntity>("LinkPart");
export interface LinkPartEntity extends Entities.Entity, IPartEntity {
    Type: "LinkPart";
    link?: LinkElementEntity | null;
    requiresTitle?: boolean;
}

export const OmniboxPanelPartEntity = new Type<OmniboxPanelPartEntity>("OmniboxPanelPart");
export interface OmniboxPanelPartEntity extends Entities.Entity, IPartEntity {
    Type: "OmniboxPanelPart";
    requiresTitle?: boolean;
}

export const PanelPartEntity = new Type<PanelPartEntity>("PanelPartEntity");
export interface PanelPartEntity extends Entities.EmbeddedEntity {
    Type: "PanelPartEntity";
    title?: string | null;
    row?: number;
    startColumn?: number;
    columns?: number;
    style?: PanelStyle;
    content?: IPartEntity | null;
}

export const PanelStyle = new EnumType<PanelStyle>("PanelStyle");
export type PanelStyle =
    "Default" |
    "Primary" |
    "Success" |
    "Info" |
    "Warning" |
    "Danger";

export const UserChartPartEntity = new Type<UserChartPartEntity>("UserChartPart");
export interface UserChartPartEntity extends Entities.Entity, IPartEntity {
    Type: "UserChartPart";
    userChart?: Chart.UserChartEntity | null;
    showData?: boolean;
    requiresTitle?: boolean;
}

export const UserQueryCountPartEntity = new Type<UserQueryCountPartEntity>("UserQueryCountPart");
export interface UserQueryCountPartEntity extends Entities.Entity, IPartEntity {
    Type: "UserQueryCountPart";
    requiresTitle?: boolean;
    userQuery?: Entities.Lite<UserQueries.UserQueryEntity> | null;
    iconClass?: string | null;
    showName?: boolean;
}

export const UserQueryPartEntity = new Type<UserQueryPartEntity>("UserQueryPart");
export interface UserQueryPartEntity extends Entities.Entity, IPartEntity {
    Type: "UserQueryPart";
    userQuery?: UserQueries.UserQueryEntity | null;
    allowSelection?: boolean;
    requiresTitle?: boolean;
}


