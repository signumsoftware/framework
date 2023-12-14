//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as DynamicQuery from '../../Signum/React/Signum.DynamicQuery'

import { QueryToken } from '@framework/FindOptions'

export interface QueryTokenEmbedded {
    token?: QueryToken;
    parseException?: string;
}

export const PinnedQueryFilterEmbedded = new Type<PinnedQueryFilterEmbedded>("PinnedQueryFilterEmbedded");
export interface PinnedQueryFilterEmbedded extends Entities.EmbeddedEntity {
  Type: "PinnedQueryFilterEmbedded";
  label: string | null;
  column: number | null;
  colSpan: number | null;
  row: number | null;
  active: DynamicQuery.PinnedFilterActive;
  splitValue: boolean;
}

export const QueryColumnEmbedded = new Type<QueryColumnEmbedded>("QueryColumnEmbedded");
export interface QueryColumnEmbedded extends Entities.EmbeddedEntity {
  Type: "QueryColumnEmbedded";
  token: QueryTokenEmbedded;
  displayName: string | null;
  summaryToken: QueryTokenEmbedded | null;
  hiddenColumn: boolean;
  combineRows: DynamicQuery.CombineRows | null;
}

export const QueryFilterEmbedded = new Type<QueryFilterEmbedded>("QueryFilterEmbedded");
export interface QueryFilterEmbedded extends Entities.EmbeddedEntity {
  Type: "QueryFilterEmbedded";
  token: QueryTokenEmbedded | null;
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
  token: QueryTokenEmbedded;
  orderType: DynamicQuery.OrderType;
}

export const QueryTokenEmbedded = new Type<QueryTokenEmbedded>("QueryTokenEmbedded");
export interface QueryTokenEmbedded extends Entities.EmbeddedEntity {
  Type: "QueryTokenEmbedded";
  tokenString: string;
}

export module UserAssetQueryMessage {
  export const SwitchToValue = new MessageKey("UserAssetQueryMessage", "SwitchToValue");
  export const SwitchToExpression = new MessageKey("UserAssetQueryMessage", "SwitchToExpression");
}

