import * as React from 'react'
import { openModal, IModalProps } from './Modals';
import { SelectorMessage, JavascriptMessage } from './Signum.Entities'
import { TypeReference, Binding, getFieldMembers, PropertyRoute } from './Reflection'
import { TypeContext } from './TypeContext'
import { MemberInfo } from './Reflection';
import { BsSize, KeyNames } from './Components';
import { useForceUpdate } from './Hooks';
import { Modal } from 'react-bootstrap';
import { AutoFocus } from './Components/AutoFocus';
import type { AutoLineProps } from './Lines/AutoLine';

const AutoLine = React.lazy(() => import("./Lines/AutoLine").then(module => ({ default: module.AutoLine })));

interface AutoLineModalProps extends IModalProps<any> {
  options: AutoLineModalOptions;
}

function AutoLineModal(p: AutoLineModalProps): React.ReactElement {

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
    if (e.key == KeyNames.enter) {
      btnOkRef.current!.focus();
      window.setTimeout(() => {
        handleOkClick();
      }, 100);
    }
  }

  const ctx = new TypeContext(undefined, undefined, undefined as any, Binding.create(value, s => s.current), "valueLineModal");

  var member = props.propertyRoute?.member;

  var label = props.label !== undefined ? props.label : member?.niceName;

  var alp: AutoLineProps = {
    ctx: ctx,
    format: props.format !== undefined ? props.format : member?.format,
    unit: props.unit !== undefined ? props.unit : member?.unit,
    label: label,
    type: props.type ?? member?.type,
    propertyRoute: props.propertyRoute,
    formGroupStyle: label ? "Basic" : "SrOnly",
    onChange: forceUpdate,
    mandatory: p.options.allowEmptyValue == false,
  };

  const disabled = p.options.allowEmptyValue == false && (ctx.value == null || ctx.value == "");

  const error = p.options.validateValue ? p.options.validateValue(ctx.value) : undefined;



  return (
    <Modal size={p.options.modalSize ?? "lg" as any} show={show} onExited={handleOnExited} onHide={handleCancelClicked}>
      <div className="modal-header">
        <h5 className="modal-title">{title ?? member?.niceName ?? SelectorMessage.ChooseAValue.niceToString()}</h5>
        <button type="button" className="btn-close" data-dismiss="modal" aria-label="Close" onClick={handleCancelClicked} />
      </div>
      <div className="modal-body" onKeyUp={(member && member.isMultiline || p.options.doNotCloseByEnter) ? undefined : handleFiltersKeyUp}>
        <p>
          {message === undefined ? SelectorMessage.PleaseChooseAValueToContinue.niceToString() : message}
        </p>
        <AutoFocus>
          {p.options.customComponent ? p.options.customComponent(alp) :
            <React.Suspense fallback={JavascriptMessage.loading.niceToString()}>
              <AutoLine {...alp} />
            </React.Suspense>
          }
        </AutoFocus>
        {p.options.validateValue && <p className="text-danger">
          {error}
        </p>}
      </div>
      <div className="modal-footer">
        <button disabled={disabled || error != null} className="btn btn-primary sf-entity-button sf-ok-button" onClick={handleOkClick} ref={btnOkRef}>
          {JavascriptMessage.ok.niceToString()}
        </button>
        <button className="btn btn-tertiary sf-entity-button sf-close-button" onClick={handleCancelClicked}>
          {JavascriptMessage.cancel.niceToString()}
        </button>
      </div>
    </Modal>
  );
}

namespace AutoLineModal {
  export var show = (options: AutoLineModalOptions): Promise<any> => {
    return openModal<any>(<AutoLineModal options={options} />);
  }
}

export default AutoLineModal;

export interface AutoLineModalOptions {
  propertyRoute?: PropertyRoute;
  type?: TypeReference;
  initialValue?: any;
  title?: React.ReactElement | string | null;
  message?: React.ReactElement | string | null;
  label?: React.ReactElement | string | null;
  customComponent?: (p: AutoLineProps) => React.ReactElement;
  validateValue?: (val: any) => string | undefined;
  format?: string;
  unit?: string;
  valueHtmlAttributes?: React.HTMLAttributes<any>;
  allowEmptyValue?: boolean;
  modalSize?: BsSize;
  doNotCloseByEnter?: Boolean;
}


