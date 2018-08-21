/// <reference path="../globals.d.ts" />

import * as React from 'react'
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
import SearchControlLoaded, { ShowBarExtensionOption } from './SearchControlLoaded'

import "./Search.css"
import { ErrorBoundary } from '../Components';
import { MaxHeightProperty } from 'csstype';

export interface SimpleFilterBuilderProps {
    findOptions: FindOptions;
}

export interface SearchControlProps extends React.Props<SearchControl> {
    findOptions: FindOptions;

    formatters?: { [token: string]: CellFormatter };
    rowAttributes?: (row: ResultRow, columns: string[]) => React.HTMLAttributes<HTMLTableRowElement> | undefined;
    entityFormatter?: EntityFormatter;
    extraButtons?: (searchControl: SearchControlLoaded) => (React.ReactElement<any> | null | undefined | false)[];
    getViewPromise?: (e: any /*Entity*/) => undefined | string | Navigator.ViewPromise<any /*Entity*/>;
    maxResultsHeight?: MaxHeightProperty<string | number> | any;
    tag?: string | {};

    searchOnLoad?: boolean;
    allowSelection?: boolean
    showContextMenu?: boolean | "Basic";
    hideButtonBar?: boolean;
    hideFullScreenButton?: boolean;
    showHeader?: boolean;
    showBarExtension?: boolean;
    showBarExtensionOption?: ShowBarExtensionOption;
    showFilters?: boolean;
    showSimpleFilterBuilder?: boolean;
    showFilterButton?: boolean;
    showSystemTimeButton?: boolean;
    showGroupButton?: boolean;
    showFooter?: boolean;
    allowChangeColumns?: boolean;
    allowChangeOrder?: boolean;
    create?: boolean;
    navigate?: boolean;
    largeToolbarButtons?: boolean;
    avoidAutoRefresh?: boolean;
    avoidChangeUrl?: boolean;
    throwIfNotFindable?: boolean;
    refreshKey?: string | number;

    simpleFilterBuilder?: (qd: QueryDescription, initialFilterOptions: FilterOptionParsed[]) => React.ReactElement<any> | undefined;
    onNavigated?: (lite: Lite<Entity>) => void;
    onDoubleClick?: (e: React.MouseEvent<any>, row: ResultRow) => void;
    onSelectionChanged?: (entity: ResultRow[]) => void;
    onFiltersChanged?: (filters: FilterOptionParsed[]) => void;
    onHeighChanged?: () => void;
    onSearch?: (fo: FindOptionsParsed, dataChange: boolean) => void;
    onResult?: (table: ResultTable, dataChange: boolean) => void;
    onCreate?: () => void;
}

export interface SearchControlState {
    findOptions?: FindOptionsParsed;
    queryDescription?: QueryDescription;
}
export default class SearchControl extends React.Component<SearchControlProps, SearchControlState> {

    static defaultProps = {
        allowSelection: true,
        avoidFullScreenButton: false,
        maxResultsHeight: "400px"
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

        this.setState({ findOptions: undefined, queryDescription: undefined }, () => {
            this.initialLoad(newProps.findOptions);
        });
    }

    doSearch() {
        this.searchControlLoaded && this.searchControlLoaded.doSearch();
    }

    doSearchPage1() {
        this.searchControlLoaded && this.searchControlLoaded.doSearchPage1();
    }

    initialLoad(fo: FindOptions) {

        if (!Finder.isFindable(fo.queryName, false))
        {
            if (this.props.throwIfNotFindable)
                throw Error(`Query ${fo.queryName} not allowed`);

            return;
        }

        Finder.getQueryDescription(fo.queryName).then(qd => {

            this.setState({ queryDescription: qd });

            if (Finder.validateNewEntities(fo))
                this.setState({ findOptions: undefined });
            else
                Finder.parseFindOptions(fo, qd).then(fop => {
                    this.setState({ findOptions: fop, });
                }).done();
        }).done();
    }

    searchControlLoaded?: SearchControlLoaded;

