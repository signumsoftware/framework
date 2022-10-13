import * as React from 'react'
import * as History from 'history'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Dic } from '@framework/Globals'
import * as AppContext from '@framework/AppContext'
import { JavascriptMessage } from '@framework/Signum.Entities'
import { MapMessage } from '../Signum.Entities.Map'
import * as MapClient from '../MapClient'
import { SchemaMapInfo, ITableInfo, MListRelationInfo, IRelationInfo, ClientColorProvider, SchemaMapD3 } from './SchemaMap'
import { RouteComponentProps } from "react-router";
import "./schemaMap.css"
import { useSize } from '@framework/Hooks'
import { useExpand } from '@framework/AppContext'
import { QueryString } from '@framework/QueryString'

interface SchemaMapState {
  schemaMapInfo?: SchemaMapInfo;
  providers?: { [name: string]: ClientColorProvider };
}

interface ParsedQueryString {
  filter?: string;
  color?: string;
  tables: Tables;
}

interface Tables {
  [tableName: string]: { x: number; y: number }
}

function getParsedQuery(location: History.Location): ParsedQueryString {

  const result: ParsedQueryString = { tables: {} };

  const query = QueryString.parse(location.search) as { [name: string]: string };
  if (!query)
    return result;

  Dic.foreach(query, (name, value) => {

    if (name == "filter")
      result.filter = value;
    else if (name == "color")
      result.color = value;
    else {
      result.tables[name] = {
        x: parseFloat(value.before(",")),
        y: parseFloat(value.after(",")),
      };
    }
  });

  return result;
}


export default function SchemaMapPage(p: RouteComponentProps<{}>) {

  const [filter, setFilter] = React.useState<string>("");
  const [color, setColor] = React.useState<string>("");
  const [tables, setTables] = React.useState<Tables | undefined>(undefined);
  const [schemaInfo, setSchemaInfo] = React.useState<SchemaMapInfo | undefined>(undefined);
  const [providers, setProviders] = React.useState<{ [name: string]: ClientColorProvider } | undefined>(undefined);
  useExpand();

  React.useEffect(() => {
    MapClient.API.types()
      .then(smi => {
        const parsedQuery = getParsedQuery(p.location);
        MapClient.getAllProviders(smi).then(providers => {

          const missingProviders = smi.providers.filter(p => !providers.some(p2 => p2.name == p.name));
          if (missingProviders.length)
            throw new Error(`Missing ClientColorProvider for ${missingProviders.map(a => "'" + a.name + "'").joinComma("and")} found`);

          const extraProviders = providers.filter(p => !smi.providers.some(p2 => p2.name == p.name));
          if (extraProviders.length)
            throw new Error(`Extra ClientColorProvider for ${extraProviders.map(a => "'" + a.name + "'").joinComma("and")} found`);

          setFilter(parsedQuery.filter ?? "");
          setTables(parsedQuery.tables);
          setColor(parsedQuery.color ?? smi.providers.first().name);
          setSchemaInfo(smi);
          setProviders(providers.toObject(a => a.name));

        });
      });
  }, []);

  const { size, setContainer } = useSize();

  function handleSetFilter(e: React.FormEvent<any>) {
    setFilter((e.currentTarget as HTMLInputElement).value);
  }

  function handleSetColor(e: React.FormEvent<any>) {
    setColor((e.currentTarget as HTMLInputElement).value);
  }

  function handleFullscreenClick(e: React.MouseEvent<any>) {
    e.preventDefault();

    const tables = schemaInfo!.allNodes.filter(a => a.fx != null && a.fy != null)
      .toObject(a => a.tableName, a =>
        (a.fx! / size!.width!).toPrecision(4) + "," +
        (a.fy! / size!.height!).toPrecision(4));

    const query = {
      ...tables, filter: filter, color: color
    };

    const url = AppContext.history.createHref({ pathname: "~/map", search: QueryString.stringify(query) });

    window.open(url);
  }

  function renderFilter() {

    return (
      <div className="container">
        <div className="row align-items-center">
          <div className="col-auto">
            <label htmlFor="filter"> {MapMessage.Filter.niceToString()}</label>&nbsp;
          </div>

          <div className="col-auto">
            <input type="text" className="form-control form-control-sm" id="filter" placeholder="type or namespace" value={filter} onChange={handleSetFilter} />
          </div>
          <div className="col-auto">
            <label htmlFor="color"> {MapMessage.Color.niceToString()}</label>&nbsp;
          </div>
          <div className="col-auto">
            <select className="form-select form-select-sm" id="color" value={color} onChange={handleSetColor}>
              {
                schemaInfo &&
                schemaInfo.providers.map((a, i) =>
                  <option key={i} value={a.name}>{a.niceName}</option>)
              }
            </select>
          </div>
          <div className="col-auto">
            <span style={{ marginLeft: "10px" }}>
              {MapMessage.Press0ToExploreEachTable.niceToString().formatHtml(<u>Ctrl + Click</u>)}
            </span>
            &nbsp;
            <a id="sfFullScreen" className="sf-popup-fullscreen" onClick={handleFullscreenClick} href="#">
              <FontAwesomeIcon icon="up-right-from-square" />
            </a>
          </div>
        </div>
      </div>
    );
  }
  if (AppContext.Expander.onGetExpanded && !AppContext.Expander.onGetExpanded())
    return null;

  return (
    <div style={{ display: "flex", flexDirection: "column" }}>
      {renderFilter()}
      {!(schemaInfo && schemaInfo && providers) ?
        <span>{JavascriptMessage.loading.niceToString()}</span> :
        <div ref={setContainer} style={{ display: "flex", flexGrow: 1 }}>
          {size?.height && size?.width &&
            <SchemaMapRenderer
              schemaMapInfo={schemaInfo}
              tables={tables!}
              filter={filter}
              color={color}
              height={size.height}
              width={size.width}
              providers={providers}
            />
          }
        </div>}
    </div>
  );
}

