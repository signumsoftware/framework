//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as UserAssets from '../Signum.UserAssets/Signum.UserAssets'
import * as Queries from '../Signum.UserAssets/Signum.UserAssets.Queries'
import * as Chart from './Signum.Chart'
import * as Dashboard from '../Signum.Dashboard/Signum.Dashboard'

import { ChartRequestModel } from './Signum.Chart'

export type IChartBase = ChartRequestModel | UserChartEntity;

export const CombinedUserChartElementEmbedded = new Type<CombinedUserChartElementEmbedded>("CombinedUserChartElementEmbedded");
export interface CombinedUserChartElementEmbedded extends Entities.EmbeddedEntity {
  Type: "CombinedUserChartElementEmbedded";
  userChart: UserChartEntity;
  isQueryCached: boolean;
}

export const CombinedUserChartPartEntity = new Type<CombinedUserChartPartEntity>("CombinedUserChartPart");
export interface CombinedUserChartPartEntity extends Entities.Entity, Dashboard.IPartEntity {
  Type: "CombinedUserChartPart";
  userCharts: Entities.MList<CombinedUserChartElementEmbedded>;
  showData: boolean;
  allowChangeShowData: boolean;
  combinePinnedFiltersWithSameLabel: boolean;
  useSameScale: boolean;
  minHeight: number | null;
  requiresTitle: boolean;
}

export const UserChartEntity = new Type<UserChartEntity>("UserChart");
export interface UserChartEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "UserChart";
  query: Basics.QueryEntity;
  entityType: Entities.Lite<Basics.TypeEntity> | null;
  hideQuickLink: boolean;
  owner: Entities.Lite<Entities.Entity> | null;
  displayName: string;
  includeDefaultFilters: boolean | null;
  maxRows: number | null;
  chartScript: Chart.ChartScriptSymbol;
  parameters: Entities.MList<Chart.ChartParameterEmbedded>;
  columns: Entities.MList<Chart.ChartColumnEmbedded>;
  filters: Entities.MList<Queries.QueryFilterEmbedded>;
  customDrilldowns: Entities.MList<Entities.Lite<Entities.Entity>>;
  guid: string /*Guid*/;
}

export module UserChartOperation {
  export const Save : Operations.ExecuteSymbol<UserChartEntity> = registerSymbol("Operation", "UserChartOperation.Save");
  export const Delete : Operations.DeleteSymbol<UserChartEntity> = registerSymbol("Operation", "UserChartOperation.Delete");
}

export const UserChartPartEntity = new Type<UserChartPartEntity>("UserChartPart");
export interface UserChartPartEntity extends Entities.Entity, Dashboard.IPartEntity {
  Type: "UserChartPart";
  userChart: UserChartEntity;
  isQueryCached: boolean;
  showData: boolean;
  allowChangeShowData: boolean;
  createNew: boolean;
  autoRefresh: boolean;
  minHeight: number | null;
  requiresTitle: boolean;
}

