
import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import { Dic } from '../Globals'
import * as Finder from '../Finder'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../FindOptions'
import { SearchMessage, JavascriptMessage } from '../Signum.Entities'
import { getQueryNiceName } from '../Reflection'
import * as Navigator from '../Navigator'
import SearchControl, { SearchControlProps } from './SearchControl'
import * as QueryString from 'query-string'

interface SearchPageProps extends RouteComponentProps<{ queryName: string }> {

}

interface SearchPageState {
    findOptions: FindOptions;
}

export default class SearchPage extends React.Component<SearchPageProps, SearchPageState> {

    constructor(props: SearchPageProps) {
        super(props);
        this.state = this.calculateState(this.props);
    }

    searchControl: SearchControl;

    componentWillReceiveProps(nextProps: SearchPageProps) {
        this.setState(this.calculateState(nextProps));
    }

    
    componentWillUnmount() {
        Navigator.setTitle();
    }

    calculateState(props: SearchPageProps): SearchPageState {

        Navigator.setTitle(getQueryNiceName(props.match.params.queryName));

        return {
            findOptions: {
                showFilters: true,
                ...Finder.parseFindOptionsPath(props.match.params.queryName, QueryString.parse(props.location.search))
            },
        };
    }

    changeUrl() {

        const scl = this.searchControl.searchControlLoaded; 

        const findOptions = Finder.toFindOptions(scl.props.findOptions, scl.props.queryDescription);

        const newPath = Finder.findOptionsPath(findOptions);

        const currentLocation = Navigator.history.location;

        if (currentLocation.pathname + currentLocation.search != newPath)
            Navigator.history.replace(newPath);
    }

    render() {
        const fo = this.state.findOptions;

        return (
            <div id="divSearchPage">
                <h2>
                    <span className="sf-entity-title">{getQueryNiceName(fo.queryName)}</span>
                    &nbsp;
                    <a className="sf-popup-fullscreen" href="#" onClick={(e) => this.searchControl.handleFullScreenClick(e) }>
                        <span className="glyphicon glyphicon-new-window"></span>
                    </a>
                </h2>
                <SearchControl ref={(e: SearchControl) => this.searchControl = e}
                    throwIfNotFindable={true}
                    showBarExtension={true}
                    hideFullScreenButton={true}
                    largeToolbarButtons={true}
                    findOptions={fo}
                    onSearch={result => this.changeUrl()} />
            </div>
        );
    }
}



