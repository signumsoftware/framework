import * as React from 'react'
import { openModal, IModalProps } from './Modals';
import { SelectorMessage, JavascriptMessage } from './Signum.Entities'
import { TypeReference, Binding } from './Reflection'
import { TypeContext } from './TypeContext'
import { MemberInfo } from './Reflection';
import { BsSize } from './Components';
import { useForceUpdate } from './Hooks';
import { Modal } from 'react-bootstrap';
import { AutoFocus } from './Components/AutoFocus';
import { AutoLine, AutoLineProps } from './Lines/AutoLine';

interface AutoLineModalProps extends IModalProps<any> {
  options: AutoLineModalOptions;
}

export default function AutoLineModal(p: AutoLineModalProps) {

  const [show, setShow] = React.useState(true);
  const forceUpdate = useForceUpdate();
  const { title, message, initialValue, ...props } = p.options;
  const value = React.useRef<any>(initialValue);
  const selectedValue = React.useRef<any>(undefined);
  const btnOkRef = React.useRef<HTMLButtonElement>(null);
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
      btnOkRef.current!.focus();
      window.setTimeout(() => {
        handleOkClick();
      }, 100);
    }
  }

  const ctx = new TypeContext(undefined, undefined, undefined as any, Binding.create(value, s => s.current), "valueLineModal");

  var label = props.label !== undefined ? props.label : props.member?.niceName;

  var alp: AutoLineProps = {
    ctx: ctx,
    format: props.format !== undefined ? props.format : props.member?.format,
    unit: props.unit !== undefined ? props.unit : props.member?.unit,
    label: label,
    type: props.type ?? props.member?.type,
    formGroupStyle: label ? "Basic" : "SrOnly",
    onChange: forceUpdate
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
          {p.options.customComponent ? p.options.customComponent(alp) :<AutoLine {...alp} />}
        </AutoFocus>
        {p.options.validateValue && <p className="text-danger">
          {error}
        </p>}
      </div>
      <div className="modal-footer">
        <button disabled={disabled || error != null} className="btn btn-primary sf-entity-button sf-ok-button" onClick={handleOkClick} ref={btnOkRef}>
          {JavascriptMessage.ok.niceToString()}
        </button>
        <button className="btn btn-light sf-entity-button sf-close-button" onClick={handleCancelClicked}>
          {JavascriptMessage.cancel.niceToString()}
        </button>
      </div>
    </Modal>
  );
}

AutoLineModal.show = (options: AutoLineModalOptions): Promise<any> => {
  return openModal<any>(<AutoLineModal options={options} />);
}

export interface AutoLineModalOptions {
  member?: MemberInfo;
  type?: TypeReference;
  initialValue?: any;
  title?: React.ReactChild;
  message?: React.ReactChild;
  label?: React.ReactChild;
  customComponent?: (p: AutoLineProps) => React.ReactElement;
  validateValue?: (val: any) => string | undefined;
  format?: string;
  unit?: string;
  valueHtmlAttributes?: React.HTMLAttributes<any>;
  allowEmptyValue?: boolean;
  modalSize?: BsSize;
}


