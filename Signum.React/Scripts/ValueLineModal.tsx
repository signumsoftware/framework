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
import { AutoFocus } from './Components/AutoFocus';

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

  function handleFiltersKeyUp(e: React.KeyboardEvent<HTMLDivElement>) {
    if (e.keyCode == 13) {
      setTimeout(() => {
        handleOkClick();
      }, 100);
    }
  }

  const ctx = new TypeContext(undefined, undefined, undefined as any, Binding.create(value, s => s.current), "valueLineModal");

  var vlp: ValueLineProps = {
    ctx: ctx,
    format: props.format !== undefined ? props.format : props.member?.format,
    unit: props.unit !== undefined ? props.unit : props.member?.unit,
    label: props.label !== undefined ? props.label : props.member?.niceName,
    type: props.type ?? props.member?.type,
    valueLineType: props.valueLineType ?? (props.member?.isMultiline ? "TextArea" : undefined),
    valueHtmlAttributes: props.valueHtmlAttributes,
    initiallyFocused: props.initiallyFocused,
    initiallyShowOnly: props.initiallyShowOnly,
  };

  const disabled = p.options.allowEmptyValue == false && (ctx.value == null || ctx.value == "");

  const error = p.options.validateValue ? p.options.validateValue(ctx.value) : undefined;

  return (
    <Modal size={p.options.modalSize ?? "lg" as any} show={show} onExited={handleOnExited} onHide={handleCancelClicked}>
      <div className="modal-header">
        <h5 className="modal-title">{title === undefined ? SelectorMessage.ChooseAValue.niceToString() : title}</h5>
        <button type="button" className="btn-close" data-dismiss="modal" aria-label="Close" onClick={handleCancelClicked}/>
      </div>
      <div className="modal-body" onKeyUp={handleFiltersKeyUp}>
        <p>
          {message === undefined ? SelectorMessage.PleaseChooseAValueToContinue.niceToString() : message}
        </p>
        <AutoFocus>
          <ValueLine
            formGroupStyle={vlp.label ? "Basic" : "SrOnly"} {...vlp} onChange={forceUpdate} />
        </AutoFocus>
        {p.options.validateValue && <p className="text-danger">
          { error}
        </p>}
      </div>
      <div className="modal-footer">
        <button disabled={disabled || error != null} className="btn btn-primary sf-entity-button sf-ok-button" onClick={handleOkClick}>
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
  label?: React.ReactChild;
  validateValue?: (val: any) => string | undefined;
  format?: string;
  unit?: string;
  initiallyFocused?: boolean;
  initiallyShowOnly?: "Date" | "Time";
  valueHtmlAttributes?: React.HTMLAttributes<any>;
  allowEmptyValue?: boolean;
  modalSize?: BsSize;
}


