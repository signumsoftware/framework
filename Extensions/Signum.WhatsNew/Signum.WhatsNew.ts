//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Files from '../Signum.Files/Signum.Files'
import * as Authorization from '../Signum.Authorization/Signum.Authorization'


export const WhatsNewEntity = new Type<WhatsNewEntity>("WhatsNew");
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

export module WhatsNewFileType {
  export const WhatsNewAttachmentFileType : Files.FileTypeSymbol = registerSymbol("FileType", "WhatsNewFileType.WhatsNewAttachmentFileType");
  export const WhatsNewPreviewFileType : Files.FileTypeSymbol = registerSymbol("FileType", "WhatsNewFileType.WhatsNewPreviewFileType");
}

export const WhatsNewLogEntity = new Type<WhatsNewLogEntity>("WhatsNewLog");
export interface WhatsNewLogEntity extends Entities.Entity {
  Type: "WhatsNewLog";
  whatsNew: Entities.Lite<WhatsNewEntity>;
  user: Entities.Lite<Authorization.UserEntity>;
  readOn: string /*DateTime*/;
}

export module WhatsNewLogOperation {
  export const Delete : Operations.DeleteSymbol<WhatsNewLogEntity> = registerSymbol("Operation", "WhatsNewLogOperation.Delete");
}

export module WhatsNewMessage {
  export const News = new MessageKey("WhatsNewMessage", "News");
  export const NewNews = new MessageKey("WhatsNewMessage", "NewNews");
  export const YourNews = new MessageKey("WhatsNewMessage", "YourNews");
  export const MyActiveNews = new MessageKey("WhatsNewMessage", "MyActiveNews");
  export const YouDoNotHaveAnyUnreadNews = new MessageKey("WhatsNewMessage", "YouDoNotHaveAnyUnreadNews");
  export const ViewMore = new MessageKey("WhatsNewMessage", "ViewMore");
  export const CloseAll = new MessageKey("WhatsNewMessage", "CloseAll");
  export const AllMyNews = new MessageKey("WhatsNewMessage", "AllMyNews");
  export const NewUnreadNews = new MessageKey("WhatsNewMessage", "NewUnreadNews");
  export const ReadFurther = new MessageKey("WhatsNewMessage", "ReadFurther");
  export const Downloads = new MessageKey("WhatsNewMessage", "Downloads");
  export const _0ContiansNoVersionForCulture1 = new MessageKey("WhatsNewMessage", "_0ContiansNoVersionForCulture1");
  export const Language = new MessageKey("WhatsNewMessage", "Language");
  export const ThisNewIsNoLongerAvailable = new MessageKey("WhatsNewMessage", "ThisNewIsNoLongerAvailable");
  export const BackToOverview = new MessageKey("WhatsNewMessage", "BackToOverview");
  export const NewsPage = new MessageKey("WhatsNewMessage", "NewsPage");
  export const Preview = new MessageKey("WhatsNewMessage", "Preview");
  export const IsRead = new MessageKey("WhatsNewMessage", "IsRead");
}

export const WhatsNewMessageEmbedded = new Type<WhatsNewMessageEmbedded>("WhatsNewMessageEmbedded");
export interface WhatsNewMessageEmbedded extends Entities.EmbeddedEntity {
  Type: "WhatsNewMessageEmbedded";
  culture: Basics.CultureInfoEntity;
  title: string;
  description: string;
}

export module WhatsNewOperation {
  export const Save : Operations.ExecuteSymbol<WhatsNewEntity> = registerSymbol("Operation", "WhatsNewOperation.Save");
  export const Delete : Operations.DeleteSymbol<WhatsNewEntity> = registerSymbol("Operation", "WhatsNewOperation.Delete");
  export const Publish : Operations.ExecuteSymbol<WhatsNewEntity> = registerSymbol("Operation", "WhatsNewOperation.Publish");
  export const Unpublish : Operations.ExecuteSymbol<WhatsNewEntity> = registerSymbol("Operation", "WhatsNewOperation.Unpublish");
}

export const WhatsNewState = new EnumType<WhatsNewState>("WhatsNewState");
export type WhatsNewState =
  "Draft" |
  "Publish";

