
import * as React from 'react'
import * as moment from 'moment'
import { TypeContext, EntityFrame } from '@framework/TypeContext'
import { PropertyRoute, ReadonlyBinding } from '@framework/Reflection'
import { ValueLine } from '@framework/Lines'
import { EntityPack } from '@framework/Signum.Entities'
import { ButtonBar } from '@framework/Frames/ButtonBar'
import { CaseActivityEntity, CaseActivityMessage, WorkflowActivityEntity } from '../Signum.Entities.Workflow'
import { DynamicViewMessage } from '../../Dynamic/Signum.Entities.Dynamic'

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
          <strong>{ca.doneBy && ca.doneBy.toStr}</strong>,
          ca.doneDate && <strong>{moment(ca.doneDate).format("L LT")} ({moment(ca.doneDate).fromNow()})</strong>)
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
          DynamicViewMessage.HideHelp.niceToString() :
          DynamicViewMessage.ShowHelp.niceToString()}
      </a>
      {open &&
        <div dangerouslySetInnerHTML={{ __html: p.activity.userHelp! }} />}
    </div>
  );
}
