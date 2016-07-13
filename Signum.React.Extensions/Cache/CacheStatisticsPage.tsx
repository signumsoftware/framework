
import * as React from 'react'
import { Link } from 'react-router'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { API, CacheTable, CacheState } from './CacheClient'


interface CacheStatisticsPageProps extends ReactRouter.RouteComponentProps<{}, {}> {

}

export default class CacheStatisticsPage extends React.Component<CacheStatisticsPageProps, CacheState> {

    state: CacheState = { tables: undefined, isEnabled: undefined };

    componentWillMount() {
        this.loadState().done();
    }

    loadState() {
        return API.view()
            .then(s => this.setState({ tables: s.tables, isEnabled: s.isEnabled }));
    }

    handleDisabled = (e: React.MouseEvent) => {
        API.disable().then(() => this.loadState()).done();
    }

    handleEnabled = (e: React.MouseEvent) => {
        API.enable().then(() => this.loadState()).done();
    }

    handleClear = (e: React.MouseEvent) => {
        API.clear().then(() => this.loadState()).done();
    }

    render() {
        document.title = "Cache Statistics";    

        const list: React.ReactNode[] = [];
        if (this.state.tables)
            this.state.tables.forEach(st => this.showTree(list, st, 0));

        return (
            <div>
                <h2>Cache Statistics</h2>
                <div className="btn-toolbar">
                    { this.state.isEnabled == true && <a href="#" onClick={this.handleDisabled} className="sf-button btn btn-default" style={{ color: "red" }}>Disable</a> }
                    { this.state.isEnabled == false && <a href="#" onClick={this.handleEnabled} className="sf-button btn btn-default" style={{ color: "green" }}>Enabled</a> }
                    { <a href="#" onClick={this.handleClear} className="sf-button btn btn-default" style={{ color: "blue" }}>Clear</a> }
                </div >
                <table className="table table-condensed">
                    <thead>
                        <tr>
                            <th>Table</th>
                            <th>Type</th>
                            <th>Count</th>
                            <th>Hits</th>
                            <th>Invalidations</th>
                            <th>Loads</th>
                            <th>LoadTime</th>
                        </tr>
                    </thead>
                    <tbody>
                        { list }
                    </tbody>
                </table>
            </div>
        );
    }


    showTree(list: React.ReactNode[], table: CacheTable, depth: number) {

        const opacity =
            depth == 0 ? 1 :
                depth == 1 ? .7 :
                    depth == 2 ? .5 :
                        depth == 3 ? .4 : .3;

        list.push(
            <tr style={{ opacity: opacity }} key={list.length}>
                <td> { Array.repeat(depth, " → ").join("") + table.tableName }</td >
                <td> { table.typeName} </td>
                <td> { table.count != undefined ? table.count.toString() : "-- not loaded --"} </td>
                <td> { table.hits} </td>
                <td> { table.invalidations }</td>
                <td> { table.loads }</td>
                <td> { table.sumLoadTime} </td>
            </tr>
        );

        if (table.subTables)
            table.subTables.forEach(st => this.showTree(list, st, depth + 1));
    }
}



