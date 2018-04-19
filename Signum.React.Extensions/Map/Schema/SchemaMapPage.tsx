import * as React from 'react'
import * as ReactDOM from 'react-dom'
import * as d3 from 'd3'
import * as QueryString from 'query-string'
import { DomUtils, Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import  { Expander } from '../../../../Framework/Signum.React/Scripts/Navigator'
import { is, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ResultTable, FindOptions, FilterOption, QueryDescription, SubTokensOptions, QueryToken, QueryTokenType, ColumnOption } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { MapMessage } from '../Signum.Entities.Map'
import * as MapClient from '../MapClient'
import { SchemaMapInfo, EntityBaseType, ITableInfo, MListRelationInfo, IRelationInfo, ClientColorProvider, SchemaMapD3 } from './SchemaMap'
import { RouteComponentProps } from "react-router";

import "./schemaMap.css"

interface SchemaMapPageProps extends RouteComponentProps<{}> {

}

interface SchemaMapPropsState {
    schemaMapInfo?: SchemaMapInfo;
    filter?: string;
    color?: string;
    width?: number;
    height?: number;
    providers?: { [name: string]: ClientColorProvider };
    parsedQuery?: ParsedQueryString;
}

interface ParsedQueryString {
    filter?: string;
    color?: string;
    tables: { [tableName: string]: { x: number; y: number } };
}

export default class SchemaMapPage extends React.Component<SchemaMapPageProps, SchemaMapPropsState> {

    constructor(props: SchemaMapPageProps) {
        super(props);
        this.state = { filter: "", color: "" };
    }

    wasExpanded!: boolean;

    componentWillMount() {

        this.wasExpanded = Navigator.Expander.setExpanded(true);

        MapClient.API.types()
            .then(smi => {
                const parsedQuery = this.getParsedQuery();
                MapClient.getAllProviders(smi).then(providers => {

                    const missingProviders = smi.providers.filter(p => !providers.some(p2 => p2.name == p.name));
                    if (missingProviders.length)
                        throw new Error(`Missing ClientColorProvider for ${missingProviders.map(a => "'" + a.name + "'").joinComma("and")} found`);

                    const extraProviders = providers.filter(p => !smi.providers.some(p2 => p2.name == p.name));
                    if (extraProviders.length)
                        throw new Error(`Extra ClientColorProvider for ${extraProviders.map(a => "'" + a.name + "'").joinComma("and")} found`);

                    this.setState({
                        providers: providers.toObject(a => a.name),
                        schemaMapInfo: smi,
                        parsedQuery: parsedQuery,
                        filter: parsedQuery.filter || "",
                        color: parsedQuery.color || smi.providers.first().name
                    });
                }).done();
            }).done();
    }


    componentWillUnmount(){
        Navigator.Expander.setExpanded(this.wasExpanded);
    }



    getParsedQuery(): ParsedQueryString {
    
        const result: ParsedQueryString = { tables: {} };

        const query = QueryString.parse(this.props.location.search) as { [name: string]: string };
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
                {this.renderFilter() }
                {!s.schemaMapInfo || this.div == undefined ?
                    <span>{ JavascriptMessage.loading.niceToString() }</span> :
                    <SchemaMapRenderer
                        schemaMapInfo={s.schemaMapInfo}
                        parsedQuery={s.parsedQuery!}
                        filter={s.filter!}
                        color={s.color!}
                        height={s.height!}
                        width={s.width!}
                        providers={s.providers!} />}
            </div>
        );
    }

    handleSetFilter = (e: React.FormEvent<any>) => {
        this.setState({
            filter: (e.currentTarget as HTMLInputElement).value
        });
    }

    handleSetColor = (e: React.FormEvent<any>) => {
        this.setState({
            color: (e.currentTarget as HTMLInputElement).value
        });
    }

    handleFullscreenClick = (e: React.MouseEvent<any>) => {

        e.preventDefault();

        const s = this.state;

        const tables = s.schemaMapInfo!.allNodes.filter(a => a.fx != null && a.fy != null)
            .toObject(a => a.tableName, a =>
                (a.fx! / s.width!).toPrecision(4) + "," +
                (a.fy! / s.height!).toPrecision(4));

        const query = {
            ...tables, filter: s.filter, color: s.color
        };

        const url = Navigator.history.createHref({ pathname: "~/map", search: QueryString.stringify(query) });

        window.open(url);
    }

    renderFilter() {

        const s = this.state;

        return (
            <div className="form-inline container" style={{ marginTop: "10px" }}>
                <div className="form-group form-group-sm">
                    <label htmlFor="filter"> { MapMessage.Filter.niceToString() }</label>&nbsp;
                    <input type="text" className="form-control form-control-sm" id="filter" placeholder="type or namespace" value={s.filter} onChange={this.handleSetFilter}/>
                </div>
                <div className="form-group form-group-sm" style={{ marginLeft: "10px" }}>
                    <label htmlFor="color"> { MapMessage.Color.niceToString() }</label>&nbsp;
                    <select className="form-control form-control-sm" id="color" value={s.color} onChange={this.handleSetColor}>
                        {
                            s.schemaMapInfo &&
                            s.schemaMapInfo.providers.map((a, i) =>
                                <option key={i} value={a.name}>{ a.niceName}</option>)
                        }
                    </select>
                </div>
                <span style={{ marginLeft: "10px" }}>
                    { MapMessage.Press0ToExploreEachTable.niceToString().formatHtml(<u>Ctrl + Click</u>) }
                </span>
                &nbsp;
                <a id="sfFullScreen" className="sf-popup-fullscreen" onClick={this.handleFullscreenClick} href="#">
                    <span className="fa fa-external-link"></span>
                </a>
            </div>
        );

    }

}

