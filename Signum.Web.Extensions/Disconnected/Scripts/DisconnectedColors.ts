/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import d3 = require("d3")
import Map = require("Extensions/Signum.Web.Extensions/Maps/Scripts/Map")

export function disconnectedColors(nodes: Map.ITableInfo[]): Map.ColorProvider {

    return {
        getColor: t => t.extra["disc-upload"] == null ? "white" : "url(#disconnected-" + t.extra["disc-upload"] + "-" + t.extra["disc-download"] + ")",
        getTooltip: t => t.extra["disc-upload"] == null ? "" : "Download " + t.extra["disc-download"] + " - " + "Upload " + t.extra["disc-upload"]
    };
}