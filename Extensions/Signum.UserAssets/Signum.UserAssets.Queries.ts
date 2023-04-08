//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as DynamicQuery from '../../Signum/React/Signum.DynamicQuery'
import * as QueryTokens from './Signum.UserAssets.QueryTokens'


export const PinnedQueryFilterEmbedded = new Type<PinnedQueryFilterEmbedded>("PinnedQueryFilterEmbedded");
export interface PinnedQueryFilterEmbedded extends Entities.EmbeddedEntity {
  Type: "PinnedQueryFilterEmbedded";
  label: string | null;
  column: number | null;
  row: number | null;
  active: DynamicQuery.PinnedFilterActive;
  splitText: boolean;
}

export const QueryColumnEmbedded = new Type<QueryColumnEmbedded>("QueryColumnEmbedded");
export interface QueryColumnEmbedded extends Entities.EmbeddedEntity {
  Type: "QueryColumnEmbedded";
  token: QueryTokens.QueryTokenEmbedded;
  displayName: string | null;
  summaryToken: QueryTokens.QueryTokenEmbedded | null;
  hiddenColumn: boolean;
  combineRows: DynamicQuery.CombineRows | null;
}

export const QueryFilterEmbedded = new Type<QueryFilterEmbedded>("QueryFilterEmbedded");
export interface QueryFilterEmbedded extends Entities.EmbeddedEntity {
  Type: "QueryFilterEmbedded";
  token: QueryTokens.QueryTokenEmbedded | null;
  isGroup: boolean;
  groupOperation: DynamicQuery.FilterGroupOperation | null;
  operation: DynamicQuery.FilterOperation | null;
  valueString: string | null;
  pinned: PinnedQueryFilterEmbedded | null;
  dashboardBehaviour: DynamicQuery.DashboardBehaviour | null;
  indentation: number;
}

export const QueryOrderEmbedded = new Type<QueryOrderEmbedded>("QueryOrderEmbedded");
export interface QueryOrderEmbedded extends Entities.EmbeddedEntity {
  Type: "QueryOrderEmbedded";
  token: QueryTokens.QueryTokenEmbedded;
  orderType: DynamicQuery.OrderType;
}

