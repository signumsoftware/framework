import * as React from 'react'
import { useLocation, Location } from 'react-router'
import { DateTime } from 'luxon'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes, Dic, softCast } from '@framework/Globals'
import { Finder } from '@framework/Finder'
import { parseLite, is, Lite, toLite, newMListElement, liteKey, SearchMessage, MList, MListElement, getToString, Entity, toMList, translated } from '@framework/Signum.Entities'
import * as AppContext from '@framework/AppContext'
import { Navigator } from '@framework/Navigator'
import { SystemTimeEmbedded, UserQueryEntity, UserQueryLiteModel, UserQueryMessage, UserQueryOperation, UserQueryPermission } from './Signum.UserQueries'
import { UserQueryClient } from './UserQueryClient'
import { UserAssetClient } from '../Signum.UserAssets/UserAssetClient'
import { Dropdown } from 'react-bootstrap';
import { getQueryKey } from '@framework/Reflection';
import { Operations } from '@framework/Operations';
import { FilterOption, FilterOptionParsed } from '@framework/Search'
import { FindOptionsParsed, isFilterCondition, isFilterGroup, PinnedFilter, SubTokensOptions } from '@framework/FindOptions'
import { QueryString } from '@framework/QueryString'
import { AutoFocus } from '@framework/Components/AutoFocus'
import { KeyNames } from '@framework/Components'
import type StringDistance from './StringDistance'
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded'
import { useForceUpdate } from '@framework/Hooks'
import { PinnedQueryFilterEmbedded, QueryColumnEmbedded, QueryFilterEmbedded, QueryOrderEmbedded, QueryTokenEmbedded } from '../Signum.UserAssets/Signum.UserAssets.Queries'
import FramePage from '@framework/Frames/FramePage'
import { AuthAdminClient } from '../Signum.Authorization/AuthAdminClient'

export interface UserQueryMenuProps {
  searchControl: SearchControlLoaded;
  isHidden: boolean;
}

