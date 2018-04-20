import * as d3 from "d3"
import * as d3sc from "d3-scale-chromatic";
import { ChartValue, ChartTable, ChartColumn, ChartRow } from "../ChartClient"


((d3.select(document) as any).__proto__ as d3.Selection<any, any, any, any>).enterData = function (this: d3.Selection<any, any, any, any>, data: any, tag: string, cssClass: string) {
    return this.selectAll(tag + "." + cssClass).data(data)
        .enter().append("svg:" + tag)
        .attr("class", cssClass);
};

declare module "d3-selection" {
    interface Selection<GElement extends d3.BaseType, Datum, PElement extends d3.BaseType, PDatum> {
        enterData<NElement extends d3.BaseType, NDatum>(data: NDatum[], tag: string, cssClass: string): Selection<NElement, Datum, GElement, Datum>;
        enterData<NElement extends d3.BaseType, NDatum>(data: (data: Datum) => NDatum[], tag: string, cssClass: string): Selection<NElement, Datum, GElement, Datum>;
    }
}


export function fillAllTokenValueFuntions(data: ChartTable) {

    for (let i = 0; ; i++) {
        if (data.columns['c' + i] == undefined)
            break;

        for (let j = 0; j < data.rows.length; j++) {
            makeItTokenValue(data.rows[j]['c' + i]);
        }
    }

    for (let j = 0; j < data.rows.length; j++) {
        makeItTokenValue(data.rows[j]['entity']);
    }
}

export function makeItTokenValue(value: ChartValue) {

    if (value == undefined)
        return;

    value.toString = function (this: ChartValue) {
        const key = (this.key !== undefined ? this.key : this);

        if (key == undefined)
            return "null";

        return key.toString();
    };

    value.valueOf = function (this: ChartValue) { return this.key as any; };

    value.niceToString = function (this: ChartValue) {
        const result = (this.toStr !== undefined ? this.toStr : this);

        if (result == undefined)
            return this.key != undefined ? "[ no text ]" : "[ null ]";

        return result.toString();
    };
}

export function ellipsis(elem: SVGTextElement, width: number, padding?: number, ellipsisSymbol?: string) {

    if (ellipsisSymbol == undefined)
        ellipsisSymbol = '…';

    if (padding)
        width -= padding * 2;

    const self = d3.select(elem);
    let textLength = (<any>self.node()).getComputedTextLength();
    let text = self.text();
    while (textLength > width && text.length > 0) {
        text = text.slice(0, -1);
        while (text[text.length - 1] == ' ' && text.length > 0)
            text = text.slice(0, -1);
        self.text(text + ellipsisSymbol);
        textLength = (<any>self.node()).getComputedTextLength();
    }
}

export function getClickKeys(row: any, columns: any) {
    let options = "";
    for (const k in columns) {
        const col = columns[k];
        if (col.isGroupKey == true) {
            const tokenValue = row[k];
            if (tokenValue != undefined) {
                options += "&" + k + "=" + (tokenValue.keyForFilter || tokenValue.toString());
            }
        }
    }

    return options;
}

export function translate(x: number, y: number) {
    if (y == undefined)
        return 'translate(' + x + ')';

    return 'translate(' + x + ',' + y + ')';
}

export function scale(x: number, y: number) {
    if (y == undefined)
        return 'scale(' + x + ')';

    return 'scale(' + x + ',' + y + ')';
}

export function rotate(angle: number, x?: number, y?: number): string {
    if (x == undefined || y == undefined)
        return 'rotate(' + angle + ')';

    return 'rotate(' + angle + ',' + y + ',' + y + ')';
}

export function skewX(angle: number): string {
    return 'skewX(' + angle + ')';
}

export function skewY(angle: number): string {
    return 'skewY(' + angle + ')';
}

export function matrix(a: number, b: number, c: number, d: number, e: number, f: number): string {
    return 'matrix(' + a + ',' + b + ',' + c + ',' + d + ',' + e + ',' + f + ')';
}

