import * as d3 from "d3"
import * as React from "react"
import { EntityData, EntityKind } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { Point, Rectangle, calculatePoint, wrap } from '../Utils'

export interface TableInfo extends ITableInfo {
    typeName: string;
    mlistTables: MListTableInfo[];
}


export interface MListTableInfo extends ITableInfo {
}

export interface ITableInfo extends d3.layout.force.Node, Rectangle {
    tableName: string;
    niceName: string;
    rows: number;
    columns: number;
    total_size_kb: number;
    entityKind: EntityKind;
    entityData: EntityData;
    entityBaseType: EntityBaseType;
    namespace: string;
    nx: number;
    ny: number;
    width: number;
    height: number;
    lineHeight: number;
    extra: { [key: string]: any };
}

export type EntityBaseType =
    "EnumEntity" |
    "Symbol" |
    "SemiSymbol" |
    "Entity" |
    "MList" |
    "Part";

export interface IRelationInfo extends d3.layout.force.Link<ITableInfo> {
    isMList?: boolean;
    repetitions?: number;
    sourcePoint?: Point;
    targetPoint?: Point;
}


export interface RelationInfo extends IRelationInfo {
    fromTable: string;
    toTable: string;
    nullable: boolean;
    lite: boolean;
}

export interface MListRelationInfo extends IRelationInfo {
}

export interface ColorProviderInfo {
    name: string;
    niceName: string;
}

export interface SchemaMapInfo {
    tables: TableInfo[];
    relations: RelationInfo[];
    providers: ColorProviderInfo[];

    allNodes?: ITableInfo[];
    allLinks?: IRelationInfo[];
}


export interface ClientColorProvider {
    name: string;
    getFill: (t: ITableInfo) => string;
    getStroke?: (t: ITableInfo) => string;
    getTooltip: (t: ITableInfo) => string;
    getMask?: (t: ITableInfo) => string;
    defs?: JSX.Element[];
}

export class SchemaMapD3 {
    
    nodes: ITableInfo[];
    links: IRelationInfo[];
    force: d3.layout.Force<IRelationInfo, ITableInfo>;
    fanIn: { [key: string]: IRelationInfo[] };

    selectedTable: ITableInfo;

    link: d3.Selection<IRelationInfo>;

    nodeGroup: d3.Selection<ITableInfo>;
    node: d3.Selection<ITableInfo>;
    label: d3.Selection<ITableInfo>;
    titles: d3.Selection<ITableInfo>;