export interface SchemaMapRendererProps {
  schemaMapInfo: SchemaMapInfo;
  filter: string;
  color: string;
  width: number;
  height: number;
  providers: { [name: string]: ClientColorProvider };
  tables: Tables;
}

export function SchemaMapRenderer(p: SchemaMapRendererProps) {

  const mapD3Ref = React.useRef<SchemaMapD3 | undefined>(undefined);
  const svgRef = React.useRef<SVGSVGElement>(null);

  React.useEffect(() => {
    fixSchemaMap(p.schemaMapInfo, p.tables);
    mapD3Ref.current = new SchemaMapD3(svgRef.current!, p.providers, p.schemaMapInfo, p.filter, p.color, p.width, p.height);

    return () => { mapD3Ref.current!.stop(); };
  }, []);

  React.useEffect(() => mapD3Ref.current!.setColor(p.color), [p.color]);
  React.useEffect(() => mapD3Ref.current!.setFilter(p.filter), [p.filter]);

  function fixSchemaMap(map: SchemaMapInfo, tables: Tables) {
    map.tables.forEach(t => t.mlistTables.forEach(ml => {
      ml.entityKind = t.entityKind;
      ml.entityData = t.entityData;
      ml.entityBaseType = "MList";
      ml.namespace = t.namespace;
    }));

    map.allNodes = (map.tables as ITableInfo[]).concat(map.tables.flatMap(t => t.mlistTables));

    map.allNodes.forEach(a => {
      const c = tables[a.tableName];
      if (c) {
        a.fx = c.x * p.width;
        a.fy = c.y * p.height;
      }
      else {
        a.x = Math.random() * p.width;
        a.y = Math.random() * p.height;
      }
    });


    const nodesDic = map.allNodes.toObject(g => g.tableName);
    map.relations.forEach(a => {
      a.source = nodesDic[a.fromTable];
      a.target = nodesDic[a.toTable];
    });


    map.allLinks = map.relations.map(a => a as IRelationInfo)
      .concat(map.tables.flatMap(t => t.mlistTables.map(tm => ({
        source: t,
        target: tm,
        isMList: true,
      }) as any as MListRelationInfo)));

    const repsDic: { [tableName: string]: number } = {};

    map.allLinks.forEach(l => {

      const sourceName = (l.source as ITableInfo).tableName;
      const targetName = (l.target as ITableInfo).tableName;

      const relName = sourceName > targetName ?
        sourceName + "-" + targetName :
        targetName + "-" + sourceName;

      if (repsDic[relName] == undefined)
        repsDic[relName] = 0;

      l.repetitions = repsDic[relName];
      repsDic[relName]++;

    });
  }

  return (
    <div id="map" style={{ backgroundColor: "transparent", width: "100%", height: p.height + "px" }}>
      <svg id="svgMap" ref={svgRef}>
        <defs>
          <marker id="normal_arrow" viewBox="0 -5 10 10" refX="10" refY="0" markerWidth="10" markerHeight="10" orient="auto">
            <path fill="gray" d="M0,0L0,-5L10,0L0,5L0,0" />
          </marker>

          <marker id="lite_arrow" viewBox="0 -5 10 10" refX="10" refY="0" markerWidth="10" markerHeight="10" orient="auto">
            <path fill="gray" d="M5,0L0,-5L10,0L0,5L5,0" />
          </marker>

          <marker id="mlist_arrow" viewBox="-10 -5 20 10" refX="10" refY="0" markerWidth="10" markerHeight="20" orient="auto">
            <path fill="gray" d="M0,0L0,-5L10,0L0,5L0,0L-10,5L-10,-5L0,0" />
          </marker>

          <marker id="virtual_mlist_arrow" viewBox="-10 -5 20 10" refX="-10" refY="0" markerWidth="10" markerHeight="20" orient="auto">
            <path fill="gray" d="M0,0 L0,-8 L-10,0 L0,8 L0,0 L10,8 L10,-8 L0,0" />
          </marker>
          {
            React.Children.map(Dic.getValues(p.providers).map(a => a.defs).filter(defs => !!defs).flatMap(defs => defs!),
              (c, i) => React.cloneElement(c as React.ReactElement<any>, { key: i }))
          }
        </defs>
      </svg>
    </div>
  );
}





