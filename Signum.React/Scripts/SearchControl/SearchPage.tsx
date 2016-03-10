
import * as React from 'react'
import { Dic } from '../Globals'
import * as Finder from '../Finder'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../FindOptions'
import { SearchMessage, JavascriptMessage } from '../Signum.Entities'
import { getQueryNiceName } from '../Reflection'
import { default as SearchControl, ExternalFullScreenButton} from './SearchControl'



interface SearchPageProps extends ReactRouter.RouteComponentProps<{}, { queryName: string }> {

}

export default class SearchPage extends React.Component<SearchPageProps, { findOptions?: FindOptions, }> {

    externalButton: ExternalFullScreenButton = { onClick: null };

    constructor(props) {
        super(props);
        this.state = this.calculateState(this.props);
    }


    componentWillReceiveProps(nextProps: SearchPageProps) {
        this.setState(this.calculateState(nextProps));
    }

    calculateState(props: SearchPageProps) {
        return {
            findOptions: Dic.extend({ showFilters: true }, Finder.parseFindOptionsPath(props.routeParams.queryName, props.location.query)),
        };
    }

    render() {

        const fo = this.state.findOptions;

        return (
            <div id="divSearchPage">
                <h2>
                    <span className="sf-entity-title">{getQueryNiceName(fo.queryName) }</span>&nbsp;
                    <a className="sf-popup-fullscreen" href="#" onClick={(e) => this.externalButton.onClick(e) }>
                        <span className="glyphicon glyphicon-new-window"></span>
                    </a>
                </h2>
                <SearchControl externalFullScreenButton={this.externalButton} findOptions={fo} />
            </div>
        );
    }
}



