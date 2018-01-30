
import * as React from 'react'
import { Modal, ModalHeader, ModalBody, ButtonToolbar } from 'reactstrap'
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

    handleOnClosed = () => {
        this.props.onExited!(undefined);
    }

    render() {
        
        return (
            <Modal size="lg" toggle={this.handleCancelClicked} isOpen={this.state.show} onClosed={this.handleOnClosed} className="sf-selector-modal">
                <ModalHeader toggle={this.handleCancelClicked}>
                    <h4 className="modal-title">
                        {this.props.typeName + "Component code"}
                    </h4>
                </ModalHeader>

                <ModalBody>
                    <pre>
                        {renderFile(this.props.typeName, this.props.node)}
                    </pre>
                </ModalBody>
            </Modal>
        );
    }

    static showCode(typeName: string, node: BaseNode): Promise<void> {
        return openModal<void>(<ShowCodeModal typeName={typeName} node={node} />);
    }
}


function renderFile(typeName: string, node: BaseNode): string {

    var cc = new NodeUtils.CodeContext("ctx", [], {}, []);

    var text = NodeUtils.renderCode(node, cc).indent(12);

    return (
        `
import * as React from 'react'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import { getMixin } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ${typeName}Entity } from '../[your namespace]'
import { ValueLine, EntityLine, RenderEntity, EntityCombo, EntityList, EntityDetail, EntityStrip, 
         EntityRepeater, EntityCheckboxList, EntityTabRepeater, TypeContext, EntityTable } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl, ValueSearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
${Dic.getValues(cc.imports.toObjectDistinct(a => a)).join("\n")}

export default class ${typeName}Component extends React.Component<{ ctx: TypeContext<${typeName}Entity> }> {

    render() {
        const ctx = this.props.ctx;
${Dic.map(cc.assignments, (k, v) => `const ${k} = ${v};`).join("\n").indent(8)}
        return (
${text}
        );
    }
}`
    );

}





