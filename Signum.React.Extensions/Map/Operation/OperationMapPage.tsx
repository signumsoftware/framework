import * as React from 'react'
import { Location } from 'history'
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
import { useExpand, useAPI, useSize } from '../../../../Framework/Signum.React/Scripts/Hooks'

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
  nodes: Nodes;
}

export interface Nodes {
  [nodeName: string]: { x: number; y: number }
}


function getParsedQuery(loc: Location): ParsedQueryString {
  const result: ParsedQueryString = { nodes: {} };

  const query = QueryString.parse(loc.search) as { [name: string]: string };
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

export default function OperationMapPage(p: OperationMapPageProps) {

  useExpand();

  const [color, setColor] = React.useState<string>("");
  const [nodes, setNodes] = React.useState<Nodes | undefined>(undefined);

  const operationMapInfo = useAPI(() => MapClient.API.operations(p.match.params.type), [p.match.params.type]);

  React.useEffect(() => {
    const parsedQuery = getParsedQuery(p.location);

    setNodes(parsedQuery.nodes);
    setColor(parsedQuery.color ?? "");
  }, []);

  const { size, setContainer } = useSize();

  function handleFullscreenClick(e: React.MouseEvent<any>) {
    e.preventDefault();

    const tables = operationMapInfo!.allNodes.filter(a => a.fx != null && a.fy != null)
      .toObject(a => a.key, a =>
        (a.fx! / size!.width!).toPrecision(4) + "," +
        (a.fy! / size!.height!).toPrecision(4));

    var query = { ...tables, color: color };

    const url = Navigator.history.createHref({
      pathname: "~/map/" + p.match.params.type,
      search: QueryString.stringify(query)
    });

    window.open(url);
  }

  function renderFilter() {
    return (
      <div className="form-inline form-sm container" style={{ marginTop: "10px" }}>
        <div className="form-group" style={{ marginLeft: "10px" }}>
          <label htmlFor="color"> {MapMessage.Color.niceToString()}</label>
          &nbsp;
          <select className="form-control" id="color" value={color} onChange={e => setColor(e.currentTarget.value)}>
            <option value="state">{MapMessage.StateColor.niceToString()}</option>
            <option value="rows">{MapMessage.Rows.niceToString()}</option>
          </select>
        </div>
        <span style={{ marginLeft: "10px" }}>
          {MapMessage.Press0ToExploreEachTable.niceToString().formatHtml(<u>Ctrl + Click</u>)}
        </span>
        &nbsp;
                <a id="sfFullScreen" className="sf-popup-fullscreen" onClick={handleFullscreenClick} href="#">
          <FontAwesomeIcon icon="external-link-alt" />
        </a>
      </div>
    );
  }
  if (Navigator.Expander.onGetExpanded && !Navigator.Expander.onGetExpanded())
    return null;

  return (
    <div ref={setContainer}>
      {renderFilter()}
      {!(operationMapInfo && size && nodes) ?
        <span>{JavascriptMessage.loading.niceToString()}</span> :
        <OperationMapRenderer operationMapInfo={operationMapInfo} nodes={nodes} color={color!} height={size.height!} width={size.width!} queryName={p.match.params.type} />}
    </div>
  );
}

export interface OperationMapRendererProps {
  queryName: string;
  operationMapInfo: OperationMapInfo;
  width: number;
  height: number;
  nodes: Nodes;
  color: string;
}

export function OperationMapRenderer(p: OperationMapRendererProps) {

  const svg = React.useRef<SVGSVGElement>(null);
  const mapD3 = React.useRef<OperationMapD3 | null>(null);

  React.useEffect(() => {
    fixSchemaMap(p.operationMapInfo, p.nodes);
    mapD3.current = new OperationMapD3(svg.current!, p.queryName, p.operationMapInfo, p.color, p.width, p.height);
    return () => mapD3.current!.stop();
  });

  React.useEffect(() => {
    mapD3.current!.setColor(p.color);
  }, [p.color]);


  function fixSchemaMap(map: OperationMapInfo, nodes: Nodes) {
    map.allNodes = (map.operations as ForceNode[]).concat(map.states);

    map.allNodes.forEach(a => {
      const c = nodes[a.key];
      if (c) {
        a.fx = c.x * p.width;
        a.fy = c.y * p.height;
      } else {
        a.x = Math.random() * p.width;
        a.y = Math.random() * p.height;
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

  function componentWillReceiveProps(newProps: OperationMapRendererProps) {
  }


  return (
    <div id="map" style={{ backgroundColor: "transparent", width: "100%", height: p.height + "px" }}>
      <svg id="svgMap" ref={svg => svg = svg!}>
        <defs>
          <marker id="normal_arrow" viewBox="0 -5 10 10" refX="10" refY="0" markerWidth="10" markerHeight="10" orient="auto">
            <path fill="gray" d="M0,0L0,-5L10,0L0,5L0,0" />
          </marker>
        </defs>
      </svg>
    </div>
  );
}


