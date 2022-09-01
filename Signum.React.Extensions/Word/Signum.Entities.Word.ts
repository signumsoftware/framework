//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Scripts/Reflection'
import * as Entities from '../../Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../Signum.React/Scripts/Signum.Entities.Basics'
import * as Mailing from '../Mailing/Signum.Entities.Mailing'
import * as Templating from '../Templating/Signum.Entities.Templating'
import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets'
import * as Signum from '../Basics/Signum.Entities.Basics'
import * as UserQueries from '../UserQueries/Signum.Entities.UserQueries'
import * as Files from '../Files/Signum.Entities.Files'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'


export const WordAttachmentEntity = new Type<WordAttachmentEntity>("WordAttachment");
export interface WordAttachmentEntity extends Entities.Entity, Mailing.IAttachmentGeneratorEntity {
  Type: "WordAttachment";
  fileName: string | null;
  wordTemplate: Entities.Lite<WordTemplateEntity>;
  overrideModel: Entities.Lite<Entities.Entity> | null;
  modelConverter: Templating.ModelConverterSymbol | null;
}

export const WordConverterSymbol = new Type<WordConverterSymbol>("WordConverter");
export interface WordConverterSymbol extends Entities.Symbol {
  Type: "WordConverter";
}

export const WordModelEntity = new Type<WordModelEntity>("WordModel");
export interface WordModelEntity extends Entities.Entity {
  Type: "WordModel";
  fullClassName: string;
}

export const WordTemplateEntity = new Type<WordTemplateEntity>("WordTemplate");
export interface WordTemplateEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "WordTemplate";
  guid: string /*Guid*/;
  name: string;
  query: Basics.QueryEntity;
  model: WordModelEntity | null;
  culture: Signum.CultureInfoEntity;
  groupResults: boolean;
  filters: Entities.MList<UserQueries.QueryFilterEmbedded>;
  orders: Entities.MList<UserQueries.QueryOrderEmbedded>;
  applicable: Templating.TemplateApplicableEval | null;
  disableAuthorization: boolean;
  template: Entities.Lite<Files.FileEntity>;
  fileName: string;
  wordTransformer: WordTransformerSymbol | null;
  wordConverter: WordConverterSymbol | null;
}

export module WordTemplateMessage {
  export const ModelShouldBeSetToUseModel0 = new MessageKey("WordTemplateMessage", "ModelShouldBeSetToUseModel0");
  export const Type0DoesNotHaveAPropertyWithName1 = new MessageKey("WordTemplateMessage", "Type0DoesNotHaveAPropertyWithName1");
  export const ChooseAReportTemplate = new MessageKey("WordTemplateMessage", "ChooseAReportTemplate");
  export const _01RequiresExtraParameters = new MessageKey("WordTemplateMessage", "_01RequiresExtraParameters");
  export const SelectTheSourceOfDataForYourTableOrChart = new MessageKey("WordTemplateMessage", "SelectTheSourceOfDataForYourTableOrChart");
  export const WriteThisKeyAsTileInTheAlternativeTextOfYourTableOrChart = new MessageKey("WordTemplateMessage", "WriteThisKeyAsTileInTheAlternativeTextOfYourTableOrChart");
  export const NoDefaultTemplateDefined = new MessageKey("WordTemplateMessage", "NoDefaultTemplateDefined");
  export const WordReport = new MessageKey("WordTemplateMessage", "WordReport");
}

export module WordTemplateOperation {
  export const Save : Entities.ExecuteSymbol<WordTemplateEntity> = registerSymbol("Operation", "WordTemplateOperation.Save");
  export const Delete : Entities.DeleteSymbol<WordTemplateEntity> = registerSymbol("Operation", "WordTemplateOperation.Delete");
  export const CreateWordReport : Entities.ExecuteSymbol<WordTemplateEntity> = registerSymbol("Operation", "WordTemplateOperation.CreateWordReport");
  export const CreateWordTemplateFromWordModel : Entities.ConstructSymbol_From<WordTemplateEntity, WordModelEntity> = registerSymbol("Operation", "WordTemplateOperation.CreateWordTemplateFromWordModel");
}

export module WordTemplatePermission {
  export const GenerateReport : Authorization.PermissionSymbol = registerSymbol("Permission", "WordTemplatePermission.GenerateReport");
}

export const WordTemplateVisibleOn = new EnumType<WordTemplateVisibleOn>("WordTemplateVisibleOn");
export type WordTemplateVisibleOn =
  "Single" |
  "Multiple" |
  "Query";

export const WordTransformerSymbol = new Type<WordTransformerSymbol>("WordTransformer");
export interface WordTransformerSymbol extends Entities.Symbol {
  Type: "WordTransformer";
}


