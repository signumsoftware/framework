import * as React from 'react'
import * as ReactDOM from 'react-dom'
import * as d3 from 'd3'
import { DomUtils, Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { is, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ResultTable, FindOptions, FilterOption, QueryDescription, SubTokensOptions, QueryToken, QueryTokenType, ColumnOption } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { MapMessage } from '../Signum.Entities.Map'
import * as MapClient from '../MapClient'
import { SchemaMapInfo, EntityBaseType, ITableInfo, MListRelationInfo, IRelationInfo, ClientColorProvider, SchemaMapD3 } from './SchemaMap'
var colorbrewer = require("colorbrewer");

require("!style!css!./schemaMap.css");




interface SchemaMapPageProps extends ReactRouter.RouteComponentProps<{}, {}> {

}

interface SchemaMapPropsState {
    schemaMapInfo?: SchemaMapInfo;
    filter?: string;
    color?: string;
    width?: number;
    height?: number;
    providers?: { [name: string]: ClientColorProvider };
}

interface ParsedQueryString {
    filter?: string;
    color?: string;
    tables: { [tableName: string]: { x: number; y: number } };
}

export default class SchemaMapPage extends React.Component<SchemaMapPageProps, SchemaMapPropsState> {

    state = {} as SchemaMapPropsState;

    componentWillMount() {
            MapClient.API.types()
                .then(smi => {
                    var parsedQuery = this.getParsedQuery();
                    this.fixSchemaMap(smi, parsedQuery);
                    MapClient.getAllProviders(smi).then(providers => {
                        this.setState({
                            providers: providers.toObject(a => a.name)
                        });


                    this.setState({
                        schemaMapInfo: smi,
                        filter: parsedQuery.filter || "",
                        color: parsedQuery.color || smi.providers.first().name
                    });
                }).done();
        }).done();


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
                a.x = c.x * this.state.width;
                a.y = c.y * this.state.height;
                a.fixed = true;
            }
        });


        const nodesDic = map.allNodes.toObject(g => g.tableName);
        map.relations.forEach(a => {
            a.source = nodesDic[a.fromTable];
            a.target = nodesDic[a.toTable];
        });
        

        map.allLinks = map.relations.map(a => a as IRelationInfo)
            .concat(map.tables.flatMap(t => t.mlistTables.map(tm => ({ source: t, target: tm, isMList: true }) as MListRelationInfo)));
    }


    getParsedQuery(): ParsedQueryString {
    
        var result: ParsedQueryString = { tables: {} };

        var query = this.props.location.query as { [name: string]: string };
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

    div: HTMLDivElement;
    handleSetInitialSize = (div: HTMLDivElement) => {

        if (this.div)
            return;

        this.div = div;
        var rect = div.getBoundingClientRect();
        this.setState({ width: rect.width, height: window.innerHeight - 200 });
    }


    render() {

        var s = this.state;
        return (
            <div ref={this.handleSetInitialSize}>
                {this.renderFilter() }
                {!s.schemaMapInfo ?
                    <span>{ JavascriptMessage.loading.niceToString() }</span> :
                    <SchemaMapRenderer schemaMapInfo={s.schemaMapInfo} filter={s.filter} color={s.color}  height={s.height} width={s.width} providers={s.providers} />}
            </div>
        );
    }

    handleSetFilter = (e: React.FormEvent) => {
        this.setState({
            filter: (e.currentTarget as HTMLInputElement).value
        });
    }

    handleSetColor = (e: React.FormEvent) => {
        this.setState({
            color: (e.currentTarget as HTMLInputElement).value
        });
    }

    handleFullscreenClick = (e: React.MouseEvent) => {

        e.preventDefault();

        var s = this.state;

        var tables = s.schemaMapInfo.allNodes.filter(a => a.fixed)
            .toObject(a => a.tableName, a =>
                (a.x / s.width).toPrecision(4) + "," +
                (a.y / s.height).toPrecision(4));


        var query = Dic.extend(tables, { filter: s.filter, color: s.color });

        var url = Navigator.currentHistory.createHref({ pathname: "/map/types", query: query });

        window.open(url);
    }

    renderFilter() {

        var s = this.state;

        return (
            <div className="form-inline form-sm container" style={{ marginTop: "10px" }}>
                <div className="form-group">
                    <label htmlFor="filter"> { MapMessage.Filter.niceToString() }</label>&nbsp;
                    <input type="text" className="form-control" id="filter" placeholder="type or namespace" value={s.filter} onChange={this.handleSetFilter}/>
                </div>
                <div className="form-group" style={{ marginLeft: "10px" }}>
                    <label htmlFor="color"> { MapMessage.Color.niceToString() }</label>&nbsp;
                    <select className="form-control" id="color" value={s.color} onChange={this.handleSetColor}>
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
                    <span className="glyphicon glyphicon-new-window"></span>
                </a>
            </div>
        );

    }

}

export type SchemaMapRendererProps = SchemaMapPropsState;

export class SchemaMapRenderer extends React.Component<SchemaMapRendererProps, { mapD3: SchemaMapD3 }> { 

    componentDidMount() {
        var p = this.props;
        var d3 = new SchemaMapD3(this.svg, p.providers, p.schemaMapInfo, p.filter, p.color, p.width, p.height);
        this.setState({ mapD3: d3 });
    }

    componentWillReceiveProps(newProps: SchemaMapRendererProps) {

        if (newProps.color != this.props.color)
            this.state.mapD3.setColor(newProps.color);

        if (newProps.filter != this.props.filter)
            this.state.mapD3.setFilter(newProps.filter);
    }


    svg: SVGElement;

    render() {

        return (
            <div id="map" style={{ backgroundColor: "white", width: "100%", height: this.props.height + "px" }}>
                <svg id="svgMap" ref={svg => this.svg = svg}>
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
                            React.Children.map(Dic.getValues(this.props.providers).map(a => a.defs).filter(d=>!!d),
                                (c, i) => React.cloneElement(c as React.ReactElement<any>, { key: i }))
                        }
                    </defs>
                </svg>
            </div>
        );
    }
}





