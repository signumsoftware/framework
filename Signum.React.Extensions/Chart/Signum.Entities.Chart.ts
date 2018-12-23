//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as DynamicQuery from '../../../Framework/Signum.React/Scripts/Signum.Entities.DynamicQuery'
import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets'
import * as UserQueries from '../UserQueries/Signum.Entities.UserQueries'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'

import { FilterOptionParsed, OrderOptionParsed, FilterRequest, OrderRequest } from '@framework/FindOptions'

//Partial
export interface ChartRequestModel {
  queryKey: string;
  
  filterOptions: FilterOptionParsed[];

  filters: FilterRequest[];
}

export interface ChartScriptParameterEmbedded {
  enumValues: { name: string, typeFilter : ChartColumnType }[];
}

export type IChartBase = ChartRequestModel | UserChartEntity;

export const ChartColorEntity = new Type<ChartColorEntity>("ChartColor");
export interface ChartColorEntity extends Entities.Entity {
  Type: "ChartColor";
  related?: Entities.Lite<Entities.Entity> | null;
  color?: Basics.ColorEmbedded | null;
}

export const ChartColumnEmbedded = new Type<ChartColumnEmbedded>("ChartColumnEmbedded");
export interface ChartColumnEmbedded extends Entities.EmbeddedEntity {
  Type: "ChartColumnEmbedded";
  token?: UserAssets.QueryTokenEmbedded | null;
  displayName?: string | null;
  orderByIndex?: number | null;
  orderByType?: DynamicQuery.OrderType | null;
}

export const ChartColumnType = new EnumType<ChartColumnType>("ChartColumnType");
export type ChartColumnType =
  "Integer" |
  "Real" |
  "Date" |
  "DateTime" |
  "String" |
  "Lite" |
  "Enum" |
  "RealGroupable" |
  "Magnitude" |
  "Positionable" |
  "Groupable";

export module ChartMessage {
  export const _0CanOnlyBeCreatedFromTheChartWindow = new MessageKey("ChartMessage", "_0CanOnlyBeCreatedFromTheChartWindow");
  export const _0CanOnlyBeCreatedFromTheSearchWindow = new MessageKey("ChartMessage", "_0CanOnlyBeCreatedFromTheSearchWindow");
  export const Chart = new MessageKey("ChartMessage", "Chart");
  export const ChartToken = new MessageKey("ChartMessage", "ChartToken");
  export const Chart_ChartSettings = new MessageKey("ChartMessage", "Chart_ChartSettings");
  export const Chart_Dimension = new MessageKey("ChartMessage", "Chart_Dimension");
  export const DrawChart = new MessageKey("ChartMessage", "DrawChart");
  export const Chart_Group = new MessageKey("ChartMessage", "Chart_Group");
  export const Chart_Query0IsNotAllowed = new MessageKey("ChartMessage", "Chart_Query0IsNotAllowed");
  export const Chart_ToggleInfo = new MessageKey("ChartMessage", "Chart_ToggleInfo");
  export const ColorsFor0 = new MessageKey("ChartMessage", "ColorsFor0");
  export const CreatePalette = new MessageKey("ChartMessage", "CreatePalette");
  export const MyCharts = new MessageKey("ChartMessage", "MyCharts");
  export const CreateNew = new MessageKey("ChartMessage", "CreateNew");
  export const EditUserChart = new MessageKey("ChartMessage", "EditUserChart");
  export const ViewPalette = new MessageKey("ChartMessage", "ViewPalette");
  export const ChartFor = new MessageKey("ChartMessage", "ChartFor");
  export const ChartOf0 = new MessageKey("ChartMessage", "ChartOf0");
  export const _0IsKeyBut1IsAnAggregate = new MessageKey("ChartMessage", "_0IsKeyBut1IsAnAggregate");
  export const _0ShouldBeAnAggregate = new MessageKey("ChartMessage", "_0ShouldBeAnAggregate");
  export const _0ShouldBeSet = new MessageKey("ChartMessage", "_0ShouldBeSet");
  export const _0ShouldBeNull = new MessageKey("ChartMessage", "_0ShouldBeNull");
  export const _0IsNot1 = new MessageKey("ChartMessage", "_0IsNot1");
  export const _0IsAnAggregateButTheChartIsNotGrouping = new MessageKey("ChartMessage", "_0IsAnAggregateButTheChartIsNotGrouping");
  export const _0IsNotOptional = new MessageKey("ChartMessage", "_0IsNotOptional");
  export const SavePalette = new MessageKey("ChartMessage", "SavePalette");
  export const NewPalette = new MessageKey("ChartMessage", "NewPalette");
  export const Data = new MessageKey("ChartMessage", "Data");
  export const ChooseABasePalette = new MessageKey("ChartMessage", "ChooseABasePalette");
  export const DeletePalette = new MessageKey("ChartMessage", "DeletePalette");
  export const Preview = new MessageKey("ChartMessage", "Preview");
}

