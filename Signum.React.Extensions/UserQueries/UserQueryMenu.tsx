import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import { parseLite, is, Lite, toLite, newMListElement, toMList, liteKey } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded'
import { UserQueryEntity, UserQueryMessage, QueryColumnEmbedded, QueryOrderEmbedded, UserQueryOperation } from './Signum.Entities.UserQueries'
import * as UserQueryClient from './UserQueryClient'
import * as UserAssetClient from '../UserAssets/UserAssetClient'
import { QueryTokenEmbedded } from '../UserAssets/Signum.Entities.UserAssets';
import { Dropdown, DropdownButton } from 'react-bootstrap';
import { getQueryKey, Type } from '@framework/Reflection';
import * as Operations from '@framework/Operations';
import { useAPI } from '../../../Framework/Signum.React/Scripts/Hooks'
import { FilterOptionParsed } from '../../../Framework/Signum.React/Scripts/Search'
import { isFilterGroupOptionParsed } from '@framework/FindOptions'

export interface UserQueryMenuProps {
  searchControl: SearchControlLoaded;
}

interface UserQueryMenuState {
  currentUserQuery?: Lite<UserQueryEntity>;
  userQueries?: Lite<UserQueryEntity>[];
  isOpen: boolean;
}

export default function UserQueryMenu(p: UserQueryMenuProps) {

  const [isOpen, setIsOpen] = React.useState<boolean>(false);
  const [currentUserQuery, setCurrentUserQuery] = React.useState<Lite<UserQueryEntity> | undefined>(() => {
    let uq = window.location.search.tryAfter("userQuery=");
    uq = uq && decodeURIComponent(uq.tryBefore("&") || uq);
    return uq ? parseLite(uq) as Lite<UserQueryEntity> : undefined;
  });

  const [userQueries, setUserQueries] = React.useState<Lite<UserQueryEntity>[] | undefined>(undefined);

  React.useEffect(() => {
    const userQuery = window.location.search.tryAfter("userQuery=");
    if (userQuery) {
      const uq = parseLite(decodeURIComponent(userQuery.tryBefore("&") ?? userQuery)) as Lite<UserQueryEntity>;
      setCurrentUserQuery(uq);
      Navigator.API.fillToStrings(uq)
        .done();
    }
  }, []);

  const oldExtraParams = React.useRef(p.searchControl.extraParams);
  React.useEffect(() => {
    oldExtraParams.current = p.searchControl.extraParams;
    p.searchControl.extraParams = () => ({ ...oldExtraParams.current(), userQuery: currentUserQuery && liteKey(currentUserQuery) });
    return () => { p.searchControl.extraParams = oldExtraParams.current };
  }, []);

  function handleSelectedToggle(isOpen: boolean) {
    if (isOpen && userQueries == undefined)
      reloadList().done();

    setIsOpen(isOpen);
  }

  function reloadList(): Promise<void> {
    return UserQueryClient.API.forQuery(p.searchControl.props.findOptions.queryKey)
      .then(list => setUserQueries(list));
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
        sc.setState({ showFilters: !(nfo.filterOptions.length == 0 || anyPinned(nfo.filterOptions)) });
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
          sc.setState({ showFilters: !(nfo.filterOptions.length == 0 || anyPinned(nfo.filterOptions)) });
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
      .then(userQuery => Navigator.navigate(userQuery))
      .then(() => reloadList())
      .then(() => applyUserQuery(currentUserQuery!))
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

    const qe = await Finder.API.fetchQueryEntity(getQueryKey(fo.queryName));

    const uq = await Navigator.view(UserQueryEntity.New({
      query: qe,
      owner: Navigator.currentUser && toLite(Navigator.currentUser),
      groupResults: fo.groupResults,
      filters: qfs.map(f => newMListElement(UserAssetClient.Converter.toQueryFilterEmbedded(f))),
      includeDefaultFilters: fo.includeDefaultFilters,
      columns: (fo.columnOptions ?? []).map(c => newMListElement(QueryColumnEmbedded.New({
        token: QueryTokenEmbedded.New({ tokenString: c.token.toString() }),
        displayName: c.displayName
      }))),
      columnsMode: fo.columnOptionsMode,
      orders: (fo.orderOptions ?? []).map(c => newMListElement(QueryOrderEmbedded.New({
        orderType: c.orderType,
        token: QueryTokenEmbedded.New({ tokenString: c.token.toString() })
      }))),
      paginationMode: fo.pagination && fo.pagination.mode,
      elementsPerPage: fo.pagination && fo.pagination.elementsPerPage
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

    const label = <span title={currentUserQueryToStr}><FontAwesomeIcon icon={["far", "list-alt"]} />&nbsp;{labelText ? " " + labelText : undefined}</span>;
    return (
    <Dropdown
      onToggle={handleSelectedToggle} show={isOpen}>
      <Dropdown.Toggle id="userQueriesDropDown" className="sf-userquery-dropdown" variant="light" >
        {label}
      </Dropdown.Toggle>
      <Dropdown.Menu>
          {
            userQueries?.map((uq, i) =>
            <Dropdown.Item key={i}
              className={classes("sf-userquery", is(uq, currentUserQuery) && "active")}
              onClick={() => handleOnClick(uq)}>
                {uq.toStr}
            </Dropdown.Item>)
          }
          {userQueries && userQueries.length > 0 && <Dropdown.Divider />}
        <Dropdown.Item onClick={handleBackToDefault} ><FontAwesomeIcon icon={["fas", "undo"]} className="mr-2" />{UserQueryMessage.UserQueries_BackToDefault.niceToString()}</Dropdown.Item>
        {currentUserQuery && <Dropdown.Item onClick={handleEdit} ><FontAwesomeIcon icon={["fas", "edit"]} className="mr-2" />{UserQueryMessage.UserQueries_Edit.niceToString()}</Dropdown.Item>}
        {Operations.isOperationAllowed(UserQueryOperation.Save, UserQueryEntity) && <Dropdown.Item onClick={() => { createUserQuery().done() }}><FontAwesomeIcon icon={["fas", "plus"]} className="mr-2" />{UserQueryMessage.UserQueries_CreateNew.niceToString()}</Dropdown.Item>}
      </Dropdown.Menu>
      </Dropdown>
    );
}

function anyPinned(filterOptions?: FilterOptionParsed[]): boolean {
  if (filterOptions == null)
    return false;

  return filterOptions.some(a => Boolean(a.pinned) || isFilterGroupOptionParsed(a) && anyPinned(a.filters));
}



