import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as QueryString from "query-string"
import { RouteComponentProps } from 'react-router'
import { Dic } from '@framework/Globals'
import * as Navigator from '@framework/Navigator'
import { JavascriptMessage } from '@framework/Signum.Entities'
import { MapMessage } from '../Signum.Entities.Map'
import * as MapClient from '../MapClient'
import { OperationMapInfo, OperationMapD3, ForceNode, ForceLink, Transition } from './OperationMap'
import "./operationMap.css"

interface OperationMapPageProps extends RouteComponentProps<{ type: string }> {

}

interface OperationMapPropsState {
  operationMapInfo?: OperationMapInfo;
  width?: number;
  height?: number;
  parsedQuery?: ParsedQueryString;
  color?: string;
}

interface ParsedQueryString {
  color?: string;
  nodes: { [tableName: string]: { x: number; y: number } };
}

export default class OperationMapPage extends React.Component<OperationMapPageProps, OperationMapPropsState> {
  state = { filter: "", color: "" } as OperationMapPropsState;
  wasExpanded!: boolean;

  componentWillMount() {
    this.wasExpanded = Navigator.Expander.setExpanded(true);

    MapClient.API.operations(this.props.match.params.type)
      .then(omi => {
        const parsedQuery = this.getParsedQuery();

        this.setState({
          operationMapInfo: omi,
          parsedQuery: parsedQuery,
          color: parsedQuery.color
        });
      }).done();
  }

  componentWillUnmount() {
    Navigator.Expander.setExpanded(this.wasExpanded);
  }

  getParsedQuery(): ParsedQueryString {
    const result: ParsedQueryString = { nodes: {} };

    const query = QueryString.parse(this.props.location.search) as { [name: string]: string };
    if (!query)
      return result;

    Dic.foreach(query, (name, value) => {

      if (name == "color")
        result.color = value;
      else {
        result.nodes[name] = {
          x: parseFloat(value.before(",")),
          y: parseFloat(value.after(",")),
        };
      }
    });

    return result;
  }

  div!: HTMLDivElement;
  handleSetInitialSize = (div: HTMLDivElement) => {
    if (this.div)
      return;

    this.div = div;
    const rect = div.getBoundingClientRect();
    this.setState({ width: rect.width, height: window.innerHeight - 200 });
  }

  render() {
    if (Navigator.Expander.onGetExpanded && !Navigator.Expander.onGetExpanded())
      return null;

    const s = this.state;
    return (
      <div ref={this.handleSetInitialSize}>
        {this.renderFilter()}
        {!s.operationMapInfo || this.div == undefined ?
          <span>{JavascriptMessage.loading.niceToString()}</span> :
          <OperationMapRenderer operationMapInfo={s.operationMapInfo} parsedQuery={s.parsedQuery!} color={s.color!} height={s.height!} width={s.width!} queryName={this.props.match.params.type} />}
      </div>
    );
  }

  handleSetColor = (e: React.FormEvent<any>) => {
    this.setState({
      color: (e.currentTarget as HTMLInputElement).value
    });
  }

  handleFullscreenClick = (e: React.MouseEvent<any>) => {

    e.preventDefault();

    const s = this.state;

    const tables = s.operationMapInfo!.allNodes.filter(a => a.fx != null && a.fy != null)
      .toObject(a => a.key, a =>
        (a.fx! / s.width!).toPrecision(4) + "," +
        (a.fy! / s.height!).toPrecision(4));

    var query = { ...tables, color: s.color };

    const url = Navigator.history.createHref({
      pathname: "~/map/" + this.props.match.params.type,
      search: QueryString.stringify(query)
    });

    window.open(url);
  }

