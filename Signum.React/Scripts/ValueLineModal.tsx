import * as React from 'react'
import { openModal, IModalProps } from './Modals';
import { SelectorMessage, JavascriptMessage } from './Signum.Entities'
import { TypeReference, Binding } from './Reflection'
import { TypeContext } from './TypeContext'
import { ValueLineType, ValueLine } from './Lines/ValueLine'
import { ValueLineProps } from "./Lines";
import { MemberInfo } from './Reflection';
import { BsSize } from './Components';
import { useForceUpdate } from './Hooks';
import { Modal } from 'react-bootstrap';

interface ValueLineModalProps extends IModalProps<any> {
  options: ValueLineModalOptions;
}

export default function ValueLineModal(p: ValueLineModalProps) {

  const [show, setShow] = React.useState(true);
  const forceUpdate = useForceUpdate();
  const { title, message, initialValue, ...props } = p.options;
  const value = React.useRef<any>(initialValue);
  const selectedValue = React.useRef<any>(undefined);
  function handleOkClick() {
    selectedValue.current = value.current;
    setShow(false);
  }

  function handleCancelClicked() {
    selectedValue.current = undefined;
    setShow(false);
  }

  function handleOnExited() {
    p.onExited!(selectedValue.current);
  }

  const ctx = new TypeContext(undefined, undefined, undefined as any, Binding.create(value, s => s.current), "valueLineModal");

  var vlp: ValueLineProps = {
    ctx: ctx,
    formatText: props.formatText !== undefined ? props.formatText : props.member?.format,
    unitText: props.unitText !== undefined ? props.unitText : props.member?.unit,
    labelText: props.labelText !== undefined ? props.labelText : props.member?.niceName,
    type: props.type ?? props.member?.type,
    valueLineType: props.valueLineType ?? (props.member?.isMultiline ? "TextArea" : undefined),
    valueHtmlAttributes: props.valueHtmlAttributes,
    initiallyFocused: props.initiallyFocused,
  };

  const disabled = p.options.allowEmptyValue == false ? (ctx.value as string).trim() ? false : true : undefined;
  const valueOnChanged = p.options.allowEmptyValue == false ? () => forceUpdate() : undefined;

  return (
    <Modal size={p.options.modalSize ?? "lg" as any} show={show} onExited={handleOnExited} onHide={handleCancelClicked}>
      <div className="modal-header">
        <h5 className="modal-title">{title === undefined ? SelectorMessage.ChooseAValue.niceToString() : title}</h5>
        <button type="button" className="close" data-dismiss="modal" aria-label="Close" onClick={handleCancelClicked}>
          <span aria-hidden="true">&times;</span>
        </button>
      </div>
      <div className="modal-body">
        <p>
          {message === undefined ? SelectorMessage.PleaseChooseAValueToContinue.niceToString() : message}
        </p>
        <ValueLine
          formGroupStyle={props.labelText ? "Basic" : "SrOnly"} {...vlp} onChange={valueOnChanged} />
      </div>
      <div className="modal-footer">
        <button disabled={disabled} className="btn btn-primary sf-entity-button sf-ok-button" onClick={handleOkClick}>
          {JavascriptMessage.ok.niceToString()}
        </button>
        <button className="btn btn-light sf-entity-button sf-close-button" onClick={handleCancelClicked}>
          {JavascriptMessage.cancel.niceToString()}
        </button>
      </div>
    </Modal>
  );

}

ValueLineModal.show = (options: ValueLineModalOptions): Promise<any> => {
  return openModal<any>(<ValueLineModal options={options} />);
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


