import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import * as Constructor from '@framework/Constructor'
import * as Finder from '@framework/Finder'
import { Lite, Entity, newMListElement, registerToString, JavascriptMessage, getToString } from '@framework/Signum.Entities'
import { EntityOperationSettings } from '@framework/Operations'
import { PseudoType, Type, getTypeName, isTypeEntity } from '@framework/Reflection'
import * as Operations from '@framework/Operations'
import { EmailMessageEntity, EmailTemplateMessageEmbedded, EmailMasterTemplateEntity, EmailMasterTemplateMessageEmbedded, EmailMessageOperation, EmailPackageEntity, EmailRecipientEmbedded, EmailConfigurationEmbedded, EmailTemplateEntity, AsyncEmailSenderPermission, EmailModelEntity, IEmailOwnerEntity, EmailFromEmbedded, ExchangeWebServiceEmailServiceEntity, MicrosoftGraphEmailServiceEntity, SmtpEmailServiceEntity } from './Signum.Entities.Mailing'
import { EmailSenderConfigurationEntity, Pop3ConfigurationEntity, Pop3ReceptionEntity, EmailAddressEmbedded } from './Signum.Entities.Mailing'
import { SendEmailTaskEntity, EmailTemplateVisibleOn } from './Signum.Entities.Mailing'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import * as QuickLinks from '@framework/QuickLinks'
import { ImportRoute } from "@framework/AsyncImport";
import { ModifiableEntity } from "@framework/Signum.Entities";
import { ContextualItemsContext, MenuItemBlock } from "@framework/SearchControl/ContextualItems";
import { ModelEntity } from "@framework/Signum.Entities";
import { QueryRequest } from "@framework/FindOptions";
import * as ContexualItems from '@framework/SearchControl/ContextualItems'
import MailingMenu from "./MailingMenu";
import * as DynamicClientOptions from '../Dynamic/DynamicClientOptions';
import { Dropdown } from 'react-bootstrap';
import { registerExportAssertLink } from '../UserAssets/UserAssetClient';
import "./Mailing.css";
import { CultureInfoEntity } from '../Basics/Signum.Entities.Basics';
import { currentCulture } from '../Translation/CultureClient';


export var allTypes: string[] = [];

