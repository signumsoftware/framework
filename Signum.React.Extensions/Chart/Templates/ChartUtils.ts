/// <reference path="../../../../framework/signum.react/typings/d3/d3.d.ts" />
import * as d3 from "d3"
import { ChartValue, ChartTable, ChartColumn,  } from "../ChartClient"


(Array.prototype as any as d3.Selection<any>).enterData = function (this: d3.Selection<any>, data: any, tag: string, cssClass: string) {
    return this.selectAll(tag + "." + cssClass).data(data)
        .enter().append("svg:" + tag)
        .attr("class", cssClass);
};

export function fillAllTokenValueFuntions(data: ChartTable) {

    for (let i = 0; ; i++) {
        if (data.columns['c' + i] == undefined)
            break;

        for (let j = 0; j < data.rows.length; j++) {
            makeItTokenValue(data.rows[j]['c' + i]);
        }
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
        return d3.scale.ordinal()
            .domain(values)
            .rangeBands([minRange, maxRange]);

    if (scaleName == "ZeroMax")
        return d3.scale.linear()
            .domain([0, d3.max(values)])
            .range([minRange, maxRange]);

    if (scaleName == "MinMax") {
        if (column.type == "Date" || column.type == "DateTime") {
            const scale = d3.time.scale()
                .domain([new Date(<any>d3.min(values)), new Date(<any>d3.max(values))])
                .range([minRange, maxRange]);

            const f = function (d: string) { return scale(new Date(d)); };
            (<any>f).ticks = scale.ticks;
            (<any>f).tickFormat = scale.tickFormat;
            return f;
        }
        else {
            return d3.scale.linear()
                .domain([d3.min(values), d3.max(values)])
                .range([minRange, maxRange]);
        }
    }

    if (scaleName == "Log")
        return d3.scale.log()
            .domain([d3.min(values),
                d3.max(values)])
            .range([minRange, maxRange]);

    if (scaleName == "Sqrt")
        return d3.scale.pow().exponent(.5)
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

    debugX(chart: d3.Selection<any>) {

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

    debugY(chart: d3.Selection<any>) {

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


export function toTree<T>(elements: T[], getKey: (elem: T) => string, getParent: (elem: T) => T): Node<T>[] {

    const root: Node<T> = { item: undefined as any, children: [] };

    const dic: { [key: string]: Node<T> } = {};

    function getOrCreateNode(elem: T) {

        const key = getKey(elem);

        if (dic[key])
            return dic[key];

        const node: Node<T> = { item: elem, children: [] };

        const parent = getParent(elem);

        if (parent) {
            const parentNode = getOrCreateNode(parent);

            parentNode.children.push(node);
        } else {
            root.children.push(node);
        }

        dic[key] = node;

        return node;
    }

    elements.forEach(getOrCreateNode);

    return root.children;
}


export interface Node<T> {
    item: T;
    children: Node<T>[];
}

declare module "d3" {
    interface Selection<Datum> {
        enterData<S>(data: S[], tag: string, cssClass: string): d3.Selection<S>
        enterData<S>(data: (data: Datum) => S[], tag: string, cssClass: string): d3.Selection<S>
    }
}
