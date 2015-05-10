/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import d3 = require("d3")
import Map = require("Extensions/Signum.Web.Extensions/Maps/Scripts/SchemaMap")

export function cacheColors(nodes: Map.ITableInfo[], title: string, key: string): Map.ColorProvider {

    var max = nodes.map(a=> a.extra[key]).filter(n=> n != undefined).max();

    var color = Map.colorScale(max);

    return {
        getFill: t => t.extra["cache-semi"] == undefined ? "lightgray" : <any>color(t.extra[key]),
        getMask: t => t.extra["cache-semi"] == undefined ? null :
            t.extra["cache-semi"] ? "url(#mask-stripe)" : null,
        getTooltip: t => t.extra["cache-semi"] == undefined ? "NO Cached" : 
                    t.extra["cache-semi"] == true ? ("SEMI Cached - " + t.extra[key] + "  " + title) :
                                                    ("Cached - " + t.extra[key] + "  " + title)
      
    };
}