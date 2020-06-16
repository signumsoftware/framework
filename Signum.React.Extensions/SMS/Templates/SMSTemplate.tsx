import * as React from 'react'
import { ValueLine, EntityTabRepeater, EntityCombo, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { SMSTemplateEntity, SMSCharactersMessage, SMSTemplateMessageEmbedded, SMSTemplateMessage } from '../Signum.Entities.SMS'
import { useForceUpdate, useAPI, useThrottle } from '@framework/Hooks';
import ValueLineModal from '@framework/ValueLineModal';
import TemplateControls from '../../Templating/TemplateControls';
import * as SMSClient from '../SMSClient';
import QueryTokenEmbeddedBuilder from '../../UserAssets/Templates/QueryTokenEmbeddedBuilder'
import { SubTokensOptions } from '@framework/FindOptions';

export default function SMSTemplate(p: { ctx: TypeContext<SMSTemplateEntity> }) {
  var forceUpdate = useForceUpdate();
  var ctx = p.ctx.subCtx({ labelColumns: 3 });
  var ctx8 = p.ctx.subCtx({ labelColumns: 8 });
  return (
    <div>
      <ValueLine ctx={p.ctx.subCtx(a => a.name)} />
      <div className="row">
        <div className="col-sm-8">
          <ValueLine ctx={ctx.subCtx(a => a.isActive)} />
          <EntityLine ctx={ctx.subCtx(a => a.query)} onChange={forceUpdate} remove={ctx.value.messages.length > 0 || ctx.value.to != null} />
          <EntityCombo ctx={ctx.subCtx(a => a.model)} />
          <ValueLine ctx={ctx.subCtx(a => a.from)} />
          {ctx.value.query &&
            <QueryTokenEmbeddedBuilder
              ctx={ctx.subCtx(a => a.to)}
              queryKey={ctx.value.query.key}
              subTokenOptions={SubTokensOptions.CanElement}
              helpText="Expression pointing to an SMSOwnerData" />
          }
        </div>
        <div className="col-sm-4">
          <ValueLine ctx={ctx8.subCtx(a => a.messageLengthExceeded)} />
          <ValueLine ctx={ctx8.subCtx(a => a.certified)} />
          <ValueLine ctx={ctx8.subCtx(a => a.editableMessage)} />
          <ValueLine ctx={ctx8.subCtx(a => a.removeNoSMSCharacters)} onChange={forceUpdate} />
        </div>
      </div>

      {ctx.value.query &&
        <EntityTabRepeater ctx={ctx.subCtx(a => a.messages)} onChange={() => forceUpdate()} getComponent={(sc: TypeContext<SMSTemplateMessageEmbedded>) =>
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

export function SMSTemplateMessageComponent(p: SMSTemplateMessageComponentProps) {
  const forceUpdate = useForceUpdate();

  function handleOnInsert(newCode: string) {
    ValueLineModal.show({
      type: { name: "string" },
      initialValue: newCode,
      title: "Template",
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
    }).done();
  }


  var throttleText = useThrottle(p.ctx.value.message ?? "", 1000);
  var remaining = useAPI(abort => SMSClient.API.getRemainingCharacters(throttleText, p.removeNoSMSCharacters), [throttleText, p.removeNoSMSCharacters], { avoidReset: true });

  const ec = p.ctx.subCtx({ labelColumns: { sm: 1 } });
  return (
    <div className="sf-sms-template-message">
      <EntityCombo ctx={ec.subCtx(e => e.cultureInfo)} onChange={p.invalidate} valueColumns={3} />
      <div>
        <TemplateControls queryKey={p.queryKey} onInsert={handleOnInsert} forHtml={true} />
        <ValueLine ctx={ec.subCtx(a => a.message)} onChange={forceUpdate} formGroupStyle="SrOnly" formGroupHtmlAttributes={{ className: "pt-2" }} helpText={
            <span className={remaining == null ? "" : remaining < 0 ? "text-danger" : remaining < 20 ? "text-warning" : "text-success"}>
              {SMSTemplateMessage._0CharactersRemainingBeforeReplacements.niceToString(remaining == null ? "…" : remaining)}
            </span>
          } />
      </div>
    </div>
  );
}
