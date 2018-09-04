import * as React from 'react'
import { classes } from '@framework/Globals'
import { Lite } from '@framework/Signum.Entities';
import { StyleContext } from '@framework/TypeContext'
import { WorkflowEntity, WorkflowActivityEntity, WorkflowActivityMessage } from '../Signum.Entities.Workflow';
import * as Finder from '@framework/Finder'
import * as WorkflowClient from '../WorkflowClient'
import { TypeHelpMode } from '../../TypeHelp/TypeHelpClient'
import ValueLineModal from '@framework/ValueLineModal';

interface WorkflowHelpComponentProps {
    typeName: string;
    mode: TypeHelpMode;
}

export default class WorkflowHelpComponent extends React.Component<WorkflowHelpComponentProps> {

    render() {
        return (
            <input type="button"
                className="btn btn-success btn-sm sf-button"
                style={{ marginBottom: "3px" }}
                value={WorkflowActivityMessage.ActivityIs.niceToString()}
                onClick={() => this.handleActivityIsClick()} />
        );
    }

    handleActivityIsClick = () => {

        Finder.find<WorkflowEntity>({
            queryName: WorkflowEntity,
            parentToken: "Entity.MainEntityType.CleanName",
            parentValue: this.props.typeName,
        }).then(w => {

            if (!w)
                return;

            Finder.findMany<WorkflowActivityEntity>({
                queryName: WorkflowActivityEntity,
                parentToken: "Workflow",
                parentValue: w
            }).then(acts => {

                if (!acts)
                    return;

                var text = acts.map(a => this.props.mode == "CSharp" ?
                    `WorkflowActivityInfo.Current.Is("${w.toStr}", "${a.toStr}")` :
                    `modules.WorkflowClient.inWorkflow(ctx, "${w.toStr}", "${a.toStr}")`                     
                    ).join(" ||\r\n");

                ValueLineModal.show({
                    type: { name: "string" },
                    initialValue: text,
                    valueLineType: "TextArea",
                    title: WorkflowActivityMessage.ActivityIs.niceToString(),
                    message: "Copy to clipboard: Ctrl+C, ESC",
                    initiallyFocused: true,
                    valueHtmlAttributes: { style: { height: "200px" } },
                }).done();

            }).done();

        }).done();
    }
}
