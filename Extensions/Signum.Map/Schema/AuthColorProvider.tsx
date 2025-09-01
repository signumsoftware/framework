import * as React from 'react'
import { ClientColorProvider, SchemaMapInfo } from './ClientColorProvider';
import { BasicPermission, TypeAllowedBasic } from '../../Signum.Authorization/Rules/Signum.Authorization.Rules';
import { isPermissionAuthorized } from '@framework/AppContext';
import { JSX } from 'react';

export default function getDefaultProviders(info: SchemaMapInfo): ClientColorProvider[] {
  if (!isPermissionAuthorized(BasicPermission.AdminRules))
    return [];

  return info.providers.filter(p => p.name.startsWith("role-")).map((p, i) => ({
    name: p.name,
    getFill: t => t.extra[p.name + "-db"] == undefined ? "white" : "url(#" + t.extra[p.name + "-db"] + ")",
    getStroke: t => t.extra[p.name + "-ui"] == undefined ? "white" : "url(#" + t.extra[p.name + "-ui"] + ")",
    getTooltip: t => t.extra[p.name + "-tooltip"] == undefined ? undefined : t.extra[p.name + "-tooltip"],
    defs: i == 0 ? getDefs(info) : undefined
  }) as ClientColorProvider);
}

function getDefs(info: SchemaMapInfo): JSX.Element[] {
  const roles = info.providers.filter(p => p.name.startsWith("role-")).map(a => a.name);

  const distinctValues = info.tables.flatMap(t => roles.flatMap(r => [
    t.extra[r + "-db"] as string,
    t.extra[r + "-ui"] as string]))
    .filter(a => a != undefined)
    .groupBy(a => a)
    .map(gr => gr.key);

  return distinctValues.map(val => gradient(val))
}


function gradient(name: string) {

  const list = name.after("auth-").split("-").map(a => a as TypeAllowedBasic | "Error");

  return (
    <linearGradient id={name} x1="0%" y1="0% " x2="100% " y2="0%">
      {list.map((l, i) => [
        <stop key={i} offset={(100 * i / list.length) + "%"} stopColor={color(l)} />,
        <stop key={i + "b"} offset={((100 * (i + 1) / list.length) - 1) + "%"} stopColor={color(l)} />])
      }
    </linearGradient>
  );
}


function color(typeAllowedBasic: TypeAllowedBasic | "Error"): string {
  switch (typeAllowedBasic) {
    case undefined: return "var(--bs-body-color)";
    case "Write": return "var(--bs-green)";
    case "Read": return "var(--bs-yellow)";
    case "None": return "var(--bs-red)";
    case "Error": return "var(--bs-magenta)";
    default: throw new Error();
  }
}
