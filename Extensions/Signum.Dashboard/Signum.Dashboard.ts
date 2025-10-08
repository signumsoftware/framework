//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as UserAssets from '../Signum.UserAssets/Signum.UserAssets'
import * as Queries from '../Signum.UserAssets/Signum.UserAssets.Queries'
import * as Files from '../Signum.Files/Signum.Files'
import * as Scheduler from '../Signum.Scheduler/Signum.Scheduler'
import * as Toolbar from '../Signum.Toolbar/Signum.Toolbar'


export const CachedQueryEntity: Type<CachedQueryEntity> = new Type<CachedQueryEntity>("CachedQuery");
export interface CachedQueryEntity extends Entities.Entity {
  Type: "CachedQuery";
  dashboard: Entities.Lite<DashboardEntity>;
  userAssets: Entities.MList<Entities.Lite<UserAssets.IUserAssetEntity>>;
  file: Files.FilePathEmbedded;
  numRows: number;
  numColumns: number;
  creationDate: string /*DateTime*/;
  queryDuration: number;
  uploadDuration: number;
}

export namespace CachedQueryFileType {
  export const CachedQuery : Files.FileTypeSymbol = registerSymbol("FileType", "CachedQueryFileType.CachedQuery");
}

export const CacheQueryConfigurationEmbedded: Type<CacheQueryConfigurationEmbedded> = new Type<CacheQueryConfigurationEmbedded>("CacheQueryConfigurationEmbedded");
export interface CacheQueryConfigurationEmbedded extends Entities.EmbeddedEntity {
  Type: "CacheQueryConfigurationEmbedded";
  timeoutForQueries: number;
  maxRows: number;
  autoRegenerateWhenOlderThan: number | null;
}

export const CustomPartEntity: Type<CustomPartEntity> = new Type<CustomPartEntity>("CustomPart");
export interface CustomPartEntity extends Entities.Entity, IPartEntity {
  Type: "CustomPart";
  requiresTitle: boolean;
  customPartName: string;
}

export const DashboardEmbedededInEntity: EnumType<DashboardEmbedededInEntity> = new EnumType<DashboardEmbedededInEntity>("DashboardEmbedededInEntity");
export type DashboardEmbedededInEntity =
  "None" |
  "Top" |
  "Bottom" |
  "Tab";

export const DashboardEntity: Type<DashboardEntity> = new Type<DashboardEntity>("Dashboard");
export interface DashboardEntity extends Entities.Entity, UserAssets.IUserAssetEntity, Scheduler.ITaskEntity {
  Type: "Dashboard";
  entityType: Entities.Lite<Basics.TypeEntity> | null;
  embeddedInEntity: DashboardEmbedededInEntity | null;
  owner: Entities.Lite<Entities.Entity> | null;
  dashboardPriority: number | null;
  autoRefreshPeriod: number | null;
  displayName: string;
  hideDisplayName: boolean;
  combineSimilarRows: boolean;
  cacheQueryConfiguration: CacheQueryConfigurationEmbedded | null;
  parts: Entities.MList<PanelPartEmbedded>;
  tokenEquivalencesGroups: Entities.MList<TokenEquivalenceGroupEntity>;
  guid: string /*Guid*/;
  key: string | null;
  hideQuickLink: boolean;
  iconName: string | null;
  iconColor: string | null;
  titleColor: string | null;
}

export const DashboardLiteModel: Type<DashboardLiteModel> = new Type<DashboardLiteModel>("DashboardLiteModel");
export interface DashboardLiteModel extends Entities.ModelEntity {
  Type: "DashboardLiteModel";
  displayName: string;
  hideQuickLink: boolean;
}

export namespace DashboardMessage {
  export const CreateNewPart: MessageKey = new MessageKey("DashboardMessage", "CreateNewPart");
  export const DashboardDN_TitleMustBeSpecifiedFor0: MessageKey = new MessageKey("DashboardMessage", "DashboardDN_TitleMustBeSpecifiedFor0");
  export const Preview: MessageKey = new MessageKey("DashboardMessage", "Preview");
  export const _0Is1InstedOf2In3: MessageKey = new MessageKey("DashboardMessage", "_0Is1InstedOf2In3");
  export const Part0IsTooLarge: MessageKey = new MessageKey("DashboardMessage", "Part0IsTooLarge");
  export const Part0OverlapsWith1: MessageKey = new MessageKey("DashboardMessage", "Part0OverlapsWith1");
  export const RowsSelected: MessageKey = new MessageKey("DashboardMessage", "RowsSelected");
  export const ForPerformanceReasonsThisDashboardMayShowOutdatedInformation: MessageKey = new MessageKey("DashboardMessage", "ForPerformanceReasonsThisDashboardMayShowOutdatedInformation");
  export const LasUpdateWasOn0: MessageKey = new MessageKey("DashboardMessage", "LasUpdateWasOn0");
  export const TheUserQuery0HasNoColumnWithSummaryHeader: MessageKey = new MessageKey("DashboardMessage", "TheUserQuery0HasNoColumnWithSummaryHeader");
  export const Edit: MessageKey = new MessageKey("DashboardMessage", "Edit");
  export const CLickInOneChartToFilterInTheOthers: MessageKey = new MessageKey("DashboardMessage", "CLickInOneChartToFilterInTheOthers");
  export const CtrlClickToFilterByMultipleElements: MessageKey = new MessageKey("DashboardMessage", "CtrlClickToFilterByMultipleElements");
  export const AltClickToOpenResultsInAModalWindow: MessageKey = new MessageKey("DashboardMessage", "AltClickToOpenResultsInAModalWindow");
  export const CopyHealthCheckDashboardData: MessageKey = new MessageKey("DashboardMessage", "CopyHealthCheckDashboardData");
  export const _0CanOnlyBeUserInA1With2: MessageKey = new MessageKey("DashboardMessage", "_0CanOnlyBeUserInA1With2");
}