    handleFullScreenClick(ev: React.MouseEvent<any>) {
        this.searchControlLoaded && this.searchControlLoaded.handleFullScreenClick(ev);
    }

    render() {

        var errorMessage = Finder.validateNewEntities(this.props.findOptions);
        if (errorMessage) {
            return (
                <div className="alert alert-danger" role="alert">
                    <strong>Error in SearchControl ({getQueryKey(this.props.findOptions.queryName)}): </strong>
                    {errorMessage}
                </div>
            );
        }

        const fo = this.state.findOptions;
        if (!fo)
            return null;

        if (!Finder.isFindable(fo.queryKey, false))
            return null;


        const p = this.props;

        const qs = Finder.getSettings(fo.queryKey);
        const qd = this.state.queryDescription!;

        const tis = getTypeInfos(qd.columns["Entity"].type);

        return (
            <ErrorBoundary>
                <SearchControlLoaded ref={lo => this.searchControlLoaded = lo!}
                    findOptions={fo}
                    queryDescription={qd}
                    querySettings={qs}

                    formatters={p.formatters}
                    rowAttributes={p.rowAttributes}
                    entityFormatter={p.entityFormatter}
                    extraButtons={p.extraButtons}
                    getViewPromise={p.getViewPromise}
                    maxResultsHeight={p.maxResultsHeight}
                    tag={p.tag}

                    searchOnLoad={p.searchOnLoad != null ? p.searchOnLoad : true}
                    showHeader={p.showHeader != null ? p.showHeader : true}
                    showFilters={p.showFilters != null ? p.showFilters : false}
                    showSimpleFilterBuilder={p.showSimpleFilterBuilder != null ? p.showSimpleFilterBuilder : true}
                    showFilterButton={p.showFilterButton != null ? p.showFilterButton : true}
                    showSystemTimeButton={p.showSystemTimeButton != null ? p.showSystemTimeButton : qs && qs.allowSystemTime != null ? qs.allowSystemTime : tis.some(a => a.isSystemVersioned == true)}
                    showGroupButton={p.showGroupButton != null ? p.showGroupButton : false}
                    showFooter={p.showFooter != null ? p.showFooter : true}
                    allowChangeColumns={p.allowChangeColumns != null ? p.allowChangeColumns : true}
                    allowChangeOrder={p.allowChangeOrder != null ? p.allowChangeOrder : true}
                    create={p.create != null ? p.create : tis.some(ti => Navigator.isCreable(ti, false, true))}
                    navigate={p.navigate != null ? p.navigate : tis.some(ti => Navigator.isNavigable(ti, undefined, true))}


                    allowSelection={p.allowSelection != null ? p.allowSelection : true}
                    showContextMenu={p.showContextMenu != null ? p.showContextMenu : true}
                    hideButtonBar={p.hideButtonBar != null ? p.hideButtonBar : false}
                    hideFullScreenButton={p.hideFullScreenButton != null ? p.hideFullScreenButton : false}
                    showBarExtension={p.showBarExtension != null ? p.showBarExtension : true}
                    showBarExtensionOption={p.showBarExtensionOption}
                    largeToolbarButtons={p.largeToolbarButtons != null ? p.largeToolbarButtons : false}
                    avoidAutoRefresh={p.avoidAutoRefresh != null ? p.avoidAutoRefresh : false}
                    avoidChangeUrl={p.avoidChangeUrl != null ? p.avoidChangeUrl : true}
                    refreshKey={p.refreshKey}

                    simpleFilterBuilder={p.simpleFilterBuilder}

                    onCreate={p.onCreate}
                    onNavigated={p.onNavigated}
                    onSearch={p.onSearch}
                    onDoubleClick={p.onDoubleClick}
                    onSelectionChanged={p.onSelectionChanged}
                    onFiltersChanged={p.onFiltersChanged}
                    onHeighChanged={p.onHeighChanged}
                    onResult={p.onResult}
                />
            </ErrorBoundary>
        );
    }
}

export interface ISimpleFilterBuilder {
    getFilters(): FilterOption[];
}


