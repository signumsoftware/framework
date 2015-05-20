/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import d3 = require("d3")
import Map = require("Extensions/Signum.Web.Extensions/Maps/Scripts/SchemaMap")

export function authAdminColor(nodes: Map.ITableInfo[], key: string): Map.ColorProvider {
    return {
        getFill: t => t.extra[key + "-db"] == null ? "white" : "url(#" + t.extra[key + "-db"] + ")",
        getStroke: t => t.extra[key + "-ui"] == null ? "white" : "url(#" + t.extra[key + "-ui"] + ")",
        getTooltip: t => t.extra[key + "-tooltip"] == null ? null : t.extra[key + "-tooltip"]
    };
}