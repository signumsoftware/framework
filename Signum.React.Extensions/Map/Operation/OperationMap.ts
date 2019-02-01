import * as d3 from "d3"
import * as Finder from '@framework/Finder'
import { OperationLogEntity } from '@framework/Signum.Entities.Basics'
import { Point, Rectangle, calculatePoint, wrap, colorScale, forceBoundingBox } from '../Utils'

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
  fanInOutFactor: number;
}

export interface ForceNode extends d3.SimulationNodeDatum, Rectangle {
  key: string;
  nx?: number;
  ny?: number;
}

export interface ForceLink extends d3.SimulationLinkDatum<ForceNode> {
  isFrom: boolean;
}


export interface Transition {
  sourcePoint: Point;
  fromState: MapState;
  operation: MapOperation;
  toState: MapState;
  targetPoint: Point;
}

export class OperationMapD3 {

  static opacities = [1, .5, .3, .2, .1];

  simulation: d3.Simulation<ForceNode, ForceLink>;
  selectedNode: ForceNode | undefined;
  link: d3.Selection<SVGPathElement, Transition, any, any>;

  statesGroup!: d3.Selection<SVGGElement, MapState, any, any>;
  nodeStates!: d3.Selection<SVGRectElement, MapState, any, any>;
  labelStates!: d3.Selection<SVGTextElement, MapState, any, any>;

  operationsGroup!: d3.Selection<SVGGElement, MapOperation, any, any>;
  nodeOperations!: d3.Selection<SVGRectElement, MapOperation, any, any>;
  labelOperations!: d3.Selection<SVGTextElement, MapOperation, any, any>;

  constructor(
    public svgElement: SVGElement,
    public queryName: any,
    public map: OperationMapInfo,
    public color: string,
    public width: number,
    public height: number) {

    this.simulation = d3.forceSimulation<ForceNode, ForceLink>()
      .nodes(map.allNodes)
      .force("bounding", forceBoundingBox(width, height))
      .force("fx", d3.forceX(width / 2))
      .force("fy", d3.forceY(height / 2))
      .force("repulsion", d3.forceManyBody().strength(-200))
      .force("collide", d3.forceCollide(30))
      .force("links", d3.forceLink(map.allLinks))
      .force("fainInOut", forceFanInOut())
      ;


    const svg = d3.select(svgElement)
      .attr("width", width)
      .attr("height", height);


    this.link = svg.append<SVGGElement>("svg:g").attr("class", "links").selectAll(".link")
      .data(map.allTransition)
      .enter().append<SVGPathElement>("path")
      .attr("class", "link")
      .style("stroke", "black")
      .attr("marker-end", "url(#normal_arrow)");

    this.selectLinks();


    this.initStates(svg);
    this.initOperations(svg);

    this.simulation.on("tick", () => this.onTick());
  }

  initStates(svg: d3.Selection<SVGElement, any, any, any>) {

    const drag = d3.drag<SVGGElement, MapState>()
      .on("start", d => {
        if (!d3.event.active)
          this.simulation.alphaTarget(0.3).restart();

        d.fx = d.x;
        d.fy = d.y;
      })
      .on("drag", d => {
        d.fx = d3.event.x;
        d.fy = d3.event.y;
      })
      .on("end", d => {
        this.simulation.alphaTarget(0);
      });


    this.statesGroup = svg.append<SVGGElement>("svg:g").attr("class", "states")
      .selectAll(".stateGroup")
      .data(this.map.states)
      .enter()
      .append<SVGGElement>("svg:g").attr("class", "stateGroup")
      .style("cursor", d => d.token ? "pointer" : null)
      .on("click", d => {

        this.selectedNode = this.selectedNode == d ? undefined : d;

        this.selectLinks();
        this.selectNodes();

        const event = d3.event;
        if (event.defaultPrevented)
          return;

        if ((<any>event).ctrlKey && d.token) {
          window.open(Finder.findOptionsPath({ queryName: this.queryName, filterOptions: [{ token: d.token, value: d.key }] }));
          d3.event.preventDefault();
        }
      }).on("dblclick", d => {
        d.fx = null;
        d.fy = null;
        this.simulation.alpha(0.3).restart();
      }).call(drag);


    this.nodeStates = this.statesGroup.append<SVGRectElement>("rect")
      .attr("class", d => "state " + (
        d.isSpecial ? "special" :
          d.ignored ? "ignore" : undefined))
      .attr("rx", 5)
      .attr('fill-opacity', 0.1);

    this.onStateColorChange();

    const margin = 3;

    this.labelStates = this.statesGroup.append<SVGTextElement>("text")
      .attr("class", "state")
      .style("cursor", d => d.token ? "pointer" : null)
      .text(d => d.niceName)
      .each(function (d) {
        const svg = this;
        wrap(svg, 60);
        const b = svg.getBBox();
        d.width = b.width + margin * 2;
        d.height = b.height + margin * 2;
      });

    this.nodeStates.attr("width", d => d.width)
      .attr("height", d => d.height);

    this.labelStates.attr("transform", d => "translate(" + d.width / 2 + ", 0)");

    this.labelStates.append('svg:title')
      .text(t => t.niceName + " (" + t.count + ")");
  }

