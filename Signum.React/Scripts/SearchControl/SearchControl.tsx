/// <reference path="../globals.d.ts" />

import * as React from 'react'
import { DropdownButton, MenuItem, OverlayTrigger, Tooltip } from 'react-bootstrap'
import { Dic, DomUtils, classes, coalesce } from '../Globals'
import * as Finder from '../Finder'
import {
    ResultTable, ResultRow, FindOptions, FindOptionsParsed, FilterOptionParsed, FilterOption, QueryDescription, ColumnOption, ColumnOptionsMode, ColumnDescription,
    toQueryToken, Pagination, PaginationMode, OrderType, OrderOption, SubTokensOptions, filterOperations, QueryToken, QueryRequest
} from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, liteKey, Entity, is, isEntity, isLite, toLite } from '../Signum.Entities'
import { getTypeInfos, getTypeInfo, TypeReference, IsByAll, getQueryKey, TypeInfo, EntityData, QueryKey, PseudoType } from '../Reflection'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import PaginationSelector from './PaginationSelector'
import FilterBuilder from './FilterBuilder'
import ColumnEditor from './ColumnEditor'
import MultipliedMessage from './MultipliedMessage'
import { renderContextualItems, ContextualItemsContext, MarkedRowsDictionary, MarkedRow } from './ContextualItems'
import ContextMenu from './ContextMenu'
import SelectorModal from '../SelectorModal'
import SearchControlLoaded from './SearchControlLoaded'

require("!style!css!./Search.css");

export interface SimpleFilterBuilderProps {
    findOptions: FindOptions;
}


export interface SearchControlProps extends React.Props<SearchControl> {
    allowSelection?: boolean
    findOptions: FindOptions;
    onDoubleClick?: (e: React.MouseEvent, row: ResultRow) => void;
    showContextMenu?: boolean;
    onSelectionChanged?: (entity: Lite<Entity>[]) => void;
    onFiltersChanged?: (filters: FilterOptionParsed[]) => void;
    onResult?: (table: ResultTable) => void;
    hideFullScreenButton?: boolean;
    showBarExtension?: boolean;
}

export interface SearchControlState {
    findOptions?: FindOptionsParsed;
    queryDescription?: QueryDescription;

}



export default class SearchControl extends React.Component<SearchControlProps, SearchControlState> {

    static defaultProps = {
        allowSelection: true,
        avoidFullScreenButton: false
    };

    constructor(props: SearchControlProps) {
        super(props);
        this.state = {};
    }



    componentWillMount() {
        this.initialLoad(this.props.findOptions);
    }

    componentWillReceiveProps(newProps: SearchControlProps) {
        if (Finder.findOptionsPath(this.props.findOptions) == Finder.findOptionsPath(newProps.findOptions))
            return;

        this.state = {};
        this.forceUpdate();

        this.initialLoad(newProps.findOptions);
    }


    initialLoad(propsFindOptions: FindOptions) {

        if (!Finder.isFindable(propsFindOptions.queryName))
            return;

        Finder.getQueryDescription(propsFindOptions.queryName).then(qd => {
            Finder.parseFindOptions(propsFindOptions, qd).then(fop => {
                this.setState({
                    queryDescription: qd,
                    findOptions: fop,
                });
            }).done();
        }).done();
    }

    searchControlLoaded: SearchControlLoaded;

    handleFullScreenClick(ev: React.MouseEvent) {
        this.searchControlLoaded.handleFullScreenClick(ev);
    }

    render() {

        const fo = this.state.findOptions;
        if (!fo)
            return null;

        if (!Finder.isFindable(fo.queryKey))
            return null;

        return <SearchControlLoaded ref={(lo: SearchControlLoaded) => this.searchControlLoaded = lo}
            allowSelection={this.props.allowSelection}
            onDoubleClick={this.props.onDoubleClick}
            showContextMenu={this.props.showContextMenu}
            onSelectionChanged={this.props.onSelectionChanged}
            onFiltersChanged= {this.props.onFiltersChanged}
            onResult={this.props.onResult}
            hideFullScreenButton={this.props.hideFullScreenButton}
            showBarExtension={this.props.showBarExtension}
            findOptions={fo}
            queryDescription={this.state.queryDescription!}
            querySettings={Finder.getQuerySettings(fo.queryKey)}
            />
    }
}

export interface ISimpleFilterBuilder {
    getFilters(): FilterOption[];
}


