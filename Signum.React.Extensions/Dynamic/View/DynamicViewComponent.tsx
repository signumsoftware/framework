import * as React from 'react'
import { DropdownButton, MenuItem } from 'react-bootstrap'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity, OperationSymbol, JavascriptMessage, is } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { FindOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfo } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import * as Operations from '../../../../Framework/Signum.React/Scripts/Operations'
import * as EntityOperations from '../../../../Framework/Signum.React/Scripts/Operations/EntityOperations'
import { BaseNode } from './Nodes'
import { DesignerContext, DesignerNode } from './NodeUtils'
import * as NodeUtils from './NodeUtils'
import * as DynamicViewClient from '../DynamicViewClient'
import { DynamicViewInspector  } from './Designer'
import { DynamicViewTree } from './DynamicViewTree'
import { DynamicViewEntity, DynamicViewOperation, DynamicViewMessage } from '../Signum.Entities.Dynamic'

require("!style!css!./DynamicView.css");

export interface DynamicViewComponentProps {
    ctx: TypeContext<ModifiableEntity>;
    initialDynamicView: DynamicViewEntity;
}

export interface DynamicViewComponentState {
    isDesignerOpen: boolean;
    rootNode: DesignerNode<BaseNode>;
    selectedNode: DesignerNode<BaseNode>;
    dynamicView: DynamicViewEntity;
}

export default class DynamicViewComponent extends React.Component<DynamicViewComponentProps, DynamicViewComponentState>{

    constructor(props: DynamicViewComponentProps) {
        super(props);

        const root = this.getRootNode(props.initialDynamicView, props.ctx.value.Type)

        this.state = {
            dynamicView: props.initialDynamicView,
            isDesignerOpen: false,
            rootNode: root,
            selectedNode: root
        };
    }

    getRootNode(dynamicView: DynamicViewEntity, typeName: string) {

        var context = {
            onClose: this.handleClose,
            refreshView: () => { this.changeState(s => s.selectedNode = s.selectedNode.reCreateNode()); },
            getSelectedNode: () => this.state.isDesignerOpen ? this.state.selectedNode : undefined,
            setSelectedNode: (newNode) => this.changeState(s => s.selectedNode = newNode)
        } as DesignerContext;

        var baseNode = JSON.parse(dynamicView.viewContent!) as BaseNode;

        return DesignerNode.root(baseNode, context, typeName);
    }

    handleReload = (dynamicView: DynamicViewEntity) => {

        const root = this.getRootNode(dynamicView, this.props.ctx.value.Type);

        this.changeState(s => {
            s.dynamicView = dynamicView;
            s.rootNode = root;
            s.selectedNode = root;
        });
    }

    handleOpen= () => {
        this.changeState(s => s.isDesignerOpen = true);
    }

    handleClose = () => {
        this.changeState(s => s.isDesignerOpen = false);
    }

    render() {
        return (<div className="design-main">
            <div className={classes("design-left", this.state.isDesignerOpen && "open")}>
                {!this.state.isDesignerOpen ?
                    <i className="fa fa-pencil-square-o design-open-icon" aria-hidden="true" onClick={this.handleOpen}></i> :
                    <DynamicViewDesigner
                        rootNode={this.state.rootNode}
                        dynamicView={this.state.dynamicView}
                        onReload={this.handleReload}
                        onLoseChanges={this.handleLoseChanges}
                        typeName={this.props.ctx.value.Type} />
                }
            </div>
            <div className={classes("design-content", this.state.isDesignerOpen && "open")}>
                {NodeUtils.render(this.state.rootNode, this.props.ctx)}
            </div>
        </div>);
    }

    handleLoseChanges = () => {
        const node = JSON.stringify(this.state.rootNode.node);

        if (this.state.dynamicView.isNew || node != this.state.dynamicView.viewContent) {
            return confirm(JavascriptMessage.loseCurrentChanges.niceToString());
        }

        return true;
    }
}

interface DynamicViewDesignerProps {
    rootNode: DesignerNode<BaseNode>;
    dynamicView: DynamicViewEntity;
    onLoseChanges: () => boolean;
    onReload: (dynamicView: DynamicViewEntity) => void;
    typeName: string;
}


