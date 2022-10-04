import * as React from 'react'
import { ajaxPost, ajaxGet, ajaxGetRaw } from '@framework/Services';
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { UserEntity, UserADMessage, BasicPermission, ActiveDirectoryPermission, UserADQuery, ActiveDirectoryMessage, ADGroupEntity, UserADMixin, UserLiteModel } from './Signum.Entities.Authorization'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import ValueLineModal from '@framework/ValueLineModal';
import { FindOptions, FindOptionsParsed, ResultRow } from '@framework/FindOptions';
import MessageModal from '@framework/Modals/MessageModal';
import { isLite, Lite, SearchMessage, tryGetMixin } from '@framework/Signum.Entities';
import SelectorModal from '@framework/SelectorModal';
import { QueryString } from '@framework/QueryString';
import { isPermissionAuthorized } from './AuthClient';
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded';
import ProfilePhoto, { urlProviders } from './Templates/ProfilePhoto';
import * as AppContext from "@framework/AppContext"
import { TypeaheadOptions } from '../../Signum.React/Scripts/Components/Typeahead';

export function start(options: { routes: JSX.Element[], adGroups: boolean }) {
  if (window.__azureApplicationId) {
    urlProviders.push((u: UserEntity | Lite<UserEntity>, size: number) => {
      var oid =
        (UserEntity.isLite(u)) ? (u.model as UserLiteModel).oID :
        tryGetMixin(u, UserADMixin)?.oID;

      return oid == null ? null : AppContext.toAbsoluteUrl("~/api/azureUserPhoto/" + size + "/" + oid);
    })
  }

  urlProviders.push((u: UserEntity | Lite<UserEntity>, size: number) => {
    var sid =
      (UserEntity.isLite(u)) ? (u.model as UserLiteModel).sID :
        tryGetMixin(u, UserADMixin)?.sID;
    if (sid == null)
      return null;
    var url = "";
    if (UserEntity.isLite(u))
      url = AppContext.toAbsoluteUrl("~/api/adThumbnailphoto/" + ((u as Lite<UserEntity>).model as UserLiteModel)?.userName);
    else
      url = AppContext.toAbsoluteUrl("~/api/adThumbnailphoto/" + (u as UserEntity).userName);
    return url;
  });


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
        button: <button className="btn btn-info ms-2"
          onClick={e => {
            e.preventDefault();
            var promise = ValueLineModal.show({
              type: { name: "string" },
              valueLineType: "TextBox",
              modalSize: "md",
              title: <><FontAwesomeIcon icon="address-book" /> {UserADMessage.FindInActiveDirectory.niceToString()}</>,
              label: UserADMessage.NameOrEmail.niceToString(),
              initialValue: search
            }) as Promise<string>;

            return promise.then(str => !str ? null : importADUser(str))
              .then(u => u && Navigator.view(u))
              .then(u => u && ctx.searchControl.handleCreated(u));

          }}>
          <FontAwesomeIcon icon="user-plus" /> {!search ? UserADMessage.FindInActiveDirectory.niceToString() : UserADMessage.Find0InActiveDirectory.niceToString().formatHtml(search == null ? UserEntity.niceName() : <strong>{search}</strong>)}
        </button>
      }
    );
  });

  if (options.adGroups) {
    Navigator.addSettings(new Navigator.EntitySettings(ADGroupEntity, e => import('./AzureAD/ADGroup'), { isCreable: "Never" }));
    Finder.addSettings({
      queryName: UserADQuery.ActiveDirectoryUsers,
      defaultFilters: [
        {
          groupOperation: "Or",
          pinned: { label: SearchMessage.Search.niceToString(), splitText: true, active: "WhenHasValue" },
          filters: [
            { token: "DisplayName", operation: "Contains" },
            { token: "GivenName", operation: "Contains" },
            { token: "Surname", operation: "Contains" },
            { token: "Mail", operation: "Contains" },
          ],
        },
        {
          pinned: { label: () => ActiveDirectoryMessage.OnlyActiveUsers.niceToString(), active: "Checkbox_StartChecked", column: 2, row: 0 },
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
            { token: "DisplayName", operation: "Contains" },
          ],
        },
      ],
      defaultOrders: [
        { token: "DisplayName", orderType: "Ascending" }
      ],
    });
  }

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
    columnOptionsMode: "ReplaceAll",
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
