import * as React from 'react'
import { ValueLine, EntityLine, EntityCombo, FormGroup } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { AlertEntity, AlertMessage } from '../Signum.Entities.Alerts'
import * as AlertsClient from '../AlertsClient'
import { useForceUpdate } from '@framework/Hooks';

export default function Alert(p: { ctx: TypeContext<AlertEntity> }) {

  const forceUpdate = useForceUpdate();

  const [edit, setEdit] = React.useState<boolean>(false);

  const ctx = p.ctx.subCtx({ labelColumns: { sm: 2 } });

  return (
    <div>
      {!ctx.value.isNew &&
        <div>
          <EntityLine ctx={ctx.subCtx(e => e.createdBy)} readOnly={true} />
          <ValueLine ctx={ctx.subCtx(e => e.creationDate)} readOnly={true} />
        </div>
      }

      <div className="row">
        <div className="col-sm-6">
          {ctx.value.target && <EntityLine ctx={ctx.subCtx(n => n.target)} readOnly={true} labelColumns={4} />}
        </div>
        <div className="col-sm-6">
          {ctx.value.linkTarget && <EntityLine ctx={ctx.subCtx(n => n.linkTarget)} readOnly={true} labelColumns={4} />}
          {ctx.value.groupTarget && <EntityLine ctx={ctx.subCtx(n => n.groupTarget)} readOnly={true} labelColumns={4} />}
        </div>
      </div>

    
      <EntityLine ctx={ctx.subCtx(n => n.recipient)} />
      <hr />

      <EntityCombo ctx={ctx.subCtx(n => n.alertType)} onChange={forceUpdate} />
      <ValueLine ctx={ctx.subCtx(n => n.alertDate)} />
      <ValueLine ctx={ctx.subCtx(n => n.titleField)} label={AlertMessage.Title.niceToString()} valueHtmlAttributes={{ placeholder: (ctx.value.alertType && AlertsClient.getTitle(null, ctx.value.alertType)) ?? undefined }} />
      {
        !ctx.value.isNew && !edit ?
          <FormGroup ctx={ctx.subCtx(n => n.titleField)} label={AlertMessage.Text.niceToString()} >
            <div style={{ whiteSpace: "pre-wrap" }}>
              {AlertsClient.format(ctx.value.textField || ctx.value.textFromAlertType || "", ctx.value)}
              <br />
              <a href="#" className="text-muted" onClick={e => { e.preventDefault(); setEdit(true) }}>Edit</a>
            </div>
          </FormGroup>
          :
          <ValueLine ctx={ctx.subCtx(n => n.textField)} label={AlertMessage.Text.niceToString()} valueHtmlAttributes={{ style: { height: "180px" } }} />
      }
      {ctx.value.state == "Attended" &&
        <div>
          <hr />
          <ValueLine ctx={ctx.subCtx(e => e.attendedDate)} readOnly={true} />
          <EntityLine ctx={ctx.subCtx(e => e.attendedBy)} readOnly={true} />
        </div>
      }
    </div>
  );
}
