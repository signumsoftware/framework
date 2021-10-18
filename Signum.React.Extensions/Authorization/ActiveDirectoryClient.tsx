import * as React from 'react'
import { ajaxPost, ajaxGet } from '@framework/Services';
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { UserEntity, UserADMessage, BasicPermission, ActiveDirectoryPermission, UserADQuery, ActiveDirectoryMessage, ADGroupEntity } from './Signum.Entities.Authorization'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import ValueLineModal from '@framework/ValueLineModal';
import { FindOptions, FindOptionsParsed, ResultRow } from '@framework/FindOptions';
import MessageModal from '@framework/Modals/MessageModal';
import { Lite, SearchMessage } from '@framework/Signum.Entities';
import SelectorModal from '@framework/SelectorModal';
import { QueryString } from '@framework/QueryString';
import { isPermissionAuthorized } from './AuthClient';
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded';

export function start(options: { routes: JSX.Element[] }) {
  Navigator.addSettings(new Navigator.EntitySettings(ADGroupEntity, e => import('./AzureAD/ADGroup'), { isCreable: "Never" }));

  Navigator.getSettings(UserEntity)!.autocompleteConstructor = (str, aac) => isPermissionAuthorized(ActiveDirectoryPermission.InviteUsersFromAD) ? ({
    type: UserEntity,
    customElement: <em><FontAwesomeIcon icon="address-book" />&nbsp;{UserADMessage.Find0InActiveDirectory.niceToString().formatHtml(<strong>{str}</strong>)}</em>,
    onClick: () => importADUser(str),
  }) : null;

  Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {
    if (ctx.findOptions.queryKey != UserEntity.typeName || !isPermissionAuthorized(ActiveDirectoryPermission.InviteUsersFromAD))
      return undefined;

    var search = getSearch(ctx.findOptions);

    return (
      {
        order: -1,
        button: <button className="btn btn-info ml-2"
          onClick={e => {
            e.preventDefault();
            var promise = ValueLineModal.show({
              type: { name: "string" },
              valueLineType: "TextBox",
              modalSize: "md",
              title: <><FontAwesomeIcon icon="address-book" /> {UserADMessage.FindInActiveDirectory.niceToString()}</>,
              labelText: UserADMessage.NameOrEmail.niceToString(),
              initialValue: search
            }) as Promise<string>;

            return promise.then(str => !str ? null : importADUser(str))
              .then(u => u && Navigator.view(u))
              .then(u => u && ctx.searchControl.handleCreated(u))
              .done();

          }}>
          <FontAwesomeIcon icon="user-plus" /> {!search ? UserADMessage.FindInActiveDirectory.niceToString() : UserADMessage.Find0InActiveDirectory.niceToString().formatHtml(search == null ? UserEntity.niceName() : <strong>{search}</strong>)}
        </button>
      }
    );
  });

  Finder.addSettings({
    queryName: UserADQuery.ActiveDirectoryUsers,
    defaultFilters: [
      {
        groupOperation: "Or",
        pinned: { label: SearchMessage.Search.niceToString(), splitText: true, active: "WhenHasValue" },
        filters: [
          { token: "DisplayName", operation: "StartsWith" },
          { token: "GivenName", operation: "StartsWith" },
          { token: "Surname", operation: "StartsWith" },
          { token: "Mail", operation: "StartsWith" },
        ],
      },
      {
        pinned: { label: ()=> ActiveDirectoryMessage.OnlyActiveUsers.niceToString(), active: "Checkbox_StartChecked", column: 2, row: 0 },
        token: "AccountEnabled", operation: "EqualTo", value: true
      },
      { token: "CreationType", operation: "DistinctTo", value: "Invitation" }
    ],
    hiddenColumns: [
      { token: "Id" },
      { token: "OnPremisesImmutableId" },
    ],
    defaultOrders: [
      { token: "DisplayName", orderType: "Ascending" }
    ],
  });

  Finder.addSettings({
    queryName: UserADQuery.ActiveDirectoryGroups,
    defaultFilters: [
      {
        groupOperation: "Or",
        pinned: { label: SearchMessage.Search.niceToString(), splitText: true, active: "WhenHasValue" },
        filters: [
          { token: "DisplayName", operation: "StartsWith" },
        ],
      },
      //{
      //  pinned: { label: ActiveDirectoryMessage.OnlyActiveUsers.niceToString(), active: "Checkbox_StartChecked", column: 2, row: 0 },
      //  token: "AccountEnabled", operation: "EqualTo", value: true
      //},
      //{ token: "CreationType", operation: "DistinctTo", value: "Invitation" }
    ],
    defaultOrders: [
      { token: "DisplayName", orderType: "Ascending" }
    ],
  });

}