    constructor(
        public svgElement: SVGElement,
        public providers: { [name: string]: ClientColorProvider },
        public map: SchemaMapInfo,
        public filter: string,
        public color: string,
        public width: number,
        public height: number) {

        this.force = d3.layout.force<IRelationInfo, ITableInfo>()
            .gravity(0)
            .charge(0)
            .size([width, height]);

        this.fanIn = map.relations.groupToObject(a => a.toTable);

        this.restart();

        const svg = d3.select(svgElement)
            .attr("width", width)
            .attr("height", height);

        this.link = svg.append("svg:g").attr("class", "links").selectAll(".link")
            .data(map.allLinks)
            .enter().append("path")
            .attr("class", "link")
            .style("stroke-dasharray", d => (<RelationInfo>d).lite ? "2, 2" : null)
            .style("stroke", "black")
            .attr("marker-end", d => "url(#" + (d.isMList ? "mlist_arrow" : (<RelationInfo>d).lite ? "lite_arrow" : "normal_arrow") + ")");

        this.selectedLinks();

        const nodesG = svg.append("svg:g").attr("class", "nodes");

        const drag = this.force.drag()
            .on("dragstart", d => d.fixed = true);

        this.nodeGroup = nodesG.selectAll(".nodeGroup")
            .data(map.allNodes)
            .enter()
            .append("svg:g").attr("class", "nodeGroup")
            .style("cursor", d => (d as TableInfo).typeName && Finder.isFindable((d as TableInfo).typeName) ? "pointer" : null)
            .on("click", d => {

                this.selectedTable = this.selectedTable == d ? null : d;

                this.selectedLinks();
                this.selectedNode();

                const event = d3.event;
                if (event.defaultPrevented)
                    return;

                if ((<any>event).ctrlKey && (d as TableInfo).typeName) {
                    window.open(Finder.findOptionsPath({ queryName: (d as TableInfo).typeName }));
                    d3.event.preventDefault();
                    return false;
                }
            })
            .on("dblclick", d => {
                d.fixed = false;
            }).call(drag);

        this.node = this.nodeGroup.append("rect")
            .attr("class", d => "node " + d.entityBaseType)
            .attr("rx", n =>
                n.entityBaseType == "Entity" ? 7 :
                    n.entityBaseType == "Part" ? 4 :
                        n.entityBaseType == "SemiSymbol" ? 5 :
                            n.entityBaseType == "Symbol" ? 4 :
                                n.entityBaseType == "EnumEntity" ? 3 : 0);


        const margin = 3;

        this.label = this.nodeGroup.append("text")
            .attr("class", d => "node " + d.entityBaseType)
            .style("cursor", d => (d as TableInfo).typeName ? "pointer" : null)
            .text(d => d.niceName)
            .each(function (d) {
                wrap(this, 60);
                const b = this.getBBox();
                d.width = b.width + margin * 2;
                d.height = b.height + margin * 2;
            });

        this.node.attr("width", d => d.width)
            .attr("height", d => d.height);

        this.selectedNode();
        
        this.showHideNodes();

        this.label.attr("transform", d => "translate(" + d.width / 2 + ", 0)");

        this.titles = this.label.append('svg:title');

        this.drawColor();

        this.force.on("tick", this.onTick);
    }

    
    restart() {

        const parts = this.filter.match(/[+-]?((\w+)|\*)/g);

        function isMatch(str: string): boolean {

            if (!parts)
                return true;

            for (let i = parts.length - 1; i >= 0; i--) {
                const p = parts[i];
                const pair = p.startsWith("+") ? { isPositive: true, token: p.after("+") } :
                    p.startsWith("-") ? { isPositive: false, token: p.after("-") } :
                        { isPositive: true, token: p };

                if (pair.token == "*" || str.contains(pair.token))
                    return pair.isPositive;
            }

            return false;
        };

        this.nodes = this.map.allNodes.filter((n, i) => this.filter == null ||
            isMatch(n.namespace.toLowerCase() + "|" + n.tableName.toLowerCase() + "|" + n.niceName.toLowerCase()));

        this.links = this.map.allLinks.filter(l =>
            this.nodes.indexOf(<ITableInfo>l.source) != -1 &&
            this.nodes.indexOf(<ITableInfo>l.target) != -1);

        var numNodes = this.nodes.length;

        const distance =
            numNodes < 10 ? 80 :
                numNodes < 20 ? 60 :
                    numNodes < 30 ? 50 :
                        numNodes < 50 ? 40 :
                            numNodes < 100 ? 35 :
                                numNodes < 200 ? 30 : 25;

        this.force
            .linkDistance((d: IRelationInfo) => d.isMList ? distance * 0.7 : distance * 1.5)
            .linkStrength((d: IRelationInfo) => 0.7 * (d.isMList ? 1 : this.getOpacity((<RelationInfo>d).toTable)))
            .nodes(this.nodes)
            .links(this.links)
            .start();
    }

    selectedLinks() {
        var selectedTable = this.selectedTable;
        this.link
            .style("stroke-width", d => d.source == selectedTable || d.target == selectedTable ? 1.5 : d.isMList ? 1.5 : 1)
            .style("opacity", d => d.source == selectedTable || d.target == selectedTable ? 1 : d.isMList ? 0.8 : Math.max(.1, this.getOpacity((<RelationInfo>d).toTable)));
    }

    selectedNode() {
        this.label.style("font-weight", d => d == this.selectedTable ? "bold" : null);
    }

    showHideNodes() {
        this.nodeGroup.style("display", n => this.nodes.indexOf(n) == -1 ? "none" : "inline");
        this.link.style("display", r => this.links.indexOf(r) == -1 ? "none" : "inline");
    }


    static opacities = [1, .9, .8, .7, .6, .5, .4, .3, .25, .2, .15, .1, .07, .05, .03, .02];

    getOpacity(toTable: string) {
        const length = this.fanIn[toTable].filter(l => this.nodes.indexOf(<ITableInfo>l.source) != -1).length;

        const min = Math.min(length, SchemaMapD3.opacities.length - 1);

        return SchemaMapD3.opacities[min];
    }


