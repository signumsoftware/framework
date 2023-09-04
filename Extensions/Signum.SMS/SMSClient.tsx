import { RouteObject } from 'react-router'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Lite, registerToString, JavascriptMessage } from '@framework/Signum.Entities'
import { SMSTemplateMessageEmbedded, SMSMessageEntity, SMSTemplateEntity, SMSSendPackageEntity, SMSUpdatePackageEntity, MultipleSMSModel, ISMSOwnerEntity } from './Signum.SMS'
import * as QuickLinks from '@framework/QuickLinks'

export var allTypes: string[] = [];

export function start(options: { routes: RouteObject[] }) {

  registerToString(SMSTemplateMessageEmbedded, a => a.cultureInfo == undefined ? JavascriptMessage.newEntity.niceToString() : a.cultureInfo.englishName!);

  Navigator.addSettings(new EntitySettings(SMSMessageEntity, e => import('./Templates/SMSMessage')));
  Navigator.addSettings(new EntitySettings(SMSTemplateEntity, e => import('./Templates/SMSTemplate')));
  Navigator.addSettings(new EntitySettings(SMSSendPackageEntity, e => import('./Templates/SMSSendPackage')));
  Navigator.addSettings(new EntitySettings(SMSUpdatePackageEntity, e => import('./Templates/SMSUpdatePackage')));
  Navigator.addSettings(new EntitySettings(MultipleSMSModel, e => import('./Templates/MultipleSMS')));

  var cachedAllTypes: Promise<string[]>;
  QuickLinks.registerGlobalQuickLink(entityType => (cachedAllTypes ??= API.getAllTypes())
    .then(allTypes => [new QuickLinks.QuickLinkAction("smsMessages", () => SMSMessageEntity.nicePluralName(), ctx => getSMSMessages(ctx.lite),
      {
        isVisible: allTypes.contains(entityType) && !Navigator.isReadOnly(SMSMessageEntity),
        icon: "comment-sms",
        iconColor: "green"
      }
    )]));
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
    return ajaxPost({ url: `/api/sms/remainingCharacters` }, { message, removeNoSMSCharacters});
  }

  export function getAllTypes(signal?: AbortSignal): Promise<string[]> {
    return ajaxGet({ url: "/api/sms/getAllTypes", signal });
  }
}
