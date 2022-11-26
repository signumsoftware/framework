import { ajaxPost, ajaxGet } from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { ChartMessage, ColorPaletteEntity } from '../Signum.Entities.Chart'
import * as ColorUtils from './ColorUtils'
import { PseudoType, getTypeName, tryGetTypeInfo } from '@framework/Reflection';
import { Lite } from '@framework/Signum.Entities';

export function start(options: { routes: JSX.Element[] }) {
  Navigator.addSettings(new EntitySettings(ColorPaletteEntity, e => import('./ColorPalette')));
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

  const typeName = getTypeName(type);

  if (colorPalette[typeName] !== undefined)
    return colorPalette[typeName];

  return colorPalette[typeName] = API.colorPalette(typeName).then(pal => {
    if (pal == null)
      return pal;

    pal.cachedColors = {};
    pal.palette = ColorUtils.colorSchemes[pal.categoryName];

    if (pal.palette == null)
      throw new Error("Inavlid ColorPaletter categoryName: " + pal.categoryName);

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

  var hc = hashCode(key);

  if (hc < 0)
    hc = -hc;

  color = this.palette[(hc + this.seed) % this.palette.length];

  return this.cachedColors[key] = color;
}


function hashCode(str: string) {
  var hash: any = 0;
  for (let i = 0; i < str.length; i++) {
    hash = ((hash << 5) - hash) + str[i];
    hash |= 0; // Convert to 32bit integer
  }
  return hash;
}


export module API {

  export function colorPalette(typeName: string): Promise<ColorPalette> {
    return ajaxGet({ url: `~/api/colorPalette/${typeName}`, });
  }
}
