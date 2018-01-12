
import * as React from 'react'
import { Modal, ModalBody, ModalHeader, ButtonToolbar } from 'reactstrap'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals';
import { openModal, IModalProps } from '../../../../Framework/Signum.React/Scripts/Modals';
import { SelectorMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeInfo } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { DynamicViewMessage } from '../Signum.Entities.Dynamic'
import * as NodeUtils from './NodeUtils'
import { BaseNode } from './Nodes'


interface NodeSelectorModalProps extends React.Props<NodeSelectorModal>, IModalProps {
}

export default class NodeSelectorModal extends React.Component<NodeSelectorModalProps, { isOpen: boolean }>  {

    constructor(props: NodeSelectorModalProps) {
        super(props);

        this.state = { isOpen: true };
    }


    selectedValue: any;
    handleButtonClicked = (val: any) => {
        this.selectedValue = val;
        this.setState({ isOpen: false });

    }

    handleCancelClicked = () => {
        this.setState({ isOpen: false });
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

        return <Modal size="lg" toggle={this.handleCancelClicked} isOpen={this.state.isOpen} onExit={this.handleOnExited} className="sf-selector-modal">
            <ModalHeader toggle={this.handleCancelClicked} >

                <h4 className="modal-title">
                    {DynamicViewMessage.SelectATypeOfComponent.niceToString()}
                </h4>
            </ModalHeader>

            <ModalBody>
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
            </ModalBody>
        </Modal>;
    }

    static chooseElement(parentNode: string): Promise<NodeUtils.NodeOptions<BaseNode> | undefined> {

        var o = NodeUtils.registeredNodes[parentNode];
        if (o.validChild)
            return Promise.resolve(NodeUtils.registeredNodes[o.validChild]);

        return openModal<NodeUtils.NodeOptions<BaseNode>>(<NodeSelectorModal />);
    }
}


