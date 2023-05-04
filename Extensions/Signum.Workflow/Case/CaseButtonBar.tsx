
import * as React from 'react'
import { DateTime } from 'luxon'
import { TypeContext, EntityFrame } from '@framework/TypeContext'
import { PropertyRoute, ReadonlyBinding } from '@framework/Reflection'
import { ValueLine } from '@framework/Lines'
import { EntityPack, getToString } from '@framework/Signum.Entities'
import { ButtonBar } from '@framework/Frames/ButtonBar'
import { CaseActivityEntity, CaseActivityMessage, WorkflowActivityEntity } from '../Signum.Workflow'

interface CaseButtonBarProps {
  frame: EntityFrame;
  pack: EntityPack<CaseActivityEntity>;
}

export default function CaseButtonBar(p : CaseButtonBarProps){
  var ca = p.pack.entity;

  if (ca.doneDate != null) {
    return (
      <div className="workflow-buttons">
        {CaseActivityMessage.DoneBy0On1.niceToString().formatHtml(
          <strong>{ca.doneBy && getToString(ca.doneBy)}</strong>,
          ca.doneDate && <strong>{DateTime.fromISO(ca.doneDate).toFormat("FFF")} ({DateTime.fromISO(ca.doneDate).toRelative()})</strong>)
        }
      </div>
    );
  }

  const ctx = new TypeContext(undefined, undefined, PropertyRoute.root(CaseActivityEntity), new ReadonlyBinding(ca, "act"));
  return (
    <div>
      <div className="workflow-buttons">
        <ButtonBar frame={p.frame} pack={p.pack} />
        <ValueLine ctx={ctx.subCtx(a => a.note)} formGroupStyle="None" placeholderLabels={true} />
      </div>
      {(ca.workflowActivity as WorkflowActivityEntity).userHelp &&
        <UserHelpComponent activity={ca.workflowActivity as WorkflowActivityEntity} />}
    </div>
  );
}

interface UserHelpProps {
  activity: WorkflowActivityEntity;
}

export function UserHelpComponent(p : UserHelpProps){

  var [open, setOpen] = React.useState(false);

  function handleHelpClick(e: React.MouseEvent<any>) {
    e.preventDefault();
    setOpen(!open);
  }

  return (
    <div style={{ marginTop: "10px" }}>
      <a href="#" onClick={handleHelpClick} className="case-help-button">
        {open ?
          CaseActivityMessage.HideHelp.niceToString() :
          CaseActivityMessage.ShowHelp.niceToString()}
      </a>
      {open &&
        <div dangerouslySetInnerHTML={{ __html: p.activity.userHelp! }} />}
    </div>
  );
}
