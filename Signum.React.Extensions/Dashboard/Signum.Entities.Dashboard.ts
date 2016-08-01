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
    userQueries: Entities.MList<CountUserQueryElementEntity>;
    requiresTitle: boolean;
}

export const CountUserQueryElementEntity = new Type<CountUserQueryElementEntity>("CountUserQueryElementEntity");
export interface CountUserQueryElementEntity extends Entities.EmbeddedEntity {
    label: string;
    userQuery: UserQueries.UserQueryEntity;
    href: string;
}

export const DashboardEmbedededInEntity = new EnumType<DashboardEmbedededInEntity>("DashboardEmbedededInEntity");
export type DashboardEmbedededInEntity =
    "None" |
    "Top" |
    "Bottom";

export const DashboardEntity = new Type<DashboardEntity>("Dashboard");
export interface DashboardEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
    entityType: Entities.Lite<Basics.TypeEntity>;
    embeddedInEntity: DashboardEmbedededInEntity;
    owner: Entities.Lite<Entities.Entity>;
    dashboardPriority: number;
    autoRefreshPeriod: number;
    displayName: string;
    parts: Entities.MList<PanelPartEntity>;
    guid: string;
    forNavbar: boolean;
    key: string;
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
    export const Create : Entities.ConstructSymbol_Simple<DashboardEntity> = registerSymbol({ Type: "Operation", key: "DashboardOperation.Create" });
    export const Save : Entities.ExecuteSymbol<DashboardEntity> = registerSymbol({ Type: "Operation", key: "DashboardOperation.Save" });
    export const Clone : Entities.ConstructSymbol_From<DashboardEntity, DashboardEntity> = registerSymbol({ Type: "Operation", key: "DashboardOperation.Clone" });
    export const Delete : Entities.DeleteSymbol<DashboardEntity> = registerSymbol({ Type: "Operation", key: "DashboardOperation.Delete" });
}

export module DashboardPermission {
    export const ViewDashboard : Authorization.PermissionSymbol = registerSymbol({ Type: "Permission", key: "DashboardPermission.ViewDashboard" });
}

export interface IPartEntity extends Entities.Entity {
    requiresTitle: boolean;
}

export const LinkElementEntity = new Type<LinkElementEntity>("LinkElementEntity");
export interface LinkElementEntity extends Entities.EmbeddedEntity {
    label: string;
    link: string;
}

export const LinkListPartEntity = new Type<LinkListPartEntity>("LinkListPart");
export interface LinkListPartEntity extends Entities.Entity, IPartEntity {
    links: Entities.MList<LinkElementEntity>;
    requiresTitle: boolean;
}

export const LinkPartEntity = new Type<LinkPartEntity>("LinkPart");
export interface LinkPartEntity extends Entities.Entity, IPartEntity {
    link: LinkElementEntity;
    requiresTitle: boolean;
}

export const OmniboxPanelPartEntity = new Type<OmniboxPanelPartEntity>("OmniboxPanelPart");
export interface OmniboxPanelPartEntity extends Entities.Entity, IPartEntity {
    requiresTitle: boolean;
}

export const PanelPartEntity = new Type<PanelPartEntity>("PanelPartEntity");
export interface PanelPartEntity extends Entities.EmbeddedEntity {
    title: string;
    row: number;
    startColumn: number;
    columns: number;
    style: PanelStyle;
    content: IPartEntity;
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
    userChart: Chart.UserChartEntity;
    showData: boolean;
    requiresTitle: boolean;
}

export const UserQueryCountPartEntity = new Type<UserQueryCountPartEntity>("UserQueryCountPart");
export interface UserQueryCountPartEntity extends Entities.Entity, IPartEntity {
    requiresTitle: boolean;
    userQuery: Entities.Lite<UserQueries.UserQueryEntity>;
    iconClass: string;
    showName: boolean;
}

export const UserQueryPartEntity = new Type<UserQueryPartEntity>("UserQueryPart");
export interface UserQueryPartEntity extends Entities.Entity, IPartEntity {
    userQuery: UserQueries.UserQueryEntity;
    allowSelection: boolean;
    requiresTitle: boolean;
}