function findActiveDirectoryUser(): Promise<Lite<UserEntity> | undefined> {
  return Finder.findRow({
    queryName: UserADQuery.ActiveDirectoryUsers,
    filterOptions: [
      { token: "InGroup", value: null, pinned: { column: 2, row: 0, active: "WhenHasValue", } },
    ],
    columnOptions: [
      { token: "DisplayName" },
      { token: "UserPrincipalName" },
      { token: "GivenName" },
      { token: "Surname" },
      { token: "JobTitle" },
    ],
    columnOptionsMode: "Replace",
  }, { searchControlProps: { allowChangeOrder: false } })
    .then(a => a && API.createADUser(toActiveDirectoryUser(a.row, a.searchControl)));
}

export function toActiveDirectoryUser(row: ResultRow, scl: SearchControlLoaded): ActiveDirectoryUser {

  const columns = scl.state.resultTable!.columns;
  return ({
    displayName: row.columns[columns.indexOf("DisplayName")],
    jobTitle: row.columns[columns.indexOf("JobTitle")],
    objectID: row.columns[columns.indexOf("Id")],
    upn: row.columns[columns.indexOf("UserPrincipalName")],
  });
}

export function findActiveDirectoryGroup(): Promise<Lite<ADGroupEntity> | undefined> {
  return Finder.findRow({
    queryName: UserADQuery.ActiveDirectoryGroups,
    filterOptions: [
      { token: "HasUser", value: null, pinned: { column: 2, row: 0, active: "WhenHasValue", } },
    ]
  }, { searchControlProps: { allowChangeOrder: false } })
    .then(a => a && API.createADGroup(toADGroupRequest(a.row, a.searchControl)));
}

export function findManyActiveDirectoryGroup(): Promise<Lite<ADGroupEntity>[] | undefined> {
  return Finder.findManyRows({
    queryName: UserADQuery.ActiveDirectoryGroups,
    filterOptions: [
      { token: "HasUser", value: null, pinned: { column: 2, row: 0, active: "WhenHasValue", } },
    ]
  }, { searchControlProps: { allowChangeOrder: false } })
    .then(a => a && Promise.all(a.rows.map(r => API.createADGroup(toADGroupRequest(r, a.searchControl)))));
}

export function toADGroupRequest(row: ResultRow, scl: SearchControlLoaded): ADGroupRequest {

  const columns = scl.state.resultTable!.columns;
  return ({
    id: row.columns[columns.indexOf("Id")],
    displayName: row.columns[columns.indexOf("DisplayName")],
  });
}

function getSearch(fo: FindOptionsParsed): string | null {
  var bla = fo.filterOptions.firstOrNull(a => a.pinned?.splitText == true)?.value;
  return !bla ? null : bla as string;
}


export function importADUser(text: string): Promise<Lite<UserEntity> | undefined> {
  return API.findADUsers({ count: 10, subString: text, types: UserEntity.typeName })
    .then(adUsers => {
      if (adUsers.length == 0)
        return MessageModal.showError(UserADMessage.NoUserContaining0FoundInActiveDirectory.niceToString(text));

      return SelectorModal.chooseElement(adUsers, {
        forceShow: true,
        size: "md",
        title: UserADMessage.SelectActiveDirectoryUser.niceToString(),
        message: UserADMessage.PleaseSelectTheUserFromActiveDirectoryThatYouWantToImport.niceToString(),
        buttonDisplay: u => <div style={{ display: "flex", flexDirection: "column" }}>
          <strong>{u.displayName}</strong>
          <pre className="mb-0">{u.upn}</pre>
          {u.jobTitle && <span className="text-muted">{u.jobTitle}</span>}
        </div>
      })
        .then(adu => adu ? API.createADUser(adu) : undefined);
    })
}




export module API {

  export function findADUsers(request: Finder.API.AutocompleteRequest, signal?: AbortSignal): Promise<ActiveDirectoryUser[]> {
    return ajaxGet({ url: "~/api/findADUsers?" + QueryString.stringify({ ...request }), signal });
  }

  export function createADUser(model: ActiveDirectoryUser): Promise<Lite<UserEntity>> {
    return ajaxPost({ url: `~/api/createADUser` }, model);
  }

  export function createADGroup(request: ADGroupRequest): Promise<Lite<ADGroupEntity>> {
    return ajaxPost({ url: `~/api/createADGroup` }, request);
  }
}

export interface ActiveDirectoryUser {
  displayName: string;
  jobTitle: string;
  upn: string;
  objectID: string;
}

export interface ADGroupRequest {
  id: string;
  displayName: string
}
