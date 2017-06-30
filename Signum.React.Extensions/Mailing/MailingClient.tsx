import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { Button, OverlayTrigger, Tooltip, MenuItem } from "react-bootstrap"
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, registerToString, JavascriptMessage } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName  } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { EmailMessageEntity, EmailTemplateMessageEmbedded, EmailMasterTemplateEntity, EmailMasterTemplateMessageEmbedded, EmailMessageOperation, EmailPackageEntity, EmailRecipientEntity, EmailConfigurationEmbedded, EmailTemplateEntity, AsyncEmailSenderPermission } from './Signum.Entities.Mailing'
import { SmtpConfigurationEntity, Pop3ConfigurationEntity, Pop3ReceptionEntity, Pop3ReceptionExceptionEntity, EmailAddressEmbedded } from './Signum.Entities.Mailing'
import { NewsletterEntity, NewsletterDeliveryEntity, SendEmailTaskEntity, SystemEmailEntity, EmailTemplateVisibleOn } from './Signum.Entities.Mailing'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'
import { ImportRoute } from "../../../Framework/Signum.React/Scripts/AsyncImport";
import { ModifiableEntity } from "../../../Framework/Signum.React/Scripts/Signum.Entities";

require("./Mailing.css");

export function start(options: { routes: JSX.Element[], smtpConfig: boolean, newsletter: boolean, pop3Config: boolean, sendEmailTask: boolean, quickLinksFrom: PseudoType[] | undefined }) {
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
    Navigator.addSettings(new EntitySettings(EmailRecipientEntity, e => import('./Templates/EmailRecipient')));
    Navigator.addSettings(new EntitySettings(EmailAddressEmbedded, e => import('./Templates/EmailAddress')));
    Navigator.addSettings(new EntitySettings(EmailConfigurationEmbedded, e => import('./Templates/EmailConfiguration')));

    Operations.addSettings(new EntityOperationSettings(EmailMessageOperation.CreateMailFromTemplate, {
        onClick: (ctx) => {
            Finder.find({ queryName: ctx.entity.query!.key }).then(lite => {
                if (!lite)
                    return;
                Navigator.API.fetchAndForget(lite).then(entity =>
                    ctx.defaultClick(entity))
                    .done();
            }).done();
        }
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
                return new QuickLinks.QuickLinkExplore({ queryName: EmailMessageEntity, parentColumn: "Target", parentValue: ctx.lite });

            return undefined;
        });
    }

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
    
    export function getConstructorType(systemEmailTemplate: SystemEmailEntity): Promise<string> {
        return ajaxPost<string>({ url: "~/api/mail/constructorType" }, systemEmailTemplate);
    }

    export function getEmailTemplates(queryKey: string, visibleOn: EmailTemplateVisibleOn): Promise<Lite<EmailTemplateEntity>[]> {
        return ajaxGet<Lite<EmailTemplateEntity>[]>({ url: `~/api/mail/emailTemplates?queryKey=${queryKey}&visibleOn=${visibleOn}` });
    }
}


export interface AsyncEmailSenderState {
    AsyncSenderPeriod: number;
    Running: boolean;
    IsCancelationRequested: boolean;
    NextPlannedExecution: string;
    QueuedItems: number;
    CurrentProcessIdentifier: string;
}