export function start(options: {
  routes: JSX.Element[],
  pop3Config: boolean,
  sendEmailTask: boolean,
  contextual: boolean,
  queryButton: boolean,
  quickLinkInDefaultGroup?: boolean
}) {
  DynamicClientOptions.Options.checkEvalFindOptions.push({ queryName: EmailTemplateEntity });

  options.routes.push(<ImportRoute path="~/asyncEmailSender/view" onImportModule={() => import("./AsyncEmailSenderPage")} />);

  OmniboxClient.registerSpecialAction({
    allowed: () => AuthClient.isPermissionAuthorized(AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel),
    key: "AsyncEmailSenderPanel",
    onClick: () => Promise.resolve("~/asyncEmailSender/view")
  });

  registerToString(EmailTemplateMessageEmbedded, a => a.cultureInfo == undefined ? JavascriptMessage.newEntity.niceToString() : a.cultureInfo.englishName!);
  registerToString(EmailMasterTemplateMessageEmbedded, a => a.cultureInfo == undefined ? JavascriptMessage.newEntity.niceToString() : a.cultureInfo.englishName!);

  Navigator.addSettings(new EntitySettings(EmailMessageEntity, e => import('./Templates/EmailMessage')));
  Navigator.addSettings(new EntitySettings(EmailTemplateEntity, e => import('./Templates/EmailTemplate')));
  Navigator.addSettings(new EntitySettings(EmailMasterTemplateEntity, e => import('./Templates/EmailMasterTemplate')));
  Navigator.addSettings(new EntitySettings(EmailPackageEntity, e => import('./Templates/EmailPackage')));
  Navigator.addSettings(new EntitySettings(EmailRecipientEmbedded, e => import('./Templates/EmailRecipient')));
  Navigator.addSettings(new EntitySettings(EmailFromEmbedded, e => import('./Templates/EmailFrom')));
  Navigator.addSettings(new EntitySettings(EmailConfigurationEmbedded, e => import('./Templates/EmailConfiguration')));
  Navigator.addSettings(new EntitySettings(SmtpEmailServiceEntity, e => import('./Templates/SenderServices/SmtpEmailService')));
  Navigator.addSettings(new EntitySettings(ExchangeWebServiceEmailServiceEntity, e => import('./Templates/SenderServices/ExchangeWebServiceEmailService')));
  Navigator.addSettings(new EntitySettings(MicrosoftGraphEmailServiceEntity, e => import('./Templates/SenderServices/MicrosoftGraphEmailService')));

  Constructor.registerConstructor(EmailTemplateEntity, props => API.getDefaultCulture()
    .then(culture => culture && EmailTemplateEntity.New({
      messageFormat: 'HtmlSimple',
      messages: [newMListElement(EmailTemplateMessageEmbedded.New({ cultureInfo: culture }))]
    })));

  Operations.addSettings(new EntityOperationSettings(EmailMessageOperation.CreateEmailFromTemplate, {
    onClick: (ctx) => {
      var promise: Promise<string | undefined> = ctx.entity.model ? API.getConstructorType(ctx.entity.model) : Promise.resolve(undefined);
      return promise.then(ct => {
        if (!ct || isTypeEntity(ct))
          return Finder.find({ queryName: ctx.entity.query!.key }).then(lite => {
            if (!lite)
              return;
            return Navigator.API.fetch(lite).then(entity => ctx.defaultClick(entity));
          });
        else {
          var s = settings[ct];
          var promise = (s?.createFromTemplate ? s.createFromTemplate(ctx.entity) : Constructor.constructPack(ct).then(a => a && Navigator.view(a)));
          return promise.then(model => model && ctx.defaultClick(model));
        }
      });
    }
  }));

  Operations.addSettings(new EntityOperationSettings(EmailMessageOperation.ReadyToSend, {
    contextual: { isVisible: em => true },
    contextualFromMany: { isVisible: em => true }
  }));

  Navigator.addSettings(new EntitySettings(EmailSenderConfigurationEntity, e => import('./Templates/EmailSenderConfiguration')));

  if (options.sendEmailTask) {
    Navigator.addSettings(new EntitySettings(SendEmailTaskEntity, e => import('./Templates/SendEmailTask')));
  }

  if (options.pop3Config) {
    Navigator.addSettings(new EntitySettings(Pop3ConfigurationEntity, e => import('./Pop3/Pop3Configuration')));
    Navigator.addSettings(new EntitySettings(Pop3ReceptionEntity, e => import('./Pop3/Pop3Reception')));
  }

  if (options.contextual)
    ContexualItems.onContextualItems.push(getEmailTemplates);

  if (options.queryButton)
    Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {

      if (!ctx.searchControl.props.showBarExtension ||
        !(ctx.searchControl.props.showBarExtensionOption?.showChartButton ?? ctx.searchControl.props.largeToolbarButtons))
        return undefined;

      return { button: <MailingMenu searchControl={ctx.searchControl} /> };
    });

  API.getAllTypes().then(types => {
    allTypes = types;
    QuickLinks.registerGlobalQuickLink(ctx => new QuickLinks.QuickLinkExplore(
      {
        queryName: EmailMessageEntity,
        filterOptions: [{ token: "Target", value: ctx.lite}],
      },
      {
        isVisible: allTypes.contains(ctx.lite.EntityType) && !Navigator.isReadOnly(EmailMessageEntity),
        icon: "envelope",
        iconColor: "orange",
        color: "warning",
        group: options.quickLinkInDefaultGroup ? undefined : null,
      }));
  });

  registerExportAssertLink(EmailTemplateEntity);
  registerExportAssertLink(EmailMasterTemplateEntity);

}



export interface EmailModelSettings<T extends ModelEntity> {
  createFromTemplate?: (et: EmailTemplateEntity) => Promise<ModelEntity | undefined>;
  createFromEntities?: (et: Lite<EmailTemplateEntity>, lites: Array<Lite<Entity>>) => Promise<ModelEntity | undefined>;
  createFromQuery?: (et: Lite<EmailTemplateEntity>, req: QueryRequest) => Promise<ModelEntity | undefined>;
}

export const settings: { [typeName: string]: EmailModelSettings<ModifiableEntity> } = {};

