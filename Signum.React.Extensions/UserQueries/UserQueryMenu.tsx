import * as React from 'react'
import { DateTime } from 'luxon'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes, softCast } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import { parseLite, is, Lite, toLite, newMListElement, liteKey, SearchMessage, MList, MListElement, getToString, Entity, toMList } from '@framework/Signum.Entities'
import * as AppContext from '@framework/AppContext'
import * as Navigator from '@framework/Navigator'
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded'
import { UserQueryEntity, UserQueryMessage, QueryColumnEmbedded, QueryOrderEmbedded, UserQueryOperation, QueryFilterEmbedded, PinnedQueryFilterEmbedded } from './Signum.Entities.UserQueries'
import * as UserQueryClient from './UserQueryClient'
import * as UserAssetClient from '../UserAssets/UserAssetClient'
import { QueryTokenEmbedded } from '../UserAssets/Signum.Entities.UserAssets';
import { Dropdown } from 'react-bootstrap';
import { getQueryKey } from '@framework/Reflection';
import * as Operations from '@framework/Operations';
import { FilterOption, FilterOptionParsed } from '@framework/Search'
import { isFilterGroupOption, isFilterGroupOptionParsed, PinnedFilter } from '@framework/FindOptions'
import { QueryString } from '@framework/QueryString'
import { AutoFocus } from '@framework/Components/AutoFocus'
import { KeyCodes } from '@framework/Components'
import type StringDistance from './StringDistance'
import { translated } from '../Translation/TranslatedInstanceTools'

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
  const [currentCustomDrilldowns, setCurrentCustomDrilldownsInternal] = React.useState<MList<Lite<Entity>> | undefined>();
  const [currentUserQuery, setCurrentUserQueryInternal] = React.useState<Lite<UserQueryEntity> | undefined>(() => {
    let uq = p.searchControl.props.tag == "SearchPage" ? decodeUserQueryFromUrl() : p.searchControl.props.extraOptions?.userQuery;
    return uq;
  });

  function setCurrentUserQuery(uq: Lite<UserQueryEntity> | undefined) {
    p.searchControl.extraUrlParams.userQuery = uq && liteKey(uq);
    p.searchControl.pageSubTitle = getToString(uq);
    setCurrentUserQueryInternal(uq);
    p.searchControl.props.onPageTitleChanged?.();
  }

  function setCurrentCustomDrilldowns(value: MList<Lite<Entity>> | undefined) {
    p.searchControl.customDrilldowns = value?.map(mle => mle.element) ?? [];
    UserAssetClient.Encoder.encodeCustomDrilldowns(p.searchControl.extraUrlParams, value);
    setCurrentCustomDrilldownsInternal(value);
  }

  const [userQueries, setUserQueries] = React.useState<Lite<UserQueryEntity>[] | undefined>(undefined);
  
  React.useEffect(() => {
    p.searchControl.extraUrlParams.userQuery = currentUserQuery && liteKey(currentUserQuery);

    const cds = p.searchControl.props.tag == "SearchPage" ?
      UserAssetClient.Decoder.decodeCustomDrilldowns(QueryString.parse(window.location.search)) :
      p.searchControl.props.extraOptions?.customDrilldowns as (MList<Lite<Entity>> | undefined);

    setCurrentCustomDrilldowns(cds);
  }, []);

  React.useEffect(() => {
    if (currentUserQuery)
      reloadList();
  }, []);

  function handleSelectedToggle(isOpen: boolean) {
    if (isOpen && userQueries == undefined)
      reloadList();

    setIsOpen(isOpen);
  }

  function reloadList(): Promise<Lite<UserQueryEntity>[]> {
    return UserQueryClient.API.forQuery(p.searchControl.props.findOptions.queryKey)
      .then(list => {
        setUserQueries(list);
        if (currentUserQuery && currentUserQuery.model == null) {
          const similar = list.firstOrNull(l => is(l, currentUserQuery));
          if (similar != null) {
            currentUserQuery.model = similar.model;
            setCurrentUserQuery(currentUserQuery);
          } else {
            Navigator.API.fillLiteModels(currentUserQuery)
              .then(() => setCurrentUserQuery(currentUserQuery));
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
        setCurrentCustomDrilldowns(undefined);
        if (ofo.pagination.mode != "All") {
          sc.doSearchPage1();
        }
      });
  }


  function applyUserQuery(uq: Lite<UserQueryEntity>) {
    Navigator.API.fetch(uq).then(userQuery => {
      const sc = p.searchControl
      const oldFindOptions = sc.props.findOptions;
      UserQueryClient.Converter.applyUserQuery(oldFindOptions, userQuery, undefined, sc.props.defaultIncudeDefaultFilters)
        .then(nfo => {
          sc.setState({ refreshMode: userQuery.refreshMode });
          if (nfo.filterOptions.length == 0 || anyPinned(nfo.filterOptions))
            sc.setState({ showFilters: false, simpleFilterBuilder: undefined });
          setCurrentUserQuery(uq);
          setCurrentCustomDrilldowns(userQuery.customDrilldowns);
          if (sc.props.findOptions.pagination.mode != "All") {
            sc.doSearchPage1();
          }
        });
    })
  }

  function handleOnClick(uq: Lite<UserQueryEntity>) {
    applyUserQuery(uq);
  }

  function handleEdit() {
    Navigator.API.fetch(currentUserQuery!)
      .then(userQuery => Navigator.view(userQuery))
      .then(() => reloadList())
      .then(list => {
        if (!list.some(a => is(a, currentUserQuery))) {
          setCurrentUserQuery(undefined);
          setCurrentCustomDrilldowns(undefined);
        }
        else
          applyUserQuery(currentUserQuery!)
      });
  }

  async function applyChanges(): Promise<UserQueryEntity> {
    const sc = p.searchControl;

    const uqOld = await Navigator.API.fetch(currentUserQuery!);
    const foOld = await UserQueryClient.Converter.toFindOptions(uqOld, undefined)

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
    uqOld.elementsPerPage = uqNew.elementsPerPage;
    uqOld.customDrilldowns = uqNew.customDrilldowns;
    uqOld.modified = true;

    return uqOld;
  }

  function handleApplyChanges() {
    applyChanges()
      .then(uqOld => Navigator.view(uqOld))
      .then(() => reloadList())
      .then(list => {
        if (!list.some(a => is(a, currentUserQuery))) {
          setCurrentUserQuery(undefined);
          setCurrentCustomDrilldowns(undefined);
        }
        else
          applyUserQuery(currentUserQuery!);
      });
  }

  async function createUserQuery(): Promise<UserQueryEntity> {

    const sc = p.searchControl;

    const fo = Finder.toFindOptions(sc.props.findOptions, sc.props.queryDescription, sc.props.defaultIncudeDefaultFilters);

    const qfs = await UserAssetClient.API.stringifyFilters({
      canAggregate: fo.groupResults || false,
      queryKey: getQueryKey(fo.queryName),
      filters: (fo.filterOptions ?? []).notNull().map(fo => UserAssetClient.Converter.toFilterNode(fo))
    });

    const parsedTokens =
      [
        ...sc.props.findOptions.columnOptions.map(a => a.token),
        ...sc.props.findOptions.columnOptions.map(a => a.summaryToken),
        ...sc.props.findOptions.orderOptions.map(a => a.token),
      ].notNull()
        .toObjectDistinct(a => a.fullKey);

    const qe = await Finder.API.fetchQueryEntity(getQueryKey(fo.queryName));

    return UserQueryEntity.New({
      query: qe,
      owner: AppContext.currentUser && toLite(AppContext.currentUser),
      groupResults: fo.groupResults,
      filters: qfs.map(f => newMListElement(UserAssetClient.Converter.toQueryFilterEmbedded(f))),
      includeDefaultFilters: fo.includeDefaultFilters,
      columns: (fo.columnOptions ?? []).notNull().map(c => newMListElement(QueryColumnEmbedded.New({
        token: QueryTokenEmbedded.New({ tokenString: c.token.toString(), token: parsedTokens[c.token.toString()] }),
        displayName: typeof c.displayName == "function" ? c.displayName() : c.displayName,
        summaryToken: c.summaryToken ? QueryTokenEmbedded.New({ tokenString: c.summaryToken.toString(), token: parsedTokens[c.summaryToken.toString()] }) : null,
        hiddenColumn: c.hiddenColumn
      }))),
      columnsMode: fo.columnOptionsMode,
      orders: (fo.orderOptions ?? []).notNull().map(c => newMListElement(QueryOrderEmbedded.New({
        orderType: c.orderType,
        token: QueryTokenEmbedded.New({
          tokenString: c.token.toString(),
          token: parsedTokens[c.token.toString()]
        })
      }))),
      paginationMode: fo.pagination && fo.pagination.mode,
      elementsPerPage: fo.pagination && fo.pagination.elementsPerPage,
      refreshMode: p.searchControl.state.refreshMode ?? "Auto",
      customDrilldowns: currentCustomDrilldowns ?? [],
    });
  }

  function handleCreateUserQuery() {

    createUserQuery()
      .then(uq => Navigator.view(uq))
      .then(uq => {
        if (uq?.id) {
          reloadList().then(() => {
            applyUserQuery(toLite(uq));
          });
        }
      });
  }

  const currentUserQueryToStr = currentUserQuery ? getToString(currentUserQuery) : undefined;

  var canSave = Operations.tryGetOperationInfo(UserQueryOperation.Save, UserQueryEntity) != null;

  const label = (
    <span title={currentUserQueryToStr}>
      <FontAwesomeIcon icon={["far", "rectangle-list"]} />
      {p.searchControl.props.largeToolbarButtons == true && <>
        &nbsp;
        <span className="d-none d-sm-inline">
          {UserQueryEntity.nicePluralName()}
          {currentUserQueryToStr && " - "}
          {currentUserQueryToStr && <strong>{currentUserQueryToStr.etc(50)}</strong>}
        </span>
        <span className="d-inline d-sm-none">
          {currentUserQueryToStr && <span>{currentUserQueryToStr.etc(20)}</span>}
        </span>
      </>
      }
    </span>
  );
  return (
    <Dropdown
      title={[UserQueryEntity.nicePluralName(), currentUserQueryToStr].notNull().join(" - ")}
      onToggle={handleSelectedToggle} show={isOpen}>
      <Dropdown.Toggle id="userQueriesDropDown" className={classes("sf-userquery-dropdown", currentUserQuery ? "border-info" : undefined)} variant={"light"} >
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
                  onClick={() => handleOnClick(uq)}>
                  {getToString(uq)}
                </Dropdown.Item>
              );
          })}
        </div>
        {userQueries && userQueries.length > 0 && <Dropdown.Divider />}
        <Dropdown.Item onClick={handleBackToDefault} ><FontAwesomeIcon icon={["fas", "arrow-rotate-left"]} className="me-2" />{UserQueryMessage.BackToDefault.niceToString()}</Dropdown.Item>
        {currentUserQuery && canSave && <Dropdown.Item onClick={handleApplyChanges} ><FontAwesomeIcon icon={["fas", "share-from-square"]} className="me-2" />{UserQueryMessage.ApplyChanges.niceToString()}</Dropdown.Item>}
        {currentUserQuery && canSave && <Dropdown.Item onClick={handleEdit} ><FontAwesomeIcon icon={["fas", "pen-to-square"]} className="me-2" />{UserQueryMessage.Edit.niceToString()}</Dropdown.Item>}
        {canSave && <Dropdown.Item onClick={handleCreateUserQuery}><FontAwesomeIcon icon={["fas", "plus"]} className="me-2" />{UserQueryMessage.CreateNew.niceToString()}</Dropdown.Item>}</Dropdown.Menu>
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


