import * as React from 'react'
import { WorkflowEntity, WorkflowActivityEntity, WorkflowActivityMessage } from '../Signum.Workflow';
import { Finder } from '@framework/Finder'
import { TypeHelpClient } from '../../Signum.Eval/TypeHelp/TypeHelpClient'
import AutoLineModal from '@framework/AutoLineModal';
import { getToString } from '@framework/Signum.Entities';
import { TextAreaLine } from '../../../Signum/React/Lines';

interface WorkflowHelpComponentProps {
  typeName: string;
  mode: TypeHelpClient.TypeHelpMode;
}

export default function WorkflowHelpComponent(p : WorkflowHelpComponentProps): React.JSX.Element {

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
        ).join(" ||\n");

        AutoLineModal.show({
          type: { name: "string" },
          initialValue: text,
          customComponent: props => <TextAreaLine {...props} />,
          title: WorkflowActivityMessage.ActivityIs.niceToString(),
          message: "Copy to clipboard: Ctrl+C, ESC",
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
