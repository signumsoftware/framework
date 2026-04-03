import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import { Finder } from '@framework/Finder'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import AutoLineModal from '@framework/AutoLineModal';
import { FindOptionsParsed, ResultRow } from '@framework/FindOptions';
import MessageModal from '@framework/Modals/MessageModal';
import { Lite, SearchMessage, tryGetMixin } from '@framework/Signum.Entities';
import SelectorModal from '@framework/SelectorModal';
import { QueryString } from '@framework/QueryString';
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded';
import * as AppContext from "@framework/AppContext"
import { TextBoxLine } from '@framework/Lines';
import { ChangeLogClient } from '@framework/Basics/ChangeLogClient';
import * as User from '../Signum.Authorization/Templates/User';
import { UserEntity, UserLiteModel } from '../Signum.Authorization/Signum.Authorization';
import * as ProfilePhoto from '../Signum.Authorization/Templates/ProfilePhoto';
import { UserWindowsADMixin, WindowsADConfigurationEmbedded } from './Signum.Authorization.WindowsAD';


export namespace WindowsADClient {
  export function start(options: { routes: RouteObject[], profilePhotos: boolean; }): void {
    ChangeLogClient.registerChangeLogModule("Signum.Authentication.WindowsAD", () => import("./Changelog"));

    Navigator.addSettings(new EntitySettings(WindowsADConfigurationEmbedded, e => import('./WindowsADConfiguration')));

    User.setChangePasswordVisibleFunction((user: UserEntity) => tryGetMixin(user, UserWindowsADMixin)?.sID == null);
    User.setUserNameReadonlyFunction((user: UserEntity) => tryGetMixin(user, UserWindowsADMixin)?.sID != null);
    User.setEmailReadonlyFunction((user: UserEntity) => tryGetMixin(user, UserWindowsADMixin)?.sID != null);

    if (options.profilePhotos) {

      ProfilePhoto.urlProviders.push((u: UserEntity | Lite<UserEntity>, size: number) => {
        var sid =
          (UserEntity.isLite(u)) ? (u.model as UserLiteModel).sID :
            tryGetMixin(u, UserWindowsADMixin)?.sID;

        if (sid == null)
          return null;

        var url = UserEntity.isLite(u) ?
          AppContext.toAbsoluteUrl("/api/adThumbnailphoto/" + (u.model as UserLiteModel)?.userName) :
          AppContext.toAbsoluteUrl("/api/adThumbnailphoto/" + (u as UserEntity).userName);

        return url;
      });
    }
  }
}
