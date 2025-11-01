import * as React from 'react'
import { RouteObject } from 'react-router'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import { Constructor } from '@framework/Constructor'
import { Finder } from '@framework/Finder'
import { Lite, Entity, newMListElement, registerToString, JavascriptMessage, getToString } from '@framework/Signum.Entities'
import { Operations, EntityOperationSettings } from '@framework/Operations'
import { PseudoType, Type, getTypeName, isTypeEntity, getQueryKey, getQueryInfo } from '@framework/Reflection'
import { EmailMessageEntity, EmailMessageOperation, EmailRecipientEmbedded, EmailConfigurationEmbedded, AsyncEmailSenderPermission, EmailModelEntity, EmailFromEmbedded, SmtpEmailServiceEntity } from './Signum.Mailing'
import { EmailSenderConfigurationEntity, EmailAddressEmbedded } from './Signum.Mailing'
import * as OmniboxSpecialAction from '@framework/OmniboxSpecialAction'
import { AuthClient } from '../Signum.Authorization/AuthClient'
import { EvalClient } from '../Signum.Eval/EvalClient'
import { QuickLinkClient, QuickLinkExplore } from '@framework/QuickLinkClient'
import { UserAssetClient } from '../Signum.UserAssets/UserAssetClient'
import { ImportComponent } from '@framework/ImportComponent'
import { ModifiableEntity } from "@framework/Signum.Entities";
import { ContextualItemsContext, MenuItemBlock, ContextualMenuItem } from "@framework/SearchControl/ContextualItems";
import { ModelEntity } from "@framework/Signum.Entities";
import { QueryRequest } from "@framework/FindOptions";
import * as ContexualItems from '@framework/SearchControl/ContextualItems'
import MailingMenu from "./MailingMenu";
import { Dropdown } from 'react-bootstrap';
import "./Mailing.css";
import { SearchControlLoaded } from '@framework/Search';
import { EmailMasterTemplateEntity, EmailMasterTemplateMessageEmbedded, EmailTemplateEntity, EmailTemplateMessageEmbedded, EmailTemplateVisibleOn, FileTokenAttachmentEntity, ImageAttachmentEntity } from './Signum.Mailing.Templates';
import { CultureInfoEntity } from '@framework/Signum.Basics';
import { ChangeLogClient } from '@framework/Basics/ChangeLogClient';

export namespace MailingClient {
  
  
  export var allTypes: string[] = [];
  
