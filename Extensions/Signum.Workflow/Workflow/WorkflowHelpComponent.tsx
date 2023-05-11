import * as React from 'react'
import { WorkflowEntity, WorkflowActivityEntity, WorkflowActivityMessage } from '../Signum.Workflow';
import * as Finder from '@framework/Finder'
import { TypeHelpMode } from '../../Signum.Eval/TypeHelp/TypeHelpClient'
import ValueLineModal from '@framework/ValueLineModal';
import { getToString } from '@framework/Signum.Entities';

interface WorkflowHelpComponentProps {
  typeName: string;
  mode: TypeHelpMode;
}

export default function WorkflowHelpComponent(p : WorkflowHelpComponentProps){

  function handleActivityIsClick() {
    Finder.find<WorkflowEntity>({
      queryName: WorkflowEntity,
      filterOptions: [{ token: WorkflowEntity.token(a => a.entity.mainEntityType!.cleanName), value: p.typeName}],
    }).then(w => {
      if (!w)
        return;

      Finder.findMany<WorkflowActivityEntity>({
        queryName: WorkflowActivityEntity,
        filterOptions: [{ token: WorkflowActivityEntity.token(e => e.lane!.pool!.workflow), value: w}]
      }).then(acts => {

        if (!acts)
          return;

        var text = acts.map(a => p.mode == "CSharp" ?
          `WorkflowActivityInfo.Current.Is("${getToString(w)}", "${getToString(a)}")` :
          `modules.WorkflowClient.inWorkflow(ctx, "${getToString(w)}", "${getToString(a)}")`
        ).join(" ||\r\n");

        ValueLineModal.show({
          type: { name: "string" },
          initialValue: text,
          valueLineType: "TextArea",
          title: WorkflowActivityMessage.ActivityIs.niceToString(),
          message: "Copy to clipboard: Ctrl+C, ESC",
          initiallyFocused: true,
          valueHtmlAttributes: { style: { height: "200px" } },
        });

      });

    });
  }
  return (
    <input type="button"
      className="btn btn-success btn-sm sf-button"
      style={{ marginBottom: "3px" }}
      value={WorkflowActivityMessage.ActivityIs.niceToString()}
      onClick={() => handleActivityIsClick()} />
  );
}
