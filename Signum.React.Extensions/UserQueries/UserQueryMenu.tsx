import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import { parseLite, is, Lite, toLite, newMListElement, liteKey, SearchMessage } from '@framework/Signum.Entities'
import * as AppContext from '@framework/AppContext'
import * as Navigator from '@framework/Navigator'
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded'
import { UserQueryEntity, UserQueryMessage, QueryColumnEmbedded, QueryOrderEmbedded, UserQueryOperation } from './Signum.Entities.UserQueries'
import * as UserQueryClient from './UserQueryClient'
import * as UserAssetClient from '../UserAssets/UserAssetClient'
import { QueryTokenEmbedded } from '../UserAssets/Signum.Entities.UserAssets';
import { Dropdown } from 'react-bootstrap';
import { getQueryKey } from '@framework/Reflection';
import * as Operations from '@framework/Operations';
import { FilterOptionParsed } from '@framework/Search'
import { isFilterGroupOptionParsed } from '@framework/FindOptions'
import { QueryString } from '@framework/QueryString'
import { AutoFocus } from '@framework/Components/AutoFocus'
import { KeyCodes } from '../../../Framework/Signum.React/Scripts/Components'

export interface UserQueryMenuProps {
  searchControl: SearchControlLoaded;
}

function decodeUserQueryFromUrl() {
  var userQueryKey = QueryString.parse(window.location.search)["userQuery"];
  return userQueryKey && parseLite(userQueryKey);
}

