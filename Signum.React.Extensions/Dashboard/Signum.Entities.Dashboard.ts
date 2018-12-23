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
  combineSimilarRows?: boolean;
  parts: Entities.MList<PanelPartEmbedded>;
  guid?: string;
  forNavbar?: boolean;
  key?: string | null;
}

export module DashboardMessage {
  export const CreateNewPart = new MessageKey("DashboardMessage", "CreateNewPart");
  export const DashboardDN_TitleMustBeSpecifiedFor0 = new MessageKey("DashboardMessage", "DashboardDN_TitleMustBeSpecifiedFor0");
  export const Preview = new MessageKey("DashboardMessage", "Preview");
  export const _0Is1InstedOf2In3 = new MessageKey("DashboardMessage", "_0Is1InstedOf2In3");
  export const Part0IsTooLarge = new MessageKey("DashboardMessage", "Part0IsTooLarge");
  export const Part0OverlapsWith1 = new MessageKey("DashboardMessage", "Part0OverlapsWith1");
}

export module DashboardOperation {
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

export const LinkElementEmbedded = new Type<LinkElementEmbedded>("LinkElementEmbedded");
export interface LinkElementEmbedded extends Entities.EmbeddedEntity {
  Type: "LinkElementEmbedded";
  label?: string | null;
  link?: string | null;
}

export const LinkListPartEntity = new Type<LinkListPartEntity>("LinkListPart");
export interface LinkListPartEntity extends Entities.Entity, IPartEntity {
  Type: "LinkListPart";
  links: Entities.MList<LinkElementEmbedded>;
  requiresTitle?: boolean;
}

export const PanelPartEmbedded = new Type<PanelPartEmbedded>("PanelPartEmbedded");
export interface PanelPartEmbedded extends Entities.EmbeddedEntity {
  Type: "PanelPartEmbedded";
  title?: string | null;
  iconName?: string | null;
  iconColor?: string | null;
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
  "Secondary" |
  "Success" |
  "Info" |
  "Warning" |
  "Danger" |
  "Light" |
  "Dark";

export const UserChartPartEntity = new Type<UserChartPartEntity>("UserChartPart");
export interface UserChartPartEntity extends Entities.Entity, IPartEntity {
  Type: "UserChartPart";
  userChart?: Chart.UserChartEntity | null;
  showData?: boolean;
  allowChangeShowData?: boolean;
  requiresTitle?: boolean;
}

export const UserQueryPartEntity = new Type<UserQueryPartEntity>("UserQueryPart");
export interface UserQueryPartEntity extends Entities.Entity, IPartEntity {
  Type: "UserQueryPart";
  userQuery?: UserQueries.UserQueryEntity | null;
  renderMode?: UserQueryPartRenderMode;
  requiresTitle?: boolean;
}

export const UserQueryPartRenderMode = new EnumType<UserQueryPartRenderMode>("UserQueryPartRenderMode");
export type UserQueryPartRenderMode =
  "SearchControl" |
  "SearchControlWithoutSelection" |
  "BigValue";

export const ValueUserQueryElementEmbedded = new Type<ValueUserQueryElementEmbedded>("ValueUserQueryElementEmbedded");
export interface ValueUserQueryElementEmbedded extends Entities.EmbeddedEntity {
  Type: "ValueUserQueryElementEmbedded";
  label?: string | null;
  userQuery?: UserQueries.UserQueryEntity | null;
  href?: string | null;
}

export const ValueUserQueryListPartEntity = new Type<ValueUserQueryListPartEntity>("ValueUserQueryListPart");
export interface ValueUserQueryListPartEntity extends Entities.Entity, IPartEntity {
  Type: "ValueUserQueryListPart";
  userQueries: Entities.MList<ValueUserQueryElementEmbedded>;
  requiresTitle?: boolean;
}


