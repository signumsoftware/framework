/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import d3 = require("d3")
import Map = require("Extensions/Signum.Web.Extensions/Maps/Scripts/SchemaMap")

export function isolationColors(nodes: Map.ITableInfo[]): Map.ColorProvider {

    return {
        getFill: t => t.extra["isolation"] == null ? "white" :
            t.extra["isolation"] == "Isolated" ? "#CC0099" :
            t.extra["isolation"] == "Optional" ? "#9966FF" :
            t.extra["isolation"] == "None" ? "#00CCFF" : "black",
        getTooltip: t => t.extra["isolation"] == null ? null : t.extra["isolation"]
    };
}