import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import { Finder } from '@framework/Finder'
import { Lite, SearchMessage } from '@framework/Signum.Entities';
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded';
import * as AppContext from "@framework/AppContext"
import { ChangeLogClient } from '@framework/Basics/ChangeLogClient';
import { useAPI } from '@framework/Hooks';
import { ADGroupEntity } from './Signum.Authorization.AzureAD.ADGroup';
import * as User from '../Signum.Authorization/Templates/User';
import { UserEntity, UserLiteModel } from '../Signum.Authorization/Signum.Authorization';
import * as ProfilePhoto from '../Signum.Authorization/Templates/ProfilePhoto';
import { ResultRow } from '@framework/FindOptions';
import { AzureADConfigurationEmbedded, AzureADMailboxStatus, AzureADQuery } from './Signum.Authorization.AzureAD';
import { ActiveDirectoryMessage } from '../Signum.Authorization/Signum.Authorization.BaseAD';
import { ActiveDirectoryClient } from '../Signum.Authorization/BaseAD/ActiveDirectoryClient';
import { AzureADAuthenticator } from './AzureADAuthenticator';
import { AuthClient } from '../Signum.Authorization/AuthClient';


export namespace AzureADClient {
  export function start(options: { routes: RouteObject[], adGroups: boolean, profilePhotos: boolean | "cached"; }): void {
    ChangeLogClient.registerChangeLogModule("Signum.Authentication.AzureAD", () => import("./Changelog"));

    Navigator.addSettings(new EntitySettings(AzureADConfigurationEmbedded, e => import('./AzureADConfiguration')));

    if (options.profilePhotos) {
      ProfilePhoto.urlProviders.push((u: UserEntity | Lite<UserEntity>, size: number) => {

        var oid =
          (UserEntity.isLite(u)) ? (u.model as UserLiteModel).externalId :
            u.externalId;

        if (oid == null)
          return null;

        if (options.profilePhotos == "cached")
          return API.cachedAzureUserPhotoUrl(size, oid);

        return AppContext.toAbsoluteUrl("/api/azureUserPhoto/" + size + "/" + oid);
      })
    }

    if (options.adGroups) {
      Navigator.addSettings(new EntitySettings(ADGroupEntity, e => import('./ADGroup/ADGroup'), { isCreable: "Never" }));
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

  let mailboxStatusCache: { userKey: string | number | undefined, promise: Promise<AzureADMailboxStatus> } | undefined;

  /** Server-side (Microsoft Graph) check of whether the current user can use the Outlook / Graph mailbox features.
   *  Memoized per current user, so it auto-refreshes when the logged-in user changes. */
  export function getCurrentUserMailboxStatus(): Promise<AzureADMailboxStatus> {
    const userKey = AuthClient.currentUser()?.id;
    if (mailboxStatusCache == null || mailboxStatusCache.userKey != userKey)
      mailboxStatusCache = { userKey, promise: fetchCurrentUserMailboxStatus() };

    return mailboxStatusCache.promise;
  }

  async function fetchCurrentUserMailboxStatus(): Promise<AzureADMailboxStatus> {
    // Known client-side: a user without an ExternalId has no Active Directory account, so skip the server round-trip.
    if (AuthClient.currentUser()?.externalId == null)
      return "NoActiveDirectoryUser";

    try {
      const account = AzureADAuthenticator.getCurrentMsalAccount();
      const accessToken = account ? await AzureADAuthenticator.getAccessToken() : null;
      return await API.currentUserMailboxStatus(accessToken);
    } catch (e) {
      // Fail open: a token / network error must not block the feature (matches the previous behavior of not checking).
      console.error("Could not determine the current user's mailbox status", e);
      return "WithMailbox";
    }
  }

  export function useCurrentUserMailboxStatus(): AzureADMailboxStatus | undefined {
    return useAPI(() => getCurrentUserMailboxStatus(), [AuthClient.currentUser()?.id]);
  }

  export function toExternalUser(row: ResultRow, scl: SearchControlLoaded): ActiveDirectoryClient.ExternalUser {
    const columns = scl.state.resultTable!.columns;
    return ({
      displayName: row.columns[columns.indexOf("DisplayName")],
      jobTitle: row.columns[columns.indexOf("JobTitle")],
      externalId: row.columns[columns.indexOf("Id")],
      upn: row.columns[columns.indexOf("UserPrincipalName")],
    });
  }

  export namespace API {


    export function createADGroup(request: ADGroupRequest): Promise<Lite<ADGroupEntity>> {
      return ajaxPost({ url: `/api/createADGroup` }, request);
    }

    export const Options = { forceCacheInvalidationKey: undefined as string | undefined };
    export function cachedAzureUserPhotoUrl(size: number, oID: string): Promise<string | null> {
      return ajaxGet({ url: `/api/cachedAzureUserPhoto/${size}/${oID}` + (Options.forceCacheInvalidationKey ? ("?inv=" + Options.forceCacheInvalidationKey) : ""), cache: "default" });
    }

    export function currentUserMailboxStatus(accessToken: string | null): Promise<AzureADMailboxStatus> {
      return ajaxPost({ url: `/api/auth/currentUserMailboxStatus` }, { accessToken });
    }
  }

  export interface ADGroupRequest {
    id: string;
    displayName: string
  }
}
