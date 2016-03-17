//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection' 

import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities' 

import * as Authorization from '../Authorization/Signum.Entities.Authorization' 

import * as UserQueries from '../UserQueries/Signum.Entities.UserQueries' 

import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets' 

import * as Files from '../Files/Signum.Entities.Files' 

import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics' 

import { FilterOption, OrderOption, FilterRequest, OrderRequest } from '../../../Framework/Signum.React/Scripts/FindOptions' 

//Partial
export interface ChartRequest {
    queryKey?: string;

	filterOptions?: FilterOption[];
    orderOptions?: OrderOption[];

    filters?: FilterRequest[];
    orders?: OrderRequest[];
}

export interface ChartScriptParameterEntity {
    enumValues: { name: string, typeFilter : ChartColumnType }[];
}

export type IChartBase = ChartRequest | UserChartEntity;
export const ChartColorEntity_Type = new Type<ChartColorEntity>("ChartColor");
export interface ChartColorEntity extends Entities.Entity {
    related: Entities.Lite<Entities.Entity>;
    color: Basics.ColorEntity;
}

export const ChartColumnEntity_Type = new Type<ChartColumnEntity>("ChartColumnEntity");
export interface ChartColumnEntity extends Entities.EmbeddedEntity {
    token: UserAssets.QueryTokenEntity;
    displayName: string;
}

export enum ChartColumnType {
    Integer = "Integer" as any,
    Real = "Real" as any,
    Date = "Date" as any,
    DateTime = "DateTime" as any,
    String = "String" as any,
    Lite = "Lite" as any,
    Enum = "Enum" as any,
    RealGroupable = "RealGroupable" as any,
    Groupable = "Groupable" as any,
    Magnitude = "Magnitude" as any,
    Positionable = "Positionable" as any,
}
export const ChartColumnType_Type = new EnumType<ChartColumnType>("ChartColumnType", ChartColumnType);

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

export const ChartPaletteModel_Type = new Type<ChartPaletteModel>("ChartPaletteModel");
export interface ChartPaletteModel extends Entities.ModelEntity {
    type: Basics.TypeEntity;
    colors: Entities.MList<ChartColorEntity>;
}

export const ChartParameterEntity_Type = new Type<ChartParameterEntity>("ChartParameterEntity");
export interface ChartParameterEntity extends Entities.EmbeddedEntity {
    name: string;
    value: string;
}

export enum ChartParameterType {
    Enum = "Enum" as any,
    Number = "Number" as any,
    String = "String" as any,
}
export const ChartParameterType_Type = new EnumType<ChartParameterType>("ChartParameterType", ChartParameterType);

export module ChartPermission {
    export const ViewCharting : Authorization.PermissionSymbol = registerSymbol({ Type: "Permission", key: "ChartPermission.ViewCharting" });
}

export const ChartRequest_Type = new Type<ChartRequest>("ChartRequest");
export interface ChartRequest extends Entities.ModelEntity {
    chartScript: ChartScriptEntity;
    groupResults: boolean;
    columns: Entities.MList<ChartColumnEntity>;
    parameters: Entities.MList<ChartParameterEntity>;
    invalidator: boolean;
}

export const ChartScriptColumnEntity_Type = new Type<ChartScriptColumnEntity>("ChartScriptColumnEntity");
export interface ChartScriptColumnEntity extends Entities.EmbeddedEntity {
    displayName: string;
    isOptional: boolean;
    columnType: ChartColumnType;
    isGroupKey: boolean;
}

export const ChartScriptEntity_Type = new Type<ChartScriptEntity>("ChartScript");
export interface ChartScriptEntity extends Entities.Entity {
    name: string;
    icon: Entities.Lite<Files.FileEntity>;
    script: string;
    groupBy: GroupByChart;
    columns: Entities.MList<ChartScriptColumnEntity>;
    parameters: Entities.MList<ChartScriptParameterEntity>;
    columnsStructure: string;
}

export module ChartScriptOperation {
    export const Save : Entities.ExecuteSymbol<ChartScriptEntity> = registerSymbol({ Type: "Operation", key: "ChartScriptOperation.Save" });
    export const Clone : Entities.ConstructSymbol_From<ChartScriptEntity, ChartScriptEntity> = registerSymbol({ Type: "Operation", key: "ChartScriptOperation.Clone" });
    export const Delete : Entities.DeleteSymbol<ChartScriptEntity> = registerSymbol({ Type: "Operation", key: "ChartScriptOperation.Delete" });
}

export const ChartScriptParameterEntity_Type = new Type<ChartScriptParameterEntity>("ChartScriptParameterEntity");
export interface ChartScriptParameterEntity extends Entities.EmbeddedEntity {
    name: string;
    type: ChartParameterType;
    columnIndex: number;
    valueDefinition: string;
}

export enum GroupByChart {
    Always = "Always" as any,
    Optional = "Optional" as any,
    Never = "Never" as any,
}
export const GroupByChart_Type = new EnumType<GroupByChart>("GroupByChart", GroupByChart);

export const UserChartEntity_Type = new Type<UserChartEntity>("UserChart");
export interface UserChartEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
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

