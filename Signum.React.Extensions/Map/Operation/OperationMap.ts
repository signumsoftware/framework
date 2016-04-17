import * as d3 from "d3"
import * as React from "react"
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { OperationLogEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { Point, Rectangle, calculatePoint, wrap, colorScale } from '../Utils'

export interface OperationMapInfo {
    states: MapState[];
    operations: MapOperation[];
    allNodes: ForceNode[];
    allLinks: ForceLink[];
    allTransition: Transition[];
}

export interface MapOperation extends ForceNode {
    key: string;
    niceName: string;
    count: number;
    fromStates: string[];
    toStates: string[];
}

export interface MapState extends ForceNode {
    key: string;
    niceName: string;
    count: number;
    ignored: boolean;
    isSpecial: boolean;
    color: string;
    token: string;
    fanOut: number;
    fanIn: number;
}

export interface ForceNode extends d3.layout.force.Node, Rectangle {
    key: string;
    nx: number;
    ny: number;
}

export interface ForceLink extends d3.layout.force.Link<ForceNode> {
    isFrom: boolean;
}


export interface Transition {
    sourcePoint?: Point;
    fromState: MapState;
    operation: MapOperation;
    toState: MapState;
    targetPoint?: Point;
}

export class OperationMapD3 {

    static opacities = [1, .5, .3, .2, .1];

    force: d3.layout.Force<ForceLink, ForceNode>;
    selectedNode: ForceNode;
    link: d3.Selection<Transition>;

    statesGroup: d3.Selection<MapState>;
    nodeStates: d3.Selection<MapState>;
    labelStates: d3.Selection<MapState>;

    operationsGroup: d3.Selection<MapOperation>;
    nodeOperations: d3.Selection<MapOperation>;
    labelOperations: d3.Selection<MapOperation>;

    constructor(
        public svgElement: SVGElement,
        public queryName: any,
        public map: OperationMapInfo,
        public color: string,
        public width: number,
        public height: number) {


        this.force = d3.layout.force<ForceLink, ForceNode>()
            .gravity(0)
            .linkDistance(80)
            .charge(-600)
            //.charge(10)
            //.linkStrength((d: Line) => 0.7 * opacities[Math.min(similarLinks(d), opacities.length - 1)])
            //.linkStrength(20)
            .size([width, height]);

        var colorStates = colorScale(map.states.map(a => a.count).max());
        var colorOperations = colorScale(map.operations.map(a => a.count).max());

        this.force
            .nodes(map.allNodes)
            .links(map.allLinks)
            .start();

        var svg = d3.select(svgElement)
            .attr("width", width)
            .attr("height", height);


        this.link = svg.append("svg:g").attr("class", "links").selectAll(".link")
            .data(map.allTransition)
            .enter().append("path")
            .attr("class", "link")
            .style("stroke", "black")
            .attr("marker-end", "url(#normal_arrow)");

        this.selectLinks();


        this.initStates(svg);
        this.initOperations(svg);

        this.force.on("tick", () => this.onTick());
    }

    initStates(svg: d3.Selection<any>) {

        const drag = this.force.drag()
            .on("dragstart", d => d.fixed = true);

        this.statesGroup = svg.append("svg:g").attr("class", "states")
            .selectAll(".stateGroup")
            .data(this.map.states)
            .enter()
            .append("svg:g").attr("class", "stateGroup")
            .style("cursor", d => d.token ? "pointer" : null)
            .on("click", d => {

                this.selectedNode = this.selectedNode == d ? null : d;

                this.selectLinks();
                this.selectNodes();

                var event = d3.event;
                if (event.defaultPrevented)
                    return;

                if ((<any>event).ctrlKey && d.token) {
                    window.open(Finder.findOptionsPath({ queryName: this.queryName, filterOptions: [{ columnName: d.token, value: d.key }] }));
                    d3.event.preventDefault();
                    return false;
                }
            }).on("dblclick", d => {
                d.fixed = false;
            }).call(drag);


        this.nodeStates = this.statesGroup.append("rect")
            .attr("class", d => "state " + (
                d.isSpecial ? "special" :
                d.ignored ? "ignore" : null))
            .attr("rx", 5)
            .attr('fill-opacity', 0.1);

        this.onStateColorChange();

        var margin = 3;

        this.labelStates = this.statesGroup.append("text")
            .attr("class", "state")
            .style("cursor", d => d.token ? "pointer" : null)
            .text(d => d.niceName)
            .each(function (d) {
                wrap(this, 60);
                var b = this.getBBox();
                d.width = b.width + margin * 2;
                d.height = b.height + margin * 2;
            });

        this.nodeStates.attr("width", d => d.width)
            .attr("height", d => d.height);

        this.labelStates.attr("transform", d => "translate(" + d.width / 2 + ", 0)");

        this.labelStates.append('svg:title')
            .text(t => t.niceName + " (" + t.count + ")");
    }

    initOperations(svg: d3.Selection<any>) {

        const drag = this.force.drag()
            .on("dragstart", d => d.fixed = true);

        this.operationsGroup = svg.append("svg:g").attr("class", "operations")
            .selectAll(".operation")
            .data(this.map.operations)
            .enter()
            .append("svg:g").attr("class", "operation")
            .style("cursor", "pointer")
            .on("click", d => {

                this.selectedNode = this.selectedNode == d ? null : d;

                this.selectLinks();
                this.selectNodes();

                var event = d3.event;
                if (event.defaultPrevented)
                    return;

                if ((<any>event).ctrlKey) {
                    window.open(Finder.findOptionsPath({ queryName: OperationLogEntity, filterOptions: [{ columnName: "Operation.Key", value: d.key }] }));
                    d3.event.preventDefault();
                    return false;
                }
            }).on("dblclick", d => {
                d.fixed = false;
            }).call(drag);

        this.nodeOperations = this.operationsGroup.append("rect")
            .attr("class", "operation")

        var margin = 1;

        this.labelOperations = this.operationsGroup.append("text")
            .attr("class", "operation")
            .style("cursor", "pointer")
            .text(d => d.niceName)
            .each(function (d) {
                wrap(this, 60);
                var b = this.getBBox();
                d.width = b.width + margin * 2;
                d.height = b.height + margin * 2;
            });


        this.onOperationColorChange();

        this.nodeOperations.attr("width", d => d.width + 2)
            .attr("height", d => d.height + 2);

        this.labelOperations.attr("transform", d => "translate(" + ((d.width / 2) + 1) + ", -1)");

        this.labelOperations.append('svg:title')
            .text(t => t.niceName + " (" + t.count + ")");
    }

    selectLinks() {
        this.link.style("stroke-width", d => d.fromState == this.selectedNode || d.toState == this.selectedNode || d.operation == this.selectedNode ? 1.5 : 1)
            .style("opacity", d => d.fromState == this.selectedNode || d.toState == this.selectedNode || d.operation == this.selectedNode ? 1 : 0.5);
    }

    selectNodes() {
        this.labelStates.style("font-weight", d => d == this.selectedNode ? "bold" : null);
        this.labelOperations.style("font-weight", d => d == this.selectedNode ? "bold" : null);
    }


    setColor(newColor: string) {
        this.color = newColor;
        this.onStateColorChange();
        this.onOperationColorChange();
    }

    onStateColorChange() {

        let c: (d: MapState) => any;

        if (this.color == "rows") {
            const colorStates = colorScale(this.map.states.map(a => a.count).max());
            c = d => colorStates(d.count);
        } else {
            var scale = d3.scale.category10();
            c = d => d.color || (d.isSpecial ? "lightgray" : scale(d.key));
        }

        this.nodeStates
            .attr('stroke', c)
            .attr('fill', c);
    }

    onOperationColorChange() {
        let c: (d: MapOperation) => any;

        if (this.color == "rows") {
            const colorOperations = colorScale(this.map.operations.map(a => a.count).max());
            c = d => colorOperations(d.count);
        } else {
            c = d => "transparent";
        }

        this.nodeOperations
            .attr('stroke', c)
            .attr('fill', c);
    }


    onTick() {

        this.map.allNodes.forEach(d => {
            d.nx = d.x;
            d.ny = d.y;
        });

        this.gravity();

        this.map.allNodes.forEach(d => {
            d.x = d.nx;
            d.y = d.ny;
        });

        this.fanInOut();

        this.link.each(rel => {
            rel.sourcePoint = calculatePoint(<Rectangle><any>rel.fromState, rel.operation);
            rel.targetPoint = calculatePoint(<Rectangle><any>rel.toState, rel.operation);
        });

        this.link.attr("d", l => this.getPathExpression(l));

        this.statesGroup.attr("transform", d => "translate(" + (d.x - d.width / 2) + ", " + (d.y - d.height / 2) + ")");
        this.operationsGroup.attr("transform", d => "translate(" + (d.x - d.width / 2) + ", " + (d.y - d.height / 2) + ")");

    }

    getPathExpression(t: Transition) {
        if (t.fromState == t.toState) {

            var dx = t.sourcePoint.x - t.operation.x;
            var dy = t.sourcePoint.y - t.operation.y;

            return `M${t.sourcePoint.x} ${t.sourcePoint.y} C ${t.operation.x - dy} ${t.operation.y + dx} ${t.operation.x + dy} ${t.operation.y - dx} ${t.targetPoint.x} ${t.targetPoint.y}`;
        }


        return `M${t.sourcePoint.x} ${t.sourcePoint.y} Q ${t.operation.x} ${t.operation.y} ${t.targetPoint.x} ${t.targetPoint.y}`;
    }

    fanInOut() {

        const fanInConstant = 0.05;
        this.map.states.forEach(d => {
            if (d.fanOut > 0)
                d.y -= d.y * d.fanOut * fanInConstant * this.force.alpha();

            if (d.fanIn > 0)
                d.y += (this.height - d.y) * d.fanIn * fanInConstant * this.force.alpha();
        });
    }


    gravity() {
        this.map.allNodes.forEach(n => {
            n.nx += this.gravityDim(n.x, 0, this.width);
            n.ny += this.gravityDim(n.y, 0, this.height);
        });
    }

    gravityDim(v: number, min: number, max: number): number {

        var minF = min + 100;
        var maxF = max - 100;

        var dist =
            maxF < v ? maxF - v :
                v < minF ? minF - v : 0;

        return dist * this.force.alpha() * 0.4;
    }

    stop() {
        this.force.stop();
    }

}