export default function UserQueryMenu(p: UserQueryMenuProps) {

  const [filter, setFilter] = React.useState<string>();
  const [isOpen, setIsOpen] = React.useState<boolean>(false);
  const [currentUserQuery, setCurrentUserQuery] = React.useState<Lite<UserQueryEntity> | undefined>(() => {
    let uq = p.searchControl.props.tag == "SearchPage" ? decodeUserQueryFromUrl() : p.searchControl.props.extraOptions?.userQuery;
    return uq;
  });

  const [userQueries, setUserQueries] = React.useState<Lite<UserQueryEntity>[] | undefined>(undefined);
  const oldExtraParams = React.useRef(p.searchControl.extraParams);
  
  React.useEffect(() => {
    oldExtraParams.current = p.searchControl.extraParams;
    p.searchControl.extraParams = () => ({ ...oldExtraParams.current(), userQuery: currentUserQuery && liteKey(currentUserQuery) });
    return () => { p.searchControl.extraParams = oldExtraParams.current };
  }, []);

  React.useEffect(() => {
    if (currentUserQuery)
      reloadList();
  }, []);

  function handleSelectedToggle(isOpen: boolean) {
    if (isOpen && userQueries == undefined)
      reloadList().done();

    setIsOpen(isOpen);
  }

  function reloadList(): Promise<Lite<UserQueryEntity>[]> {
    return UserQueryClient.API.forQuery(p.searchControl.props.findOptions.queryKey)
      .then(list => {
        setUserQueries(list);
        if (currentUserQuery && currentUserQuery.toStr == null) {
          const similar = list.firstOrNull(l => is(l, currentUserQuery));
          if (similar != null) {
            currentUserQuery.toStr = similar.toStr;
            setCurrentUserQuery(currentUserQuery);
          } else {
            Navigator.API.fillToStrings(currentUserQuery)
              .then(() => setCurrentUserQuery(currentUserQuery))
              .done();
          }
        }
        return list;
      });
  }

  function handleBackToDefault() {
    const sc = p.searchControl
    const ofo = sc.props.findOptions;
    Finder.getQueryDescription(sc.props.findOptions.queryKey)
      .then(qd => Finder.parseFindOptions({ queryName: sc.props.findOptions.queryKey }, qd, sc.props.defaultIncudeDefaultFilters))
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
        if (nfo.filterOptions.length == 0 || anyPinned(nfo.filterOptions))
          sc.setState({ showFilters: false });
        sc.setState({ refreshMode: sc.props.defaultRefreshMode });
        setCurrentUserQuery(undefined);
        if (ofo.pagination.mode != "All") {
          sc.doSearchPage1();
        }
      }).done();
  }


  function applyUserQuery(uq: Lite<UserQueryEntity>) {
    Navigator.API.fetchAndForget(uq).then(userQuery => {
      const sc = p.searchControl
      const oldFindOptions = sc.props.findOptions;
      UserQueryClient.Converter.applyUserQuery(oldFindOptions, userQuery, undefined, sc.props.defaultIncudeDefaultFilters)
        .then(nfo => {
          sc.setState({ refreshMode: userQuery.refreshMode });
          if (nfo.filterOptions.length == 0 || anyPinned(nfo.filterOptions))
            sc.setState({ showFilters: false, simpleFilterBuilder: undefined });
          setCurrentUserQuery(uq);
          if (sc.props.findOptions.pagination.mode != "All") {
            sc.doSearchPage1();
          }
        });
    }).done()
  }

  function handleOnClick(uq: Lite<UserQueryEntity>) {
    applyUserQuery(uq);
  }

  function handleEdit() {
    Navigator.API.fetchAndForget(currentUserQuery!)
      .then(userQuery => Navigator.view(userQuery))
      .then(() => reloadList())
      .then(list => !list.some(a => is(a, currentUserQuery)) ? setCurrentUserQuery(undefined) : applyUserQuery(currentUserQuery!))
      .done();
  }

  async function createUserQuery(): Promise<void> {

    const sc = p.searchControl;

    const fo = Finder.toFindOptions(sc.props.findOptions, sc.props.queryDescription, sc.props.defaultIncudeDefaultFilters);

    const qfs = await UserAssetClient.API.stringifyFilters({
      canAggregate: fo.groupResults || false,
      queryKey: getQueryKey(fo.queryName),
      filters: (fo.filterOptions ?? []).map(fo => UserAssetClient.Converter.toFilterNode(fo))
    });

    const parsedTokens =
      [
        ...sc.props.findOptions.columnOptions.map(a => a.token),
        ...sc.props.findOptions.columnOptions.map(a => a.summaryToken),
        ...sc.props.findOptions.orderOptions.map(a => a.token),
      ].notNull()
        .toObjectDistinct(a => a.fullKey);

    const qe = await Finder.API.fetchQueryEntity(getQueryKey(fo.queryName));

    const uq = await Navigator.view(UserQueryEntity.New({
      query: qe,
      owner: AppContext.currentUser && toLite(AppContext.currentUser),
      groupResults: fo.groupResults,
      filters: qfs.map(f => newMListElement(UserAssetClient.Converter.toQueryFilterEmbedded(f))),
      includeDefaultFilters: fo.includeDefaultFilters,
      columns: (fo.columnOptions ?? []).map(c => newMListElement(QueryColumnEmbedded.New({
        token: QueryTokenEmbedded.New({ tokenString: c.token.toString(), token: parsedTokens[c.token.toString()] }),
        displayName: typeof c.displayName == "function" ? c.displayName() : c.displayName,
        summaryToken: c.summaryToken ? QueryTokenEmbedded.New({ tokenString: c.token.toString(), token: parsedTokens[c.token.toString()] }) : null,
        hiddenColumn: c.hiddenColumn
      }))),
      columnsMode: fo.columnOptionsMode,
      orders: (fo.orderOptions ?? []).map(c => newMListElement(QueryOrderEmbedded.New({
        orderType: c.orderType,
        token: QueryTokenEmbedded.New({
          tokenString: c.token.toString(),
          token: parsedTokens[c.token.toString()]
        })
      }))),
      paginationMode: fo.pagination && fo.pagination.mode,
      elementsPerPage: fo.pagination && fo.pagination.elementsPerPage,
      refreshMode: p.searchControl.state.refreshMode ?? "Auto",
    }));

    if (uq?.id) {
      await reloadList();

      setCurrentUserQuery(toLite(uq));
      applyUserQuery(toLite(uq));
    }
  }

  const currentUserQueryToStr = currentUserQuery ? currentUserQuery.toStr : undefined;
  const labelText = p.searchControl.props.largeToolbarButtons == true ?
    (UserQueryMessage.UserQueries_UserQueries.niceToString() + (currentUserQueryToStr ? ` - ${currentUserQueryToStr.etc(50)}` : "")) : undefined;

  var canSave = Operations.tryGetOperationInfo(UserQueryOperation.Save, UserQueryEntity) != null;

  const label = <span title={currentUserQueryToStr}><FontAwesomeIcon icon={["far", "list-alt"]} />&nbsp;{labelText ? " " + labelText : undefined}</span>;
  return (
    <Dropdown
      onToggle={handleSelectedToggle} show={isOpen}>
      <Dropdown.Toggle id="userQueriesDropDown" className="sf-userquery-dropdown" variant={currentUserQuery ? "info" : "light"} >
        {label}
      </Dropdown.Toggle>
      <Dropdown.Menu>
        {userQueries && userQueries.length > 10 &&
          <div>
            <AutoFocus disabled={!isOpen}>
              <input
                type="text"
                className="form-control form-control-sm"
                value={filter}
                placeholder={SearchMessage.Search.niceToString()}
                onKeyDown={handleSearchKeyDown}
                onChange={e => setFilter(e.currentTarget.value)} />
            </AutoFocus>
            <Dropdown.Divider />
          </div>}
        <div id="userquery-items-container" style={{ maxHeight: "300px", overflowX: "auto" }}>
          {userQueries?.map((uq, i) => {
            if (filter == undefined || uq.toStr?.search(new RegExp(RegExp.escape(filter), "i")) != -1)
              return (
                <Dropdown.Item key={i}
                  className={classes("sf-userquery", is(uq, currentUserQuery) && "active")}
                  onClick={() => handleOnClick(uq)}>
                  {uq.toStr}
                </Dropdown.Item>
              );
          })}
        </div>
        {userQueries && userQueries.length > 0 && <Dropdown.Divider />}
        <Dropdown.Item onClick={handleBackToDefault} ><FontAwesomeIcon icon={["fas", "undo"]} className="mr-2" />{UserQueryMessage.UserQueries_BackToDefault.niceToString()}</Dropdown.Item>
        {currentUserQuery && canSave && <Dropdown.Item onClick={handleEdit} ><FontAwesomeIcon icon={["fas", "edit"]} className="mr-2" />{UserQueryMessage.UserQueries_Edit.niceToString()}</Dropdown.Item>}
        {canSave && <Dropdown.Item onClick={() => { createUserQuery().done() }}><FontAwesomeIcon icon={["fas", "plus"]} className="mr-2" />{UserQueryMessage.UserQueries_CreateNew.niceToString()}</Dropdown.Item>}      </Dropdown.Menu>
    </Dropdown>
  );

  function handleSearchKeyDown(e: React.KeyboardEvent<any>) {

    if (!e.shiftKey && e.keyCode == KeyCodes.down) {

      e.preventDefault();
      const div = document.getElementById("userquery-items-container")!;
      var item = Array.from(div.querySelectorAll("a.dropdown-item")).firstOrNull();
      if (item)
        (item as HTMLAnchorElement).focus();
    }
  }
}

function anyPinned(filterOptions?: FilterOptionParsed[]): boolean {
  if (filterOptions == null)
    return false;

  return filterOptions.some(a => Boolean(a.pinned) || isFilterGroupOptionParsed(a) && anyPinned(a.filters));
}
