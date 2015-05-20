/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/d3/d3.d.ts"/>

import d3 = require("d3")
import SchemaMap = require("Extensions/Signum.Web.Extensions/Maps/Scripts/SchemaMap")

export interface MapNode extends D3.Layout.GraphNode, Rectangle {
    nx: number;
    ny: number;
}

export interface MapOperation extends MapNode {
    key: string;
    niceName: string;
    link: string;
    count: number;
    fromStates: string[];
    toStates: string[];
}

export interface MapState extends MapNode {
    key: string;
    niceName: string;
    link: string;
    count: number;
    ignored: boolean;
    fanOut: number;
    fanIn: number;
    color: string;
}

export interface Point {
    x: number;
    y: number;
}


export interface Rectangle extends Point {
    width: number;
    height: number;
}

export interface OperationMapInfo {
    states: MapState[];
    operations: MapOperation[];
}

export interface Line extends D3.Layout.GraphLink {
    isFrom: boolean;
    sourcePoint?: Point;
    targetPoint?: Point;
}

function getOperation(line: Line): MapOperation {
    return line.isFrom ? <MapOperation>line.source : <MapOperation>line.target;
}

function similarLinks(line: Line) {
    return line.isFrom ?
        (<MapOperation>line.target).fromStates.length :
        (<MapOperation>line.source).toStates.length
}


