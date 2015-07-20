/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/d3/d3.d.ts"/>

import d3 = require("d3")


export interface TableInfo extends ITableInfo {
    webTypeName: string;


    mlistTables: MListTableInfo[];
}

export interface ColorProvider {
    getFill: (t: ITableInfo) => string;
    getStroke?: (t: ITableInfo) => string;
    getTooltip: (t: ITableInfo) => string;
    getMask?: (t: ITableInfo) => string;
}

export enum EntityKind{
    SystemString,
    System,
    Relational,
    String,
    Shared,
    Main,
    Part,
    SharedPart,
}

export enum EntityData {
    Master,
    Transactional
}

export enum EntityBaseType {
    EnumEntity = 0,
    Symbol = 1,
    SemiSymbol = 2,
    Entity = 3,
    MList = 4,
    Part = 5,
}

export interface MListTableInfo extends ITableInfo {
}

export interface ITableInfo extends D3.Layout.GraphNode, Rectangle {
    findUrl: string;
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

export interface IRelationInfo extends D3.Layout.GraphLink {
    isMList?: boolean;

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

export interface MapInfo {
    tables: TableInfo[];
    relations: RelationInfo[];
}

export interface Point {
    x: number;
    y: number;
}


export interface Rectangle extends Point{
    width: number;
    height: number;
}


export function createMap(mapId: string, svgMapId: string, filterId: string, colorId : string, map: MapInfo) {

    var getProvider: (value: string, nodes: ITableInfo[]) => Promise<ColorProvider> = window["getProvider"];

    var div = mapId.get();
    var filter = filterId.get();
    var colorCombo = colorId.get();

    div.closest(".container").removeClass("container").addClass("container-fluid");

    div.css("width", "100%");
    div.css("height",(window.innerHeight - 200) + "px");

    var width = div.width(),
        height = div.height();

    map.tables.forEach(t=> t.mlistTables.forEach(ml=> {
        ml.entityKind = t.entityKind;
        ml.entityData = t.entityData;
        ml.entityBaseType = EntityBaseType.MList;
        ml.namespace = t.namespace;
    }));

    var allNodes = (<ITableInfo[]>map.tables).concat(map.tables.flatMap(t=> t.mlistTables));

    var nodesDic = allNodes.toObject(g=> g.tableName);

    map.relations.forEach(a=> {
        a.source = nodesDic[a.fromTable];
        a.target = nodesDic[a.toTable];
    });

    var allLinks = map.relations.map(a=> <IRelationInfo> a)
        .concat(map.tables.flatMap(t=> t.mlistTables.map(tm => <MListRelationInfo>{ source: t, target: tm, isMList: true })));



    var fanIn = map.relations.groupByObject(a=> a.toTable);

    var opacities = [1, .9, .8, .7, .6, .5, .4, .3, .25, .2, .15, .1, .07, .05, .03, .02];

    var nodes: ITableInfo[];
    var links: IRelationInfo[]; 

    var force = d3.layout.force()
        .gravity(0)
        .charge(0)
        .size([width, height]);

    function getOpacity(toTable: string) {
        var length = fanIn[toTable].filter(l=> nodes.indexOf(<ITableInfo>l.source) != -1).length;

        var min = Math.min(length, opacities.length - 1);

        return opacities[min];
    }

    function restart() {

        var val = (<string>filter.val()).toLowerCase();
        
        var parts = val.match(/[+-]?((\w+)|\*)/g);

        function isMatch(str: string): boolean {

            if (!parts)
                return true;

            for (var i = parts.length - 1; i >= 0; i--) {
                var p = parts[i];
                var pair = p.startsWith("+") ? { isPositive: true, token: p.after("+") } :
                    p.startsWith("-") ? { isPositive: false, token: p.after("-") } :
                        { isPositive: true, token: p };

                if (pair.token == "*" || str.contains(pair.token))
                    return pair.isPositive;
            }

            return false;
        };

        nodes = allNodes.filter((n, i) => val == null ||
            isMatch(n.namespace.toLowerCase() + "|" + n.tableName.toLowerCase() + "|" + n.niceName.toLowerCase()));

        links = allLinks.filter(l=>
            nodes.indexOf(<ITableInfo>l.source) != -1 &&
            nodes.indexOf(<ITableInfo>l.target) != -1);

        var distance = nodes.length < 10 ? 80 :
            nodes.length < 20 ? 60 :
            nodes.length < 30 ? 50 :
            nodes.length < 50 ? 40 :
            nodes.length < 100 ? 35 :
            nodes.length < 200 ? 30 : 25;

        force
            .linkDistance((d: IRelationInfo) => d.isMList ? distance * 0.7 : distance * 1.5)
            .linkStrength((d: IRelationInfo) => 0.7 * (d.isMList ? 1 : getOpacity((<RelationInfo>d).toTable)))
            .nodes(nodes)
            .links(links)
            .start();
    }

    restart();

    
    var selectedTable: ITableInfo;

    var svg = d3.select("#" + svgMapId)
        .attr("width", width)
        .attr("height", height);


    var link = svg.append("svg:g").attr("class", "links").selectAll(".link")
        .data(allLinks)
        .enter().append("line")
        .attr("class", "link")
        .style("stroke-dasharray", d=> (<RelationInfo>d).lite ? "2, 2" : null)     
        .style("stroke", "black")
        .attr("marker-end", d  => "url(#" + (d.isMList ? "mlist_arrow" : (<RelationInfo>d).lite ? "lite_arrow" : "normal_arrow") + ")");


    function selectedLinks() {
        link.style("stroke-width", d => d.source == selectedTable || d.target == selectedTable ? 1.5 : d.isMList ? 1.5 : 1)
            .style("opacity", d => d.source == selectedTable || d.target == selectedTable ? 1 : d.isMList ? 0.8 :
            Math.max(.1, getOpacity((<RelationInfo>d).toTable)));
    };

    selectedLinks();

    var nodesG = svg.append("svg:g").attr("class", "nodes");

    var drag = force.drag()
        .on("dragstart", d=> d.fixed = true);

    var nodeGroup = nodesG.selectAll(".nodeGroup")
        .data(allNodes)
        .enter()
        .append("svg:g").attr("class", "nodeGroup")
        .style("cursor", d=> d.findUrl ? "pointer" : null)
        .on("click", d=> {

        selectedTable = selectedTable == d ? null : d;

        selectedLinks();
        selectedNode();

        var event = d3.event;
        if (event.defaultPrevented)
            return;

        if ((<any>event).ctrlKey && d.findUrl) {
            window.open(d.findUrl);
            d3.event.preventDefault();
            return false;
        }
        })
        .on("dblclick", d=> {
        d.fixed = false;
        }).call(drag);

    var node = nodeGroup.append("rect")
        .attr("class", d => "node " + EntityBaseType[d.entityBaseType])
        .attr("rx", n =>
        n.entityBaseType == EntityBaseType.Entity ? 7 :
        n.entityBaseType == EntityBaseType.Part ? 4 :
        n.entityBaseType == EntityBaseType.SemiSymbol ? 5 :
        n.entityBaseType == EntityBaseType.Symbol ? 4 :
        n.entityBaseType == EntityBaseType.EnumEntity ? 3 : 0);

    var margin = 3;

    var label = nodeGroup.append("text")
        .attr("class", d => "node " + EntityBaseType[d.entityBaseType])
        .style("cursor", d=> d.findUrl ? "pointer" : null)
        .text(d=> d.niceName)
        .each(function (d) {
        wrap(this, 60);
        var b = this.getBBox();
        d.width = b.width + margin * 2;
        d.height = b.height + margin * 2;
    });

    node.attr("width", d=> d.width)
        .attr("height", d=> d.height);

    function selectedNode() {
        label.style("font-weight", d=> d == selectedTable ? "bold" : null);
    }

    filter.keyup(() => {
        restart();
        selectedLinks();
        nodeGroup.style("display", n=> nodes.indexOf(n) == -1 ? "none" : "inline");
        link.style("display", r=> links.indexOf(r) == -1 ? "none" : "inline");
    });

    label.attr("transform", d=> "translate(" + d.width / 2 + ", 0)");

    var titles = label.append('svg:title');

    function drawColor() {

        var colorVal = colorCombo.val();

        getProvider(colorVal, nodes).then(cp=> {
            node.style("fill", cp.getFill)
                .style("stroke", cp.getStroke || cp.getFill)
                .style("mask", cp.getMask);

            titles.text(t => cp.getTooltip(t) + " (" + EntityBaseType[t.entityBaseType] + ")");
        });
    };


    drawColor();

    colorCombo.change(() => drawColor());

    force.on("tick", function () {

        nodes.forEach(d=> {
            d.nx = d.x;
            d.ny = d.y;
        });

        namespaceClustering();
        gravity();

        nodes.forEach(d=> {
            d.x = d.nx;
            d.y = d.ny;
        });

        link.each(rel=> {
            rel.sourcePoint = calculatePoint(<ITableInfo>rel.source, rel.target);
            rel.targetPoint = calculatePoint(<ITableInfo>rel.target, rel.source);
        });

        link.attr("x1", l=> l.sourcePoint.x)
            .attr("y1", l=> l.sourcePoint.y)
            .attr("x2", l=> l.targetPoint.x)
            .attr("y2", l=> l.targetPoint.y);

        nodeGroup.attr("transform", d => "translate(" + (d.x - d.width / 2) + ", " + (d.y - d.height / 2) + ")");
    });



    function gravity() {

        function gravityDim(v: number, min: number, max: number): number {

            var minF = min + 100;
            var maxF = max - 100;

            var dist =
                maxF < v ? maxF - v :
                    v < minF ? minF - v : 0;

            return dist * force.alpha() * 0.4;
        }

        nodes.forEach(n=> {
            n.nx += gravityDim(n.x, 0, width);
            n.ny += gravityDim(n.y, 0, height);
        });
    }

    function namespaceClustering() {

        var quadtree = d3.geom.quadtree(nodes, width, height);

        var constant =
            nodes.length < 10 ? 100 :
            nodes.length < 20 ? 50 :
            nodes.length < 50 ? 30 :
            nodes.length < 100 ? 20 :
            nodes.length < 200 ? 15 :10;

        nodes.forEach(d=> {
            quadtree.visit((quad: { point: ITableInfo }, x1: number, y1: number, x2: number, y2: number) => {

                if (quad.point && quad.point != d) {

                    var x = d.x - quad.point.x,
                        y = d.y - quad.point.y,
                        l = Math.sqrt(x * x + y * y);

                    var lx = x / l;
                    var ly = y / l;

                    var ratio = l / 30;

                    var f = constant * force.alpha() / Math.max(ratio * ratio, 0.1);

                    if (d.namespace != quad.point.namespace)
                        f *= 4;

                    d.nx += lx * f;
                    d.ny += ly * f;
                }

                var dx = distance(d.x, x1, x2);
                var dy = distance(d.y, y1, y2);

                var dist = Math.sqrt(dx * dx + dy * dy);

                return dist > 400;

                return false;
            });
        });

    }
}

function distance(v: number, min: number, max: number) {
    if (v < min)
        return min - v;

    if (max < v)
        return v - max;

    return 0;
}


export function wrap(textElement: SVGTextElement, width: number) {
    var text = d3.select(textElement);
    var words: string[] = text.text().split(/\s+/).reverse();
    var word: string;

    var line: string[] = [];
    var tspan = text.text(null).append("tspan")
        .attr("x", 0)
        .attr("dy", "1.2em");

    while (word = words.pop()) {
        line.push(word);
        tspan.text(line.join(" "));
        if ((<SVGTSpanElement> tspan.node()).getComputedTextLength() > width && line.length > 1) {
            line.pop();
            tspan.text(line.join(" "));
            line = [word];
            tspan = text.append("tspan")
                .attr("x", 0)
            //.attr("y", y)
                .attr("dy", "1.2em").text(word);
        }
    }
}


export function colorScale(max : number) : D3.Scale.LinearScale {
    return d3.scale.linear()
        .domain([0, max / 4, max])
        .range(["green", "gold", "red"]);

}

export function colorScaleSqr(max: number): D3.Scale.LinearScale {
    return d3.scale.sqrt()
        .domain([0, max / 4, max])
        .range(["green", "gold", "red"]);

}


export function calculatePoint(rectangle: Rectangle, point: Point): Point {

    var vector = { x: point.x - rectangle.x, y: point.y - rectangle.y };

    var v2 = { x: rectangle.width / 2, y: rectangle.height / 2 };

    var ratio = getRatio(vector, v2);

    return { x: rectangle.x + vector.x * ratio, y: rectangle.y + vector.y * ratio };
}


function getRatio(vOut: Point, vIn: Point) {

    var vOut2 = { x: vOut.x, y: vOut.y };

    if (vOut2.x < 0)
        vOut2.x = -vOut2.x;

    if (vOut2.y < 0)
        vOut2.y = -vOut2.y;

    if (vOut2.x == 0 && vOut2.y == 0)
        return null;

    if (vOut2.x == 0)
        return vIn.y / vOut2.y;

    if (vOut2.y == 0)
        return vIn.x / vOut2.x;

    return Math.min(vIn.x / vOut2.x, vIn.y / vOut2.y);
}