export interface FilterPair {
  key: MListElement<QueryFilterEmbedded>;
  elements: MListElement<QueryFilterEmbedded>[];
  filter: FilterOption
}

export namespace UserQueryMerger {
  export function mergeColumns(oldUqColumns: MList<QueryColumnEmbedded>, newUqColumns: MList<QueryColumnEmbedded>, sd: StringDistance) {
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
        isFilterGroupOption(ch.removed.filter) ? ch.removed.filter.filters.notNull() : [],
        isFilterGroupOption(ch.added.filter) ? ch.added.filter.filters.notNull() : [], identation + 1, sd);


      const oldF = ch.removed.key.element;
      const newF = ch.added.key.element;

      oldF.token = newF.token;
      oldF.isGroup = newF.isGroup;
      oldF.groupOperation = newF.groupOperation;
      oldF.operation = newF.operation;
      oldF.valueString = similarValues(ch.added.filter.value, ch.removed.filter.value) ? oldF.valueString : newF.valueString;
      if (newF.pinned == null)
        oldF.pinned = null;
      else {
        oldF.pinned ??= PinnedQueryFilterEmbedded.New();
        oldF.pinned.label = newF.pinned.label == translated(oldF.pinned, a => a.label) ? oldF.pinned.label : newF.pinned.label;
        oldF.pinned.column = newF.pinned.column;
        oldF.pinned.row = newF.pinned.row;
        oldF.pinned.active = newF.pinned.active;
        oldF.pinned.splitText = newF.pinned.splitText;
      }

      oldF.modified = true;

      //preserve rowId
      return [ch.removed.key, ...merged];
    });

    return result;
  }

  function similarValues(val1: any, val2: any) {
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
      (qc1.displayName == qc2.displayName ? 0 : 1) +
      (qc1.hiddenColumn == qc2.hiddenColumn ? 0 : 1);
  }

  function distanceFilter(fo: FilterOption, fo2: FilterOption): number {
    if (isFilterGroupOption(fo)) {
      if (isFilterGroupOption(fo2)) {
        return (fo.token?.toString() == fo2.token?.toString() ? 0 : 1) +
          (fo.groupOperation == fo2.groupOperation ? 0 : 1) +
          (similarValues(fo.value, fo2.value) ? 0 : 1) +
          distancePinned(fo.pinned, fo2.pinned) +
          Array.range(0, Math.max(fo.filters.length, fo2.filters.length)).sum(i => fo.filters[i] == null ? 5 : fo2.filters[i] == null ? 5 : distanceFilter(fo.filters[i]!, fo2.filters[i]!));
      }
      else return 10;
    }
    else {
      if (isFilterGroupOption(fo2))
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
      (pin.splitText == pin2.splitText ? 0 : 1);
  }


}
