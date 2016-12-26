import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar } from 'react-bootstrap'
import { openModal, IModalProps } from './Modals';
import { Dic } from './Globals';
import { SelectorMessage, JavascriptMessage } from './Signum.Entities'
import { TypeInfo, TypeReference, Binding } from './Reflection'
import { FormGroupStyle, TypeContext } from './TypeContext'
import { ValueLineType, ValueLine } from './Lines/ValueLine'


interface ValueLinePopupModal extends React.Props<ValueLineModal>, IModalProps {

    options: ValueLinePopupOptions;
}

export default class ValueLineModal extends React.Component<ValueLinePopupModal, { show: boolean; value?: any }>  {

    constructor(props: ValueLinePopupModal) {
        super(props);

        this.state = {
            show: true,
            value: this.props.options.initialValue
        };
    }

    valueLineComponent: ValueLine;

    componentDidMount() {
        let element = this.valueLineComponent.inputElement;
        if (element) {
            if (element instanceof HTMLInputElement)
                element.setSelectionRange(0, element.value.length)
            else if (element instanceof HTMLTextAreaElement)
                element.setSelectionRange(0, element.value.length);
            element.focus();
        }
    }

    selectedValue: any;
    handleOkClick = () => {
        this.selectedValue = this.state.value;
        this.setState({ show: false });
    }

    handleCancelClicked = () => {
        this.setState({ show: false });
    }

    handleOnExited = () => {
        this.props.onExited!(this.selectedValue);
    }

    render() {
    
        const ctx = new TypeContext(undefined, undefined, undefined as any, Binding.create(this.state, s => s.value));

        const { title, message, initialValue, ...valueLineProps } = this.props.options;

        return <Modal bsSize="lg" onHide={this.handleCancelClicked} show={this.state.show} onExited={this.handleOnExited}>

            <Modal.Header closeButton={true}>
                <h4 className="modal-title">
                    {title === undefined ? SelectorMessage.ChooseAValue.niceToString() : title}
                </h4>
            </Modal.Header>

            <Modal.Body>
                <p>
                    {message === undefined ? SelectorMessage.PleaseChooseAValueToContinue.niceToString() : message}
                </p>
                <ValueLine
                    ctx={ctx}
                    formGroupStyle={valueLineProps.labelText ? "Basic" : "SrOnly"} {...valueLineProps}
                    ref={vl => this.valueLineComponent = vl} />
            </Modal.Body>
            <Modal.Footer>
                <button className ="btn btn-primary sf-entity-button sf-close-button sf-ok-button" onClick={this.handleOkClick}>
                    {JavascriptMessage.ok.niceToString()}
                </button>
            </Modal.Footer>
        </Modal>;
    }

    static show(options: ValueLinePopupOptions): Promise<any> {
        return openModal<any>(<ValueLineModal options={options}/>);
    }
}

export interface ValueLinePopupOptions {
    type: TypeReference
    valueLineType?: ValueLineType;
    initialValue?: any
    title?: string;
    message?: string;
    labelText?: string;
    format?: string;
    unit?: string;
    initiallyFocused?: boolean;
}


