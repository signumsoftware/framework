
import * as React from 'react'
import { Modal, ModalHeader, ModalBody, ButtonToolbar } from 'reactstrap'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { openModal, IModalProps } from '../../../Framework/Signum.React/Scripts/Modals';
import { SearchMessage, JavascriptMessage, Lite, Entity } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { getTypeInfo, Type } from '../../../Framework/Signum.React/Scripts/Reflection'
import { TreeViewerMessage, TreeEntity } from './Signum.Entities.Tree'
import * as TreeClient from './TreeClient'
import { TreeNode } from './TreeClient'
import { TreeViewer } from './TreeViewer'
import { FilterOption } from "../../../Framework/Signum.React/Scripts/FindOptions";


interface TreeModalProps extends React.Props<TreeModal>, IModalProps {
    typeName: string;
    filterOptions: FilterOption[];
    title?: string;
}

export default class TreeModal extends React.Component<TreeModalProps, { show: boolean; }>  {

    constructor(props: any) {
        super(props);

        this.state = {
            show: true
        };
    }

    selectedNode?: TreeNode;
    okPressed: boolean;

    handleSelectedNode = (selected: TreeNode) => {
        this.selectedNode = selected;
        this.forceUpdate();
    }

    handleOkClicked = () => {
        this.okPressed = true;
        this.setState({ show: false });
    }

    handleCancelClicked = () => {
        this.okPressed = false;
        this.setState({ show: false });
    }

    handleOnExited = () => {
        this.props.onExited!(this.okPressed ? this.selectedNode : null);
    }

    handleDoubleClick = (selectedNode: TreeNode, e: React.MouseEvent<any>) => {
        e.preventDefault();
        this.selectedNode = selectedNode;
        this.okPressed = true;
        this.setState({ show: false });
    }

    treeView?: TreeViewer;

    render() {

        const okEnabled = this.selectedNode != null;

        return (
            <Modal size="lg" isOpen={this.state.show} onExit={this.handleOnExited}>
                <ModalHeader>
                    <div className="btn-toolbar" style={{ float: "right" }}>
                        <button className="btn btn-primary sf-entity-button sf-close-button sf-ok-button" disabled={!okEnabled} onClick={this.handleOkClicked}>
                            {JavascriptMessage.ok.niceToString()}
                        </button>

                        <button className="btn btn-default sf-entity-button sf-close-button sf-cancel-button" onClick={this.handleCancelClicked}>{JavascriptMessage.cancel.niceToString()}</button>
                    </div>
                    <h4>
                        <span className="sf-entity-title"> {this.props.title || getTypeInfo(this.props.typeName).nicePluralName}</span>
                        &nbsp;
                        <a className="sf-popup-fullscreen" href="" onClick={(e) => this.treeView && this.treeView.handleFullScreenClick(e)}>
                            <span className="glyphicon glyphicon-new-window"></span>
                        </a>
                    </h4>
                </ModalHeader>

                <ModalBody>
                    <TreeViewer
                        filterOptions={this.props.filterOptions}
                        typeName={this.props.typeName}
                        onSelectedNode={this.handleSelectedNode}
                        onDoubleClick={this.handleDoubleClick}
                        ref={tv => this.treeView = tv!}
                    />
                </ModalBody>
            </Modal>
        );
    }

    static open(typeName: string, filterOptions: FilterOption[],  options?: TreeClient.TreeModalOptions): Promise<Lite<TreeEntity> | undefined> {
        return openModal<TreeNode>(<TreeModal
            filterOptions={filterOptions}
            typeName={typeName}
            title={options && options.title}
        />)
            .then(tn => tn && tn.lite);
    }
}



