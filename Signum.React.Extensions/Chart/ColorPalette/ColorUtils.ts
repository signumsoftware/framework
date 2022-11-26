import * as d3 from "d3"
import * as d3sc from "d3-scale-chromatic";
import { MemoRepository } from "../D3Scripts/Components/ReactChart";

interface InterpolatorGroup {
  categoryName: string;
  interpolators: Interpolator[]
}

interface Interpolator {
  name: string;
  interpolate: (value: number) => string;
}

export const colorInterpolatorGroups: InterpolatorGroup[] = [
  {
    categoryName: "Diverging",
    interpolators: [
      { name: "BrBG", interpolate: d3sc.interpolateBrBG },
      { name: "PRGn", interpolate: d3sc.interpolatePRGn },
      { name: "PiYG", interpolate: d3sc.interpolatePiYG },
      { name: "PuOr", interpolate: d3sc.interpolatePuOr },
      { name: "RdBu", interpolate: d3sc.interpolateRdBu },
      { name: "RdGy", interpolate: d3sc.interpolateRdGy },
      { name: "RdYlBu", interpolate: d3sc.interpolateRdYlBu },
      { name: "RdYlGn", interpolate: d3sc.interpolateRdYlGn },
      { name: "Spectral", interpolate: d3sc.interpolateSpectral },
    ]
  },
  {
    categoryName: "Single Hue",
    interpolators: [
      { name: "Blues", interpolate: d3sc.interpolateBlues },
      { name: "Greens", interpolate: d3sc.interpolateGreens },
      { name: "Greys", interpolate: d3sc.interpolateGreys },
      { name: "Oranges", interpolate: d3sc.interpolateOranges },
      { name: "Purples", interpolate: d3sc.interpolatePurples },
      { name: "Reds", interpolate: d3sc.interpolateReds },
    ]
  },
  {
    categoryName: "Multi-Hue",
    interpolators: [
      { name: "Turbo", interpolate: d3sc.interpolateTurbo },
      { name: "Viridis", interpolate: d3sc.interpolateViridis },
      { name: "Inferno", interpolate: d3sc.interpolateInferno },
      { name: "Magma", interpolate: d3sc.interpolateMagma },
      { name: "Plasma", interpolate: d3sc.interpolatePlasma },
      { name: "Cividis", interpolate: d3sc.interpolateCividis },
      { name: "Warm", interpolate: d3sc.interpolateWarm },
      { name: "Cool", interpolate: d3sc.interpolateCool },
      { name: "CubehelixDefault", interpolate: d3sc.interpolateCubehelixDefault },

      { name: "BuGn", interpolate: d3sc.interpolateBuGn },
      { name: "BuPu", interpolate: d3sc.interpolateBuPu },
      { name: "GnBu", interpolate: d3sc.interpolateGnBu },
      { name: "OrRd", interpolate: d3sc.interpolateOrRd },
      { name: "PuBuGn", interpolate: d3sc.interpolatePuBuGn },
      { name: "PuBu", interpolate: d3sc.interpolatePuBu },
      { name: "PuRd", interpolate: d3sc.interpolatePuRd },
      { name: "RdPu", interpolate: d3sc.interpolateRdPu },
      { name: "YlGnBu", interpolate: d3sc.interpolateYlGnBu },
      { name: "YlGn", interpolate: d3sc.interpolateTurbo },
      { name: "YlOrBr", interpolate: d3sc.interpolateYlOrBr },
      { name: "YlOrRd", interpolate: d3sc.interpolateYlOrRd },
    ]
  },
  {
    categoryName: "Cyclical",
    interpolators: [
      { name: "Rainbow", interpolate: d3sc.interpolateRainbow },
      { name: "Sidebow", interpolate: d3sc.interpolateSinebow },
    ]
  }
];

export let allInterpolators: undefined | { [key: string]: Interpolator } = undefined;

//https://github.com/d3/d3-scale-chromatic
export function getColorInterpolation(interpolationName: string | undefined | null): ((value: number) => string) | undefined {

  if (allInterpolators == null)
    allInterpolators = colorInterpolatorGroups.flatMap(a => a.interpolators).toObject(a => a.name);


  return interpolationName ? allInterpolators[interpolationName]?.interpolate : undefined;
}


//https://fluentcolors.com/
export const fluentColors: ReadonlyArray<string> = [
  "#FFB900", "#E74856", "#0078D7", "#0099BC", "#7A7574", "#767676", "#FF8C00", "#E81123", "#0063B1", "#2D7D9A", "#5D5A58", "#4C4A48", "#F7630C", "#EA005E",
  "#8E8CD8", "#00B7C3", "#68768A", "#69797E", "#CA5010", "#C30052", "#6B69D6", "#038387", "#515C6B", "#4A5459", "#DA3B01", "#E3008C", "#8764B8", "#00B294",
  "#567C73", "#647C64", "#EF6950", "#BF0077", "#744DA9", "#018574", "#486860", "#525E54", "#D13438", "#C239B3", "#B146C2", "#00CC6A", "#498205", "#847545",
  "#FF4343", "#9A0089", "#881798", "#10893E", "#107C10", "#7E735F"
];

//https://materialui.co/flatuicolors/
export const flatUIColors: ReadonlyArray<string> = [

  "#1ABC9C", "#2ECC71", "#3498DB", "#9B59B6", "#34495E", "#16A085", "#27AE60", "#2980B9", "#8E44AD", "#2C3E50", "#F1C40F", "#E67E22", "#E74C3C", "#ECF0F1",
  "#95A5A6", "#F39C12", "#D35400", "#C0392B", "#BDC3C7", "#7F8C8D"
];

//https://materialui.co/metrocolors/
export const metroColors: ReadonlyArray<string> = [

  "#A4C400", "#60A917", "#008A00", "#00ABA9", "#1BA1E2", "#0050EF", "#6A00FF", "#AA00FF", "#F472D0", "#D80073", "#A20025", "#E51400", "#FA6800", "#F0A30A",
  "#E3C800", "#825A2C", "#6D8764", "#647687", "#76608A", "#A0522D",
];


export const materialColors: ReadonlyArray<string> = [

  "#E53935", "#D81B60", "#8E24AA", "#5E35B1", "#3949AB", "#1E88E5", "#039BE5", "#00ACC1", "#00897B", "#43A047", "#7CB342", "#C0CA33", "#FDD835", "#FFB300",
  "#FB8C00", "#F4511E", "#6D4C41", "#757575", "#546E7A"
];


export const colorSchemes: { [name: string]: ReadonlyArray<string> } = {
  "fluent": fluentColors,
  "flatui": flatUIColors,
  "metro": metroColors,
  "material": materialColors,
  "category10": d3.schemeCategory10,
  "accent": d3sc.schemeAccent,
  "dark2": d3sc.schemeDark2,
  "paired": d3sc.schemePaired,
  "pastel1": d3sc.schemePastel1,
  "pastel2": d3sc.schemePastel2,
  "set1": d3sc.schemeSet1,
  "set2": d3sc.schemeSet2,
  "set3": d3sc.schemeSet3,
  "tableau10": d3sc.schemeTableau10,
};

