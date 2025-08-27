import * as d3 from "d3"
import { EntityData, EntityKind } from '@framework/Reflection'
import React from "react";


export const getColorProviders: Array<(info: SchemaMapInfo) => Promise<ClientColorProvider[]>> = [];

export function getAllProviders(info: SchemaMapInfo): Promise<ClientColorProvider[]> {
  return Promise.all(getColorProviders.map(func => func(info))).then(result => result.filter(ps => !!ps).flatMap(ps => ps).filter(p => !!p));
}

export interface ClientColorProvider {
  name: string;
  getFill: (t: ITableInfo) => string;
  getStroke?: (t: ITableInfo) => string;
  getTooltip: (t: ITableInfo) => string;
  getMask?: (t: ITableInfo) => string | undefined;
  defs?: React.JSX.Element[];
}

export interface TableInfo extends ITableInfo {
  typeName: string;
  mlistTables: MListTableInfo[];
}


export interface MListTableInfo extends ITableInfo {
}

export interface ITableInfo extends d3.SimulationNodeDatum, Rectangle {
  tableName: string;
  niceName: string;
  columns: number;
  sql: number | null;
  rows: number | null;
  total_size_kb: number | null;
  rows_history: number | null;
  total_size_kb_history: number | null;
  entityKind: EntityKind;
  entityData: EntityData;
  entityBaseType: EntityBaseType;
  namespace: string;
  nx: number;
  ny: number;
  width: number;
  height: number;
  lineHeight: number;
  extra: { [key: string]: any };
}

export interface Point {
  x?: number; //Realy not nullable, but d3.d.ts
  y?: number; //Realy not nullable, but d3.d.ts
}


export interface Rectangle extends Point {
  width: number;
  height: number;
}

export type EntityBaseType =
  "EnumEntity" |
  "Symbol" |
  "SemiSymbol" |
  "Entity" |
  "MList" |
  "Part";

export interface IRelationInfo extends d3.SimulationLinkDatum<ITableInfo> {
  isMList?: boolean;
  repetitions: number;
  sourcePoint: Point;
  targetPoint: Point;
}


export interface RelationInfo extends IRelationInfo {
  fromTable: string;
  toTable: string;
  nullable: boolean;
  lite: boolean;
  isVirtualMListBackReference?: boolean;
}

export interface MListRelationInfo extends IRelationInfo {
}

export interface ColorProviderInfo {
  name: string;
  niceName: string;
}

export interface SchemaMapInfo {
  tables: TableInfo[];
  relations: RelationInfo[];
  providers: ColorProviderInfo[];

  allNodes: ITableInfo[]; /*after*/
  allLinks: IRelationInfo[]; /*after*/
}
