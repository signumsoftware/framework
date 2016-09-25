import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar, Button } from 'react-bootstrap'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { classes, Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions, QueryToken, filterOperations, OrderType, ColumnOptionsMode } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, getTypeInfo, isTypeEntity, Binding } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenBuilder from '../../../../Framework/Signum.React/Scripts/SearchControl/QueryTokenBuilder'
import { ModifiableEntity, JavascriptMessage, EntityControlMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { QueryEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { FilterOperation, PaginationMode } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.DynamicQuery'
import { ExpressionOrValueComponent, FieldComponent } from './Designer'
import * as Nodes from './Nodes'
import * as NodeUtils from './NodeUtils'
import { DesignerNode } from './NodeUtils'
import { FindOptionsComponent } from './FindOptionsComponent'
import { BaseNode, SearchControlNode } from './Nodes'
import { FindOptionsExpr, FilterOptionExpr, OrderOptionExpr, ColumnOptionExpr } from './FindOptionsExpression'
import { openModal, IModalProps } from '../../../../Framework/Signum.React/Scripts/Modals';
import { DynamicViewMessage, DynamicViewValidationMessage } from '../Signum.Entities.Dynamic'


interface FindOptionsLineProps {
    binding: Binding<FindOptionsExpr | undefined>;
    dn: DesignerNode<BaseNode>
}

export class FindOptionsLine extends React.Component<FindOptionsLineProps, void>{

    renderMember(fo: FindOptionsExpr | undefined): React.ReactNode {
        return (<span
            className={fo === undefined ? "design-default" : "design-changed"}>
            {this.props.binding.member}
        </span>);
    }

    handleRemove = () => {
        this.props.binding.deleteValue();
        this.props.dn.context.refreshView();
    }

    handleCreate = () => {
        var fo = {} as FindOptionsExpr;
        this.modifyFindOptions(fo);
    }

    handleView = (e: React.MouseEvent) => {
        e.preventDefault();
        var fo = JSON.parse(JSON.stringify(this.props.binding.getValue())) as FindOptionsExpr;
        this.modifyFindOptions(fo);
    }

    modifyFindOptions(fo: FindOptionsExpr) {
        FindOptionsModal.show(fo, this.props.dn).then(result => {
            if (result)
                this.props.binding.setValue(this.clean(fo));

            this.props.dn.context.refreshView();
        }).done();
    }

    clean(fo: FindOptionsExpr) {
        delete fo.parentToken;
        if (fo.filterOptions) fo.filterOptions.forEach(f => delete f.token);
        if (fo.orderOptions) fo.orderOptions.forEach(o => delete o.token);
        if (fo.columnOptions) fo.columnOptions.forEach(c => delete c.token);
        return fo;
    }

    render() {
        const fo = this.props.binding.getValue();

        return (
            <div className="form-group">
                <label className="control-label">
                    {this.renderMember(fo)}
                </label>
                <div>
                    {fo ? <div>
                        <a href="" onClick={this.handleView}>{this.getDescription(fo)}</a>
                        {" "}
                        <a className={classes("sf-line-button", "sf-remove")}
                        onClick={this.handleRemove}
                        title={EntityControlMessage.Remove.niceToString()}>
                            <span className="glyphicon glyphicon-remove" />
                        </a></div> :
                        <a title={EntityControlMessage.Create.niceToString()}
                            className="sf-line-button sf-create"
                            onClick={this.handleCreate}>
                            <span className="glyphicon glyphicon-plus" style={{ marginRight: "5px" }} />{EntityControlMessage.Create.niceToString()}
                        </a>}
                </div>
            </div>
        );
    }

    getDescription(fo: FindOptionsExpr) {

        var filters = [
            fo.parentColumn,
            fo.filterOptions && fo.filterOptions.length && fo.filterOptions.length + " filters"]
            .filter(a => !!a).join(", ");

        return `${fo.queryKey} (${filters || "No filter"})`.trim(); 
    }
}



interface FindOptionsModalProps extends React.Props<FindOptionsModal>, IModalProps {
    findOptions: FindOptionsExpr;
    dn: DesignerNode<BaseNode>;
}

export default class FindOptionsModal extends React.Component<FindOptionsModalProps, { show: boolean }>  {

    constructor(props: FindOptionsModalProps) {
        super(props);

        this.state = { show: true };
    }

    okClicked: boolean
    handleOkClicked = () => {
        this.okClicked = true;
        this.setState({ show: false });

    }

    handleCancelClicked = () => {
        this.setState({ show: false });
    }

    handleOnExited = () => {
        this.props.onExited!(this.okClicked);
    }

    render() {
        return <Modal bsSize="lg" onHide={this.handleCancelClicked} show={this.state.show} onExited={this.handleOnExited} className="sf-selector-modal">
            <Modal.Header closeButton={true}>
                <h4 className="modal-title">
                    FindOptions
                </h4>
                <ButtonToolbar>
                    <Button className="sf-entity-button sf-close-button sf-ok-button" bsStyle="primary" onClick={this.handleOkClicked}>{JavascriptMessage.ok.niceToString()}</Button>
                    <Button className="sf-entity-button sf-close-button sf-cancel-button" bsStyle="default" onClick={this.handleCancelClicked}>{JavascriptMessage.cancel.niceToString()}</Button>
                 </ButtonToolbar>
            </Modal.Header>

            <Modal.Body>
                <FindOptionsComponent
                    dn={this.props.dn}
                    findOptions={this.props.findOptions} /> 
            </Modal.Body>
        </Modal>;
    }

    static show(findOptions: FindOptionsExpr, dn: DesignerNode<BaseNode>): Promise<boolean> {
        return openModal<boolean>(<FindOptionsModal findOptions={findOptions} dn={dn} />);
    }
}
