
import * as React from 'react'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { openModal, IModalProps } from '../../../Framework/Signum.React/Scripts/Modals';
import { SearchMessage, JavascriptMessage, Lite, Entity } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { getTypeInfo, Type } from '../../../Framework/Signum.React/Scripts/Reflection'
import { TreeViewerMessage, TreeEntity } from './Signum.Entities.Tree'
import * as TreeClient from './TreeClient'
import { TreeNode } from './TreeClient'
import { TreeViewer } from './TreeViewer'
import { FilterOption } from "../../../Framework/Signum.React/Scripts/FindOptions";
import { Modal } from '../../../Framework/Signum.React/Scripts/Components';
import { ModalHeaderButtons } from '../../../Framework/Signum.React/Scripts/Components/Modal';


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
    okPressed: boolean = false;

    handleSelectedNode = (selected: TreeNode | undefined) => {
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
            <Modal size="lg" onHide={this.handleCancelClicked} show={this.state.show} onExited={this.handleOnExited}>
                <ModalHeaderButtons onClose={this.handleCancelClicked}>
                    <span className="sf-entity-title"> {this.props.title || getTypeInfo(this.props.typeName).nicePluralName}</span>
                    &nbsp;
                        <a className="sf-popup-fullscreen" href="#" onClick={(e) => this.treeView && this.treeView.handleFullScreenClick(e)}>
                        <span className="fa fa-external-link"></span>
                    </a>
                </ModalHeaderButtons>

                <div className="modal-body">
                    <TreeViewer
                        filterOptions={this.props.filterOptions}
                        avoidChangeUrl={true}
                        typeName={this.props.typeName}
                        onSelectedNode={this.handleSelectedNode}
                        onDoubleClick={this.handleDoubleClick}
                        ref={tv => this.treeView = tv!}
                    />
                </div>
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



