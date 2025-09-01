import * as d3 from "d3"
import { EntityData, EntityKind } from '@framework/Reflection'
import { Finder } from '@framework/Finder'
import * as AppContext from '@framework/AppContext'
import { Point, ITableInfo, Rectangle, TableInfo, MListTableInfo, ClientColorProvider, IRelationInfo, SchemaMapInfo, RelationInfo } from './ClientColorProvider'
import { calculatePoint, wrap, forceBoundingBox } from '../Utils'


export class SchemaMapD3 {

  nodes!: ITableInfo[];
  links!: IRelationInfo[];
  simulation: d3.Simulation<ITableInfo, IRelationInfo>;
  fanIn: { [key: string]: IRelationInfo[] };

  selectedTable: ITableInfo | undefined;

  link: d3.Selection<SVGPathElement, IRelationInfo, any, any>;

  nodeGroup: d3.Selection<SVGGElement, ITableInfo, any, any>;
  node: d3.Selection<SVGRectElement, ITableInfo, any, any>;
  label: d3.Selection<SVGTextElement, ITableInfo, any, any>;
  titles: d3.Selection<SVGTitleElement, ITableInfo, any, any>;

  constructor(
    public svgElement: SVGElement,
    public providers: { [name: string]: ClientColorProvider },
    public map: SchemaMapInfo,
    public filter: string,
    public color: string,
    public width: number,
    public height: number) {

    this.simulation = d3.forceSimulation<ITableInfo, IRelationInfo>()
      .force("bounding", forceBoundingBox(width, height))
      .force("repulsion", d3.forceManyBody().strength(-120))
      .force("collide", d3.forceCollide(30));

    this.fanIn = map.relations.groupToObject(a => a.toTable);

    this.regenerate();

    const svg = d3.select(svgElement)
      .attr("width", width)
      .attr("height", height);

    this.link = svg.append<SVGGElement>("svg:g").attr("class", "links").selectAll(".link")
      .data(map.allLinks)
      .enter().append<SVGPathElement>("path")
      .attr("class", "link")
      .style("stroke-dasharray", d => (<RelationInfo>d).isVirtualMListBackReference? "4 4" : (d as RelationInfo).lite ? "2, 2" : null)
      .style("stroke", "var(--bs-body-color)")
      .attr("marker-end", d => "url(#" + (d.isMList ? "mlist_arrow" : (<RelationInfo>d).lite ? "lite_arrow" : "normal_arrow") + ")")
      .attr("marker-start", d => (<RelationInfo>d).isVirtualMListBackReference ? "url(#virtual_mlist_arrow)" : null);

    this.selectedLinks();

    const nodesG = svg.append<SVGGElement>("svg:g").attr("class", "nodes");


    const drag = d3.drag<SVGGElement, ITableInfo>()
      .on("start", (e, d) => {
        if (!e.active)
          this.simulation.alphaTarget(0.3).restart();

        d.fx = d.x;
        d.fy = d.y;
      })
      .on("drag", (e, d) => {
        d.fx = e.x;
        d.fy = e.y;
      })
      .on("end", d => {
        this.simulation.alphaTarget(0);
      });

    this.nodeGroup = nodesG.selectAll(".nodeGroup")
      .data(map.allNodes)
      .enter()
      .append<SVGGElement>("svg:g").attr("class", "nodeGroup")
      .style("cursor", d => (d as TableInfo).typeName && Finder.isFindable((d as TableInfo).typeName, true) ? "pointer" : null)
      .on("click", (e, d) => {

        this.selectedTable = this.selectedTable == d ? undefined : d;

        this.selectedLinks();
        this.selectedNode();

        if (e.defaultPrevented)
          return;

        if (e.ctrlKey && (d as TableInfo).typeName) {
          window.open(AppContext.toAbsoluteUrl(Finder.findOptionsPath({ queryName: (d as TableInfo).typeName })));
          e.preventDefault();
        }
      })
      .on("dblclick", (e, d) => {
        d.fx = null;
        d.fy = null;
        this.simulation.alpha(0.3).restart();
      })
      .call(drag);

    this.node = this.nodeGroup.append<SVGRectElement>("rect")
      .attr("class", d => "node " + d.entityBaseType)
      .attr("rx", n =>
        n.entityBaseType == "Entity" ? 7 :
          n.entityBaseType == "Part" ? 4 :
            n.entityBaseType == "SemiSymbol" ? 5 :
              n.entityBaseType == "Symbol" ? 4 :
                n.entityBaseType == "EnumEntity" ? 3 : 0);


    const margin = 3;

    this.label = this.nodeGroup.append<SVGTextElement>("text")
      .attr("class", d => "node " + d.entityBaseType)
      .style("cursor", d => (d as TableInfo).typeName ? "pointer" : null)
      .text(d => d.niceName)
      .each(function (d) {
        const text = this as SVGTextElement;
        wrap(text, 60);
        const b = text.getBBox();
        d.width = b.width + margin * 2;
        d.height = b.height + margin * 2;
      });

    this.node.attr("width", d => d.width)
      .attr("height", d => d.height);

    this.selectedNode();

    this.showHideNodes();

    this.label.attr("transform", d => "translate(" + d.width / 2 + ", 0)");

    this.titles = this.label.append<SVGTitleElement>('svg:title');

    this.drawColor();

    this.simulation.on("tick", this.onTick);
  }


