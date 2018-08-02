import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { RouteComponentProps } from 'react-router'
import { Dic, classes } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '@framework/FindOptions'
import { SearchMessage, JavascriptMessage, parseLite, is, Lite, toLite } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded'
import { UserQueryEntity, UserQueryMessage } from './Signum.Entities.UserQueries'
import * as UserQueryClient from './UserQueryClient'
import { DropdownMenu, DropdownItem, Dropdown, DropdownToggle } from '@framework/Components';

export interface UserQueryMenuProps {
    searchControl: SearchControlLoaded;
}


interface UserQueryMenuState {
    currentUserQuery?: Lite<UserQueryEntity>;
    userQueries?: Lite<UserQueryEntity>[];
    isOpen: boolean;
}



export default class UserQueryMenu extends React.Component<UserQueryMenuProps, UserQueryMenuState> {

    constructor(props: UserQueryMenuProps) {
        super(props);
        this.state = { isOpen: false };
    }

    componentWillMount() {
        const userQuery = window.location.search.tryAfter("userQuery=");
        if (userQuery) {
            const uq = parseLite(decodeURIComponent(userQuery.tryBefore("&") || userQuery)) as Lite<UserQueryEntity>;
            Navigator.API.fillToStrings(uq)
                .then(() => this.setState({ currentUserQuery: uq }))
                .done();
        }
    }

    handleSelectedToggle = () => {
        if (!this.state.isOpen && this.state.userQueries == undefined)
            this.reloadList().done();

        this.setState({ isOpen: !this.state.isOpen });
    }

    reloadList(): Promise<void> {
        return UserQueryClient.API.forQuery(this.props.searchControl.props.findOptions.queryKey)
            .then(list => this.setState({ userQueries: list }));
    }


    handleOnClick = (uq: Lite<UserQueryEntity>) => {

        Navigator.API.fetchAndForget(uq).then(userQuery => {
            const sc = this.props.searchControl
            const oldFindOptions = sc.props.findOptions;
            UserQueryClient.Converter.applyUserQuery(oldFindOptions, userQuery, undefined)
                .then(newFindOptions => {
                    sc.setState({ showFilters: true });
                    this.setState({ currentUserQuery: uq });
                    if (sc.props.findOptions.pagination.mode != "All") {
                        sc.doSearchPage1();
                    }
                })
                .done();
        }).then();
    }

    handleEdit = () => {
        Navigator.API.fetchAndForget(this.state.currentUserQuery!)
            .then(userQuery => Navigator.navigate(userQuery))
            .then(() => this.reloadList())
            .done();
    }


    handleCreate = () => {

        UserQueryClient.API.fromQueryRequest({
            queryRequest: this.props.searchControl.getQueryRequest(),
            defaultPagination: Finder.defaultPagination
        }).then(userQuery => Navigator.view(userQuery))
            .then(uq => {
                if (uq && uq.id) {
                    this.reloadList()
                        .then(() => this.setState({ currentUserQuery: toLite(uq) }))
                        .done();
                }
            }).done();
    }

    render() {
        const label = <span><FontAwesomeIcon icon={["far", "list-alt"]} />&nbsp;{this.props.searchControl.props.largeToolbarButtons == true ? " " + UserQueryMessage.UserQueries_UserQueries.niceToString() : undefined}</span>;
        const userQueries = this.state.userQueries;
        return (
            <Dropdown id="userQueriesDropDown" className="sf-userquery-dropdown" color="light"
                toggle={this.handleSelectedToggle} isOpen={this.state.isOpen}>
                <DropdownToggle color="light" caret>{label as any}</DropdownToggle>
                <DropdownMenu>
                    {
                        userQueries && userQueries.map((uq, i) =>
                            <DropdownItem key={i}
                                className={classes("sf-userquery", is(uq, this.state.currentUserQuery) && "active")}
                                onClick={() => this.handleOnClick(uq)}>
                                {uq.toStr}
                            </DropdownItem>)
                    }
                    {userQueries && userQueries.length > 0 && <DropdownItem divider />}
                    {this.state.currentUserQuery && <DropdownItem onClick={this.handleEdit} >{UserQueryMessage.UserQueries_Edit.niceToString()}</DropdownItem>}
                    <DropdownItem onClick={this.handleCreate}>{UserQueryMessage.UserQueries_CreateNew.niceToString()}</DropdownItem>
                </DropdownMenu>
            </Dropdown>
        );
    }

}



