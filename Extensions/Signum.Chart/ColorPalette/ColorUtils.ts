import a from "bpmn-js/lib/features/search";
import * as d3 from "d3"
import * as d3sc from "d3-scale-chromatic";
import { MemoRepository } from "../D3Scripts/Components/ReactChart";


interface Interpolator {
  categoryName: string;
  name: string;
  interpolate: (value: number) => string;
}

//https://github.com/d3/d3-scale-chromatic
export const colorInterpolators: { [name: string]: Interpolator } = [
  { categoryName: "Diverging", name: "BrBG", interpolate: d3sc.interpolateBrBG },
  { categoryName: "Diverging", name: "PRGn", interpolate: d3sc.interpolatePRGn },
  { categoryName: "Diverging", name: "PiYG", interpolate: d3sc.interpolatePiYG },
  { categoryName: "Diverging", name: "PuOr", interpolate: d3sc.interpolatePuOr },
  { categoryName: "Diverging", name: "RdBu", interpolate: d3sc.interpolateRdBu },
  { categoryName: "Diverging", name: "RdGy", interpolate: d3sc.interpolateRdGy },
  { categoryName: "Diverging", name: "RdYlBu", interpolate: d3sc.interpolateRdYlBu },
  { categoryName: "Diverging", name: "RdYlGn", interpolate: d3sc.interpolateRdYlGn },
  { categoryName: "Diverging", name: "Spectral", interpolate: d3sc.interpolateSpectral },

  { categoryName: "Single Hue", name: "Blues", interpolate: d3sc.interpolateBlues },
  { categoryName: "Single Hue", name: "Greens", interpolate: d3sc.interpolateGreens },
  { categoryName: "Single Hue", name: "Greys", interpolate: d3sc.interpolateGreys },
  { categoryName: "Single Hue", name: "Oranges", interpolate: d3sc.interpolateOranges },
  { categoryName: "Single Hue", name: "Purples", interpolate: d3sc.interpolatePurples },
  { categoryName: "Single Hue", name: "Reds", interpolate: d3sc.interpolateReds },

  { categoryName: "Multi-Hue", name: "Turbo", interpolate: d3sc.interpolateTurbo },
  { categoryName: "Multi-Hue", name: "Viridis", interpolate: d3sc.interpolateViridis },
  { categoryName: "Multi-Hue", name: "Inferno", interpolate: d3sc.interpolateInferno },
  { categoryName: "Multi-Hue", name: "Magma", interpolate: d3sc.interpolateMagma },
  { categoryName: "Multi-Hue", name: "Plasma", interpolate: d3sc.interpolatePlasma },
  { categoryName: "Multi-Hue", name: "Cividis", interpolate: d3sc.interpolateCividis },
  { categoryName: "Multi-Hue", name: "Warm", interpolate: d3sc.interpolateWarm },
  { categoryName: "Multi-Hue", name: "Cool", interpolate: d3sc.interpolateCool },
  { categoryName: "Multi-Hue", name: "CubehelixDefault", interpolate: d3sc.interpolateCubehelixDefault },

  { categoryName: "Multi-Hue", name: "BuGn", interpolate: d3sc.interpolateBuGn },
  { categoryName: "Multi-Hue", name: "BuPu", interpolate: d3sc.interpolateBuPu },
  { categoryName: "Multi-Hue", name: "GnBu", interpolate: d3sc.interpolateGnBu },
  { categoryName: "Multi-Hue", name: "OrRd", interpolate: d3sc.interpolateOrRd },
  { categoryName: "Multi-Hue", name: "PuBuGn", interpolate: d3sc.interpolatePuBuGn },
  { categoryName: "Multi-Hue", name: "PuBu", interpolate: d3sc.interpolatePuBu },
  { categoryName: "Multi-Hue", name: "PuRd", interpolate: d3sc.interpolatePuRd },
  { categoryName: "Multi-Hue", name: "RdPu", interpolate: d3sc.interpolateRdPu },
  { categoryName: "Multi-Hue", name: "YlGnBu", interpolate: d3sc.interpolateYlGnBu },
  { categoryName: "Multi-Hue", name: "YlGn", interpolate: d3sc.interpolateTurbo },
  { categoryName: "Multi-Hue", name: "YlOrBr", interpolate: d3sc.interpolateYlOrBr },
  { categoryName: "Multi-Hue", name: "YlOrRd", interpolate: d3sc.interpolateYlOrRd },

  { categoryName: "Cyclical", name: "Rainbow", interpolate: d3sc.interpolateRainbow },
  { categoryName: "Cyclical", name: "Sidebow", interpolate: d3sc.interpolateSinebow },
].toObject(a => a.name);


export function getColorInterpolation(interpolationName: string | undefined | null): ((value: number) => string) | undefined {

  if (interpolationName == null)
    return undefined;

  var inver = interpolationName.startsWith("-");

  if (inver)
    interpolationName = interpolationName.after("-");

  var interp = colorInterpolators[interpolationName]?.interpolate;

  if (inver)
    return val => interp(1 - val);

  return interp;
}





//https://fluentcolors.com/
export const fluentColors: ReadonlyArray<string> = [
  "#FFB900", 
  "#FF8C00", 
  "#F7630C", 
  "#CA5010", 
  "#DA3B01", 
  "#EF6950", 
  "#D13438", 
  "#FF4343", 
  "#E74856", 
  "#E81123", 
  "#EA005E", 
  "#C30052", 
  "#E3008C", 
  "#BF0077", 
  "#C239B3", 
  "#9A0089", 
  "#0078D7", 
  "#0063B1", 
  "#8E8CD8", 
  "#6B69D6", 
  "#8764B8", 
  "#744DA9", 
  "#B146C2", 
  "#881798", 
  "#0099BC", 
  "#2D7D9A", 
  "#00B7C3", 
  "#038387", 
  "#00B294", 
  "#018574", 
  "#00CC6A", 
  "#10893E", 
  "#7A7574", 
  "#5D5A58", 
  "#68768A", 
  "#515C6B", 
  "#567C73", 
  "#486860", 
  "#498205", 
  "#107C10", 
  "#767676",
  "#4C4A48",
  "#69797E",
  "#4A5459",
  "#647C64",
  "#525E54",
  "#847545",
  "#7E735F",
];

//https://materialui.co/flatuicolors/
export const flatUIColors: ReadonlyArray<string> = [

  "#F1C40F",
  "#F39C12",
  "#E67E22", 
  "#D35400",
  "#E74C3C",
  "#C0392B",

  "#9B59B6",
  "#8E44AD",

  "#3498DB",
  "#2980B9",

  "#1ABC9C",
  "#16A085",
  "#2ECC71",
  "#27AE60", 

  "#ECF0F1",
  "#BDC3C7",
  "#34495E",
  "#2C3E50",
  "#95A5A6",
  "#7F8C8D"
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

