export const namedColors: { [colorName: string]: string } = {
  "aliceblue": "#f0f8ff",
  "antiquewhite": "#faebd7",
  "aqua": "#00ffff",
  "aquamarine": "#7fffd4",
  "azure": "#f0ffff",
  "beige": "#f5f5dc",
  "bisque": "#ffe4c4",
  "black": "#000000",
  "blanchedalmond": "#ffebcd",
  "blue": "#0000ff",
  "blueviolet": "#8a2be2",
  "brown": "#a52a2a",
  "burlywood": "#deb887",
  "cadetblue": "#5f9ea0",
  "chartreuse": "#7fff00",
  "chocolate": "#d2691e",
  "coral": "#ff7f50",
  "cornflowerblue": "#6495ed",
  "cornsilk": "#fff8dc",
  "crimson": "#dc143c",
  "cyan": "#00ffff",
  "darkblue": "#00008b",
  "darkcyan": "#008b8b",
  "darkgoldenrod": "#b8860b",
  "darkgray": "#a9a9a9",
  "darkgreen": "#006400",
  "darkkhaki": "#bdb76b",
  "darkmagenta": "#8b008b",
  "darkolivegreen": "#556b2f",
  "darkorange": "#ff8c00",
  "darkorchid": "#9932cc",
  "darkred": "#8b0000",
  "darksalmon": "#e9967a",
  "darkseagreen": "#8fbc8f",
  "darkslateblue": "#483d8b",
  "darkslategray": "#2f4f4f",
  "darkturquoise": "#00ced1",
  "darkviolet": "#9400d3",
  "deeppink": "#ff1493",
  "deepskyblue": "#00bfff",
  "dimgray": "#696969",
  "dodgerblue": "#1e90ff",
  "firebrick": "#b22222",
  "floralwhite": "#fffaf0",
  "forestgreen": "#228b22",
  "fuchsia": "#ff00ff",
  "gainsboro": "#dcdcdc",
  "ghostwhite": "#f8f8ff",
  "gold": "#ffd700",
  "goldenrod": "#daa520",
  "gray": "#808080",
  "green": "#008000",
  "greenyellow": "#adff2f",
  "honeydew": "#f0fff0",
  "hotpink": "#ff69b4",
  "indianred ": "#cd5c5c",
  "indigo": "#4b0082",
  "ivory": "#fffff0",
  "khaki": "#f0e68c",
  "lavender": "#e6e6fa",
  "lavenderblush": "#fff0f5",
  "lawngreen": "#7cfc00",
  "lemonchiffon": "#fffacd",
  "lightblue": "#add8e6",
  "lightcoral": "#f08080",
  "lightcyan": "#e0ffff",
  "lightgoldenrodyellow": "#fafad2",
  "lightgrey": "#d3d3d3",
  "lightgreen": "#90ee90",
  "lightpink": "#ffb6c1",
  "lightsalmon": "#ffa07a",
  "lightseagreen": "#20b2aa",
  "lightskyblue": "#87cefa",
  "lightslategray": "#778899",
  "lightsteelblue": "#b0c4de",
  "lightyellow": "#ffffe0",
  "lime": "#00ff00",
  "limegreen": "#32cd32",
  "linen": "#faf0e6",
  "magenta": "#ff00ff",
  "maroon": "#800000",
  "mediumaquamarine": "#66cdaa",
  "mediumblue": "#0000cd",
  "mediumorchid": "#ba55d3",
  "mediumpurple": "#9370d8",
  "mediumseagreen": "#3cb371",
  "mediumslateblue": "#7b68ee",
  "mediumspringgreen": "#00fa9a",
  "mediumturquoise": "#48d1cc",
  "mediumvioletred": "#c71585",
  "midnightblue": "#191970",
  "mintcream": "#f5fffa",
  "mistyrose": "#ffe4e1",
  "moccasin": "#ffe4b5",
  "navajowhite": "#ffdead",
  "navy": "#000080",
  "oldlace": "#fdf5e6",
  "olive": "#808000",
  "olivedrab": "#6b8e23",
  "orange": "#ffa500",
  "orangered": "#ff4500",
  "orchid": "#da70d6",
  "palegoldenrod": "#eee8aa",
  "palegreen": "#98fb98",
  "paleturquoise": "#afeeee",
  "palevioletred": "#d87093",
  "papayawhip": "#ffefd5",
  "peachpuff": "#ffdab9",
  "peru": "#cd853f",
  "pink": "#ffc0cb",
  "plum": "#dda0dd",
  "powderblue": "#b0e0e6",
  "purple": "#800080",
  "red": "#ff0000",
  "rosybrown": "#bc8f8f",
  "royalblue": "#4169e1",
  "saddlebrown": "#8b4513",
  "salmon": "#fa8072",
  "sandybrown": "#f4a460",
  "seagreen": "#2e8b57",
  "seashell": "#fff5ee",
  "sienna": "#a0522d",
  "silver": "#c0c0c0",
  "skyblue": "#87ceeb",
  "slateblue": "#6a5acd",
  "slategray": "#708090",
  "snow": "#fffafa",
  "springgreen": "#00ff7f",
  "steelblue": "#4682b4",
  "tan": "#d2b48c",
  "teal": "#008080",
  "thistle": "#d8bfd8",
  "tomato": "#ff6347",
  "turquoise": "#40e0d0",
  "violet": "#ee82ee",
  "wheat": "#f5deb3",
  "white": "#ffffff",
  "whitesmoke": "#f5f5f5",
  "yellow": "#ffff00",
  "yellowgreen": "#9acd32"
};

