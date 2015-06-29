/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/d3/d3.d.ts"/>

import d3 = require("d3")
import Map = require("Extensions/Signum.Web.Extensions/Maps/Scripts/SchemaMap")

export function namespace(nodes: Map.ITableInfo[]): Map.ColorProvider {
    var color = d3.scale.category20();

    return {
        getFill: t => color(t.namespace),
        getTooltip: t => t.namespace
    };
}


export function tableSize(nodes: Map.ITableInfo[]): Map.ColorProvider {

    var color = Map.colorScaleSqr(nodes.map(a=> a.total_size_kb).max());

    return {
        getFill:t => <any> color(t.total_size_kb),
        getTooltip: t => t.total_size_kb + " KB"
    };
}


export function rows(nodes: Map.ITableInfo[], rowsText: string): Map.ColorProvider{

    var color = Map.colorScaleSqr(nodes.map(a=> a.rows).max());

    return {
        getFill: t => <any>color(t.rows),
        getTooltip: t => t.rows + " " + rowsText
    };
}

export function columns(nodes: Map.ITableInfo[], columnsText: string): Map.ColorProvider {

    var color = Map.colorScaleSqr(nodes.map(a=> a.columns).max());

    return {
        getFill: t => <any>color(t.columns),
        getTooltip: t => t.columns + " " + columnsText
    };
}


export function entityKind(nodes: Map.ITableInfo[]): Map.ColorProvider {
    var f: { [ek: number]: string } = {};

    f[Map.EntityKind.SystemString] = "#8c564b";
    f[Map.EntityKind.System] = "#7f7f7f";
    f[Map.EntityKind.Relational] = "#17becf";
    f[Map.EntityKind.String] = "#e377c2";
    f[Map.EntityKind.Shared] = "#2ca02c";
    f[Map.EntityKind.Main] = "#d62728";
    f[Map.EntityKind.Part] = "#ff7f0e";
    f[Map.EntityKind.SharedPart] = "#bcbd22";

    return {
        getFill: t=> f[t.entityKind],
        getTooltip: t => Map.EntityKind[t.entityKind]
    };
}


export function entityData(nodes: Map.ITableInfo[]): Map.ColorProvider{
    return {
        getFill: t =>
            t.entityData == Map.EntityData.Master ? "#2ca02c" :
                t.entityData == Map.EntityData.Transactional ? "#d62728" : "black",
        getTooltip: t => Map.EntityData[t.entityData]
    };
}