export function scaleFor(column: { type: string }, values: any[], minRange: number, maxRange: number, scaleName: string): { (x: any): any; } {

    if (scaleName == "Elements")
        return d3.scaleBand()
            .domain(values)
            .range([minRange, maxRange]);

    if (scaleName == "ZeroMax")
        return d3.scaleLinear()
            .domain([0, d3.max(values)])
            .range([minRange, maxRange]);

    if (scaleName == "MinMax") {
        if (column.type == "Date" || column.type == "DateTime") {
            const scale = d3.scaleTime()
                .domain([new Date(<any>d3.min(values)), new Date(<any>d3.max(values))])
                .range([minRange, maxRange]);

            const f = function (d: string) { return scale(new Date(d)); };
            (<any>f).ticks = scale.ticks;
            (<any>f).tickFormat = scale.tickFormat;
            return f;
        }
        else {
            return d3.scaleLinear()
                .domain([d3.min(values), d3.max(values)])
                .range([minRange, maxRange]);
        }
    }

    if (scaleName == "Log")
        return d3.scaleLog()
            .domain([d3.min(values),
            d3.max(values)])
            .range([minRange, maxRange]);

    if (scaleName == "Sqrt")
        return d3.scalePow().exponent(.5)
            .domain([d3.min(values),
            d3.max(values)])
            .range([minRange, maxRange]);

    throw Error("Unexpected scale: " + scaleName);
}

export function rule(object: any, totalSize?: number): Rule {
    return new Rule(object, totalSize);
}

export class Rule {

    private sizes: { [key: string]: number } = {};
    private starts: { [key: string]: number } = {};
    private ends: { [key: string]: number } = {};

    totalSize: number;

    constructor(object: any, totalSize?: number) {

        let fixed = 0;
        let proportional = 0;
        for (const p in object) {
            const value = object[p];
            if (typeof value === 'number')
                fixed += value;
            else if (Rule.isStar(value))
                proportional += Rule.getStar(value);
            else
                throw new Error("values should be numbers or *");
        }

        if (!totalSize) {
            if (proportional)
                throw new Error("totalSize is mandatory if * is used");

            totalSize = fixed;
        }

        this.totalSize = totalSize;

        const remaining = totalSize - fixed;
        const star = proportional <= 0 ? 0 : remaining / proportional;

        for (const p in object) {
            const value = object[p];
            if (typeof value === 'number')
                this.sizes[p] = value;
            else if (Rule.isStar(value))
                this.sizes[p] = Rule.getStar(value) * star;
        }

        let acum = 0;

        for (const p in this.sizes) {
            this.starts[p] = acum;
            acum += this.sizes[p];
            this.ends[p] = acum;
        }
    }

    static isStar(val: string) {
        return typeof val === 'string' && val[val.length - 1] == '*';
    }

    static getStar(val: string) {
        if (val === '*')
            return 1;

        return parseFloat(val.substring(0, val.length - 1));
    }


    size(name: string) {
        return this.sizes[name];
    }

    start(name: string) {
        return this.starts[name];
    }

    end(name: string) {
        return this.ends[name];
    }

    middle(name: string) {
        return this.starts[name] + this.sizes[name] / 2;
    }

    debugX(chart: d3.Selection<any, any, any, any>) {

        const keys = d3.keys(this.sizes);

        //paint x-axis rule
        chart.append('svg:g').attr('class', 'x-rule-tick')
            .enterData(keys, 'line', 'x-rule-tick')
            .attr('x1', d => this.ends[d])
            .attr('x2', d => this.ends[d])
            .attr('y1', 0)
            .attr('y2', 10000)
            .style('stroke-width', 2)
            .style('stroke', 'Pink');

        //paint y-axis rule labels
        chart.append('svg:g').attr('class', 'x-axis-rule-label')
            .enterData(keys, 'text', 'x-axis-rule-label')
            .attr('transform', (d, i) => {
                return translate(this.starts[d] + this.sizes[d] / 2 - 5, 10 + 100 * (i % 3)) +
                    rotate(90);
            })
            .attr('fill', 'DeepPink')
            .text(d => d);
    }