export namespace DashboardOperation {
  export const Save : Operations.ExecuteSymbol<DashboardEntity> = registerSymbol("Operation", "DashboardOperation.Save");
  export const RegenerateCachedQueries : Operations.ExecuteSymbol<DashboardEntity> = registerSymbol("Operation", "DashboardOperation.RegenerateCachedQueries");
  export const Clone : Operations.ConstructSymbol_From<DashboardEntity, DashboardEntity> = registerSymbol("Operation", "DashboardOperation.Clone");
  export const Delete : Operations.DeleteSymbol<DashboardEntity> = registerSymbol("Operation", "DashboardOperation.Delete");
}

export namespace DashboardPermission {
  export const ViewDashboard : Basics.PermissionSymbol = registerSymbol("Permission", "DashboardPermission.ViewDashboard");
}

export const HealthCheckElementEmbedded: Type<HealthCheckElementEmbedded> = new Type<HealthCheckElementEmbedded>("HealthCheckElementEmbedded");
export interface HealthCheckElementEmbedded extends Entities.EmbeddedEntity {
  Type: "HealthCheckElementEmbedded";
  title: string;
  checkURL: string;
  navigateURL: string;
}

export const HealthCheckPartEntity: Type<HealthCheckPartEntity> = new Type<HealthCheckPartEntity>("HealthCheckPart");
export interface HealthCheckPartEntity extends Entities.Entity, IPartEntity {
  Type: "HealthCheckPart";
  items: Entities.MList<HealthCheckElementEmbedded>;
  requiresTitle: boolean;
}

export const ImagePartEntity: Type<ImagePartEntity> = new Type<ImagePartEntity>("ImagePart");
export interface ImagePartEntity extends Entities.Entity, IPartEntity {
  Type: "ImagePart";
  imageSrcContent: string;
  clickActionURL: string | null;
  altText: string | null;
  requiresTitle: boolean;
}

export const InteractionGroup: EnumType<InteractionGroup> = new EnumType<InteractionGroup>("InteractionGroup");
export type InteractionGroup =
  "Group1" |
  "Group2" |
  "Group3" |
  "Group4" |
  "Group5" |
  "Group6" |
  "Group7" |
  "Group8";

export interface IPartEntity extends Entities.Entity {
  requiresTitle: boolean;
}

export interface IPartParseDataEntity extends IPartEntity, Entities.Entity {
}

export const LinkElementEmbedded: Type<LinkElementEmbedded> = new Type<LinkElementEmbedded>("LinkElementEmbedded");
export interface LinkElementEmbedded extends Entities.EmbeddedEntity {
  Type: "LinkElementEmbedded";
  label: string;
  link: string;
  opensInNewTab: boolean;
}

export const PanelPartEmbedded: Type<PanelPartEmbedded> = new Type<PanelPartEmbedded>("PanelPartEmbedded");
export interface PanelPartEmbedded extends Entities.EmbeddedEntity {
  Type: "PanelPartEmbedded";
  title: string | null;
  iconName: string | null;
  iconColor: string | null;
  titleColor: string | null;
  row: number;
  startColumn: number;
  columns: number;
  interactionGroup: InteractionGroup | null;
  customColor: string | null;
  content: IPartEntity;
}

export const SeparatorPartEntity: Type<SeparatorPartEntity> = new Type<SeparatorPartEntity>("SeparatorPart");
export interface SeparatorPartEntity extends Entities.Entity, IPartEntity {
  Type: "SeparatorPart";
  title: string | null;
  requiresTitle: boolean;
}

export const TextPartEntity: Type<TextPartEntity> = new Type<TextPartEntity>("TextPart");
export interface TextPartEntity extends Entities.Entity, IPartEntity {
  Type: "TextPart";
  textContent: string | null;
  textPartType: TextPartType;
  requiresTitle: boolean;
}

export const TextPartType: EnumType<TextPartType> = new EnumType<TextPartType>("TextPartType");
export type TextPartType =
  "Text" |
  "Markdown" |
  "HTML";

export const TokenEquivalenceEmbedded: Type<TokenEquivalenceEmbedded> = new Type<TokenEquivalenceEmbedded>("TokenEquivalenceEmbedded");
export interface TokenEquivalenceEmbedded extends Entities.EmbeddedEntity {
  Type: "TokenEquivalenceEmbedded";
  query: Basics.QueryEntity;
  token: Queries.QueryTokenEmbedded;
}

export const TokenEquivalenceGroupEntity: Type<TokenEquivalenceGroupEntity> = new Type<TokenEquivalenceGroupEntity>("TokenEquivalenceGroup");
export interface TokenEquivalenceGroupEntity extends Entities.Entity {
  Type: "TokenEquivalenceGroup";
  dashboard: Entities.Lite<DashboardEntity>;
  interactionGroup: InteractionGroup | null;
  tokenEquivalences: Entities.MList<TokenEquivalenceEmbedded>;
}

export const ToolbarPartEntity: Type<ToolbarPartEntity> = new Type<ToolbarPartEntity>("ToolbarPart");
export interface ToolbarPartEntity extends Entities.Entity, IPartEntity {
  Type: "ToolbarPart";
  toolbarMenu: Entities.Lite<Toolbar.ToolbarMenuEntity>;
  requiresTitle: boolean;
}

