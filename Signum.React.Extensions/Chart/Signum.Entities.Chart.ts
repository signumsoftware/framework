//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as Files from '../Files/Signum.Entities.Files'
import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets'
import * as UserQueries from '../UserQueries/Signum.Entities.UserQueries'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'



import { FilterOptionParsed, OrderOptionParsed, FilterRequest, OrderRequest } from '../../../Framework/Signum.React/Scripts/FindOptions' 

//Partial
export interface ChartRequest {
    queryKey: string;

	filterOptions: FilterOptionParsed[];
    orderOptions: OrderOptionParsed[];

    filters: FilterRequest[];
    orders: OrderRequest[];
}

export interface ChartScriptParameterEntity {
    enumValues: { name: string, typeFilter : ChartColumnType }[];
}

export type IChartBase = ChartRequest | UserChartEntity;
export const ChartColorEntity = new Type<ChartColorEntity>("ChartColor");
export interface ChartColorEntity extends Entities.Entity {
    Type: "ChartColor";
    related?: Entities.Lite<Entities.Entity>;
    color?: Basics.ColorEntity | null;
}

export const ChartColumnEntity = new Type<ChartColumnEntity>("ChartColumnEntity");
export interface ChartColumnEntity extends Entities.EmbeddedEntity {
    Type: "ChartColumnEntity";
    token?: UserAssets.QueryTokenEntity | null;
    displayName?: string | null;
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
    "Groupable" |
    "Magnitude" |
    "Positionable";

export module ChartMessage {
    export const _0CanOnlyBeCreatedFromTheChartWindow = new MessageKey("ChartMessage", "_0CanOnlyBeCreatedFromTheChartWindow");
    export const _0CanOnlyBeCreatedFromTheSearchWindow = new MessageKey("ChartMessage", "_0CanOnlyBeCreatedFromTheSearchWindow");
    export const Chart = new MessageKey("ChartMessage", "Chart");
    export const ChartToken = new MessageKey("ChartMessage", "ChartToken");
    export const Chart_ChartSettings = new MessageKey("ChartMessage", "Chart_ChartSettings");
    export const Chart_Dimension = new MessageKey("ChartMessage", "Chart_Dimension");
    export const Chart_Draw = new MessageKey("ChartMessage", "Chart_Draw");
    export const Chart_Group = new MessageKey("ChartMessage", "Chart_Group");
    export const Chart_Query0IsNotAllowed = new MessageKey("ChartMessage", "Chart_Query0IsNotAllowed");
    export const Chart_ToggleInfo = new MessageKey("ChartMessage", "Chart_ToggleInfo");
    export const EditScript = new MessageKey("ChartMessage", "EditScript");
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

export const ChartParameterEntity = new Type<ChartParameterEntity>("ChartParameterEntity");
export interface ChartParameterEntity extends Entities.EmbeddedEntity {
    Type: "ChartParameterEntity";
    name?: string | null;
    value?: string | null;
}

export const ChartParameterType = new EnumType<ChartParameterType>("ChartParameterType");
export type ChartParameterType =
    "Enum" |
    "Number" |
    "String";

export module ChartPermission {
    export const ViewCharting : Authorization.PermissionSymbol = registerSymbol({ Type: "Permission", key: "ChartPermission.ViewCharting" });
}

export const ChartRequest = new Type<ChartRequest>("ChartRequest");
export interface ChartRequest extends Entities.ModelEntity {
    Type: "ChartRequest";
    chartScript: ChartScriptEntity;
    groupResults: boolean;
    columns: Entities.MList<ChartColumnEntity>;
    parameters: Entities.MList<ChartParameterEntity>;
    invalidator: boolean;
}

export const ChartScriptColumnEntity = new Type<ChartScriptColumnEntity>("ChartScriptColumnEntity");
export interface ChartScriptColumnEntity extends Entities.EmbeddedEntity {
    Type: "ChartScriptColumnEntity";
    displayName?: string | null;
    isOptional?: boolean;
    columnType?: ChartColumnType;
    isGroupKey?: boolean;
}

export const ChartScriptEntity = new Type<ChartScriptEntity>("ChartScript");
export interface ChartScriptEntity extends Entities.Entity {
    Type: "ChartScript";
    name?: string | null;
    icon?: Entities.Lite<Files.FileEntity>;
    script?: string | null;
    groupBy?: GroupByChart;
    columns: Entities.MList<ChartScriptColumnEntity>;
    parameters: Entities.MList<ChartScriptParameterEntity>;
    columnsStructure?: string | null;
}

export module ChartScriptOperation {
    export const Save : Entities.ExecuteSymbol<ChartScriptEntity> = registerSymbol({ Type: "Operation", key: "ChartScriptOperation.Save" });
    export const Clone : Entities.ConstructSymbol_From<ChartScriptEntity, ChartScriptEntity> = registerSymbol({ Type: "Operation", key: "ChartScriptOperation.Clone" });
    export const Delete : Entities.DeleteSymbol<ChartScriptEntity> = registerSymbol({ Type: "Operation", key: "ChartScriptOperation.Delete" });
}

export const ChartScriptParameterEntity = new Type<ChartScriptParameterEntity>("ChartScriptParameterEntity");
export interface ChartScriptParameterEntity extends Entities.EmbeddedEntity {
    Type: "ChartScriptParameterEntity";
    name?: string | null;
    type?: ChartParameterType;
    columnIndex?: number | null;
    valueDefinition?: string | null;
}

export const GroupByChart = new EnumType<GroupByChart>("GroupByChart");
export type GroupByChart =
    "Always" |
    "Optional" |
    "Never";

export const UserChartEntity = new Type<UserChartEntity>("UserChart");
export interface UserChartEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
    Type: "UserChart";
    query: Basics.QueryEntity;
    entityType: Entities.Lite<Basics.TypeEntity>;
    owner: Entities.Lite<Entities.Entity>;
    displayName: string;
    chartScript: ChartScriptEntity;
    parameters: Entities.MList<ChartParameterEntity>;
    groupResults: boolean;
    columns: Entities.MList<ChartColumnEntity>;
    filters: Entities.MList<UserQueries.QueryFilterEntity>;
    orders: Entities.MList<UserQueries.QueryOrderEntity>;
    guid: string;
    invalidator: boolean;
}

export module UserChartOperation {
    export const Save : Entities.ExecuteSymbol<UserChartEntity> = registerSymbol({ Type: "Operation", key: "UserChartOperation.Save" });
    export const Delete : Entities.DeleteSymbol<UserChartEntity> = registerSymbol({ Type: "Operation", key: "UserChartOperation.Delete" });
}


