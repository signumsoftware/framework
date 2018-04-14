import * as React from 'react'
import { openModal, IModalProps } from './Modals';
import { Dic } from './Globals';
import { SelectorMessage, JavascriptMessage } from './Signum.Entities'
import { TypeInfo, TypeReference, Binding } from './Reflection'
import { FormGroupStyle, TypeContext } from './TypeContext'
import { ValueLineType, ValueLine } from './Lines/ValueLine'
import { ValueLineProps } from "./Lines";
import { ValidationMessage } from "./Signum.Entities";
import { MemberInfo } from './Reflection';
import { Modal, BsSize } from './Components';


interface ValueLineModalProps extends React.Props<ValueLineModal>, IModalProps {

    options: ValueLineModalOptions;
}

export default class ValueLineModal extends React.Component<ValueLineModalProps, { show: boolean; value?: any }>  {

    constructor(props: ValueLineModalProps) {
        super(props);

        this.state = {
            show: true,
            value: this.props.options.initialValue
        };
    }

    selectedValue: any = undefined;
    handleOkClick = () => {
        this.selectedValue = this.state.value;
        this.setState({ show: false });
    }

    handleCancelClicked = () => {
        this.selectedValue = undefined;
        this.setState({ show: false });
    }

    handleOnExited = () => {
        this.props.onExited!(this.selectedValue);
    }

    render() {

        const ctx = new TypeContext(undefined, undefined, undefined as any, Binding.create(this.state, s => s.value));

        const { title, message, initialValue, ...props } = this.props.options;
        var vlp: ValueLineProps = {
            ctx: ctx,
            formatText: props.formatText !== undefined ? props.formatText : props.member && props.member.format,
            unitText: props.unitText !== undefined ? props.unitText : props.member && props.member.unit,
            labelText: props.labelText !== undefined ? props.labelText : props.member && props.member.niceName,
            type: props.type || props.member && props.member.type,
            valueLineType: props.valueLineType || props.member && (props.member.isMultiline ? "TextArea" : undefined),
            valueHtmlAttributes: props.valueHtmlAttributes,
            initiallyFocused: props.initiallyFocused,
        };

        const disabled = this.props.options.allowEmptyValue == false ? (ctx.value as string).trim() ? false : true : undefined;
        const valueOnChanged = this.props.options.allowEmptyValue == false ? () => this.forceUpdate() : undefined;

        return (
            <Modal size={this.props.options.modalSize || "lg"} show={this.state.show} onExited={this.handleOnExited} onHide={this.handleCancelClicked}>
                <div className="modal-header">
                    <h5 className="modal-title">{title === undefined ? SelectorMessage.ChooseAValue.niceToString() : title}</h5>
                    <button type="button" className="close" data-dismiss="modal" aria-label="Close" onClick={this.handleCancelClicked}>
                        <span aria-hidden="true">&times;</span>
                    </button>
                </div>
                <div className="modal-body">
                    <p>
                        {message === undefined ? SelectorMessage.PleaseChooseAValueToContinue.niceToString() : message}
                    </p>
                    <ValueLine ctx={ctx}
                        formGroupStyle={props.labelText ? "Basic" : "SrOnly"} {...vlp} onChange={valueOnChanged} />
                </div>
                <div className="modal-footer">
                    <button disabled={disabled} className="btn btn-primary sf-entity-button sf-ok-button" onClick={this.handleOkClick}>
                        {JavascriptMessage.ok.niceToString()}
                    </button>
                    <button className="btn btn-light sf-entity-button sf-close-button" onClick={this.handleCancelClicked}>
                        {JavascriptMessage.cancel.niceToString()}
                    </button>
                </div>
            </Modal>
        );
    }

    static show(options: ValueLineModalOptions): Promise<any> {
        return openModal<any>(<ValueLineModal options={options} />);
    }
}

export interface ValueLineModalOptions {
    member?: MemberInfo;
    type?: TypeReference;
    valueLineType?: ValueLineType;
    initialValue?: any;
    title?: React.ReactChild;
    message?: React.ReactChild;
    labelText?: React.ReactChild;
    formatText?: string;
    unitText?: string;
    initiallyFocused?: boolean;
    valueHtmlAttributes?: React.HTMLAttributes<any>;
    allowEmptyValue?: boolean;
    modalSize?: BsSize;
}