export function nameToHex(name: string): string {
  return namedColors[name.toLowerCase()];
}


function clamp(val: number) {
  return Math.round(Math.max(Math.min(val, 255), 0));
}

export class Color {
  constructor(
    public r: number,
    public g: number,
    public b: number,
    public a?: number) {
  }

  toString(): string {
    if (this.a != undefined)
      return `rgba(${clamp(this.r)},${clamp(this.g)},${clamp(this.b)},${this.a.toString()})`;
    else
      return `rgb(${clamp(this.r)},${clamp(this.g)},${clamp(this.b)})`;
  }

  withAlpha(a: number): Color {
    return new Color(this.r, this.g, this.b, a);
  }

  static White = new Color(255, 255, 255);
  static Black = new Color(0, 0, 0);

  lerp(ratio: number, target: Color): Color {

    return new Color(
      this.r * (1 - ratio) + target.r * ratio,
      this.g * (1 - ratio) + target.g * ratio,
      this.b * (1 - ratio) + target.b * ratio,
      this.a == null ? target.a :
        target.a == null ? this.a :
          this.a * (1 - ratio) + target.a * ratio
    );
  }

  opositePole(): Color {
    return (this.r + this.g + this.b) / 3 > (256 / 2) ? Color.Black : Color.White;
  }

  static parse(color: string): Color {
    var result = Color.tryParse(color);
    if (!result)
      throw new Error("Impossible to parse color " + color);

    return result;
  }

  static tryParse(color: string | undefined): Color | undefined {

    if (!color)
      return undefined;

    let c = nameToHex(color) || color;

    if (c.startsWith("rgba")) {
      c = c.after("rgba(").before(")");
      var parts = c.split(",");
      return new Color(
        parseInt(parts[0]),
        parseInt(parts[1]),
        parseInt(parts[2]),
        parseInt(parts[3]),
      );
    }
    else if (c.startsWith("rgb")) {
      c = c.after("rgb(").before(")");
      var parts = c.split(",");
      return new Color(
        parseInt(parts[0]),
        parseInt(parts[1]),
        parseInt(parts[2]),
      );

    }
    else if (c.startsWith('#')) {
      c = c.substr(1);

      switch (c.length) {
        case 3: return new Color(
          parseInt(c.slice(0, 1), 16) * 16,
          parseInt(c.slice(1, 2), 16) * 16,
          parseInt(c.slice(2, 3), 16) * 16
        );

        case 6: return new Color(
          parseInt(c.slice(0, 2), 16),
          parseInt(c.slice(2, 4), 16),
          parseInt(c.slice(4, 6), 16)
        );

        case 8: return new Color(
          parseInt(c.slice(0, 2), 16),
          parseInt(c.slice(2, 4), 16),
          parseInt(c.slice(4, 6), 16),
          parseInt(c.slice(6, 8), 16)
        );

        default:
          return undefined;
      }
    }
    else
      return undefined;
  }
}

export class Gradient {
  constructor(public list: { value: number, color: Color }[]) {

  }

  getColor(value: number): Color {
    var prev = this.list.filter(a => a.value <= value).maxBy(a => a.value);
    var next = this.list.filter(a => a.value > value).minBy(a => a.value);

    if (prev == undefined)
      return next!.color;

    if (next == undefined)
      return prev!.color;

    return prev.color.lerp((value - prev.value) / (next.value - prev.value), next.color);
  }

  cache: { [num: number]: Color } = {};
  getCachedColor(value: number): Color {
    return this.cache[value] || (this.cache[value] = this.getColor(value));
  }
}
