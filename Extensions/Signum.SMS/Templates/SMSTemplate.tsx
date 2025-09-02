import * as React from 'react'
import { AutoLine, EntityTabRepeater, EntityCombo, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { SMSTemplateEntity, SMSCharactersMessage, SMSTemplateMessageEmbedded, SMSTemplateMessage } from '../Signum.SMS'
import { useForceUpdate, useAPI, useThrottle } from '@framework/Hooks';
import TemplateControls from '../../Signum.Templating/TemplateControls';
import { SMSClient } from '../SMSClient';
import QueryTokenEmbeddedBuilder from '../../Signum.UserAssets/Templates/QueryTokenEmbeddedBuilder'
import { SubTokensOptions } from '@framework/QueryToken';

export default function SMSTemplate(p: { ctx: TypeContext<SMSTemplateEntity> }): React.JSX.Element {
  var forceUpdate = useForceUpdate();
  var ctx = p.ctx.subCtx({ labelColumns: 3 });
  var ctx8 = p.ctx.subCtx({ labelColumns: 8 });
  return (
    <div>
      <AutoLine ctx={p.ctx.subCtx(a => a.name)} />
      <div className="row">
        <div className="col-sm-8">
          <AutoLine ctx={ctx.subCtx(a => a.isActive)} />
          <EntityLine ctx={ctx.subCtx(a => a.query)} onChange={forceUpdate} remove={ctx.value.messages.length > 0 || ctx.value.to != null} />
          <EntityCombo ctx={ctx.subCtx(a => a.model)} />
          <AutoLine ctx={ctx.subCtx(a => a.from)} />
          {ctx.value.query &&
            <QueryTokenEmbeddedBuilder
              ctx={ctx.subCtx(a => a.to)}
              queryKey={ctx.value.query.key}
              subTokenOptions={SubTokensOptions.CanElement}
              helpText="Expression pointing to an SMSOwnerData" />
          }
        </div>
        <div className="col-sm-4">
          <AutoLine ctx={ctx8.subCtx(a => a.messageLengthExceeded)} />
          <AutoLine ctx={ctx8.subCtx(a => a.certified)} />
          <AutoLine ctx={ctx8.subCtx(a => a.editableMessage)} />
          <AutoLine ctx={ctx8.subCtx(a => a.removeNoSMSCharacters)} onChange={forceUpdate} />
        </div>
      </div>

      {ctx.value.query &&
        <EntityTabRepeater ctx={ctx.subCtx(a => a.messages)} onChange={() => forceUpdate()} getComponent={sc =>
        <SMSTemplateMessageComponent ctx={sc} queryKey={ctx.value.query!.key!} removeNoSMSCharacters={ctx.value.removeNoSMSCharacters} invalidate={() => forceUpdate()} />
      }/>
      }
    </div>
  );
}


export interface SMSTemplateMessageComponentProps {
  ctx: TypeContext<SMSTemplateMessageEmbedded>;
  queryKey: string;
  removeNoSMSCharacters: boolean;
  invalidate: () => void;
}

export function SMSTemplateMessageComponent(p: SMSTemplateMessageComponentProps): React.JSX.Element {
  const forceUpdate = useForceUpdate();

  var throttleText = useThrottle(p.ctx.value.message ?? "", 1000);
  var remaining = useAPI(abort => SMSClient.API.getRemainingCharacters(throttleText, p.removeNoSMSCharacters), [throttleText, p.removeNoSMSCharacters], { avoidReset: true });

  const ec = p.ctx.subCtx({ labelColumns: { sm: 1 } });
  return (
    <div className="sf-sms-template-message">
      <EntityCombo ctx={ec.subCtx(e => e.cultureInfo)} onChange={p.invalidate} valueColumns={3} />
      <div>
        <TemplateControls queryKey={p.queryKey} forHtml={true} />
        <AutoLine ctx={ec.subCtx(a => a.message)} onChange={forceUpdate} formGroupStyle="SrOnly" formGroupHtmlAttributes={{ className: "pt-2" }} helpText={
            <span className={remaining == null ? "" : remaining < 0 ? "text-danger" : remaining < 20 ? "text-warning" : "text-success"}>
              {SMSTemplateMessage._0CharactersRemainingBeforeReplacements.niceToString(remaining == null ? "…" : remaining)}
            </span>
          } />
      </div>
    </div>
  );
}
