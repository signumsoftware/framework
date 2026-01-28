//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Files from '../Signum.Files/Signum.Files'
import * as Authorization from '../Signum.Authorization/Signum.Authorization'


export const WhatsNewEntity: Type<WhatsNewEntity> = new Type<WhatsNewEntity>("WhatsNew");
export interface WhatsNewEntity extends Entities.Entity {
  Type: "WhatsNew";
  name: string;
  messages: Entities.MList<WhatsNewMessageEmbedded>;
  previewPicture: Files.FilePathEmbedded | null;
  attachment: Entities.MList<Files.FilePathEmbedded>;
  creationDate: string /*DateTime*/;
  status: WhatsNewState;
  related: Entities.Lite<Entities.Entity> | null;
}

export namespace WhatsNewFileType {
  export const WhatsNewAttachmentFileType : Files.FileTypeSymbol = registerSymbol("FileType", "WhatsNewFileType.WhatsNewAttachmentFileType");
  export const WhatsNewPreviewFileType : Files.FileTypeSymbol = registerSymbol("FileType", "WhatsNewFileType.WhatsNewPreviewFileType");
}

export const WhatsNewLogEntity: Type<WhatsNewLogEntity> = new Type<WhatsNewLogEntity>("WhatsNewLog");
export interface WhatsNewLogEntity extends Entities.Entity {
  Type: "WhatsNewLog";
  whatsNew: Entities.Lite<WhatsNewEntity>;
  user: Entities.Lite<Authorization.UserEntity>;
  readOn: string /*DateTime*/;
}

export namespace WhatsNewLogOperation {
  export const Delete : Operations.DeleteSymbol<WhatsNewLogEntity> = registerSymbol("Operation", "WhatsNewLogOperation.Delete");
}

export namespace WhatsNewMessage {
  export const News: MessageKey = new MessageKey("WhatsNewMessage", "News");
  export const NewNews: MessageKey = new MessageKey("WhatsNewMessage", "NewNews");
  export const YourNews: MessageKey = new MessageKey("WhatsNewMessage", "YourNews");
  export const MyActiveNews: MessageKey = new MessageKey("WhatsNewMessage", "MyActiveNews");
  export const YouDoNotHaveAnyUnreadNews: MessageKey = new MessageKey("WhatsNewMessage", "YouDoNotHaveAnyUnreadNews");
  export const ViewMore: MessageKey = new MessageKey("WhatsNewMessage", "ViewMore");
  export const CloseAll: MessageKey = new MessageKey("WhatsNewMessage", "CloseAll");
  export const AllMyNews: MessageKey = new MessageKey("WhatsNewMessage", "AllMyNews");
  export const NewUnreadNews: MessageKey = new MessageKey("WhatsNewMessage", "NewUnreadNews");
  export const ReadFurther: MessageKey = new MessageKey("WhatsNewMessage", "ReadFurther");
  export const Downloads: MessageKey = new MessageKey("WhatsNewMessage", "Downloads");
  export const _0ContiansNoVersionForCulture1: MessageKey = new MessageKey("WhatsNewMessage", "_0ContiansNoVersionForCulture1");
  export const Language: MessageKey = new MessageKey("WhatsNewMessage", "Language");
  export const ThisNewIsNoLongerAvailable: MessageKey = new MessageKey("WhatsNewMessage", "ThisNewIsNoLongerAvailable");
  export const BackToOverview: MessageKey = new MessageKey("WhatsNewMessage", "BackToOverview");
  export const NewsPage: MessageKey = new MessageKey("WhatsNewMessage", "NewsPage");
  export const Preview: MessageKey = new MessageKey("WhatsNewMessage", "Preview");
  export const IsRead: MessageKey = new MessageKey("WhatsNewMessage", "IsRead");
  export const Close0WhatsNew: MessageKey = new MessageKey("WhatsNewMessage", "Close0WhatsNew");
  export const New: MessageKey = new MessageKey("WhatsNewMessage", "New");
}

export const WhatsNewMessageEmbedded: Type<WhatsNewMessageEmbedded> = new Type<WhatsNewMessageEmbedded>("WhatsNewMessageEmbedded");
export interface WhatsNewMessageEmbedded extends Entities.EmbeddedEntity {
  Type: "WhatsNewMessageEmbedded";
  culture: Basics.CultureInfoEntity;
  title: string;
  description: string;
}

export namespace WhatsNewOperation {
  export const Save : Operations.ExecuteSymbol<WhatsNewEntity> = registerSymbol("Operation", "WhatsNewOperation.Save");
  export const Delete : Operations.DeleteSymbol<WhatsNewEntity> = registerSymbol("Operation", "WhatsNewOperation.Delete");
  export const Publish : Operations.ExecuteSymbol<WhatsNewEntity> = registerSymbol("Operation", "WhatsNewOperation.Publish");
  export const Unpublish : Operations.ExecuteSymbol<WhatsNewEntity> = registerSymbol("Operation", "WhatsNewOperation.Unpublish");
}

export const WhatsNewState: EnumType<WhatsNewState> = new EnumType<WhatsNewState>("WhatsNewState");
export type WhatsNewState =
  "Draft" |
  "Publish";

