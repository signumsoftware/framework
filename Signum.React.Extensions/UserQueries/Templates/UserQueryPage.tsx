
import * as React from 'react'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchMessage, JavascriptMessage, parseLite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import SearchControl from '../../../../Framework/Signum.React/Scripts/SearchControl/SearchControl'
import { UserQueryEntity } from '../Signum.Entities.UserQueries'
import * as UserQueryClient from '../UserQueryClient'


interface UserQueryPageProps extends ReactRouter.RouteComponentProps<{}, { userQueryId: string; entity?: string }> {

}

export default class UserQueryPage extends React.Component<UserQueryPageProps, { userQuery?: UserQueryEntity, findOptions?: FindOptions, }> {

    constructor(props: UserQueryPageProps) {
        super(props);
        this.state = {};
    }

    componentWillMount() {
        this.load(this.props);
    }

    componentWillReceiveProps(nextProps: UserQueryPageProps) {
        this.state = {};
        this.forceUpdate();
        this.load(nextProps);
    }

    searchControl: SearchControl;


    load(props: UserQueryPageProps) {

        var { userQueryId, entity } = this.props.routeParams;

        var lite = entity == null ? null : parseLite(entity);

        Navigator.API.fillToStrings(lite ? [lite] : [])
            .then(() => Navigator.API.fetchEntity(UserQueryEntity, userQueryId))
            .then(uc => {
                this.setState({ userQuery: uc });
                return UserQueryClient.Converter.toFindOptions(uc, lite)
            })
            .then(fo => {
                this.setState({ findOptions: Dic.extend({ showFilters: true }, fo) });
            })
            .done();
    }

    render() {

        const uq = this.state.userQuery;
        const fo = this.state.findOptions;

        if (fo == null || uq == null)
            return null;

        return (
            <div id="divSearchPage">
                <h2>
                    <span className="sf-entity-title">{getQueryNiceName(fo.queryName) }</span>&nbsp;
                    <a className="sf-popup-fullscreen" href="#" onClick={(e) => this.searchControl.handleFullScreenClick(e) }>
                        <span className="glyphicon glyphicon-new-window"></span>
                    </a>
                </h2>
                <SearchControl ref={e => this.searchControl = e}
                   hideExternalButton={true}
                   findOptions={fo} />
            </div>
        );
    }
}