export const ChartPaletteModel = new Type<ChartPaletteModel>("ChartPaletteModel");
export interface ChartPaletteModel extends Entities.ModelEntity {
  Type: "ChartPaletteModel";
  type?: Basics.TypeEntity | null;
  colors: Entities.MList<ChartColorEntity>;
}

export const ChartParameterEmbedded = new Type<ChartParameterEmbedded>("ChartParameterEmbedded");
export interface ChartParameterEmbedded extends Entities.EmbeddedEntity {
  Type: "ChartParameterEmbedded";
  name?: string | null;
  value?: string | null;
}

export const ChartParameterType = new EnumType<ChartParameterType>("ChartParameterType");
export type ChartParameterType =
  "Enum" |
  "Number" |
  "String";

export module ChartPermission {
  export const ViewCharting : Authorization.PermissionSymbol = registerSymbol("Permission", "ChartPermission.ViewCharting");
}

export const ChartRequestModel = new Type<ChartRequestModel>("ChartRequestModel");
export interface ChartRequestModel extends Entities.ModelEntity {
  Type: "ChartRequestModel";
  chartScript: ChartScriptSymbol;
  columns: Entities.MList<ChartColumnEmbedded>;
  parameters: Entities.MList<ChartParameterEmbedded>;
  invalidator: boolean;
}

export const ChartScriptSymbol = new Type<ChartScriptSymbol>("ChartScript");
export interface ChartScriptSymbol extends Entities.Symbol {
  Type: "ChartScript";
}

export module D3ChartScript {
  export const Bars : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.Bars");
  export const Columns : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.Columns");
  export const Line : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.Line");
  export const MultiBars : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.MultiBars");
  export const MultiColumns : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.MultiColumns");
  export const MultiLines : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.MultiLines");
  export const StackedBars : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.StackedBars");
  export const StackedColumns : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.StackedColumns");
  export const StackedLines : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.StackedLines");
  export const Pie : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.Pie");
  export const BubblePack : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.BubblePack");
  export const Scatterplot : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.Scatterplot");
  export const Bubbleplot : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.Bubbleplot");
  export const ParallelCoordinates : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.ParallelCoordinates");
  export const Punchcard : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.Punchcard");
  export const CalendarStream : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.CalendarStream");
  export const Treemap : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.Treemap");
}

export module GoogleMapsCharScript {
  export const Heatmap : ChartScriptSymbol = registerSymbol("ChartScript", "GoogleMapsCharScript.Heatmap");
  export const Markermap : ChartScriptSymbol = registerSymbol("ChartScript", "GoogleMapsCharScript.Markermap");
}

export const UserChartEntity = new Type<UserChartEntity>("UserChart");
export interface UserChartEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "UserChart";
  query: Basics.QueryEntity;
  entityType: Entities.Lite<Basics.TypeEntity> | null;
  hideQuickLink: boolean;
  owner: Entities.Lite<Entities.Entity> | null;
  displayName: string;
  chartScript: ChartScriptSymbol;
  parameters: Entities.MList<ChartParameterEmbedded>;
  columns: Entities.MList<ChartColumnEmbedded>;
  filters: Entities.MList<UserQueries.QueryFilterEmbedded>;
  guid: string;
  invalidator: boolean;
}

export module UserChartOperation {
  export const Save : Entities.ExecuteSymbol<UserChartEntity> = registerSymbol("Operation", "UserChartOperation.Save");
  export const Delete : Entities.DeleteSymbol<UserChartEntity> = registerSymbol("Operation", "UserChartOperation.Delete");
}


