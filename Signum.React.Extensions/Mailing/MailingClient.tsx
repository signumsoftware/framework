import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Lite, Entity, registerToString, JavascriptMessage } from '@framework/Signum.Entities'
import { EntityOperationSettings } from '@framework/Operations'
import { PseudoType, Type, getTypeName } from '@framework/Reflection'
import * as Operations from '@framework/Operations'
import { EmailMessageEntity, EmailTemplateMessageEmbedded, EmailMasterTemplateEntity, EmailMasterTemplateMessageEmbedded, EmailMessageOperation, EmailPackageEntity, EmailRecipientEmbedded, EmailConfigurationEmbedded, EmailTemplateEntity, AsyncEmailSenderPermission } from './Signum.Entities.Mailing'
import { SmtpConfigurationEntity, Pop3ConfigurationEntity, Pop3ReceptionEntity, EmailAddressEmbedded } from './Signum.Entities.Mailing'
import { NewsletterEntity, NewsletterDeliveryEntity, SendEmailTaskEntity, SystemEmailEntity, EmailTemplateVisibleOn } from './Signum.Entities.Mailing'
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
import { DropdownItem } from '@framework/Components';
import { registerExportAssertLink } from '../UserAssets/UserAssetClient';
import "./Mailing.css";


export function start(options: {
  routes: JSX.Element[], smtpConfig: boolean,
  newsletter: boolean,
  pop3Config: boolean,
  sendEmailTask: boolean,
  contextual: boolean,
  queryButton: boolean,
  quickLinksFrom: PseudoType[] | undefined
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
  Navigator.addSettings(new EntitySettings(EmailAddressEmbedded, e => import('./Templates/EmailAddress')));
  Navigator.addSettings(new EntitySettings(EmailConfigurationEmbedded, e => import('./Templates/EmailConfiguration')));

  Operations.addSettings(new EntityOperationSettings(EmailMessageOperation.CreateEmailFromTemplate, {
    onClick: (ctx) => {

      var promise: Promise<string | undefined> = ctx.entity.systemEmail ? API.getConstructorType(ctx.entity.systemEmail) : Promise.resolve(undefined);
      promise

      Finder.find({ queryName: ctx.entity.query!.key }).then(lite => {
        if (!lite)
          return;
        Navigator.API.fetchAndForget(lite).then(entity =>
          ctx.defaultClick(entity))
          .done();
      }).done();
    }
  }));

  Operations.addSettings(new EntityOperationSettings(EmailMessageOperation.ReadyToSend, {
    contextual: { isVisible: em => true },
    contextualFromMany: { isVisible: em => true }
  }));

  if (options.smtpConfig) {
    Navigator.addSettings(new EntitySettings(SmtpConfigurationEntity, e => import('./Templates/SmtpConfiguration')));
  }

  if (options.newsletter) {
    Navigator.addSettings(new EntitySettings(NewsletterEntity, e => import('./Newsletters/Newsletter')));
    Navigator.addSettings(new EntitySettings(NewsletterDeliveryEntity, e => import('./Newsletters/NewsletterDelivery')));
  }

  if (options.sendEmailTask) {
    Navigator.addSettings(new EntitySettings(SendEmailTaskEntity, e => import('./Templates/SendEmailTask')));
  }

  if (options.pop3Config) {
    Navigator.addSettings(new EntitySettings(Pop3ConfigurationEntity, e => import('./Pop3/Pop3Configuration')));
    Navigator.addSettings(new EntitySettings(Pop3ReceptionEntity, e => import('./Pop3/Pop3Reception')));
  }

  if (options.quickLinksFrom) {
    QuickLinks.registerGlobalQuickLink(ctx => {
      if (options.quickLinksFrom!.some(e => getTypeName(e) == ctx.lite.EntityType))
        return new QuickLinks.QuickLinkExplore({ queryName: EmailMessageEntity, parentToken: EmailMessageEntity.token(e => e.target), parentValue: ctx.lite });

      return undefined;
    });
  }

  if (options.contextual)
    ContexualItems.onContextualItems.push(getEmailTemplates);

  if (options.queryButton)
    Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {

      if (!ctx.searchControl.props.showBarExtension)
        return undefined;

      return <MailingMenu searchControl={ctx.searchControl} />;
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

  return API.getEmailTemplates(ctx.queryDescription.queryKey, ctx.lites.length > 1 ? "Multiple" : "Single", { lite: (ctx.lites.length == 1 ? ctx.lites[0] : null) })
    .then(wts => {
      if (!wts.length)
        return undefined;

      return {
        header: EmailTemplateEntity.nicePluralName(),
        menuItems: wts.map(wt =>
          <DropdownItem data-operation={wt.EntityType} onClick={() => handleMenuClick(wt, ctx)}>
            <FontAwesomeIcon icon={["far", "envelope"]} className="icon" />
            {wt.toStr}
          </DropdownItem>
        )
      } as MenuItemBlock;
    });
}

export function handleMenuClick(et: Lite<EmailTemplateEntity>, ctx: ContextualItemsContext<Entity>) {

  Navigator.API.fetchAndForget(et)
    .then(emailTemplate => emailTemplate.systemEmail ? API.getConstructorType(emailTemplate.systemEmail) : Promise.resolve(undefined))
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
    })
    .done();
}

export function createAndViewEmail(template: Lite<EmailTemplateEntity>, ...args: any[]) {

  Operations.API.constructFromLite(template, EmailMessageOperation.CreateEmailFromTemplate, ...args)
    .then(pack => pack && Navigator.navigate(pack))
    .done();
}

export module API {
  export function start(): Promise<void> {
    return ajaxPost<void>({ url: "~/api/asyncEmailSender/start" }, undefined);
  }

  export function stop(): Promise<void> {
    return ajaxPost<void>({ url: "~/api/asyncEmailSender/stop" }, undefined);
  }

  export function view(): Promise<AsyncEmailSenderState> {
    return ajaxGet<AsyncEmailSenderState>({ url: "~/api/asyncEmailSender/view" });
  }


  export interface CreateEmailRequest {
    template: Lite<EmailTemplateEntity>;
    lite?: Lite<Entity>;
    entity?: ModifiableEntity;
  }

  export interface GetEmailTemplatesRequest {
    lite: Lite<Entity> | null;
  }

  export function getConstructorType(systemEmailTemplate: SystemEmailEntity): Promise<string> {
    return ajaxPost<string>({ url: "~/api/email/constructorType" }, systemEmailTemplate);
  }

  export function getEmailTemplates(queryKey: string, visibleOn: EmailTemplateVisibleOn, request: GetEmailTemplatesRequest): Promise<Lite<EmailTemplateEntity>[]> {
    return ajaxPost<Lite<EmailTemplateEntity>[]>({ url: `~/api/email/emailTemplates?queryKey=${queryKey}&visibleOn=${visibleOn}` }, request);
  }
}


export interface AsyncEmailSenderState {
  asyncSenderPeriod: number;
  running: boolean;
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
