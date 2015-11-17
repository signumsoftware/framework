
import * as React from 'react'
import { QuerySettings } from 'framework/signum.react/Scripts/QuerySettings'
import { ResultTable, FindOptions, FilterOptions, QueryDescription } from 'framework/signum.react/Scripts/FindOptions'
import { SearchMessage, JavascriptMessage } from 'framework/signum.react/Scripts/Signum.Entities'


export interface SimpleFilterBuilderProps {
    findOptions: FindOptions;
}


export interface SearchControlProps extends React.Props<SearchControl> {
    findOptions: FindOptions,
    simpleFilterBuilder: React.ComponentClass<SimpleFilterBuilderProps>
}

export interface SearchControlState {
    resultTable: ResultTable;
    findOptions: FindOptions;
    querySettings: QuerySettings;
    queryDescription: QueryDescription;
    loading: boolean;
}


export class SearchControl extends React.Component<SearchControlProps, SearchControlState> {

    toggleFilters() {
        this.props.findOptions.showFilters = !this.props.findOptions.showFilters;
        this.forceUpdate();
    }

    search() {

    }

    render() {

        var fo = this.props.findOptions;

        var SFB = this.props.simpleFilterBuilder;

        return (
            <div className="sf-search-control SF-control-container">
            {SFB && <div className="simple-filter-builder"><SFB  findOptions={fo}/></div> }
            {fo.showHeader && fo.showFilters && <FilterControl queryName={fo.queryName} filterOptions={fo.filterOptions}/> }
            {fo.showHeader && this.renderToolBar() }
                </div>);
    }

    renderToolBar() {
        var fo = this.props.findOptions;

        return <div className="sf-query-button-bar">
                { fo.showFilterButton && <a
                    className={"sf-query-button sf-filters-header btn btn-default" + (fo.showFilters ? " active" : "") }
                    onClick={() => this.toggleFilters() }
                    title={ fo.showFilters ? JavascriptMessage.hideFilters.niceToString() : JavascriptMessage.showFilters.niceToString() }><span className="glyphicon glyphicon glyphicon-filter"></span></a >}
                <button className={"sf-query-button sf-search btn btn-primary" + (this.state.loading ? " disabled" : "") } onClick={() => this.search() }>{SearchMessage.Search.niceToString() } </button>
                {fo.create && <a className="sf-query-button btn btn-default sf-line-button sf-create" title={}><span className="glyphicon glyphicon-plus"></span></a>}
            </div>;
    }

    createTitle() {

        var entityColType = this.state.queryDescription.columns["Entity"].type;

        return SearchMessage.CreateNew0_G.niceToString().forGenderAndNumber().FormatWith(this.state.IsByAll ? "?" : implementations.Types.CommaOr(a => a.NiceName())); 
    }
}




export interface FilterControlProps {
    filterOptions?: FilterOptions[];
    queryName: any;
}


export class FilterControl extends React.Component<FilterControlProps, {}>
{
    render() {
        return <div></div>
    }
}

