import { ajaxGet } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import * as React from 'react';
import { RouteObject } from 'react-router'
import * as AppContext from '@framework/AppContext'
import * as ColorUtils from './ColorUtils'
import { PseudoType, getTypeInfo, getTypeName } from '@framework/Reflection';
import { Lite } from '@framework/Signum.Entities';
import { Constructor } from '@framework/Constructor';
import { Finder } from '@framework/Finder';
import { Dic } from '@framework/Globals';
import { getColorInterpolation } from './ColorUtils';
import { ColorPaletteEntity } from './Signum.Chart.ColorPalette';

export namespace ColorPaletteClient {
  
  export function start(options: { routes: RouteObject[] }): void {
    Navigator.addSettings(new EntitySettings(ColorPaletteEntity, e => import('./ColorPalette')));
  
    Finder.registerPropertyFormatter(ColorPaletteEntity.tryPropertyRoute(a => a.categoryName),
      new Finder.CellFormatter(cat => cat && <span><ColorScheme colorScheme={cat} />{cat}</span>, true));
  
    Constructor.registerConstructor(ColorPaletteEntity, props => ColorPaletteEntity.New({ seed: 0, categoryName: Dic.getKeys(ColorUtils.colorSchemes).first(), ...props }));
  
    Navigator.registerEntityChanged(ColorPaletteEntity, () => Dic.clear(colorPalette));
  
    AppContext.clearSettingsActions.push(() => Dic.clear(colorPalette));
  }
  
  export interface ColorPalette {
    lite: Lite<ColorPaletteEntity>;
    typeName: string;
    categoryName: string;
    seed: number;
    specificColors: { [key: string]: string };
  
    cachedColors: { [key: string]: string };
    palette: ReadonlyArray<string>;
    getColor(key: string): string;
  }
  
  export let colorPalette: { [typeName: string]: Promise<ColorPalette | null> } = {};
  export function getColorPalette(type: PseudoType): Promise<ColorPalette | null> {

    const ti = getTypeInfo(type);

    if (colorPalette[ti.name] !== undefined)
      return colorPalette[ti.name];

    if (ti.noSchema)
      return colorPalette[ti.name] = Promise.resolve(null);

    return colorPalette[ti.name] = API.colorPalette(ti.name).then(pal => {
      if (pal == null)
        return pal;
  
      pal.cachedColors = {};
      pal.palette = ColorUtils.colorSchemes[pal.categoryName];
  
      if (pal.palette == null)
        throw new Error("Invalid ColorPalette categoryName: " + pal.categoryName);
  
      pal.getColor = getColor;
      return pal;
    });
  }
  
  function getColor(this: ColorPalette, key: string) {
  
    let color = this.cachedColors[key];
    if (color != null)
      return color;
  
    color = this.specificColors[key];
    if (color != null) {
      return this.cachedColors[key] = color;
    }
  
    color = calculateColor(key, this.palette, this.seed)
  
    return this.cachedColors[key] = color;
  }
  
  export function calculateColor(key: string, palette: readonly string[], seed: number) : string {
    var hc = hashCode(key);
  
    if (hc < 0)
      hc = -hc;
  
    return palette[(hc + seed) % palette.length];
  }
  
  
  
  function hashCode(s: string): number {
    var h = 0;
    for (let i = 0; i < s.length; i++)
      h = Math.imul(31, h) + s.charCodeAt(i) | 0;
  
    return h;
  }
  
  export namespace API {
  
    export function colorPalette(typeName: string): Promise<ColorPalette> {
      return ajaxGet({ url: `/api/colorPalette/${typeName}`, });
    }
  }
  

  

}

export function ColorScheme(p: { colorScheme: string }): React.JSX.Element {
  return (<div style={{ height: "20px", width: "150px", display: "inline-flex", verticalAlign: "text-bottom" }} className="me-2">
    {ColorUtils.colorSchemes[p.colorScheme]?.map(c => <div key={c} style={{ flex: "1", backgroundColor: c }} />)}
  </div>);
}

export function ColorInterpolate(p: { colorInterpolator: string }): React.JSX.Element {

  const inter = getColorInterpolation(p.colorInterpolator);

  return (<div style={{ height: "20px", width: "150px", display: "inline-flex", verticalAlign: "text-bottom" }} className="me-2">
    {inter && Array.range(0, 10).map(i => <div key={i} style={{ flex: "1", background: `linear-gradient(90deg, ${inter(i / 10)} 0%, ${inter((i + 1) / 10)} 100%)` }} />)}
  </div>);
}
