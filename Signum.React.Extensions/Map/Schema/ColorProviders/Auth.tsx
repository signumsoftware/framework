import * as React from 'react'
import * as ReactDOM from 'react-dom'
import * as d3 from 'd3'
import { ClientColorProvider, SchemaMapInfo  } from '../SchemaMap'
import { TypeAllowedBasic, BasicPermission } from '../../../Authorization/Signum.Entities.Authorization.ts'
import { isPermissionAuthorized } from '../../../Authorization/AuthClient'
import { colorScale, colorScaleSqr  } from '../../Utils'

export default function getDefaultProviders(info: SchemaMapInfo): ClientColorProvider[] {   
    
    if (!isPermissionAuthorized(BasicPermission.AdminRules))
        return null;

    return info.providers.filter(p => p.name.startsWith("role-")).map((p, i) => ({
        name: p.name,
        getFill: t => t.extra[p.name + "-db"] == null ? "white" : "url(#" + t.extra[p.name + "-db"] + ")",
        getStroke: t => t.extra[p.name + "-ui"] == null ? "white" : "url(#" + t.extra[p.name + "-ui"] + ")",
        getTooltip: t => t.extra[p.name + "-tooltip"] == null ? null : t.extra[p.name + "-tooltip"],
        defs: i == 0 ? getDefs(info) : null
    }));
}


function getDefs(info: SchemaMapInfo): JSX.Element[]{
    var roles = info.providers.filter(p => p.name.startsWith("role-")).map(a => a.name);

    var distinctValues = info.tables.flatMap(t => roles.flatMap(r => [
        t.extra[r + "-db"] as string,
        t.extra[r + "-ui"] as string]))
        .filter(a => a != null)
        .groupBy(a => a)
        .map(gr => gr.key);

    return distinctValues.map(val => gradient(val))
}


function gradient(name: string) {

    var list = name.after("auth-").split("-").map(a=>a as TypeAllowedBasic);

    return (
        <linearGradient id={name} x1="0%" y1="0% " x2="100% " y2="0%">
        { list.map((l, i) => [
            <stop offset={ (100 * i / list.length)  +  "%" } stopColor={color(l) } />,
            <stop offset={ ((100 * (i + 1) / list.length) - 1) + "%" } stopColor={color(l) } />]) 
        }
        </linearGradient>
    );
}


function color(typeAllowedBasic: TypeAllowedBasic) : string
{
    switch (typeAllowedBasic)
    {
        case null: return "black";
        case "Create": return "#0066FF";
        case "Modify": return "green";
        case "Read": return "gold";
        case "None": return "red";
        default: throw new Error();
    }
}