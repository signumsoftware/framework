import * as React from 'react'
import { ajaxPost, ajaxGet } from '@framework/Services';
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { UserEntity, UserADMessage, BasicPermission, ActiveDirectoryPermission, UserADQuery } from './Signum.Entities.Authorization'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import ValueLineModal from '../../../Framework/Signum.React/Scripts/ValueLineModal';
import { FindOptionsParsed } from '@framework/FindOptions';
import MessageModal from '../../../Framework/Signum.React/Scripts/Modals/MessageModal';
import { Lite, SearchMessage } from '@framework/Signum.Entities';
import SelectorModal from '../../../Framework/Signum.React/Scripts/SelectorModal';
import { QueryString } from '../../../Framework/Signum.React/Scripts/QueryString';
import { isPermissionAuthorized } from './AuthClient';

export let types: boolean;
export let properties: boolean;
export let operations: boolean;
export let queries: boolean;
export let permissions: boolean;

export function start(options: { routes: JSX.Element[]  }) {

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
        ]
      }],
    pagination: { mode: "Firsts", elementsPerPage: 20 },
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
}

export interface ActiveDirectoryUser {
  displayName: string;
  jobTitle: string;
  upn: string;
  objectID: string;
}

