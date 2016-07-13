
import * as React from 'react'
import {RouteComponentProps, Router, RouteContext } from 'react-router'
import { DropdownButton, MenuItem, } from 'react-bootstrap'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchMessage, JavascriptMessage, parseLite, is, Lite, toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import SearchControl from '../../../Framework/Signum.React/Scripts/SearchControl/SearchControl'
import { UserQueryEntity, UserQueryMessage  } from './Signum.Entities.UserQueries'
import * as UserQueryClient from './UserQueryClient'

export interface UserQueryMenuProps {
    searchControl: SearchControl;
}

export default class UserQueryMenu extends React.Component<UserQueryMenuProps, { currentUserQuery?: Lite<UserQueryEntity>, userQueries?: Lite<UserQueryEntity>[] }> {

    constructor(props: UserQueryMenuProps) {
        super(props);
        this.state = { };
    }

    componentWillMount() {
        const props = this.props as RouteComponentProps<any, any>;
        const userQuery = window.location.search.tryAfter("userQuery=");
        if (userQuery) {
            const uq = parseLite(decodeURIComponent(userQuery.tryBefore("&") || userQuery)) as Lite<UserQueryEntity>;
            Navigator.API.fillToStrings([uq])
                .then(() => this.setState({ currentUserQuery: uq }))
                .done();
        }
    }

    handleSelectedToggle = (isOpen: boolean) => {

        if (isOpen && this.state.userQueries == undefined)
            this.reloadList().done();
    }

    reloadList(): Promise<void> {
        return UserQueryClient.API.forQuery(this.props.searchControl.getQueryKey())
            .then(list => this.setState({ userQueries: list }));
    }


    handleSelect = (uq: Lite<UserQueryEntity>) => {

        Navigator.API.fetchAndForget(uq).then(userQuery => {
            const oldFindOptions = this.props.searchControl.state.findOptions;
            UserQueryClient.Converter.applyUserQuery(oldFindOptions, userQuery, undefined)
                .then(newFindOptions => {
                    this.props.searchControl.resetFindOptions(newFindOptions);
                    this.setState({ currentUserQuery: uq });
                })
                .done();
        }).then();
    }

    handleEdit = () => {
        Navigator.API.fetchAndForget(this.state.currentUserQuery)
            .then(userQuery => Navigator.navigate(userQuery))
            .then(() => this.reloadList())
            .done();
    }


    handleCreate = () => {

        UserQueryClient.API.fromQueryRequest({
            queryRequest: this.props.searchControl.getQueryRequest(),
            defaultPagination: this.props.searchControl.defaultPagination()
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
        const label = UserQueryMessage.UserQueries_UserQueries.niceToString();
        const userQueries = this.state.userQueries;
        return (
            <DropdownButton title={label} label={label} id="userQueriesDropDown" className="sf-userquery-dropdown"
                onToggle={this.handleSelectedToggle}>
                {
                    userQueries && userQueries.map((uq, i) =>
                        <MenuItem key={i}
                            className={classes("sf-userquery", is(uq, this.state.currentUserQuery) && "active") }
                            onSelect={() => this.handleSelect(uq) }>
                            { uq.toStr }
                        </MenuItem>)
                }
                { userQueries && userQueries.length > 0 && <MenuItem divider/> }
                { this.state.currentUserQuery && <MenuItem onSelect={this.handleEdit} >{UserQueryMessage.UserQueries_Edit.niceToString() }</MenuItem> }
                <MenuItem onSelect={this.handleCreate}>{UserQueryMessage.UserQueries_CreateNew.niceToString() }</MenuItem>
            </DropdownButton>
        );
    }
 
}