    setFilter(newFilter: string) {
        this.filter = newFilter;

        this.restart();
        this.selectedLinks();
        this.showHideNodes();
    }

    
    setColor(newColor: string) {

        this.color = newColor;
        this.drawColor();
    }

    drawColor() {
        var cp = this.providers[this.color];

        this.node.style("fill", cp.getFill)
            .style("stroke", cp.getStroke || cp.getFill)
            .style("mask", cp.getMask);

        this.titles.text(t => cp.getTooltip(t) + " (" + t.entityBaseType + ")");
    }

    stop(){
        this.force.stop();
    }


    onTick = () => {
        this.nodes.forEach(d => {
            d.nx = 0;
            d.ny = 0;
        });

        this.namespaceClustering();
        this.gravity();

        this.nodes.forEach(d => {
            d.x += d.nx;
            d.y += d.ny;
        });

        const visibleLink = this.link.filter(f => this.links.indexOf(f) != -1);

        visibleLink.each(rel => {
            rel.sourcePoint = calculatePoint(<ITableInfo>rel.source, rel.target);
            rel.targetPoint = calculatePoint(<ITableInfo>rel.target, rel.source);
        });

        visibleLink.attr("d", l => this.getPathExpression(l));

        this.nodeGroup.filter(d => this.nodes.indexOf(d) != -1)
            .attr("transform", d => "translate(" +
                (d.x - d.width / 2) + ", " +
                (d.y - d.height / 2) + ")");
    }

    getPathExpression(l : IRelationInfo){
       
        var s = l.sourcePoint;
        var t = l.targetPoint;

        var m : Point = { 
            x : (s.x + t.x) / 2, 
            y : (s.y + t.y) / 2
        };

        var d : Point = {
            x : (s.x - t.x), 
            y : (s.y - t.y) 
        };

        var h = Math.sqrt(d.x * d.x + d.y * d.y);

        if(h == 0)
            h = 1;

        //0, 10, -10, 20, -20, 30, -30
        var repPixels = Math.floor(l.repetitions + 1 / 2) * ((l.repetitions % 2) * 2  - 1); 

        var p : Point = {
            x : m.x + (d.y / h) * 20 * repPixels,
            y : m.y - (d.x / h) * 20 * repPixels
        };
        
        
        return `M${s.x} ${s.y} Q ${p.x} ${p.y} ${t.x} ${t.y}`

    }



    gravity() {
        this.nodes.forEach(n => {
            n.nx += this.gravityDim(n.x, 0, this.width);
            n.ny += this.gravityDim(n.y, 0, this.height);
        });         
    }

    gravityDim(v: number, min: number, max: number): number {

        const minF = min + 100;
        const maxF = max - 100;

        const dist =
            maxF < v ? maxF - v :
                v < minF ? minF - v : 0;

        return dist * this.force.alpha() * 0.4;
    }

    namespaceClustering() {
        const quadtree = d3.geom.quadtree<ITableInfo>()
            .x(p => p.x)
            .y(p => p.y)(this.nodes);

        var numNodes = this.nodes.length;

        const constant =
            numNodes < 10 ? 100 :
                numNodes < 20 ? 50 :
                    numNodes < 50 ? 30 :
                        numNodes < 100 ? 20 :
                            numNodes < 200 ? 15 : 10;


        function distance(v: number, min: number, max: number) {
            if (v < min)
                return min - v;

            if (max < v)
                return v - max;

            return 0;
        }

        this.nodes.forEach(d => {
            quadtree.visit((quad, x1, y1, x2, y2) => {
                if (quad.point && quad.point != d) {

                    let x = d.x - quad.point.x;
                    let y = d.y - quad.point.y;

                    if (x == 0 && y == 0) {
                        x = (Math.random() - 0.5) * 10;
                        y = (Math.random() - 0.5) * 10;
                    }

                    var l = Math.sqrt(x * x + y * y);

                    const lx = x / l;
                    const ly = y / l;

                    const ratio = l / 20;

                    let f = constant * this.force.alpha() / Math.max(ratio * ratio, 0.1);
    
                    if (d.namespace != quad.point.namespace)
                        f *= 4;

                    f = Math.min(f, 1);

                    d.nx += lx * f;
                    d.ny += ly * f;
                }

                const dx = distance(d.x, x1, x2);
                const dy = distance(d.y, y1, y2);

                const dist = Math.sqrt(dx * dx + dy * dy);

                return dist > 400;
            });
        });
    }

}


