import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import { Finder } from '@framework/Finder'
import { UserEntity, UserLiteModel} from '../Signum.Authorization/Signum.Authorization'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import AutoLineModal from '@framework/AutoLineModal';
import { FindOptionsParsed, ResultRow } from '@framework/FindOptions';
import MessageModal from '@framework/Modals/MessageModal';
import { Lite, SearchMessage, tryGetMixin } from '@framework/Signum.Entities';
import SelectorModal from '@framework/SelectorModal';
import { QueryString } from '@framework/QueryString';
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded';
import * as ProfilePhoto from '../Signum.Authorization/Templates/ProfilePhoto';
import * as AppContext from "@framework/AppContext"
import { ADGroupEntity, ActiveDirectoryConfigurationEmbedded, ActiveDirectoryMessage, ActiveDirectoryPermission, UserADMessage, UserADMixin } from './Signum.Authorization.ActiveDirectory';
import * as User from '../Signum.Authorization/Templates/User'
import { AzureADQuery } from './Signum.Authorization.ActiveDirectory.Azure';
import { TextBoxLine } from '@framework/Lines';
import { ChangeLogClient } from '@framework/Basics/ChangeLogClient';

export namespace ActiveDirectoryClient {

  export function start(options: { routes: RouteObject[], adGroups: boolean, inviteUsers: boolean, profilePhotos: boolean | "cached"; }): void {

    ChangeLogClient.registerChangeLogModule("Signum.ActiveDirectory", () => import("./Changelog"));

    Navigator.addSettings(new EntitySettings(ActiveDirectoryConfigurationEmbedded, e => import('./ActiveDirectoryConfiguration')));

    User.setChangePasswordVisibleFunction((user: UserEntity) => tryGetMixin(user, UserADMixin)?.oID == null);
    User.setUserNameReadonlyFunction((user: UserEntity) => tryGetMixin(user, UserADMixin)?.oID != null);
    User.setEmailReadonlyFunction((user: UserEntity) => tryGetMixin(user, UserADMixin)?.oID != null);

    if (options.profilePhotos) {
      if (window.__azureADConfig) {
        ProfilePhoto.urlProviders.push((u: UserEntity | Lite<UserEntity>, size: number) => {

          var oid =
            (UserEntity.isLite(u)) ? (u.model as UserLiteModel).oID :
              tryGetMixin(u, UserADMixin)?.oID;

          if (oid == null)
            return null;

          if (options.profilePhotos == "cached")
            return API.cachedAzureUserPhotoUrl(size, oid);

          return AppContext.toAbsoluteUrl("/api/azureUserPhoto/" + size + "/" + oid);
        })
      }

      ProfilePhoto.urlProviders.push((u: UserEntity | Lite<UserEntity>, size: number) => {
        var sid =
          (UserEntity.isLite(u)) ? (u.model as UserLiteModel).sID :
            tryGetMixin(u, UserADMixin)?.sID;

        if (sid == null)
          return null;

        var url = UserEntity.isLite(u) ?
          AppContext.toAbsoluteUrl("/api/adThumbnailphoto/" + (u.model as UserLiteModel)?.userName) :
          AppContext.toAbsoluteUrl("/api/adThumbnailphoto/" + (u as UserEntity).userName);

        return url;
      });
    }

    if (options.inviteUsers) {

      Navigator.getSettings(UserEntity)!.autocompleteConstructor = (str, aac) => AppContext.isPermissionAuthorized(ActiveDirectoryPermission.InviteUsersFromAD) && str.length > 2 ? ({
        type: UserEntity,
        customElement: <em><FontAwesomeIcon icon="address-book" title={UserADMessage.Find0InActiveDirectory.niceToString()} />&nbsp;{UserADMessage.Find0InActiveDirectory.niceToString().formatHtml(<strong>{str}</strong>)}</em>,
        onClick: () => importADUser(str),
      }) : null;

      Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {
        if (ctx.findOptions.queryKey != UserEntity.typeName || !AppContext.isPermissionAuthorized(ActiveDirectoryPermission.InviteUsersFromAD) ||
          (ctx.searchControl.props.extraOptions?.avoidFindInActiveDirectory ?? (!ctx.searchControl.props.create)))
          return undefined;

        var search = getSearch(ctx.findOptions);

        return (
          {
            order: -1,
            button: <button className="btn btn-info ms-2"
              onClick={e => {
                var promise = AutoLineModal.show({
                  type: { name: "string" },
                  modalSize: "md",
                  title: <><FontAwesomeIcon aria-hidden={true} icon="address-book" /> {UserADMessage.FindInActiveDirectory.niceToString()}</>,
                  label: UserADMessage.NameOrEmail.niceToString(),
                  initialValue: search
                }) as Promise<string>;

                return promise.then(str => !str ? null : importADUser(str))
                  .then(u => u && Navigator.view(u))
                  .then(u => u && ctx.searchControl.handleCreated(u));

              }}>
              <FontAwesomeIcon icon="user-plus" title={!search ? UserADMessage.FindInActiveDirectory.niceToString() : UserADMessage.Find0InActiveDirectory.niceToString()} /> {!search ? UserADMessage.FindInActiveDirectory.niceToString() : UserADMessage.Find0InActiveDirectory.niceToString().formatHtml(search == null ? UserEntity.niceName() : <strong>{search}</strong>)}
            </button>
          }
        );
      });
    }

