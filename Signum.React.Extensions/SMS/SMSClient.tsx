import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Lite, Entity, registerToString, JavascriptMessage, toLite } from '@framework/Signum.Entities'
import { EntityOperationSettings } from '@framework/Operations'
import { PseudoType, Type, getTypeName, getAllTypes } from '@framework/Reflection'
import * as Operations from '@framework/Operations'
import { SMSTemplateMessageEmbedded, SMSMessageEntity, SMSTemplateEntity, SMSSendPackageEntity, SMSUpdatePackageEntity, MultipleSMSModel, SMSMessageOperation, ISMSOwnerEntity } from './Signum.Entities.SMS'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import * as QuickLinks from '@framework/QuickLinks'
import { ImportRoute } from "@framework/AsyncImport";
import { ModifiableEntity } from "@framework/Signum.Entities";
import { ContextualItemsContext, MenuItemBlock } from "@framework/SearchControl/ContextualItems";
import { ModelEntity } from "@framework/Signum.Entities";
import { QueryRequest, ColumnOption } from "@framework/FindOptions";
import * as ContexualItems from '@framework/SearchControl/ContextualItems'
import * as DynamicClientOptions from '../Dynamic/DynamicClientOptions';
import { registerExportAssertLink } from '../UserAssets/UserAssetClient';
import { TypeEntity } from '@framework/Signum.Entities.Basics';

export var allTypes: string[] = [];

export function start(options: { routes: JSX.Element[] }) {

  registerToString(SMSTemplateMessageEmbedded, a => a.cultureInfo == undefined ? JavascriptMessage.newEntity.niceToString() : a.cultureInfo.englishName!);

  Navigator.addSettings(new EntitySettings(SMSMessageEntity, e => import('./Templates/SMSMessage')));
  Navigator.addSettings(new EntitySettings(SMSTemplateEntity, e => import('./Templates/SMSTemplate')));
  Navigator.addSettings(new EntitySettings(SMSSendPackageEntity, e => import('./Templates/SMSSendPackage')));
  Navigator.addSettings(new EntitySettings(SMSUpdatePackageEntity, e => import('./Templates/SMSUpdatePackage')));
  Navigator.addSettings(new EntitySettings(MultipleSMSModel, e => import('./Templates/MultipleSMS')));

  API.getAllTypes().then(types => {
    allTypes = types;
    QuickLinks.registerGlobalQuickLink(ctx => new QuickLinks.QuickLinkAction("smsMessages",
      () => SMSMessageEntity.nicePluralName(),
      e => getSMSMessages(ctx.lite),
      {
        isVisible: allTypes.contains(ctx.lite.EntityType) && !Navigator.isReadOnly(SMSMessageEntity),
        icon: "comment-sms",
        iconColor: "green"
      }));
  });
}

function getSMSMessages(referred: Lite<ISMSOwnerEntity>) {
  return Finder.find(
    {
      queryName: SMSMessageEntity,
      filterOptions: [{ token: "Referred", value: referred}],
      columnOptionsMode: "Remove",
      columnOptions: [{ token: "Referred" }],
    });
}

export module API {
 
  export function getRemainingCharacters(message: string, removeNoSMSCharacters: boolean,): Promise<number> {
    return ajaxPost({ url: `~/api/sms/remainingCharacters` }, { message, removeNoSMSCharacters});
  }

  export function getAllTypes(signal?: AbortSignal): Promise<string[]> {
    return ajaxGet({ url: "~/api/sms/getAllTypes", signal });
  }
}