class DynamicViewDesigner extends React.Component<DynamicViewDesignerProps, { viewNames?: string[]; }>{

    constructor(props: DynamicViewDesignerProps) {
        super(props);
        this.state = {};
    }

    render() {
        var dv = this.props.dynamicView;
        var ctx = TypeContext.root(DynamicViewEntity, dv);

        return (
            <div className="form-vertical">
                <button type="button" className="close" aria-label="Close" onClick={this.props.rootNode.context.onClose}><span aria-hidden="true">×</span></button>
                <h3>
                    <small>{Navigator.getTypeTitle(this.props.dynamicView, undefined)}</small>
                </h3>
                <ValueLine ctx={ctx.subCtx(e => e.viewName)} formGroupStyle="SrOnly" placeholderLabels={true} />
                {this.renderButtonBar()}
                <DynamicViewTree rootNode={this.props.rootNode} />
                <DynamicViewInspector selectedNode={this.props.rootNode.context.getSelectedNode()} />
            </div>
        );
    }



    reload(entity: DynamicViewEntity) {
        this.changeState(s => s.viewNames = undefined);
        this.props.onReload(entity);
    }

    handleSave = () => {

        this.props.dynamicView.viewContent = JSON.stringify(this.props.rootNode.node);

        Operations.API.executeEntity(this.props.dynamicView, DynamicViewOperation.Save)
            .then(pack => { this.reload(pack.entity); return EntityOperations.notifySuccess(); })
            .done();
    }

    handleCreate = () => {

        if (!this.props.onLoseChanges())
            return;

        DynamicViewClient.createDefaultDynamicView(this.props.typeName)
            .then(entity => { this.reload(entity); return EntityOperations.notifySuccess(); })
            .done();
    }

    handleClone = () => {

        if (!this.props.onLoseChanges())
            return;

        Operations.API.constructFromEntity(this.props.dynamicView, DynamicViewOperation.Clone)
            .then(pack => { this.reload(pack.entity); return EntityOperations.notifySuccess(); })
            .done();
    }
    
    handleChangeView = (viewName: string) => {
        if (!this.props.onLoseChanges())
            return;

        DynamicViewClient.API.getDynamicView(this.props.typeName, viewName)
            .then(entity => { this.reload(entity!); })
            .done();
    }

    handleOnToggle = (isOpen: boolean) => {
        if (isOpen && !this.state.viewNames)
            DynamicViewClient.API.getDynamicViewNames(this.props.typeName)
                .then(viewNames => this.changeState(s => s.viewNames = viewNames))
                .done();
    }


    renderButtonBar() {

        var operations = Operations.operationInfos(getTypeInfo(DynamicViewEntity)).toObject(a => a.key);

        return (
            <div className="btn-group btn-group-sm" role="group" style={{ marginBottom: "5px"}}>
                {operations[DynamicViewOperation.Save.key] && <button type="button" className="btn btn-primary" onClick={this.handleSave}>{operations[DynamicViewOperation.Save.key].niceName}</button>}

                <DropdownButton title=" … " id="bg-nested-dropdown" onToggle={this.handleOnToggle} bsSize="sm">
                    {operations[DynamicViewOperation.Create.key] && <MenuItem eventKey="create" onSelect={this.handleCreate}>{operations[DynamicViewOperation.Create.key].niceName}</MenuItem>}
                    {operations[DynamicViewOperation.Clone.key] && !this.props.dynamicView.isNew && <MenuItem eventKey="clone" onSelect={this.handleClone}>{operations[DynamicViewOperation.Clone.key].niceName}</MenuItem>}
                    {this.state.viewNames && this.state.viewNames.length > 0 && <MenuItem divider={true} />}
                    {this.state.viewNames && this.state.viewNames.map(vn => <MenuItem key={vn}
                        eventKey={"view-" + vn}
                        className={classes("sf-dynamic-view", vn == this.props.dynamicView.viewName && "active")}
                        onSelect={() => this.handleChangeView(vn)}>
                        {vn}
                    </MenuItem>)}
                </DropdownButton>
            </div >);
    }
}

