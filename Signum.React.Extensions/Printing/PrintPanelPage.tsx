import * as React from 'react'
import { Link } from 'react-router'
import * as numbro from 'numbro'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import EntityLink from '../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import { ValueSearchControl, SearchControl } from '../../../Framework/Signum.React/Scripts/Search'
import { QueryDescription, SubTokensOptions } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { API, PrintStat } from './PrintClient'
import { PrintPackageEntity, PrintLineState, PrintLineEntity } from './Signum.Entities.Printing'



export interface PrintPanelPageState {
    stats: PrintStat[];
}

export default class PrintPanelPage extends React.Component<{}, PrintPanelPageState> {

    constructor(props: any) {
        super(props);

        this.state = { stats: [] };
    }

    componentWillMount() {
        API.getStats().then(stats => {
            this.changeState(b => b.stats = stats);
        }).done();
    }


    render() {

        return (
            <div>
                <h2>PrintPanel</h2>
                <div>
                    {this.state.stats.map((s, i) => <p key={i}>{s.fileTypeSymbol.key} {s.count}</p>)}
                </div>
                <h3>{PrintLineState.niceName("ReadyToPrint")}</h3>
                <SearchControl findOptions={{
                    queryName: PrintLineEntity,
                    orderOptions: [{ columnName: "CreationDate", orderType: "Descending" }],
                    searchOnLoad: false,
                    showFilters: true,
                    filterOptions: [{ columnName: "State", value: "ReadyToPrint" }],
                }} />


                <h3>{PrintLineState.niceName("Printed")}</h3>
                <SearchControl findOptions={{
                    queryName: PrintLineEntity,
                    orderOptions: [{ columnName: "PrintedOn", orderType: "Descending" }],
                    filterOptions: [{ columnName: "State", value: "Printed" }],
                    searchOnLoad: false,
                    showFilters: true
                }} />
            </div>
        );
    }
}