import * as React from 'react'
import { ClientColorProvider, SchemaMapInfo } from '../SchemaMap'
import { CachePermission } from '../../../Cache/Signum.Entities.Cache'
import { isPermissionAuthorized } from '../../../Authorization/AuthClient'
import { colorScale } from '../../Utils'

export default function getDefaultProviders(info: SchemaMapInfo): ClientColorProvider[] {
  if (!isPermissionAuthorized(CachePermission.ViewCache))
    return [];

  return [
    getColorProvider(info, "cache-rows", "Rows", true),
    getColorProvider(info, "cache-invalidations", "Invalidations", false),
    getColorProvider(info, "cache-loads", "Loads", false),
    getColorProvider(info, "cache-load-time", "ms Load Time", false)
  ];
}

function getColorProvider(info: SchemaMapInfo, name: string, title: string, withDefs: boolean): ClientColorProvider {
  const max = info.tables.map(a => a.extra[name]).filter(n => n != undefined).max()!;
  const color = colorScale(max);

  return {
    name: name,
    getFill: t => t.extra["cache-semi"] == undefined ? "lightgray" : color(t.extra[name]),
    getMask: t => t.extra["cache-semi"] == undefined ? undefined :
      t.extra["cache-semi"] ? "url(#mask-stripe)" : undefined,
    getTooltip: t => t.extra["cache-semi"] == undefined ? "NO Cached" :
      t.extra["cache-semi"] == true ? ("SEMI Cached - " + t.extra[name] + "  " + title) :
        ("Cached - " + t.extra[name] + "  " + title),
    defs: withDefs ? [
      <pattern id="pattern-stripe" width="4" height="4" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">
        <rect width="2" height="4" transform="translate(0, 0)" fill="white"></rect >
      </pattern >,
      <mask id="mask-stripe">
        <rect x="0" y="0" width="100% " height="100% " fill="url(#pattern - stripe)"></rect >
      </mask>
    ] : undefined
  }

}