    if (options.adGroups) {
      Navigator.addSettings(new EntitySettings(ADGroupEntity, e => import('./ADGroup'), { isCreable: "Never" }));
      Finder.addSettings({
        queryName: AzureADQuery.ActiveDirectoryUsers,
        defaultFilters: [
          {
            groupOperation: "Or",
            pinned: { label: SearchMessage.Search.niceToString(), splitValue: true, active: "WhenHasValue" },
            filters: [
              { token: "DisplayName", operation: "Contains" },
              { token: "GivenName", operation: "Contains" },
              { token: "Surname", operation: "Contains" },
              { token: "Mail", operation: "Contains" },
            ],
          },
          {
            pinned: { label: () => ActiveDirectoryMessage.OnlyActiveUsers.niceToString(), active: "Checkbox_Checked", column: 1, row: 0 },
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
        queryName: AzureADQuery.ActiveDirectoryGroups,
        defaultFilters: [
          {
            groupOperation: "Or",
            pinned: { label: SearchMessage.Search.niceToString(), splitValue: true, active: "WhenHasValue" },
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
      queryName: AzureADQuery.ActiveDirectoryUsers,
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
      queryName: AzureADQuery.ActiveDirectoryGroups,
      filterOptions: [
        { token: "HasUser", value: null, pinned: { column: 1, row: 0, active: "WhenHasValue", } },
      ]
    }, { searchControlProps: { allowChangeOrder: false } })
      .then(a => a && API.createADGroup(toADGroupRequest(a.row, a.searchControl)));
  }
  
  export function findManyActiveDirectoryGroup(): Promise<Lite<ADGroupEntity>[] | undefined> {
    return Finder.findManyRows({
      queryName: AzureADQuery.ActiveDirectoryGroups,
      filterOptions: [
        { token: "HasUser", value: null, pinned: { column: 1, row: 0, active: "WhenHasValue", } },
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
    var bla = fo.filterOptions.firstOrNull(a => a.pinned?.splitValue == true)?.value;
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
  
  
  
  
  export namespace API {
  
    export function findADUsers(request: Finder.API.AutocompleteRequest, signal?: AbortSignal): Promise<ActiveDirectoryUser[]> {
      return ajaxGet({ url: "/api/findADUsers?" + QueryString.stringify({ ...request }), signal });
    }
  
    export function createADUser(model: ActiveDirectoryUser): Promise<Lite<UserEntity>> {
      return ajaxPost({ url: `/api/createADUser` }, model);
    }
  
    export function createADGroup(request: ADGroupRequest): Promise<Lite<ADGroupEntity>> {
      return ajaxPost({ url: `/api/createADGroup` }, request);
    }

    export let forceCacheInvalidationKey: string | undefined = undefined
    export function cachedAzureUserPhotoUrl(size: number, oID: string): Promise<string | null> {
      return ajaxGet({ url: `/api/cachedAzureUserPhoto/${size}/${oID}` + (forceCacheInvalidationKey ? ("?inv=" + forceCacheInvalidationKey) : ""), cache: "default" });
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
}
