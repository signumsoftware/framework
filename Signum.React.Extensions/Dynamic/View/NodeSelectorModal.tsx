import * as React from 'react'
import { Dic } from '@framework/Globals';
import { openModal, IModalProps } from '@framework/Modals';
import { SelectorMessage } from '@framework/Signum.Entities'
import { TypeInfo } from '@framework/Reflection'
import { DynamicViewMessage } from '../Signum.Entities.Dynamic'
import * as NodeUtils from './NodeUtils'
import { BaseNode } from './Nodes'
import { Modal } from '@framework/Components';
import { ModalHeaderButtons } from '@framework/Components/Modal';


interface NodeSelectorModalProps extends React.Props<NodeSelectorModal>, IModalProps {
}

export default class NodeSelectorModal extends React.Component<NodeSelectorModalProps, { show: boolean }>  {

    constructor(props: NodeSelectorModalProps) {
        super(props);

        this.state = { show: true };
    }


    selectedValue: any;
    handleButtonClicked = (val: any) => {
        this.selectedValue = val;
        this.setState({ show: false });

    }

    handleCancelClicked = () => {
        this.setState({ show: false });
    }

    handleOnExited = () => {
        this.props.onExited!(this.selectedValue);
    }

    render() {

        const columnWidth = "200px";

        const nodes = Dic.getValues(NodeUtils.registeredNodes);

        var columns = nodes
            .filter(n => n.group != null)
            .groupBy(n => n.group!)
            .groupsOf(nodes.length / 3, g => g.elements.length);

        return (
            <Modal size="lg" onHide={this.handleCancelClicked} show={this.state.show} onExited={this.handleOnExited} className="sf-selector-modal">
                <ModalHeaderButtons onClose={this.handleCancelClicked} >
                        {DynamicViewMessage.SelectATypeOfComponent.niceToString()}
                </ModalHeaderButtons>
                <div className="modal-body">
                    <div className="row">
                        {
                            columns.map((c, i) =>
                                <div key={i} className={"col-sm-" + (12 / columns.length)}>
                                    {
                                        c.map(gr => <fieldset key={gr.key}>
                                            <legend>{gr.key}</legend>
                                            {
                                                gr.elements.orderBy(n => n.order!).map(n =>
                                                    <button key={n.kind} type="button" onClick={() => this.handleButtonClicked(n)}
                                                        className="sf-chooser-button sf-close-button btn btn-light">
                                                        {n.kind}
                                                    </button>)
                                            }
                                        </fieldset>)
                                    }
                                </div>)
                        }
                    </div>
                </div>
            </Modal>
        );
    }

    static chooseElement(parentNode: string): Promise<NodeUtils.NodeOptions<BaseNode> | undefined> {

        var o = NodeUtils.registeredNodes[parentNode];
        if (o.validChild)
            return Promise.resolve(NodeUtils.registeredNodes[o.validChild]);

        return openModal<NodeUtils.NodeOptions<BaseNode>>(<NodeSelectorModal />);
    }
}


