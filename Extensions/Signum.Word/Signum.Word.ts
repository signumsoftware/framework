//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Templates from '../Signum.Mailing/Signum.Mailing.Templates'
import * as Templating from '../Signum.Templating/Signum.Templating'
import * as UserAssets from '../Signum.UserAssets/Signum.UserAssets'
import * as Queries from '../Signum.UserAssets/Signum.UserAssets.Queries'
import * as Files from '../Signum.Files/Signum.Files'


export const WordAttachmentEntity: Type<WordAttachmentEntity> = new Type<WordAttachmentEntity>("WordAttachment");
export interface WordAttachmentEntity extends Entities.Entity, Templates.IAttachmentGeneratorEntity {
  Type: "WordAttachment";
  fileName: string | null;
  wordTemplate: Entities.Lite<WordTemplateEntity>;
  overrideModel: Entities.Lite<Entities.Entity> | null;
  modelConverter: Templating.ModelConverterSymbol | null;
}

export const WordConverterSymbol: Type<WordConverterSymbol> = new Type<WordConverterSymbol>("WordConverter");
export interface WordConverterSymbol extends Basics.Symbol {
  Type: "WordConverter";
}

export const WordModelEntity: Type<WordModelEntity> = new Type<WordModelEntity>("WordModel");
export interface WordModelEntity extends Entities.Entity {
  Type: "WordModel";
  fullClassName: string;
}

export const WordTemplateEntity: Type<WordTemplateEntity> = new Type<WordTemplateEntity>("WordTemplate");
export interface WordTemplateEntity extends Entities.Entity, UserAssets.IUserAssetEntity, Templating.IContainsQuery {
  Type: "WordTemplate";
  guid: string /*Guid*/;
  name: string;
  query: Basics.QueryEntity | null;
  model: WordModelEntity | null;
  culture: Basics.CultureInfoEntity;
  groupResults: boolean;
  filters: Entities.MList<Queries.QueryFilterEmbedded>;
  orders: Entities.MList<Queries.QueryOrderEmbedded>;
  applicable: Templating.TemplateApplicableEval | null;
  disableAuthorization: boolean;
  disableValidation: boolean;
  template: Entities.Lite<Files.FileEntity>;
  fileName: string;
  wordTransformer: WordTransformerSymbol | null;
  wordConverter: WordConverterSymbol | null;
}

export namespace WordTemplateMessage {
  export const ModelShouldBeSetToUseModel0: MessageKey = new MessageKey("WordTemplateMessage", "ModelShouldBeSetToUseModel0");
  export const Type0DoesNotHaveAPropertyWithName1: MessageKey = new MessageKey("WordTemplateMessage", "Type0DoesNotHaveAPropertyWithName1");
  export const ChooseAReportTemplate: MessageKey = new MessageKey("WordTemplateMessage", "ChooseAReportTemplate");
  export const _01RequiresExtraParameters: MessageKey = new MessageKey("WordTemplateMessage", "_01RequiresExtraParameters");
  export const SelectTheSourceOfDataForYourTableOrChart: MessageKey = new MessageKey("WordTemplateMessage", "SelectTheSourceOfDataForYourTableOrChart");
  export const WriteThisKeyAsTileInTheAlternativeTextOfYourTableOrChart: MessageKey = new MessageKey("WordTemplateMessage", "WriteThisKeyAsTileInTheAlternativeTextOfYourTableOrChart");
  export const NoDefaultTemplateDefined: MessageKey = new MessageKey("WordTemplateMessage", "NoDefaultTemplateDefined");
  export const WordReport: MessageKey = new MessageKey("WordTemplateMessage", "WordReport");
}

export namespace WordTemplateOperation {
  export const Save : Operations.ExecuteSymbol<WordTemplateEntity> = registerSymbol("Operation", "WordTemplateOperation.Save");
  export const Delete : Operations.DeleteSymbol<WordTemplateEntity> = registerSymbol("Operation", "WordTemplateOperation.Delete");
  export const CreateWordReport : Operations.ExecuteSymbol<WordTemplateEntity> = registerSymbol("Operation", "WordTemplateOperation.CreateWordReport");
  export const CreateWordTemplateFromWordModel : Operations.ConstructSymbol_From<WordTemplateEntity, WordModelEntity> = registerSymbol("Operation", "WordTemplateOperation.CreateWordTemplateFromWordModel");
}

export namespace WordTemplatePermission {
  export const GenerateReport : Basics.PermissionSymbol = registerSymbol("Permission", "WordTemplatePermission.GenerateReport");
}

export const WordTemplateVisibleOn: EnumType<WordTemplateVisibleOn> = new EnumType<WordTemplateVisibleOn>("WordTemplateVisibleOn");
export type WordTemplateVisibleOn =
  "Single" |
  "Multiple" |
  "Query";

export const WordTransformerSymbol: Type<WordTransformerSymbol> = new Type<WordTransformerSymbol>("WordTransformer");
export interface WordTransformerSymbol extends Basics.Symbol {
  Type: "WordTransformer";
}

