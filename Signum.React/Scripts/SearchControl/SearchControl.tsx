/// <reference path="../globals.d.ts" />

import * as React from 'react'
import { DropdownButton, MenuItem, OverlayTrigger, Tooltip } from 'react-bootstrap'
import { Dic, DomUtils, classes, coalesce } from '../Globals'
import * as Finder from '../Finder'
import { CellFormatter, EntityFormatter } from '../Finder'
import {
    ResultTable, ResultRow, FindOptions, FindOptionsParsed, FilterOptionParsed, FilterOption, QueryDescription, ColumnOption, ColumnOptionsMode, ColumnDescription,
    toQueryToken, Pagination, PaginationMode, OrderType, OrderOption, SubTokensOptions, filterOperations, QueryToken, QueryRequest, FilterOperation
} from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, liteKey, Entity, is, isEntity, isLite, toLite, ModifiableEntity } from '../Signum.Entities'
import { getTypeInfos, getTypeInfo, TypeReference, IsByAll, getQueryKey, TypeInfo, EntityData, QueryKey, PseudoType } from '../Reflection'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import PaginationSelector from './PaginationSelector'
import ColumnEditor from './ColumnEditor'
import MultipliedMessage from './MultipliedMessage'
import { renderContextualItems, ContextualItemsContext, MarkedRowsDictionary, MarkedRow } from './ContextualItems'
import ContextMenu from './ContextMenu'
import SelectorModal from '../SelectorModal'
import SearchControlLoaded from './SearchControlLoaded'

require("./Search.css");

export interface SimpleFilterBuilderProps {
    findOptions: FindOptions;
}


export interface SearchControlProps extends React.Props<SearchControl> {
    allowSelection?: boolean
    findOptions: FindOptions;
    onDoubleClick?: (e: React.MouseEvent<any>, row: ResultRow) => void;
    formatters?: { [columnName: string]: CellFormatter };
    rowAttributes?: (row: ResultRow, columns: string[]) => React.HTMLAttributes<HTMLTableRowElement> | undefined;
    entityFormatter?: EntityFormatter;
    showContextMenu?: boolean | "Basic";
    onSelectionChanged?: (entity: Lite<Entity>[]) => void;
    onFiltersChanged?: (filters: FilterOptionParsed[]) => void;
    onResult?: (table: ResultTable) => void;
    onSearch?: (fo: FindOptionsParsed) => void;
    hideButtonBar?: boolean;
    hideFullScreenButton?: boolean;
    showBarExtension?: boolean;
    largeToolbarButtons?: boolean; 
    throwIfNotFindable?: boolean;
    extraButtons?: (searchControl: SearchControlLoaded) => React.ReactNode
    onCreate?: () => Promise<void>;
    getViewPromise?: (e: ModifiableEntity) => Navigator.ViewPromise<ModifiableEntity>;
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
        var path = Finder.findOptionsPath(newProps.findOptions);
        if (path == Finder.findOptionsPath(this.props.findOptions))
            return;

        if (this.state.findOptions && this.state.queryDescription) {
            var fo = Finder.toFindOptions(this.state.findOptions, this.state.queryDescription);
            if (path == Finder.findOptionsPath(fo))
                return;
        }

        this.state = {};
        this.forceUpdate();

        this.initialLoad(newProps.findOptions);
    }


    initialLoad(propsFindOptions: FindOptions) {

        if (!Finder.isFindable(propsFindOptions.queryName))
        {
            if (this.props.throwIfNotFindable)
                throw Error(`Query ${propsFindOptions.queryName} not allowed`);

            return;
        }

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

    handleFullScreenClick(ev: React.MouseEvent<any>) {
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
            formatters={this.props.formatters}
            rowAttributes={this.props.rowAttributes}
            entityFormatter={this.props.entityFormatter}
            showContextMenu={this.props.showContextMenu}
            onSelectionChanged={this.props.onSelectionChanged}
            onFiltersChanged={this.props.onFiltersChanged}
            onSearch={this.props.onSearch}
            onResult={this.props.onResult}
            hideButtonBar={this.props.hideButtonBar}
            hideFullScreenButton={this.props.hideFullScreenButton}
            showBarExtension={this.props.showBarExtension}
            extraButtons={this.props.extraButtons}
            largeToolbarButtons={this.props.largeToolbarButtons}
            findOptions={fo}
            queryDescription={this.state.queryDescription!}
            querySettings={Finder.getSettings(fo.queryKey)}
            onCreate={this.props.onCreate}
            getViewPromise={this.props.getViewPromise}
            />
    }
}

export interface ISimpleFilterBuilder {
    getFilters(): FilterOption[];
}