  export function start(options: {
    routes: RouteObject[],
    contextual: boolean,
    queryButton: boolean,
    quickLinkInDefaultGroup?: boolean
  }): void {
  
    ChangeLogClient.registerChangeLogModule("Signum.Mailing", () => import("./Changelog"));
  
    EvalClient.Options.checkEvalFindOptions.push({ queryName: EmailTemplateEntity });
  
    options.routes.push({ path: "/asyncEmailSender/view", element: <ImportComponent onImport={() => import("./AsyncEmailSenderPage")} /> });
  
    OmniboxSpecialAction.registerSpecialAction({
      allowed: () => AppContext.isPermissionAuthorized(AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel),
      key: "AsyncEmailSenderPanel",
      onClick: () => Promise.resolve("/asyncEmailSender/view")
    });
  
    registerToString(EmailTemplateMessageEmbedded, a => a.cultureInfo == undefined ? JavascriptMessage.newEntity.niceToString() : a.cultureInfo.englishName!);
    registerToString(EmailMasterTemplateMessageEmbedded, a => a.cultureInfo == undefined ? JavascriptMessage.newEntity.niceToString() : a.cultureInfo.englishName!);
  
    Navigator.addSettings(new EntitySettings(EmailMessageEntity, e => import('./Templates/EmailMessage')));
    Navigator.addSettings(new EntitySettings(EmailTemplateEntity, e => import('./Templates/EmailTemplate')));
    Navigator.addSettings(new EntitySettings(ImageAttachmentEntity, e => import('./Templates/ImageAttachment')));
    Navigator.addSettings(new EntitySettings(FileTokenAttachmentEntity, e => import('./Templates/FileTokenAttachment')));
    Navigator.addSettings(new EntitySettings(EmailMasterTemplateEntity, e => import('./Templates/EmailMasterTemplate')));
    Navigator.addSettings(new EntitySettings(EmailRecipientEmbedded, e => import('./Templates/EmailRecipient')));
    Navigator.addSettings(new EntitySettings(EmailFromEmbedded, e => import('./Templates/EmailFrom')));
    Navigator.addSettings(new EntitySettings(EmailConfigurationEmbedded, e => import('./Templates/EmailConfiguration')));
    Navigator.addSettings(new EntitySettings(SmtpEmailServiceEntity, e => import('./Templates/SenderServices/SmtpEmailService')));
  
    Constructor.registerConstructor(EmailTemplateEntity, props => API.getDefaultCulture()
      .then(culture => culture && EmailTemplateEntity.New({
        messageFormat: 'HtmlSimple',
        messages: [newMListElement(EmailTemplateMessageEmbedded.New({ cultureInfo: culture }))]
      })));
  
    Operations.addSettings(new EntityOperationSettings(EmailMessageOperation.CreateEmailFromTemplate, {
      onClick: async (ctx) => {
        const ct = ctx.entity.model ? await API.getConstructorType(ctx.entity.model) : undefined;
  
        if (!ct || isTypeEntity(ct)) {
          if (ctx.entity.query == null)
            return ctx.defaultClick();

          const lite = ct && ctx.entity.query.key != ct && getQueryInfo(ct) ? 
            await Finder.find({ queryName: ct }):
            await Finder.find({ queryName: ctx.entity.query!.key });
  
          if (!lite) return;
  
          const entity = await Navigator.API.fetch(lite);
          return ctx.defaultClick(entity);
        } else {
          const s = settings[ct];
  
          const model = s?.createFromTemplate
            ? await s.createFromTemplate(ctx.entity)
            : await Constructor.constructPack(ct).then(a => a && Navigator.view(a));
  
          if (model) {
            return ctx.defaultClick(model);
          }
        }
      }
    }));
  
    Operations.addSettings(new EntityOperationSettings(EmailMessageOperation.ReadyToSend, {
      contextual: { isVisible: em => true },
      contextualFromMany: { isVisible: em => true }
    }));
  
    Navigator.addSettings(new EntitySettings(EmailSenderConfigurationEntity, e => import('./Templates/EmailSenderConfiguration')));
  
    if (options.contextual)
      ContexualItems.onContextualItems.push(getEmailTemplates);
  
    if (options.queryButton)
      Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {
  
        if (!ctx.searchControl.props.showBarExtension ||
          !(ctx.searchControl.props.showBarExtensionOption?.showMailButton ?? ctx.searchControl.props.largeToolbarButtons))
          return undefined;
  
        return { button: <MailingMenu searchControl={ctx.searchControl} /> };
      });
  
  
    if (Finder.isFindable(EmailMessageEntity, false)) {
      var cachedAllTypes: Promise<string[]>;
      QuickLinkClient.registerGlobalQuickLink(entityType => (cachedAllTypes ??= API.getAllTypes())
        .then(types => !types.contains(entityType) ? [] :
          [new QuickLinkExplore(EmailMessageEntity, ctx => ({ queryName: EmailMessageEntity, filterOptions: [{ token: "Entity.Target", value: ctx.lite }] }),
            {
              key: getQueryKey(EmailMessageEntity),
              text: () => EmailMessageEntity.nicePluralName(),
              icon: "envelope",
              iconColor: "orange",
              color: "warning",
              group: options.quickLinkInDefaultGroup ? undefined : null,
            }
          )]));
    }
  
