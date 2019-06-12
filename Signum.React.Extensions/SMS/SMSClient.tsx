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
import { SMSTemplateMessageEmbedded, SMSMessageEntity, SMSTemplateEntity, SMSSendPackageEntity, SMSUpdatePackageEntity, MultipleSMSModel } from './Signum.Entities.SMS'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import * as QuickLinks from '@framework/QuickLinks'
import { ImportRoute } from "@framework/AsyncImport";
import { ModifiableEntity } from "@framework/Signum.Entities";
import { ContextualItemsContext, MenuItemBlock } from "@framework/SearchControl/ContextualItems";
import { ModelEntity } from "@framework/Signum.Entities";
import { QueryRequest } from "@framework/FindOptions";
import * as ContexualItems from '@framework/SearchControl/ContextualItems'
import * as DynamicClientOptions from '../Dynamic/DynamicClientOptions';
import { DropdownItem } from '@framework/Components';
import { registerExportAssertLink } from '../UserAssets/UserAssetClient';


export function start(options: { routes: JSX.Element[] }) {

  registerToString(SMSTemplateMessageEmbedded, a => a.cultureInfo == undefined ? JavascriptMessage.newEntity.niceToString() : a.cultureInfo.englishName!);

  Navigator.addSettings(new EntitySettings(SMSMessageEntity, e => import('./Templates/SMSMessage')));
  Navigator.addSettings(new EntitySettings(SMSTemplateEntity, e => import('./Templates/SMSTemplate')));
  Navigator.addSettings(new EntitySettings(SMSSendPackageEntity, e => import('./Templates/SMSSendPackage')));
  Navigator.addSettings(new EntitySettings(SMSUpdatePackageEntity, e => import('./Templates/SMSUpdatePackage')));
  Navigator.addSettings(new EntitySettings(MultipleSMSModel, e => import('./Templates/MultipleSMS')));

  /*
  Operations.addSettings(new EntityOperationSettings(EmailMessageOperation.ReadyToSend, {
    contextual: { isVisible: em => true },
    contextualFromMany: { isVisible: em => true }
  }));
  */
}

export module API {
  /*
  export function getEmailTemplates(queryKey: string, visibleOn: EmailTemplateVisibleOn, request: GetEmailTemplatesRequest): Promise<Lite<EmailTemplateEntity>[]> {
    return ajaxPost<Lite<EmailTemplateEntity>[]>({ url: `~/api/email/emailTemplates?queryKey=${queryKey}&visibleOn=${visibleOn}` }, request);
  }
  */
}