    debugY(chart: d3.Selection<any, any, any, any>) {

        const keys = d3.keys(this.sizes);

        //paint y-axis rule
        chart.append('svg:g').attr('class', 'y-rule-tick')
            .enterData(keys, 'line', 'y-rule-tick')
            .attr('x1', 0)
            .attr('x2', 10000)
            .attr('y1', d => this.ends[d])
            .attr('y2', d => this.ends[d])
            .style('stroke-width', 2)
            .style('stroke', 'Violet');

        //paint y-axis rule labels
        chart.append('svg:g').attr('class', 'y-axis-rule-label')
            .enterData(keys, 'text', 'y-axis-rule-label')
            .attr('transform', (d, i) => translate(100 * (i % 3), this.starts[d] + this.sizes[d] / 2 + 4))
            .attr('fill', 'DarkViolet')
            .text(d => d);
    }
}


export function getStackOffset(curveName: string): ((series: d3.Series<any, any>, order: number[]) => void) | undefined {
    switch (curveName) {
        case "zero": return d3.stackOffsetNone;
        case "expand": return d3.stackOffsetExpand;
        case "silhouette": return d3.stackOffsetSilhouette;
        case "wiggle": return d3.stackOffsetWiggle;
    }

    return undefined;
}



export function getStackOrder(schemeName: string): ((series: d3.Series<any, any>) => number[]) | undefined {
    switch (schemeName) {
        case "none": return d3.stackOrderNone;
        case "ascending": return d3.stackOrderAscending;
        case "descending": return d3.stackOrderDescending;
        case "insideOut": return d3.stackOrderInsideOut;
        case "reverse": return d3.stackOrderReverse;
    }

    return undefined;
}


export function getCurveByName(curveName: string): d3.CurveFactoryLineOnly | undefined {
    switch (curveName) {
        case "basis": return d3.curveBasis;
        case "bundle": return d3.curveBundle.beta(0.5);
        case "cardinal": return d3.curveCardinal;
        case "catmull-rom": return d3.curveCatmullRom;
        case "linear": return d3.curveLinear;
        case "monotone": return d3.curveMonotoneX;
        case "natural": return d3.curveNatural;
        case "step": return d3.curveStep;
        case "step-after": return d3.curveStepAfter;
        case "step-before": return d3.curveStepBefore;
    }

    return undefined;
}

export function getColorInterpolation(interpolationName: string): ((value: number) => string) | undefined {
    switch (interpolationName) {
        case "YlGn": return d3sc.interpolateYlGn;
        case "YlGnBu": return d3sc.interpolateYlGnBu;
        case "GnBu": return d3sc.interpolateGnBu;
        case "BuGn": return d3sc.interpolateBuGn;
        case "PuBuGn": return d3sc.interpolatePuBuGn;
        case "PuBu": return d3sc.interpolatePuBu;
        case "BuPu": return d3sc.interpolateBuPu;
        case "RdPu": return d3sc.interpolateRdPu;
        case "PuRd": return d3sc.interpolatePuRd;
        case "OrRd": return d3sc.interpolateOrRd;
        case "YlOrRd": return d3sc.interpolateYlOrRd;
        case "YlOrBr": return d3sc.interpolateYlOrBr;
        case "Purples": return d3sc.interpolatePurples;
        case "Blues": return d3sc.interpolateBlues;
        case "Greens": return d3sc.interpolateGreens;
        case "Oranges": return d3sc.interpolateOranges;
        case "Reds": return d3sc.interpolateReds;
        case "Greys": return d3sc.interpolateGreys;
        case "PuOr": return d3sc.interpolatePuOr;
        case "BrBG": return d3sc.interpolateBrBG;
        case "PRGn": return d3sc.interpolatePRGn;
        case "PiYG": return d3sc.interpolatePiYG;
        case "RdBu": return d3sc.interpolateRdBu;
        case "RdGy": return d3sc.interpolateRdGy;
        case "RdYlBu": return d3sc.interpolateRdYlBu;
        case "Spectral": return d3sc.interpolateSpectral;
        case "RdYlGn": return d3sc.interpolateRdYlGn;
    }

    return undefined;
}