export default function UserQueryMenu(p: UserQueryMenuProps): React.JSX.Element | null {

  const [filter, setFilter] = React.useState<string>();
  const [isOpen, setIsOpen] = React.useState<boolean>(false);
  const [currentUserQuery, setCurrentUserQueryInternal] = React.useState<Lite<UserQueryEntity> | undefined>();
  const [currentEntity, setCurrentEntityInternal] = React.useState<Lite<Entity> | undefined>();
  const [userQueries, setUserQueries] = React.useState<Lite<UserQueryEntity>[] | undefined>(undefined);
  const forceUpdate = useForceUpdate();
  const location = useLocation();


  function setCurrentEntity(entity: Lite<Entity> | undefined) {
    setCurrentEntityInternal(entity);
    p.searchControl.extraUrlParams.entity = entity && liteKey(entity);
  }

  function setCurrentUserQuery(uq: Lite<UserQueryEntity> | undefined, subTitle: string | undefined) {
    p.searchControl.extraUrlParams.userQuery = uq && liteKey(uq);

    p.searchControl.getCurrentUserQuery = () => uq;
    setCurrentUserQueryInternal(uq);

    p.searchControl.pageSubTitle = subTitle;
    p.searchControl.props.onPageTitleChanged?.();
    if (uq != null && subTitle == null)
      UserQueryClient.API.translated(uq).then(model => {
        uq.model = model;
        if (p.searchControl.getCurrentUserQuery?.() == uq) {
          p.searchControl.pageSubTitle = model.displayName;
          p.searchControl.props.onPageTitleChanged?.();
        }
        forceUpdate();
      });
  }
  
  React.useEffect(() => {
    const query = p.searchControl.props.tag == "SearchPage" ? QueryString.parse(location.search) : null;

    function tryParseLite(key: string | null | undefined) {
      return key && parseLite(key);
    }

    const uq = query ? tryParseLite(query["userQuery"]) : p.searchControl.props.extraOptions?.userQuery;
    if (uq && UserQueryEntity.isLite(uq)) {
      if (!is(p.searchControl.getCurrentUserQuery?.(), uq) || !p.searchControl.pageSubTitle)
        setCurrentUserQuery(uq, undefined);
    }
    else
      setCurrentUserQuery(undefined, undefined);

    const entity = query ? tryParseLite(query["entity"]) : p.searchControl.props.extraOptions?.entity;
    p.searchControl.extraUrlParams.entity = entity && liteKey(entity);
    setCurrentEntity(entity);
  }, [location,
    p.searchControl.props.extraOptions?.userQuery && liteKey(p.searchControl.props.extraOptions.userQuery),
    p.searchControl.props.extraOptions?.entity && liteKey(p.searchControl.props.extraOptions.entity)
  ]);


  if (p.isHidden)
    return null;

  function handleSelectedToggle(isOpen: boolean) {
    if (isOpen && userQueries == undefined)
      reloadList();

    setIsOpen(isOpen);
  }

  function reloadList(): Promise<Lite<UserQueryEntity>[]> {
    return UserQueryClient.API.forQuery(p.searchControl.props.findOptions.queryKey)
      .then(list => {
        setUserQueries(list);
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
          sc.handleChangeFiltermode('Simple');
        sc.setState({ refreshMode: sc.props.defaultRefreshMode });
        setCurrentUserQuery(undefined, undefined);
        setCurrentEntity(undefined);
        if (ofo.pagination.mode != "All") {
          sc.doSearchPage1();
        }
      });
  }


  function applyUserQueryToSearchControl(uq: Lite<UserQueryEntity>) {
    Navigator.API.fetch(uq).then(userQuery => {
      const sc = p.searchControl;
      const oldFindOptions = sc.props.findOptions;
      UserQueryClient.Converter.applyUserQuery(oldFindOptions, userQuery, currentEntity ?? sc.props.extraOptions?.entity, sc.props.defaultIncudeDefaultFilters)
        .then(nfo => {
          sc.setState({ refreshMode: userQuery.refreshMode });
          sc.handleChangeFiltermode(nfo.filterOptions.length == 0 || anyPinned(nfo.filterOptions) ? 'Simple' : "Advanced", false, true);
          setCurrentUserQuery(uq, translated(userQuery, a => a.displayName));
          //setCurrentEntity(undefined);
          if (sc.props.findOptions.pagination.mode != "All") {
            sc.doSearchPage1();
          }
        });
    })
  }

  function handleSelectUserQuery(uq: Lite<UserQueryEntity>) {
    applyUserQueryToSearchControl(uq);
  }

  async function handleEdit() {
    const userQuery = await Navigator.API.fetch(currentUserQuery!);
    await Navigator.view(userQuery);

    await reloadList();
    if (currentUserQuery && await Navigator.API.exists(currentUserQuery))
      applyUserQueryToSearchControl(currentUserQuery!);
    else {
      setCurrentUserQuery(undefined, undefined);
      setCurrentEntity(undefined);
    }
  }

  async function applyChangesToUserQuery(): Promise<UserQueryEntity> {
    const sc = p.searchControl;

    const uqOld = await Navigator.API.fetch(currentUserQuery!);
    const foOld = await UserQueryClient.Converter.toFindOptions(uqOld, currentEntity)

    const uqNew = await createUserQuery();
    const foNew = Finder.toFindOptions(sc.props.findOptions, sc.props.queryDescription, sc.props.defaultIncudeDefaultFilters);

    const sd = await import("./StringDistance").then(mod => new mod.default());

    uqOld.groupResults = uqNew.groupResults;
    uqOld.includeDefaultFilters = uqNew.includeDefaultFilters;
    uqOld.filters = UserQueryMerger.mergeFilters(uqOld.filters, uqNew.filters, foOld.filterOptions?.notNull() ?? [], foNew.filterOptions?.notNull() ?? [], 0, sd);
    uqOld.columns = UserQueryMerger.mergeColumns(uqOld.columns, uqNew.columns, sd);
    uqOld.columnsMode = uqNew.columnsMode;
    uqOld.orders = uqNew.orders;
    uqOld.paginationMode = uqNew.paginationMode;
    uqOld.systemTime = uqNew.systemTime == null ? null : SystemTimeEmbedded.New({
      mode: uqNew.systemTime.mode,
      joinMode: uqNew.systemTime.joinMode,
      startDate: uqNew.systemTime.startDate && UserQueryMerger.similarValues(foOld.systemTime!.startDate, foNew.systemTime!.startDate) ? uqOld.systemTime!.startDate : uqNew.systemTime.startDate,
      endDate: uqNew.systemTime.endDate && UserQueryMerger.similarValues(foOld.systemTime!.endDate, foNew.systemTime!.endDate) ? uqOld.systemTime!.endDate : uqNew.systemTime.endDate,
      timeSeriesStep: uqNew.systemTime.timeSeriesStep ?? null,
      timeSeriesUnit: uqNew.systemTime.timeSeriesUnit ?? null,
      timeSeriesMaxRowsPerStep: uqNew.systemTime.timeSeriesMaxRowsPerStep ?? null,
      splitQueries: uqNew.systemTime.splitQueries ?? false,
    });
    uqOld.elementsPerPage = uqNew.elementsPerPage;
    uqOld.customDrilldowns = uqNew.customDrilldowns;
    uqOld.modified = true;

    return uqOld;
  }

  async function handleApplyChanges() {
    const uqOld = await applyChangesToUserQuery();
    await Navigator.view(uqOld);
    const list = await reloadList();

    if (currentUserQuery && await Navigator.API.exists(currentUserQuery))
      applyUserQueryToSearchControl(currentUserQuery!);
    else {
      setCurrentUserQuery(undefined, undefined);
      setCurrentEntity(undefined);
    }
  }

  async function createUserQuery(): Promise<UserQueryEntity> {

    const sc = p.searchControl;

    const fo = Finder.toFindOptions(sc.props.findOptions, sc.props.queryDescription, sc.props.defaultIncudeDefaultFilters);

    const qfs = await UserAssetClient.API.stringifyFilters({
      canAggregate: fo.groupResults || false,
      canTimeSeries: fo.systemTime?.mode == 'TimeSeries',
      queryKey: getQueryKey(fo.queryName),
      filters: (fo.filterOptions ?? []).notNull().map(fo => UserAssetClient.Converter.toFilterNode(fo))
    });



    var parser = new Finder.TokenCompleter(sc.props.queryDescription);
    [
      ...fo.columnOptions?.map(a => a?.token) ?? [],
      ...fo.columnOptions?.map(a => a?.summaryToken) ?? [],
      ...fo.orderOptions?.map(a => a?.token) ?? [],
    ].notNull().forEach(a => parser.request(a.toString()));

    await parser.finished();

    const qe = await Finder.API.fetchQueryEntity(getQueryKey(fo.queryName));

    var stoColumn = (fo.groupResults ? SubTokensOptions.CanAggregate : 0) | SubTokensOptions.CanElement | SubTokensOptions.CanOperation | SubTokensOptions.CanSnippet | SubTokensOptions.CanToArray | SubTokensOptions.CanManual;
    var stoOrder = (fo.groupResults ? SubTokensOptions.CanAggregate : 0) | SubTokensOptions.CanElement | SubTokensOptions.CanSnippet;
    var stoSummary = SubTokensOptions.CanAggregate | SubTokensOptions.CanElement;

    
    return UserQueryEntity.New({
      query: qe,
      owner: UserQueryEntity.typeInfo().minTypeAllowed != "None" ? null :  AppContext.currentUser && toLite(AppContext.currentUser),
      groupResults: fo.groupResults,
      filters: qfs.map(f => newMListElement(UserAssetClient.Converter.toQueryFilterEmbedded(f))),
      includeDefaultFilters: fo.includeDefaultFilters,
      columns: (fo.columnOptions ?? []).notNull().map(c => newMListElement(QueryColumnEmbedded.New({
        token: QueryTokenEmbedded.New({ tokenString: c.token.toString(), token: parser.get(c.token.toString(), stoColumn) }),
        displayName: typeof c.displayName == "function" ? c.displayName() : c.displayName,
        summaryToken: c.summaryToken ? QueryTokenEmbedded.New({ tokenString: c.summaryToken.toString(), token: parser.get(c.summaryToken.toString(), stoSummary) }) : null,
        hiddenColumn: c.hiddenColumn,
        combineRows: c.combineRows ?? null,
      }))),
      columnsMode: fo.columnOptionsMode,
      orders: (fo.orderOptions ?? []).notNull().map(c => newMListElement(QueryOrderEmbedded.New({
        orderType: c.orderType,
        token: QueryTokenEmbedded.New({
          tokenString: c.token.toString(),
          token: parser.get(c.token.toString(), stoOrder)
        })
      }))),
      systemTime: fo.systemTime && SystemTimeEmbedded.New({
        mode: fo.systemTime.mode,
        startDate: fo.systemTime.startDate && await UserAssetClient.API.stringifyDate(fo.systemTime.startDate),
        endDate: fo.systemTime.endDate && await UserAssetClient.API.stringifyDate(fo.systemTime.endDate),
        joinMode: fo.systemTime.joinMode,
        timeSeriesStep: fo.systemTime.timeSeriesStep ?? null,
        timeSeriesUnit: fo.systemTime.timeSeriesUnit ?? null,
        timeSeriesMaxRowsPerStep: fo.systemTime.timeSeriesMaxRowsPerStep ?? null,
        splitQueries: fo.systemTime.splitQueries ?? false,
      }),
      paginationMode: fo.pagination && fo.pagination.mode,
      elementsPerPage: fo.pagination && fo.pagination.elementsPerPage,
      refreshMode: p.searchControl.state.refreshMode ?? "Auto",
    });
  }

  function handleCreateUserQuery() {

    createUserQuery()
      .then(uq => Navigator.view(uq))
      .then(uq => {
        if (uq?.id) {
          reloadList().then(() => {
            applyUserQueryToSearchControl(toLite(uq));
          });
        }
      });
  }

  const currentUserQueryToStr = currentUserQuery ? getToString(currentUserQuery) : undefined;

  var canSave = UserQueryEntity.tryOperationInfo(UserQueryOperation.Save) != null;

  const label = (
    <span title={currentUserQueryToStr}>
      <FontAwesomeIcon icon={ "rectangle-list"} />
      {p.searchControl.props.largeToolbarButtons == true && <>
        &nbsp;
        <span className="d-none d-sm-inline">
          {currentUserQueryToStr ? <strong>{currentUserQueryToStr.etc(50)}</strong> : UserQueryEntity.nicePluralName()}
        </span>
      </>
      }
    </span>
  );
  return (
    <Dropdown
      title={[UserQueryEntity.nicePluralName(), currentUserQueryToStr].notNull().join(" - ")}
      onToggle={handleSelectedToggle} show={isOpen}>
      <Dropdown.Toggle id="userQueriesDropDown" variant="tertiary" >
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
            if (filter == undefined || getToString(uq)?.search(new RegExp(RegExp.escape(filter), "i")) != -1)
              return (
                <Dropdown.Item key={i}
                  className={classes("sf-userquery", is(uq, currentUserQuery) && "active")}
                  onClick={() => handleSelectUserQuery(uq)}>
                  {getToString(uq)}
                </Dropdown.Item>
              );
          })}
        </div>
        {userQueries && userQueries.length > 0 && <Dropdown.Divider />}
        {p.searchControl.props.allowChangeColumns && <Dropdown.Item onClick={handleBackToDefault} > <FontAwesomeIcon aria-hidden={true} icon={"arrow-rotate-left"} className="me-2" />{UserQueryMessage.BackToDefault.niceToString()}</Dropdown.Item>}
        {currentUserQuery && canSave && <Dropdown.Item onClick={handleApplyChanges} ><FontAwesomeIcon aria-hidden={true} icon={"share-from-square"} className="me-2" />{UserQueryMessage.ApplyChanges.niceToString()}</Dropdown.Item>}
        {currentUserQuery && canSave && <Dropdown.Item onClick={handleEdit} ><FontAwesomeIcon aria-hidden={true} icon={"pen-to-square"} className="me-2" />{UserQueryMessage.Edit.niceToString()}</Dropdown.Item>}
        {canSave && <Dropdown.Item onClick={handleCreateUserQuery}><FontAwesomeIcon aria-hidden={true} icon={"plus"} className="me-2" />{UserQueryMessage.CreateNew.niceToString()}</Dropdown.Item>}</Dropdown.Menu>
    </Dropdown>
  );

  function handleSearchKeyDown(e: React.KeyboardEvent<any>) {

    if (!e.shiftKey && e.key == KeyNames.arrowDown) {

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

  return filterOptions.some(a => Boolean(a.pinned) || isFilterGroup(a) && anyPinned(a.filters));
}


export interface FilterPair {
  key: MListElement<QueryFilterEmbedded>;
  elements: MListElement<QueryFilterEmbedded>[];
  filter: FilterOption
}

export namespace UserQueryMerger {
  export function mergeColumns(oldUqColumns: MList<QueryColumnEmbedded>, newUqColumns: MList<QueryColumnEmbedded>, sd: StringDistance): MListElement<QueryColumnEmbedded>[] {
    const choices = sd.levenshteinChoices(oldUqColumns, newUqColumns, c => c.added == null ? 5 : c.removed == null ? 5 : distanceColumns(c.added.element, c.removed.element));

    return choices.flatMap(ch => {
      if (ch.added == null)
        return [];

      if (ch.removed == null)
        return [ch.added];


      const oldCol = ch.removed.element;
      const newCol = ch.added.element;

      oldCol.token = newCol.token;
      oldCol.displayName = (newCol.displayName == translated(oldCol, a => a.displayName) ? oldCol.displayName : newCol.displayName) ?? null;
      oldCol.summaryToken = newCol.summaryToken;
      oldCol.combineRows = newCol.combineRows;
      oldCol.hiddenColumn = newCol.hiddenColumn;
      oldCol.modified = true;
      //preserve rowId
      return [ch.removed];
    });
  }

  export function mergeFilters(oldUqFilters: MList<QueryFilterEmbedded>, newUqFilters: MList<QueryFilterEmbedded>,
    oldFilterOptions: FilterOption[], newFilterOptions: FilterOption[],
    identation: number, sd: StringDistance): MList<QueryFilterEmbedded> {

    const oldGroups = oldUqFilters.groupWhen(a => a.element.indentation == identation);
    const newGroups = newUqFilters.groupWhen(a => a.element.indentation == identation);

    if (oldGroups.length != oldFilterOptions.length || newGroups.length != newFilterOptions.length)
      throw Error("Unexpected filter lengths");

    const oldPairs = oldGroups.map((gr, i) => softCast<FilterPair>({ key: gr.key, elements: gr.elements, filter: oldFilterOptions[i] }));
    const newPairs = newGroups.map((gr, i) => softCast<FilterPair>({ key: gr.key, elements: gr.elements, filter: newFilterOptions[i] }));

    const choices = sd.levenshteinChoices(oldPairs, newPairs, c => c.added == null ? 5 : c.removed == null ? 5 : distanceFilter(c.added.filter, c.removed.filter));

    const result = choices.flatMap(ch => {
      if (ch.added == null)
        return [];

      if (ch.removed == null)
        return [ch.added.key, ...ch.added.elements];

      const merged = mergeFilters(
        ch.removed.elements,
        ch.added.elements,
        isFilterGroup(ch.removed.filter) ? ch.removed.filter.filters.notNull() : [],
        isFilterGroup(ch.added.filter) ? ch.added.filter.filters.notNull() : [], identation + 1, sd);


      const oldF = ch.removed.key.element;
      const newF = ch.added.key.element;

      oldF.token = newF.token;
      oldF.isGroup = newF.isGroup;
      oldF.groupOperation = newF.groupOperation;
      oldF.operation = newF.operation;
      oldF.valueString = similarValues(ch.added.filter.value, ch.removed.filter.value) || oldF.valueString?.startsWith("[") && oldF.valueString.endsWith("]") ? oldF.valueString : newF.valueString;
      if (newF.pinned == null)
        oldF.pinned = null;
      else {
        oldF.pinned ??= PinnedQueryFilterEmbedded.New();
        oldF.pinned.label = newF.pinned.label == translated(oldF.pinned, a => a.label) ? oldF.pinned.label : newF.pinned.label;
        oldF.pinned.column = newF.pinned.column;
        oldF.pinned.row = newF.pinned.row;
        oldF.pinned.active = newF.pinned.active;
        oldF.pinned.splitValue = newF.pinned.splitValue;
        oldF.pinned.modified = true;
      }

      oldF.modified = true;

      //preserve rowId
      return [ch.removed.key, ...merged];
    });

    return result;
  }

  export function similarValues(val1: any, val2: any): boolean {
    if (val1 == val2)
      return true;

    var dt1 = DateTime.fromISO(val1);
    var dt2 = DateTime.fromISO(val2);

    if (dt1.isValid && dt2.isValid && Math.abs(dt1.diff(dt2, "hour").hours) < 2)
      return true;

    return false;
  }

  function distanceColumns(qc1: QueryColumnEmbedded, qc2: QueryColumnEmbedded): number {
    return (qc1.token?.tokenString == qc2.token?.tokenString ? 0 : 3) +
      (qc1.summaryToken?.tokenString == qc2.summaryToken?.tokenString ? 0 : 1) +
      (qc1.combineRows == qc2.combineRows ? 0 : 1) +
      (qc1.displayName == qc2.displayName ? 0 : 1) +
      (qc1.hiddenColumn == qc2.hiddenColumn ? 0 : 1);
  }

  function distanceFilter(fo: FilterOption, fo2: FilterOption): number {
    if (isFilterGroup(fo)) {
      if (isFilterGroup(fo2)) {
        return (fo.token?.toString() == fo2.token?.toString() ? 0 : 1) +
          (fo.groupOperation == fo2.groupOperation ? 0 : 1) +
          (similarValues(fo.value, fo2.value) ? 0 : 1) +
          distancePinned(fo.pinned, fo2.pinned) +
          Array.range(0, Math.max(fo.filters.length, fo2.filters.length)).sum(i => fo.filters[i] == null ? 5 : fo2.filters[i] == null ? 5 : distanceFilter(fo.filters[i]!, fo2.filters[i]!));
      }
      else return 10;
    }
    else {
      if (isFilterGroup(fo2))
        return 10;
      else
        return (fo.token?.toString() == fo2.token?.toString() ? 0 : 1) +
          (fo.operation == fo2.operation ? 0 : 1) +
          (fo.value == fo2.value ? 0 : 1) +
          distancePinned(fo.pinned, fo2.pinned);
    }
  }

  function distancePinned(pin: PinnedFilter | undefined, pin2: PinnedFilter | undefined) {
    if (pin == null && pin2 == null)
      return 0;

    if (pin == null || pin2 == null)
      return 4;

    return (pin.active == pin2.active ? 0 : 1) +
      (pin.column == pin2.column ? 0 : 1) +
      (pin.row == pin2.row ? 0 : 1) +
      (pin.label == pin2.label ? 0 : 1) +
      (pin.splitValue == pin2.splitValue ? 0 : 1);
  }


}
