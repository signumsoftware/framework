
import * as React from 'react'
import { Dic } from '../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchMessage, JavascriptMessage, parseLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Reflection from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import SearchControl from '../../../Framework/Signum.React/Scripts/SearchControl/SearchControl'
import { UserQueryEntity_Type, UserQueryEntity  } from './Signum.Entities.UserQueries'
import * as UserQueryClient from './UserQueryClient'


interface UserQueryPageProps extends ReactRouter.RouteComponentProps<{}, { userQueryId: string }> {

}

export default class UserQueryPage extends React.Component<UserQueryPageProps, { userQuery?: UserQueryEntity, findOptions?: FindOptions, }> {
    
    constructor(props) {
        super(props);
        this.state = { userQuery: null, findOptions: null };

        this.requestFindOptions();
    }
    
    componentWillReceiveProps(nextProps: UserQueryPageProps) {

        this.state = { userQuery: null, findOptions: null };

        this.requestFindOptions();
    }

    requestFindOptions() {
        Navigator.API.fetchEntity(UserQueryEntity_Type, this.props.routeParams.userQueryId).then(uq => {
            this.setState({ userQuery: uq });

            UserQueryClient.Converter.toFindOptions(uq, null).then(findOptions => {
                this.setState({ findOptions: findOptions });
            }).done();
        }).done();
    }

    searchControl: SearchControl

    render() {

        const fo = this.state.findOptions;
        const uq = this.state.userQuery;

        return (
            <div id="divSearchPage">
                <h2>
                    <span>
                        <span className="sf-entity-title">{Reflection.getQueryNiceName(uq.query.key) }</span> & nbsp;
                        { fo && <a className="sf-popup-fullscreen" href="#" onClick={(e) => this.searchControl.handleFullScreenClick(e) }>
                            <span className="glyphicon glyphicon-new-window"></span>
                        </a> }
                    </span>
                    { this.state.userQuery && < small > { this.state.userQuery.displayName }</small> }
                </h2>
                { fo && <SearchControl ref={e => this.searchControl = e}
                    hideExternalButton={true} findOptions={fo} /> }
            </div>
        );
    }
}