export function getColorScheme(schemeName: string, k: number = 11): ReadonlyArray<string> | undefined {
    switch (schemeName) {
        case "category10": return d3.schemeCategory10;
        case "accent": return d3sc.schemeAccent;
        case "dark2": return d3sc.schemeDark2;
        case "paired": return d3sc.schemePaired;
        case "pastel1": return d3sc.schemePastel1;
        case "pastel2": return d3sc.schemePastel2;
        case "set1": return d3sc.schemeSet1;
        case "set2": return d3sc.schemeSet2;
        case "set3": return d3sc.schemeSet3;
        case "BrBG[K]": return d3sc.schemeBrBG[k];
        case "PRGn[K]": return d3sc.schemePRGn[k];
        case "PiYG[K]": return d3sc.schemePiYG[k];
        case "PuOr[K]": return d3sc.schemePuOr[k];
        case "RdBu[K]": return d3sc.schemeRdBu[k];
        case "RdGy[K]": return d3sc.schemeRdGy[k];
        case "RdYlBu[K]": return d3sc.schemeRdYlBu[k];
        case "RdYlGn[K]": return d3sc.schemeRdYlGn[k];
        case "Spectral[K]": return d3sc.schemeSpectral[k];
        case "Blues[K]": return d3sc.schemeBlues[k];
        case "Greys[K]": return d3sc.schemeGreys[k];
        case "Oranges[K]": return d3sc.schemeOranges[k];
        case "Purples[K]": return d3sc.schemePurples[k];
        case "Reds[K]": return d3sc.schemeReds[k];
        case "BuGn[K]": return d3sc.schemeBuGn[k];
        case "BuPu[K]": return d3sc.schemeBuPu[k];
        case "OrRd[K]": return d3sc.schemeOrRd[k];
        case "PuBuGn[K]": return d3sc.schemePuBuGn[k];
        case "PuBu[K]": return d3sc.schemePuBu[k];
        case "PuRd[K]": return d3sc.schemePuRd[k];
        case "RdPu[K]": return d3sc.schemeRdPu[k];
        case "YlGnBu[K]": return d3sc.schemeYlGnBu[k];
        case "YlGn[K]": return d3sc.schemeYlGn[k];
        case "YlOrBr[K]": return d3sc.schemeYlOrBr[k];
        case "YlOrRd[K]": return d3sc.schemeYlOrRd[k];
    }

    return undefined;
}



