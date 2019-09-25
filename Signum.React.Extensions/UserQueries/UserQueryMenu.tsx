import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import { parseLite, is, Lite, toLite, newMListElement, toMList } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded'
import { UserQueryEntity, UserQueryMessage, QueryColumnEmbedded, QueryOrderEmbedded, UserQueryOperation } from './Signum.Entities.UserQueries'
import * as UserQueryClient from './UserQueryClient'
import * as UserAssetClient from '../UserAssets/UserAssetClient'
import { QueryTokenEmbedded } from '../UserAssets/Signum.Entities.UserAssets';
import { DropdownMenu, DropdownItem, Dropdown, DropdownToggle } from '@framework/Components';
import { getQueryKey, Type } from '@framework/Reflection';
import * as Operations from '@framework/Operations';

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

  handleBackToDefault = () => {

    const sc = this.props.searchControl
    const ofo = sc.props.findOptions;
    Finder.getQueryDescription(sc.props.findOptions.queryKey)
      .then(qd => Finder.parseFindOptions({ queryName: sc.props.findOptions.queryKey }, qd))
      .then(nfo => {

        ofo.filterOptions = [
          ...ofo.filterOptions.filter(a => a.frozen),
          ...nfo.filterOptions
        ];
        ofo.columnOptions = nfo.columnOptions;
        ofo.orderOptions = nfo.orderOptions;
        ofo.groupResults = nfo.groupResults;
        ofo.pagination = nfo.pagination;
        ofo.systemTime = nfo.systemTime;
        sc.setState({ showFilters: false });
        this.setState({ currentUserQuery: undefined });
        if (ofo.pagination.mode != "All") {
          sc.doSearchPage1();
        }
      }).done();
  }


  applyUserQuery(uq: Lite<UserQueryEntity>) {

    Navigator.API.fetchAndForget(uq).then(userQuery => {
      const sc = this.props.searchControl
      const oldFindOptions = sc.props.findOptions;
      UserQueryClient.Converter.applyUserQuery(oldFindOptions, userQuery, undefined)
        .then(newFindOptions => {
          sc.setState({ showFilters: true });
          this.setState({
            currentUserQuery: uq,
          });
          if (sc.props.findOptions.pagination.mode != "All") {
            sc.doSearchPage1();
          }
        });
    }).done()
  }

  handleOnClick = (uq: Lite<UserQueryEntity>) => {

    this.applyUserQuery(uq);
  }

  handleEdit = () => {
    Navigator.API.fetchAndForget(this.state.currentUserQuery!)
      .then(userQuery => Navigator.navigate(userQuery))
      .then(() => this.reloadList())
      .then(() => this.applyUserQuery(this.state.currentUserQuery!))
      .done();
  }


  async createUserQuery(): Promise<void> {

    const sc = this.props.searchControl;

    const fo = Finder.toFindOptions(sc.props.findOptions, sc.props.queryDescription);

    const qfs = await UserAssetClient.API.stringifyFilters({
      canAggregate: fo.groupResults || false,
      queryKey: getQueryKey(fo.queryName),
      filters: (fo.filterOptions || []).map(fo => UserAssetClient.Converter.toFilterNode(fo))
    });

    const qe = await Finder.API.fetchQueryEntity(getQueryKey(fo.queryName));

    const uq = await Navigator.view(UserQueryEntity.New({
      query: qe,
      owner: Navigator.currentUser && toLite(Navigator.currentUser),
      groupResults: fo.groupResults,
      filters: qfs.map(f => newMListElement(UserAssetClient.Converter.toQueryFilterEmbedded(f))),
      columns: (fo.columnOptions || []).map(c => newMListElement(QueryColumnEmbedded.New({
        token: QueryTokenEmbedded.New({ tokenString: c.token.toString() }),
        displayName: c.displayName
      }))),
      columnsMode: fo.columnOptionsMode,
      orders: (fo.orderOptions || []).map(c => newMListElement(QueryOrderEmbedded.New({
        orderType: c.orderType,
        token: QueryTokenEmbedded.New({ tokenString: c.token.toString() })
      }))),
      paginationMode: fo.pagination && fo.pagination.mode,
      elementsPerPage: fo.pagination && fo.pagination.elementsPerPage
    }));

    if (uq && uq.id) {
      await this.reloadList();
      this.setState({ currentUserQuery: toLite(uq) },
        () => this.applyUserQuery(this.state.currentUserQuery!));
    }
  }

  render() {
    const currentUserQueryToStr = this.state.currentUserQuery ? this.state.currentUserQuery.toStr : undefined;
    const labelText = this.props.searchControl.props.largeToolbarButtons == true ?
      (UserQueryMessage.UserQueries_UserQueries.niceToString() + (currentUserQueryToStr ? ` - ${currentUserQueryToStr.etc(50)}` : "")) : undefined;

    const label = <span title={currentUserQueryToStr}><FontAwesomeIcon icon={["far", "list-alt"]} />&nbsp;{labelText ? " " + labelText : undefined}</span>;
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
          <DropdownItem onClick={this.handleBackToDefault} ><FontAwesomeIcon icon={["fas", "undo"]} className="mr-2" />{UserQueryMessage.UserQueries_BackToDefault.niceToString()}</DropdownItem>
          {this.state.currentUserQuery && <DropdownItem onClick={this.handleEdit} ><FontAwesomeIcon icon={["fas", "edit"]} className="mr-2" />{UserQueryMessage.UserQueries_Edit.niceToString()}</DropdownItem>}
          {Operations.isOperationAllowed(UserQueryOperation.Save, UserQueryEntity) && <DropdownItem onClick={() => { this.createUserQuery().done() }}><FontAwesomeIcon icon={["fas", "plus"]} className="mr-2" />{UserQueryMessage.UserQueries_CreateNew.niceToString()}</DropdownItem>}
        </DropdownMenu>
      </Dropdown>
    );
  }

}



