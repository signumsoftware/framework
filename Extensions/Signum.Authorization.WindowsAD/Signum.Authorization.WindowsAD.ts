//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as ADGroups from '../Signum.Authorization/Signum.Authorization.ADGroups'
import * as Scheduler from '../Signum.Scheduler/Signum.Scheduler'


export const UserWindowsADMixin: Type<UserWindowsADMixin> = new Type<UserWindowsADMixin>("UserWindowsADMixin");
export interface UserWindowsADMixin extends Entities.MixinEntity {
  Type: "UserWindowsADMixin";
  sID: string | null;
}

export const WindowsADConfigurationEmbedded: Type<WindowsADConfigurationEmbedded> = new Type<WindowsADConfigurationEmbedded>("WindowsADConfigurationEmbedded");
export interface WindowsADConfigurationEmbedded extends ADGroups.BaseADConfigurationEmbedded {
  loginWithWindowsAuthenticator: boolean;
  loginWithActiveDirectoryRegistry: boolean;
  domainName: string | null;
  directoryRegistry_Username: string | null;
  directoryRegistry_Password: string | null;
}

export namespace WindowsADMessage {
  export const TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet: MessageKey = new MessageKey("WindowsADMessage", "TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet");
  export const LoginWithWindowsUser: MessageKey = new MessageKey("WindowsADMessage", "LoginWithWindowsUser");
  export const NoWindowsUserFound: MessageKey = new MessageKey("WindowsADMessage", "NoWindowsUserFound");
  export const LooksLikeYourWindowsUserIsNotAllowedToUseThisApplication: MessageKey = new MessageKey("WindowsADMessage", "LooksLikeYourWindowsUserIsNotAllowedToUseThisApplication");
}

export namespace WindowsADTask {
  export const DeactivateUsers : Scheduler.SimpleTaskSymbol = registerSymbol("SimpleTask", "WindowsADTask.DeactivateUsers");
}