export function stratifyTokens(
    data: ChartTable,
    keyColumn: string, /*Employee*/
    keyColumnParent: string, /*Employee.ReportsTo*/):
    d3.HierarchyNode<ChartRow | Folder | Root> {

    const folders = data.rows
        .filter(r => r[keyColumnParent] && r[keyColumnParent].key != null)
        .map(r => ({ folder: r[keyColumnParent] }) as Folder)
        .toObjectDistinct(r => r.folder.key!.toString());

    const root: Root = { isRoot: true };

    const NullConst = "- Null -";

    const dic = data.rows.filter(r => r[keyColumn].key != null).toObjectDistinct(r => r[keyColumn]!.key as string);

    const getParent = (d: ChartRow | Folder | Root) => {
        if ((d as Root).isRoot)
            return null;

        if ((d as Folder).folder) {
            const r = dic[(d as Folder).folder.key!];

            if (!r)
                return root;

            const parentValue = r[keyColumnParent];
            if (!parentValue || parentValue.key == null)
                return root;  //Either null

            return folders[parentValue.key as string]; // Parent folder
        }

        if ((d as ChartRow)[keyColumn]) {
            const r = d as ChartRow;

            var fold = r[keyColumn] && r[keyColumn].key != null && folders[r[keyColumn].key as string];
            if (fold)
                return fold; //My folder

            const parentValue = r[keyColumnParent];

            const parentFolder = parentValue && parentValue.key != null && folders[parentValue.key as string];

            if (!parentFolder)
                return root; //No key an no parent

            return folders[parentFolder.folder!.key as string]; //only parent
        }

        throw new Error("Unexpected " + JSON.stringify(d))
    };

    var getKey = (r: ChartRow | Folder | Root) => {

        if ((r as Root).isRoot)
            return "#Root";

        if ((r as Folder).folder)
            return "F#" + (r as Folder).folder.key;

        const cr = (r as ChartRow);

        if (cr[keyColumn].key != null)
            return cr[keyColumn].key as string;

        return NullConst;
    }

    var rootNode = d3.stratify<ChartRow | Folder | Root>()
        .id(getKey)
        .parentId(r => {
            var parent = getParent(r);
            return parent ? getKey(parent) : null
        })([root, ...Object.values(folders), ...data.rows]);

    return rootNode

}

interface Folder {
    folder: ChartValue;
}

interface Root {
    isRoot: true;
}


export function toPivotTable(data: ChartTable,
    col0: string, /*Employee*/
    otherCols: string[]): PivotTable {

    var usedCols = otherCols
        .filter(function (cn) { return data.columns[cn].token != undefined; });

    var rows = data.rows
        .map(function (r) {
            return {
                rowValue: r[col0],
                values: usedCols.toObject(cn => cn, (cn): PivotValue => ({
                    rowClick: r,
                    value: r[cn],
                    valueTitle: `${r[col0].niceToString!()}, ${data.columns[cn].title}: ${r[cn].niceToString!()}`
                }))
            } as PivotRow;
        });

    var title = otherCols
        .filter(function (cn) { return data.columns[cn].token != undefined; })
        .map(function (cn) { return data.columns[cn].title; })
        .join(" | ");

    return {
        title,
        columns: d3.values(usedCols.toObject(c => c, c => ({
            color: null,
            key: c,
            niceName: data.columns[c].title,
        } as PivotColumn))),
        rows,
    };
}

export function groupedPivotTable(data: ChartTable,
    col0: string, /*Employee*/
    colSplit: string,
    colValue: string): PivotTable {

    var columns = d3.values(data.rows.toObjectDistinct(cr => cr[colSplit].key as string, cr => ({
        niceName: cr[colSplit].niceToString!(),
        color: cr[colSplit].color,
        key: cr[colSplit].key,
    }) as PivotColumn));

    var rows = data.rows.groupBy(r => "k" + r[col0].key)
        .map(gr => {

            var rowValue = gr.elements[0][col0];
            return {
                rowValue: rowValue,
                values: gr.elements.toObject(
                    r => r[colSplit].key as string,
                    (r) : PivotValue => ({
                        value: r[colValue],
                        rowClick: r,
                        valueTitle: `${rowValue.niceToString!()}, ${r[colSplit].niceToString!()}: ${r[colValue].niceToString!()}`
                    })),
            } as PivotRow;
        });

    var title = data.columns.c2.title + " / " + data.columns.c1.title;

    return {
        title,
        columns,
        rows,
    } as PivotTable;
}

interface PivotTable {
    title: string;
    columns: PivotColumn[];
    rows: PivotRow[];
}

interface PivotColumn {
    key: string;
    color?: string | null;
    niceName?: string | null;
}

interface PivotRow {
    rowValue: ChartValue;
    values: { [key: string /*| number*/]: PivotValue };
}


interface PivotValue {
    rowClick: ChartRow;
    value: ChartValue;
    valueTitle: string;
}