export function register<T extends ModifiableEntity>(type: Type<T>, setting: EmailModelSettings<T>) {
  settings[type.typeName] = setting;
}

export function getEmailTemplates(ctx: ContextualItemsContext<Entity>): Promise<MenuItemBlock | undefined> | undefined {

  if (ctx.lites.length == 0)
    return undefined;

  if (EmailTemplateEntity.tryTypeInfo() == null)
    return undefined;

  return API.getEmailTemplates(ctx.queryDescription.queryKey, ctx.lites.length > 1 ? "Multiple" : "Single", { lite: (ctx.lites.length == 1 ? ctx.lites[0] : null) })
    .then(wts => {
      if (!wts.length)
        return undefined;

      return {
        header: EmailTemplateEntity.nicePluralName(),
        menuItems: wts.map(et =>
          <Dropdown.Item data-operation={et.EntityType} onClick={() => handleMenuClick(et, ctx)}>
            <FontAwesomeIcon icon={["far", "envelope"]} className="icon" />
            {getToString(et)}
          </Dropdown.Item>
        )
      } as MenuItemBlock;
    });
}

export function handleMenuClick(et: Lite<EmailTemplateEntity>, ctx: ContextualItemsContext<Entity>) {

  Navigator.API.fetch(et)
    .then(emailTemplate => emailTemplate.model ? API.getConstructorType(emailTemplate.model) : Promise.resolve(undefined))
    .then(ct => {
      if (!ct)
        return createAndViewEmail(et, ctx.lites.single());

      var s = settings[ct];
      if (!s)
        throw new Error("No 'WordModelSettings' defined for '" + ct + "'");

      if (!s.createFromEntities)
        throw new Error("No 'createFromEntities' defined in the WordModelSettings of '" + ct + "'");

      return s.createFromEntities(et, ctx.lites)
        .then(m => m && createAndViewEmail(et, m));
    });
}

export function createAndViewEmail(template: Lite<EmailTemplateEntity>, ...args: any[]) {

  Operations.API.constructFromLite(template, EmailMessageOperation.CreateEmailFromTemplate, ...args)
    .then(pack => pack && Navigator.view(pack));
}

export module API {
  export function start(): Promise<void> {
    return ajaxPost({ url: "~/api/asyncEmailSender/start" }, undefined);
  }

  export function stop(): Promise<void> {
    return ajaxPost({ url: "~/api/asyncEmailSender/stop" }, undefined);
  }

  export function view(): Promise<AsyncEmailSenderState> {
    return ajaxGet({ url: "~/api/asyncEmailSender/view" });
  }


  export interface CreateEmailRequest {
    template: Lite<EmailTemplateEntity>;
    lite?: Lite<Entity>;
    entity?: ModifiableEntity;
  }

  export interface GetEmailTemplatesRequest {
    lite: Lite<Entity> | null;
  }

  export function getConstructorType(emailModelEntity: EmailModelEntity): Promise<string> {
    return ajaxPost({ url: "~/api/email/constructorType" }, emailModelEntity);
  }

  export function getEmailTemplates(queryKey: string, visibleOn: EmailTemplateVisibleOn, request: GetEmailTemplatesRequest): Promise<Lite<EmailTemplateEntity>[]> {
    return ajaxPost({ url: `~/api/email/emailTemplates?queryKey=${queryKey}&visibleOn=${visibleOn}` }, request);
  }

  export function getAllTypes(signal?: AbortSignal): Promise<string[]> {
    return ajaxGet({ url: "~/api/email/getAllTypes", signal });
  }

  export function getDefaultCulture(signal?: AbortSignal): Promise<CultureInfoEntity> {
    return ajaxGet({ url: "~/api/email/getDefaultCulture", signal });
  }
}


export interface AsyncEmailSenderState {
  asyncSenderPeriod: number;
  running: boolean;
  initialDelayMilliseconds: number | null;
  machineName: string;
  isCancelationRequested: boolean;
  nextPlannedExecution: string;
  queuedItems: number;
  currentProcessIdentifier: string;
}

declare module '@framework/FindOptions' {

  export interface QueryDescription {
    emailTemplates?: Array<Lite<EmailTemplateEntity>>;
  }
}
