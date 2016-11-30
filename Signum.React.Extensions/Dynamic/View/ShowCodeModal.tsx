
import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar } from 'react-bootstrap'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals';
import { openModal, IModalProps } from '../../../../Framework/Signum.React/Scripts/Modals';
import { SelectorMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeInfo } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { TypeContext, StyleContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { DynamicViewMessage } from '../Signum.Entities.Dynamic'
import * as NodeUtils from './NodeUtils'
import { BaseNode } from './Nodes'


interface ShowCodeModalProps extends React.Props<ShowCodeModal>, IModalProps {
    typeName: string;
    node: BaseNode;
}

export default class ShowCodeModal extends React.Component<ShowCodeModalProps, { show: boolean }>  {

    constructor(props: ShowCodeModalProps) {
        super(props);

        this.state = { show: true };
    }

    handleCancelClicked = () => {
        this.setState({ show: false });
    }

    handleOnExited = () => {
        this.props.onExited!(undefined);
    }

    render() {
        
        return (
            <Modal bsSize="lg" onHide={this.handleCancelClicked} show={this.state.show} onExited={this.handleOnExited} className="sf-selector-modal">
                <Modal.Header closeButton={true}>
                    <h4 className="modal-title">
                        {this.props.typeName}
                    </h4>
                </Modal.Header>

                <Modal.Body>
                    <pre>
                        {renderFile(this.props.typeName, this.props.node)}
                    </pre>
                </Modal.Body>
            </Modal>
        );
    }

    static showCode(typeName: string, node: BaseNode): Promise<void> {
        return openModal<void>(<ShowCodeModal typeName={typeName} node={node} />);
    }
}


function renderFile(typeName: string, node: BaseNode): string {

    var ctx = new NodeUtils.CodeContext();
    ctx.usedNames = {};
    ctx.ctxName = "ctx";

    var text = NodeUtils.renderCode(node, ctx).indent(12);

    return (
        `
import * as React from 'react'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import { ${typeName}Entity } from '../[your namespace]'
import { ValueLine, EntityLine, RenderEntity, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater, EntityCheckboxList, EntityTabRepeater, TypeContext, EntityTable } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl, ValueSearchControl } from '../../../../Framework/Signum.React/Scripts/Search'

export default class ${typeName} extends React.Component<{ ctx: TypeContext<${typeName}Entity> }, void> {

    render() {
        var ctx = this.props.ctx;
        return (
${text}
        );
    }
}`
    );

}





