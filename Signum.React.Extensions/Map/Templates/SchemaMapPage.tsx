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
import { SchemaMapInfo, EntityBaseType, ITableInfo, MListRelationInfo, IRelationInfo, ClientColorProvider } from './SchemaMap'
var colorbrewer = require("colorbrewer");

require("!style!css!./SchemaMap.css");




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

        MapClient.start


        MapClient.API.types()
            .then(smi => {
                var parsedQuery = this.getParsedQuery();
                this.fixSchemaMap(smi, parsedQuery);
                this.setState({
                    schemaMapInfo: smi,
                    filter: parsedQuery.filter, 
                    color: parsedQuery.color || smi.providers.first().name
                });
            }).done();


    }

    fixSchemaMap(map: SchemaMapInfo, parsedQuery: ParsedQueryString) {
        map.tables.forEach(t => t.mlistTables.forEach(ml => {
            ml.entityKind = t.entityKind;
            ml.entityData = t.entityData;
            ml.entityBaseType = EntityBaseType.MList;
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

    getStringQuery() {

    }


    render() {

        var s = this.state;
        return (
            <div>
                {this.renderFilter() }
                {s.schemaMapInfo ?
                    <span>{ JavascriptMessage.loading.niceToString() }</span> :
                    <SchemaMapRenderer schemaMapInfo={s.schemaMapInfo} filter={s.filter} color={s.color}  height = {s.height} width = {s.width} providers={s.providers} />}
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

        return (
            <div className="form-inline form-sm container" style={{ marginTop: "10px" }}>
                <div className="form-group">
                    <label htmlFor="filter"> { MapMessage.Filter.niceToString() }</label>
                    <input type="text" className="form-control" id="filter" placeholder="type or namespace" value={this.state.filter} onChange={this.handleSetFilter}/>
                </div>
                <div className="form-group" style={{ marginLeft: "10px" }}>
                    <label htmlFor="color"> { MapMessage.Color.niceToString() }</label>
                    <select className="form-control" id="color" value={this.state.color} onChange={this.handleSetColor}>
                        {
                            this.state.schemaMapInfo.providers.map((a, i) =>
                                <option key={i} value="@cp.Name">{ a.niceName}</option>)
                        }
                    </select>
                </div>
                <span style={{ marginLeft: "10px" }}>
                    { MapMessage.Press0ToExploreEachTable.niceToString().formatHtml(<u>Ctrl + Click</u>) }
                </span>
                <a id="sfFullScreen" className="sf-popup-fullscreen" onClick={this.handleFullscreenClick}>
                    <span className="glyphicon glyphicon-new-window"></span>
                </a>
            </div>
        );

    }

}

export type SchemaMapRendererProps = SchemaMapPropsState;

export class SchemaMapRenderer extends React.Component<SchemaMapRendererProps, void> { 

   

    componentWillMount(){


    }

    componentDidMount() {
        this.redraw();
    }

    componentDidUpdate() {
        this.redraw();
    }

    redraw() {

        var node = ReactDOM.findDOMNode(this);
        while (node.firstChild) {
            node.removeChild(node.firstChild);
        }


    }

    svg: SVGElement;

    render() {
        return (
            <div id="map" style={{ backgroundColor: "white", width: "100%", height: (window.innerHeight - 200) + "px" }}>
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
                            React.Children.map(Dic.getValues(this.props.providers).map(a => a.defs),
                                (c, i) => React.cloneElement(c as React.ReactElement<any>, { key: i }))
                        }
                    </defs>
                </svg>
            </div>
        );
    }
}