export interface SchemaMapRendererProps {
    schemaMapInfo: SchemaMapInfo;
    filter: string;
    color: string;
    width: number;
    height: number;
    providers: { [name: string]: ClientColorProvider };
    parsedQuery: ParsedQueryString;
}

export class SchemaMapRenderer extends React.Component<SchemaMapRendererProps, { mapD3: SchemaMapD3 }> { 

    componentDidMount() {
        const p = this.props;

        this.fixSchemaMap(p.schemaMapInfo, p.parsedQuery);

        const d3 = new SchemaMapD3(this.svg, p.providers, p.schemaMapInfo, p.filter, p.color, p.width, p.height);
        this.setState({ mapD3: d3 });
    }

    
    fixSchemaMap(map: SchemaMapInfo, parsedQuery: ParsedQueryString) {
        map.tables.forEach(t => t.mlistTables.forEach(ml => {
            ml.entityKind = t.entityKind;
            ml.entityData = t.entityData;
            ml.entityBaseType = "MList";
            ml.namespace = t.namespace;
        }));

        map.allNodes = (map.tables as ITableInfo[]).concat(map.tables.flatMap(t => t.mlistTables));

        map.allNodes.forEach(a => {
            const c = parsedQuery.tables[a.tableName];
            if (c) {
                a.fx = c.x * this.props.width;
                a.fy = c.y * this.props.height;
            }
            else {
                a.x = Math.random() * this.props.width;
                a.y = Math.random() * this.props.height;
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
        
        const repsDic : {[tableName: string]: number} = {};

        map.allLinks.forEach(l => {

            const sourceName = (l.source as ITableInfo).tableName;
            const targetName = (l.target as ITableInfo).tableName;

            const relName = sourceName > targetName? 
                sourceName + "-" + targetName : 
                targetName + "-" + sourceName;

            if(repsDic[relName] == undefined)
                repsDic[relName] = 0;

            l.repetitions = repsDic[relName];
            repsDic[relName]++;

        });
    }

    componentWillReceiveProps(newProps: SchemaMapRendererProps) {

        if (newProps.color != this.props.color)
            this.state.mapD3.setColor(newProps.color);

        if (newProps.filter != this.props.filter)
            this.state.mapD3.setFilter(newProps.filter);
    }

    componentWillUnmount(){
        this.state.mapD3.stop();
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

                        <marker id="lite_arrow" viewBox="0 -5 10 10" refX="10" refY="0" markerWidth="10" markerHeight="10" orient="auto">
                            <path fill="gray" d="M5,0L0,-5L10,0L0,5L5,0" />
                        </marker>

                        <marker id="mlist_arrow" viewBox="-10 -5 20 10" refX="10" refY="0" markerWidth="10" markerHeight="20" orient="auto">
                            <path fill="gray" d="M0,0L0,-5L10,0L0,5L0,0L-10,5L-10,-5L0,0" />
                        </marker>
                        {
                            React.Children.map(Dic.getValues(this.props.providers).map(a => a.defs).filter(defs => !!defs).flatMap(defs => defs!),
                                (c, i) => React.cloneElement(c as React.ReactElement<any>, { key: i }))
                        }
                    </defs>
                </svg>
            </div>
        );
    }
}