  regenerate(): void {

    const parts = this.filter.match(/[+-]?((\w+)|\*)/g);

    function isMatch(str: string): boolean {

      if (!parts)
        return true;

      for (let i = parts.length - 1; i >= 0; i--) {
        const p = parts[i];
        const pair = p.startsWith("+") ? { isPositive: true, token: p.after("+") } :
          p.startsWith("-") ? { isPositive: false, token: p.after("-") } :
            { isPositive: true, token: p };

        if (pair.token == "*" || str.toLowerCase().contains(pair.token.toLowerCase()))
          return pair.isPositive;
      }

      return false;
    };

    this.nodes = this.map.allNodes.filter((n, i) => this.filter == undefined ||
      isMatch(n.namespace.toLowerCase() + "|" + n.tableName.toLowerCase() + "|" + n.niceName.toLowerCase()));

    this.links = this.map.allLinks.filter(l =>
      this.nodes.contains(<ITableInfo>l.source) &&
      this.nodes.contains(<ITableInfo>l.target));

    const numNodes = this.nodes.length;

    let distance =
      numNodes < 10 ? 110 :
        numNodes < 20 ? 80 :
          numNodes < 30 ? 65 :
            numNodes < 50 ? 50 :
              numNodes < 100 ? 35 :
                numNodes < 200 ? 30 : 25;

    this.simulation
      .force("link", d3.forceLink<ITableInfo, IRelationInfo>(this.links)
        .distance(d =>
          d.isMList ? distance * 0.7 :
            (d as RelationInfo).lite ? distance * 1.6 :
              distance * 1.2)
        .strength(d => 0.7 * (d.isMList ? 1 : this.getOpacity((<RelationInfo>d).toTable)))
      )
      .nodes(this.nodes)
      .alpha(1)
      .restart();
  }

  selectedLinks(): void {
    const selectedTable = this.selectedTable;
    this.link
      .style("stroke-width", d => d.source == selectedTable || d.target == selectedTable ? 1.5 : d.isMList ? 1.5 : 1)
      .style("opacity", d => d.source == selectedTable || d.target == selectedTable ? 1 : d.isMList ? 0.8 : Math.max(.1, this.getOpacity((<RelationInfo>d).toTable)));
  }

  selectedNode(): void {
    this.label.style("font-weight", d => d == this.selectedTable ? "bold" : null);
  }

  showHideNodes(): void {
    this.nodeGroup.style("display", n => this.nodes.indexOf(n) == -1 ? "none" : "inline");
    this.link.style("display", r => this.links.indexOf(r) == -1 ? "none" : "inline");
  }


  static opacities: number[] = [1, .9, .8, .7, .6, .5, .4, .3, .25, .2, .15, .1, .07, .05, .03, .02];

  getOpacity(toTable: string): number {
    const length = this.fanIn[toTable].filter(l => this.nodes.indexOf(<ITableInfo>l.source) != -1).length;

    const min = Math.min(length, SchemaMapD3.opacities.length - 1);

    return SchemaMapD3.opacities[min];
  }


  setFilter(newFilter: string): void {
    this.filter = newFilter;

    this.regenerate();
    this.selectedLinks();
    this.showHideNodes();
  }


  setColor(newColor: string): void {

    this.color = newColor;
    this.drawColor();
  }

  drawColor(): void {
    const cp = this.providers[this.color];

    this.node.style("fill", cp.getFill)
      .style("stroke", cp.getStroke || cp.getFill)
      .style("mask", a => cp.getMask && cp.getMask(a) || null);

    this.titles.text(t => cp.getTooltip(t) + " (" + t.entityBaseType + ")");
  }

  stop(): void {
    this.simulation.stop();
  }


  onTick = (): void => {

    const visibleLink = this.link.filter(f => this.links.indexOf(f) != -1);

    visibleLink.each(rel => {
      rel.sourcePoint = calculatePoint(rel.source as ITableInfo, rel.target as ITableInfo);
      rel.targetPoint = calculatePoint(rel.target as ITableInfo, rel.source as ITableInfo);
    });

    visibleLink.attr("d", l => this.getPathExpression(l));

    this.nodeGroup.filter(d => this.nodes.indexOf(d) != -1)
      .attr("transform", d => "translate(" +
        (d.x! - d.width / 2) + ", " +
        (d.y! - d.height / 2) + ")");
  }

  getPathExpression(l: IRelationInfo): string {

    const s = l.sourcePoint;
    const t = l.targetPoint;

    if (l.source == l.target) {

      const dx = (l.repetitions % 2) * 2 - 1;
      const dy = ((l.repetitions + 1) % 2) * 2 - 1;

      const source = l.source as ITableInfo;

      const c = calculatePoint(source as ITableInfo, {
        x: source.x! + dx * (source.width / 2),
        y: source.y! + dy * (source.height / 2),
      });

      return `M${c.x} ${c.y} C ${c.x! + 50 * dx} ${c.y} ${c.x} ${c.y! + 50 * dy} ${c.x} ${c.y}`;
    } else {
      let p = this.getPointRepetitions(s, t, l.repetitions);
      return `M${s.x} ${s.y} Q ${p.x} ${p.y} ${t.x} ${t.y}`;
    }
  }

  getPointRepetitions(s: Point, t: Point, repetitions: number): Point {

    const m: Point = {
      x: (s.x! + t.x!) / 2,
      y: (s.y! + t.y!) / 2
    };

    const d: Point = {
      x: (s.x! - t.x!),
      y: (s.y! - t.y!)
    };

    let h = Math.sqrt(d.x! * d.x! + d.y! * d.y!);

    if (h == 0)
      h = 1;

    //0, 10, -10, 20, -20, 30, -30
    const repPixels = Math.floor(repetitions + 1 / 2) * ((repetitions % 2) * 2 - 1);

    const p: Point = {
      x: m.x! + (d.y! / h) * 20 * repPixels,
      y: m.y! - (d.x! / h) * 20 * repPixels
    };

    return p;
  }
}


