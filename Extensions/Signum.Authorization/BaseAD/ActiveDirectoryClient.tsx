import { RouteObject } from 'react-router'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { Navigator } from '@framework/Navigator'
import { Finder } from '@framework/Finder'
import { UserEntity } from '../Signum.Authorization'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import AutoLineModal from '@framework/AutoLineModal';
import { FindOptionsParsed } from '@framework/FindOptions';
import MessageModal from '@framework/Modals/MessageModal';
import { Lite } from '@framework/Signum.Entities';
import SelectorModal from '@framework/SelectorModal';
import { QueryString } from '@framework/QueryString';
import * as AppContext from "@framework/AppContext"
import { ActiveDirectoryPermission, UserADMessage } from '../Signum.Authorization.ADGroups';

export namespace ActiveDirectoryClient {

  export function start(options: { routes: RouteObject[], adGroups: boolean, inviteUsers: boolean, profilePhotos: boolean | "cached"; }): void {

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
  }

  export interface ActiveDirectoryUser {
    displayName: string;
    jobTitle: string;
    upn: string;
    objectID: string;
  }

}