    UserAssetClient.registerExportAssertLink(EmailTemplateEntity);
    UserAssetClient.registerExportAssertLink(EmailMasterTemplateEntity);
  
  }
  
  export interface EmailModelSettings<T extends ModelEntity> {
    createFromTemplate?: (et: EmailTemplateEntity) => Promise<ModelEntity | undefined>;
    createFromEntities?: (et: Lite<EmailTemplateEntity>, lites: Array<Lite<Entity>>) => Promise<ModelEntity | undefined>;
    createFromQuery?: (et: Lite<EmailTemplateEntity>, req: QueryRequest) => Promise<ModelEntity | undefined>;
  }
  
  export const settings: { [typeName: string]: EmailModelSettings<ModifiableEntity> } = {};
  
  export function register<T extends ModifiableEntity>(type: Type<T>, setting: EmailModelSettings<T>): void {
    settings[type.typeName] = setting;
  }
  
  export function getEmailTemplates(ctx: ContextualItemsContext<Entity>): Promise<MenuItemBlock | undefined> | undefined {
  
    if (ctx.lites.length == 0)
      return undefined;
  
    if (EmailTemplateEntity.tryTypeInfo() == null)
      return undefined;
  
    if (ctx.container instanceof SearchControlLoaded && ctx.container.state.resultFindOptions?.systemTime)
      return undefined;
  
    return API.getEmailTemplates(ctx.queryDescription.queryKey, ctx.lites.length > 1 ? "Multiple" : "Single", { lite: (ctx.lites.length == 1 ? ctx.lites[0] : null) })
      .then(wts => {
        if (!wts.length)
          return undefined;
  
        return {
          header: EmailTemplateEntity.nicePluralName(),
          menuItems: wts.map(et =>
          ({
            fullText: getToString(et),
            menu: <Dropdown.Item data-operation={et.EntityType} onClick={() => handleMenuClick(et, ctx)}>
              <FontAwesomeIcon aria-hidden={true} icon="envelope" className="icon" />
              {getToString(et)}
            </Dropdown.Item>
          } as ContextualMenuItem)
          )
        } as MenuItemBlock;
      });
  }
  
  export function handleMenuClick(et: Lite<EmailTemplateEntity>, ctx: ContextualItemsContext<Entity>): void {
  
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
  
  export function createAndViewEmail(template: Lite<EmailTemplateEntity>, ...args: any[]): void {
  
    Operations.API.constructFromLite(template, EmailMessageOperation.CreateEmailFromTemplate, ...args)
      .then(pack => pack && Navigator.view(pack));
  }
  
  export namespace API {
    export function start(): Promise<void> {
      return ajaxPost({ url: "/api/asyncEmailSender/start" }, undefined);
    }
  
    export function stop(): Promise<void> {
      return ajaxPost({ url: "/api/asyncEmailSender/stop" }, undefined);
    }
  
    export function view(): Promise<AsyncEmailSenderState> {
      return ajaxGet({ url: "/api/asyncEmailSender/view" });
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
      return ajaxPost({ url: "/api/email/constructorType" }, emailModelEntity);
    }
  
    export function getEmailTemplates(queryKey: string, visibleOn: EmailTemplateVisibleOn, request: GetEmailTemplatesRequest): Promise<Lite<EmailTemplateEntity>[]> {
      return ajaxPost({ url: `/api/email/emailTemplates?queryKey=${queryKey}&visibleOn=${visibleOn}` }, request);
    }
  
    export function getAllTypes(signal?: AbortSignal): Promise<string[]> {
      return ajaxGet({ url: "/api/email/getAllTypes", signal });
    }
  
    export function getDefaultCulture(signal?: AbortSignal): Promise<CultureInfoEntity> {
      return ajaxGet({ url: "/api/email/getDefaultCulture", signal });
    }
  }
  
  export interface AsyncEmailSenderState {
    asyncSenderPeriod: number;
    running: boolean;
    initialDelayMilliseconds: number | null;
    machineName: string;
    isCancelationRequested: boolean;
    nextPlannedExecution: string;
    lastExecutionFinishedOn: string;
    queuedItems: number;
    currentProcessIdentifier: string;
  }

}

declare module '@framework/FindOptions' {

  export interface QueryDescription {
    emailTemplates?: Array<Lite<EmailTemplateEntity>> | "error";
  }
}

declare module '@framework/SearchControl/SearchControlLoaded' {

  export interface ShowBarExtensionOption {
    showMailButton?: boolean;
  }
}