  renderFilter() {

    const s = this.state;

    return (
      <div className="form-inline form-sm container" style={{ marginTop: "10px" }}>
        <div className="form-group" style={{ marginLeft: "10px" }}>
          <label htmlFor="color"> {MapMessage.Color.niceToString()}</label>&nbsp;
                    <select className="form-control" id="color" value={s.color} onChange={this.handleSetColor}>
            <option value="state">{MapMessage.StateColor.niceToString()}</option>
            <option value="rows">{MapMessage.Rows.niceToString()}</option>
          </select>
        </div>
        <span style={{ marginLeft: "10px" }}>
          {MapMessage.Press0ToExploreEachTable.niceToString().formatHtml(<u>Ctrl + Click</u>)}
        </span>
        &nbsp;
                <a id="sfFullScreen" className="sf-popup-fullscreen" onClick={this.handleFullscreenClick} href="#">
          <FontAwesomeIcon icon="external-link-alt" />
        </a>
      </div>
    );
  }
}

export interface OperationMapRendererProps {
  queryName: string;
  operationMapInfo: OperationMapInfo;
  width: number;
  height: number;
  parsedQuery: ParsedQueryString;
  color: string;
}

export class OperationMapRenderer extends React.Component<OperationMapRendererProps, { mapD3?: OperationMapD3 }> {

  constructor(props: OperationMapRendererProps) {
    super(props);
    this.state = {};
  }

  componentDidMount() {
    const p = this.props;

    this.fixSchemaMap(p.operationMapInfo, p.parsedQuery);

    const d3 = new OperationMapD3(this.svg, p.queryName, p.operationMapInfo, p.color, p.width, p.height);
    this.setState({ mapD3: d3 });
  }


  fixSchemaMap(map: OperationMapInfo, parsedQuery: ParsedQueryString) {

    map.allNodes = (map.operations as ForceNode[]).concat(map.states);

    map.allNodes.forEach(a => {
      const c = parsedQuery.nodes[a.key];
      if (c) {
        a.fx = c.x * this.props.width;
        a.fy = c.y * this.props.height;
      } else {
        a.x = Math.random() * this.props.width;
        a.y = Math.random() * this.props.height;
      }
    });

    const statesDic = map.states.toObject(g => g.key);

    const fromRelationships = map.operations.filter(op => op.fromStates != undefined)
      .flatMap(op => op.fromStates.map(s => ({ source: statesDic[s], target: op, isFrom: true }) as ForceLink));

    const toRelationships = map.operations.filter(op => op.toStates != undefined)
      .flatMap(op => op.toStates.map(s => ({ source: op, target: statesDic[s], isFrom: false }) as ForceLink));

    map.allLinks = fromRelationships.concat(toRelationships);
    map.allTransition = map.operations.flatMap(o => o.fromStates.flatMap(f => o.toStates.map(t => ({
      fromState: statesDic[f],
      operation: o,
      toState: statesDic[t]
    }) as Transition)));

    const fanOut = map.operations.flatMap(a => a.fromStates.map(s => ({ s: s, weight: 1.0 / a.fromStates.length }))).groupToObject(a => a.s);
    const fanIn = map.operations.flatMap(a => a.toStates.map(s => ({ s: s, weight: 1.0 / a.toStates.length }))).groupToObject(a => a.s);

    map.states.forEach(m => {
      m.fanOut = (fanOut[m.key] ? fanOut[m.key].reduce((acum, e) => acum + e.weight, 0) : 0);
      m.fanIn = (fanIn[m.key] ? fanIn[m.key].reduce((acum, e) => acum + e.weight, 0) : 0);

      m.fanInOutFactor = (m.fanIn - m.fanOut) / (m.fanIn + m.fanOut);
    });
  }

  componentWillReceiveProps(newProps: OperationMapRendererProps) {
    if (newProps.color != this.props.color)
      this.state.mapD3!.setColor(newProps.color);
  }

  componentWillUnmount() {
    this.state.mapD3!.stop();
  }

  svg!: SVGElement;

  render() {
    return (
      <div id="map" style={{ backgroundColor: "transparent", width: "100%", height: this.props.height + "px" }}>
        <svg id="svgMap" ref={svg => this.svg = svg!}>
          <defs>
            <marker id="normal_arrow" viewBox="0 -5 10 10" refX="10" refY="0" markerWidth="10" markerHeight="10" orient="auto">
              <path fill="gray" d="M0,0L0,-5L10,0L0,5L0,0" />
            </marker>
          </defs>
        </svg>
      </div>
    );
  }
}