  initOperations(svg: d3.Selection<SVGElement, any, any, any>) {

    const drag = d3.drag<SVGGElement, MapOperation>()
      .on("start", d => {
        if (!d3.event.active)
          this.simulation.alphaTarget(0.3).restart();

        d.fx = d.x;
        d.fy = d.y;
      })
      .on("drag", d => {
        d.fx = d3.event.x;
        d.fy = d3.event.y;
      })
      .on("end", d => {
        this.simulation.alphaTarget(0);
      });

    this.operationsGroup = svg.append<SVGGElement>("svg:g").attr("class", "operations")
      .selectAll(".operation")
      .data(this.map.operations)
      .enter()
      .append<SVGGElement>("svg:g").attr("class", "operation")
      .style("cursor", "pointer")
      .on("click", d => {

        this.selectedNode = this.selectedNode == d ? undefined : d;

        this.selectLinks();
        this.selectNodes();

        const event = d3.event;
        if (event.defaultPrevented)
          return;

        if ((<any>event).ctrlKey) {
          window.open(Finder.findOptionsPath({ queryName: OperationLogEntity, filterOptions: [{ token: "Operation.Key", value: d.key }] }));
          d3.event.preventDefault();
        }
      }).on("dblclick", d => {
        d.fx = null;
        d.fy = null;
        this.simulation.alpha(0.3).restart();
      }).call(drag);

    this.nodeOperations = this.operationsGroup.append<SVGRectElement>("rect")
      .attr("class", "operation")

    const margin = 1;

    this.labelOperations = this.operationsGroup.append<SVGTextElement>("text")
      .attr("class", "operation")
      .style("cursor", "pointer")
      .text(d => d.niceName)
      .each(function (d) {
        const svg = this as SVGTextElement;
        wrap(svg, 60);
        const b = svg.getBBox();
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
      const colorStates = colorScale(this.map.states.map(a => a.count).max()!);
      c = d => colorStates(d.count);
    } else {
      const scale = d3.scaleOrdinal(d3.schemeCategory10);
      c = d => d.color || (d.isSpecial ? "lightgray" : scale(d.key));
    }

    this.nodeStates
      .attr('stroke', c)
      .attr('fill', c);
  }

  onOperationColorChange() {
    let c: (d: MapOperation) => any;

    if (this.color == "rows") {
      const colorOperations = colorScale(this.map.operations.map(a => a.count).max()!);
      c = d => colorOperations(d.count);
    } else {
      c = d => "transparent";
    }

    this.nodeOperations
      .attr('stroke', c)
      .attr('fill', c);
  }


  onTick() {


    //this.fanInOut();

    this.link.each(rel => {
      rel.sourcePoint = calculatePoint(<Rectangle><any>rel.fromState, rel.operation);
      rel.targetPoint = calculatePoint(<Rectangle><any>rel.toState, rel.operation);
    });

    this.link.attr("d", l => this.getPathExpression(l));

    this.statesGroup.attr("transform", d => "translate(" + (d.x! - d.width / 2) + ", " + (d.y! - d.height / 2) + ")");
    this.operationsGroup.attr("transform", d => "translate(" + (d.x! - d.width / 2) + ", " + (d.y! - d.height / 2) + ")");

  }

  getPathExpression(t: Transition) {
    if (t.fromState == t.toState) {

      const dx = t.sourcePoint.x! - t.operation.x!;
      const dy = t.sourcePoint.y! - t.operation.y!;

      return `M${t.sourcePoint.x} ${t.sourcePoint.y} C ${t.operation.x! - dy} ${t.operation.y! + dx} ${t.operation.x! + dy} ${t.operation.y! - dx} ${t.targetPoint.x} ${t.targetPoint.y}`;
    }

    return `M${t.sourcePoint.x} ${t.sourcePoint.y} Q ${t.operation.x} ${t.operation.y} ${t.targetPoint.x} ${t.targetPoint.y}`;
  }


  stop() {
    this.simulation.stop();
  }
}


export function forceFanInOut<T extends d3.SimulationNodeDatum>() {
  var nodes: MapState[];
  const fanInConstant = 30;
  function force(alpha: number) {
    nodes.forEach(d => {
      if (d.fanInOutFactor != null) {
        d.vx = d.vx! + d.fanInOutFactor * fanInConstant * alpha;
      }
    });
  }

  (force as any).initialize = function (_: MapState[]) {
    nodes = _;
  };

  return force;
}