export function createMap(mapId: string, svgMapId: string, colorId: string, map: OperationMapInfo) {

    var div = mapId.get();
    var colorCombo = colorId.get();

    //div.closest(".container").removeClass("container").addClass("container-fluid");

    div.css("width", "100%");
    div.css("height", (window.innerHeight - 200) + "px");

    var width = div.width(),
        height = div.height();

    var nodes = (<MapNode[]>map.operations).concat(map.states);

    var nodesDic = map.states.toObject(g=> g.key);

    var fromRelationships = map.operations.filter(op=> op.fromStates != null)
        .flatMap(op=> op.fromStates.map(s=> <Line>{ source: nodesDic[s], target: op, isFrom: true }));

    var toRelationships = map.operations.filter(op=> op.toStates != null)
        .flatMap(op=> op.toStates.map(s=> <Line>{ source: op, target: nodesDic[s], isFrom: false }));

    var links = fromRelationships.concat(toRelationships);

    var opacities = [1, .5, .3, .2, .1];

    var fanOut = map.operations.flatMap(a=> a.fromStates.map(s=> ({ s: s, weight: 1.0 / a.fromStates.length }))).groupByObject(a=> a.s);
    var fanIn = map.operations.flatMap(a=> a.toStates.map(s=> ({ s: s, weight: 1.0 / a.toStates.length }))).groupByObject(a=> a.s);

    map.states.forEach(m=> {
        m.fanOut = (fanOut[m.key] ? fanOut[m.key].reduce((acum, e) => acum + e.weight, 0) : 0);
        m.fanIn = (fanIn[m.key] ? fanIn[m.key].reduce((acum, e) => acum + e.weight, 0) : 0);
    });

    var force = d3.layout.force()
        .gravity(0)
        .linkDistance(80)
        .charge(-600)
    //.charge(10)
    //.linkStrength((d: Line) => 0.7 * opacities[Math.min(similarLinks(d), opacities.length - 1)])
    //.linkStrength(20)
        .size([width, height]);

    var colorStates = SchemaMap.colorScale(map.states.map(a=> a.count).max());
    var colorOperations = SchemaMap.colorScale(map.operations.map(a=> a.count).max());

    force
        .nodes(nodes)
        .links(links)
        .start();

    var svg = d3.select("#" + svgMapId)
        .attr("width", width)
        .attr("height", height);



    var link = svg.append("svg:g").attr("class", "links").selectAll(".link")
        .data(links)
        .enter().append("line")
        .attr("class", "link")
        .style("stroke", "black")
        .attr("marker-end", (d: Line) => d.isFrom ? null : "url(#normal_arrow)");

    var selectedTable: D3.Layout.GraphNode;

    function selectedLinks() {
        link.style("stroke-width", d => d.source == selectedTable || d.target == selectedTable ? 1.5 : 1)
            .style("opacity", d => d.source == selectedTable || d.target == selectedTable ? 1 :
            Math.max(.1, opacities[Math.min(similarLinks(d), opacities.length - 1)]));
    };

    selectedLinks();

    var statesGroup = svg.append("svg:g").attr("class", "states")
        .selectAll(".stateGroup")
        .data(map.states)
        .enter()
        .append("svg:g").attr("class", "stateGroup")
        .style("cursor", d=> d.link ? "pointer" : null)
        .on("click", d=> {

        selectedTable = selectedTable == d ? null : d;

        selectedLinks();
        selectedNode();

        var event = d3.event;
        if (event.defaultPrevented)
            return;

        if ((<any>event).ctrlKey && d.link) {
            window.open(d.link);
            d3.event.preventDefault();
            return false;
        }
    }).call(force.drag);

    var labelStates: D3.Selection<MapState>;
    {


        var nodeStates = statesGroup.append("rect")
            .attr("class", d => "state " + (
            d.key == "DefaultState.Start" ? "start" :
                d.key == "DefaultState.All" ? "all" :
                    d.key == "DefaultState.End" ? "end" :
                        d.ignored ? "ignore" : null))
            .attr("rx", 5)
            .attr('fill-opacity', 0.1)
        //.style("fill", "white")
        //.style("stroke", "black");


        function onStateColorChange() {

            function color(d: MapState): any {
                if (colorCombo.val() == "rows")
                    return colorStates(d.count);
                else
                    return d.color || (d.key.startsWith("DefaltState") ? null : "lightgray");
            }

            nodeStates
                .attr('stroke', color)
                .attr('fill', color);
        }

        colorCombo.change(onStateColorChange);

        onStateColorChange();

        var margin = 3;

        labelStates = statesGroup.append("text")
            .attr("class", "state")
            .style("cursor", d=> d.link ? "pointer" : null)
            .text(d=> d.niceName)
            .each(function (d) {
            SchemaMap.wrap(this, 60);
            var b = this.getBBox();
            d.width = b.width + margin * 2;
            d.height = b.height + margin * 2;
        });

        nodeStates.attr("width", d=> d.width)
            .attr("height", d=> d.height);

        labelStates.attr("transform", d=> "translate(" + d.width / 2 + ", 0)");

        labelStates.append('svg:title')
            .text(t => t.niceName + " (" + t.count + ")");
    }


    var operationsGroup = svg.append("svg:g").attr("class", "operations")
        .selectAll(".operation")
        .data(map.operations)
        .enter()
        .append("svg:g").attr("class", "operation")
        .style("cursor", d=> d.link ? "pointer" : null)
        .on("click", d=> {

        selectedTable = selectedTable == d ? null : d;

        selectedLinks();
        selectedNode();

        var event = d3.event;
        if (event.defaultPrevented)
            return;

        if ((<any>event).ctrlKey && d.link) {
            window.open(d.link);
            d3.event.preventDefault();
            return false;
        }
    }).call(force.drag);

    var labelOperations: D3.Selection<MapOperation>;
    {
        var nodeOperations = operationsGroup.append("rect")
            .attr("class", "operation")

        var margin = 1;

        labelOperations = operationsGroup.append("text")
            .attr("class", "operation")
            .style("cursor", d=> d.link ? "pointer" : null)
            .text(d=> d.niceName)
            .each(function (d) {
            SchemaMap.wrap(this, 60);
            var b = this.getBBox();
            d.width = b.width + margin * 2;
            d.height = b.height + margin * 2;
        });

        function onOperationColorChange() {

            function color(d: MapOperation): any {
                if (colorCombo.val() == "rows")
                    return colorOperations(d.count);
                else
                    return "transparent";
            }

            nodeOperations
                .attr('stroke', color)
                .attr('fill', color);
        }

        colorCombo.change(onOperationColorChange);

        onOperationColorChange();

        nodeOperations.attr("width", d => d.width)
            .attr("height", d => d.height);

        labelOperations.attr("transform", d => "translate(" + d.width / 2 + ", 0)");

        labelOperations.append('svg:title')
            .text(t => t.niceName + " (" + t.count + ")");
    }

    function selectedNode() {
        labelStates.style("font-weight", d=> d == selectedTable ? "bold" : null);
        labelOperations.style("font-weight", d=> d == selectedTable ? "bold" : null);
    }

    force.on("tick", function () {

        nodes.forEach(d=> {
            d.nx = d.x;
            d.ny = d.y;
        });

        gravity();

        nodes.forEach(d=> {
            d.x = d.nx;
            d.y = d.ny;
        });

        fanInOut();

        link.each(rel=> {
            rel.sourcePoint = SchemaMap.calculatePoint(<Rectangle><any>rel.source, rel.target);
            rel.targetPoint = SchemaMap.calculatePoint(<Rectangle><any>rel.target, rel.source);
        });

        link.attr("x1", l=> l.sourcePoint.x)
            .attr("y1", l=> l.sourcePoint.y)
            .attr("x2", l=> l.targetPoint.x)
            .attr("y2", l=> l.targetPoint.y);

        statesGroup.attr("transform", d => "translate(" + (d.x - d.width / 2) + ", " + (d.y - d.height / 2) + ")");
        operationsGroup.attr("transform", d => "translate(" + (d.x - d.width / 2) + ", " + (d.y - d.height / 2) + ")");
    });

    var fanInConstant = 0.05;

    function fanInOut() {
        map.states.forEach(function (d) {
            if (d.fanOut > 0)
                d.y -= d.y * d.fanOut * fanInConstant * force.alpha();

            if (d.fanIn > 0)
                d.y += (height - d.y) * d.fanIn * fanInConstant * force.alpha();
        });
    }




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
}

