import * as React from 'react'
import { Link } from 'react-router'
import * as numbro from 'numbro'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import EntityLink from '../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import {ValueSearchControl, SearchControl } from '../../../Framework/Signum.React/Scripts/Search'
import { QueryDescription, SubTokensOptions } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { API } from './PrintClient'
import { PrintPackageEntity, PrintLineState } from './Signum.Entities.Printing'



interface PrintPanelProps extends ReactRouter.RouteComponentProps<{}, {}> {

}

export default class PrintPanelPage extends React.Component<PrintPanelProps, PrintLineState> {

    componentWillMount() {
        //this.loadState().done();
    }

    handleStart = (e: React.MouseEvent) => {
        //API.start().then(() => this.loadState()).done();
    }


    render() {
        document.title = "PrintLogic state";

        if (this.state == undefined)
            return <h2>PrintLogic state (loading...) </h2>;

        const s = this.state;

        return (
            <div>
                    <h2>Latest Processes</h2>
                    <SearchControl findOptions={{
                        queryName: PrintPackageEntity, 
                        orderOptions: [{ columnName: "PrintedOn", orderType: "Descending" }], 
                        searchOnLoad: true, 
                        showFilters: false, 
                        pagination: { elementsPerPage: 10, mode: "Firsts"}}}/>
                </div>
         );
    }
}



