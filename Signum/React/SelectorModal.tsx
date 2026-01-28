import * as React from 'react'
import { openModal, IModalProps } from './Modals';
import { SelectorMessage, Lite, getToString, liteKey, Entity, JavascriptMessage } from './Signum.Entities'
import { TypeInfo, EnumType, Type, getTypeInfo } from './Reflection'
//import { Finder } from './Finder' //allow without log-in
import { BsSize } from './Components';
import { Modal } from 'react-bootstrap';

interface SelectorModalProps extends IModalProps<any> {
  options: { value: unknown; displayName: React.ReactNode; name: string; htmlAttributes?: React.HTMLAttributes<HTMLButtonElement>, groupKey?: string }[];
  title: React.ReactNode;
  message: React.ReactNode;
  size?: BsSize;
  dialogClassName?: string;
  multiSelect?: {
    minElements?: number | null, /*Default 1*/
    maxElements?: number | null;
  },
  renderGroup?: (groupKey: string) => React.ReactNode;
}

function SelectorModal(p: SelectorModalProps): React.ReactElement {

  const [show, setShow] = React.useState(true);
  const [selectedItems, setSelectedItems] = React.useState<unknown[]>([]);
  const selectedValue = React.useRef<any>(undefined);

  function handleButtonClicked(val: any) {
    selectedValue.current = val;
    setShow(false);
  }

  function handleOkClicked() {
    selectedValue.current = selectedItems;
    setShow(false);
  }

  function handleDoubleClickClicked(val: any) {
    selectedValue.current = [val];
    setShow(false);
  }

  function handleCancelClicked() {
    setShow(false);
  }

  function handleOnExited() {
    p.onExited!(selectedValue.current);
  }

  function handleCheckboxOnChange(e: React.ChangeEvent<HTMLInputElement>, value: unknown) {
    if (p.multiSelect?.maxElements == 1) {
      if (e.currentTarget.checked) {
        setSelectedItems([value]);
      }
      else {
        setSelectedItems([]);
      }
    } else {
      if (e.currentTarget.checked) {
        setSelectedItems([...selectedItems, value]);
      }
      else {
        setSelectedItems(selectedItems.filter(v => v != value));
      }
    }


  };

  var groups = p.options.groupBy(a => a.groupKey ?? "");

  return (
    <Modal size={p.size || "sm" as any} show={show} onExited={handleOnExited}
      className="sf-selector-modal" dialogClassName={p.dialogClassName} onHide={handleCancelClicked}>
      <div className="modal-header">
        {p.title &&
          <h1 className="modal-title h4">
            {p.title}
          </h1>
        }
        <button type="button" className="btn-close" data-dismiss="modal" aria-label="Close" onClick={handleCancelClicked} />
      </div>

      <div className="modal-body">
        <div>
          {p.message && (typeof p.message == "string" ? <p>{p.message}</p> : p.message)}

          <div className="row">
            {groups
              .map(gr => <div className="col-sm" key={gr.key}>
                {gr.key && (p.renderGroup == null ? <h2 className="lead h4">{gr.key}</h2> : p.renderGroup!(gr.key))}
                {gr.elements.map((o, i) =>
                  p.multiSelect ?
                    <label className="m-2" style={{ display: "block", userSelect: "none" }} onDoubleClick={e => { e.preventDefault(); handleDoubleClickClicked(o.value); }} key={i}>
                      <input type={p.multiSelect?.maxElements == 1 ? "radio" : "checkbox"} onChange={e => handleCheckboxOnChange(e, o.value)} className={"form-check-input"} name={o.displayName?.toString()!} checked={selectedItems.contains(o.value)} />
                      {" "}{o.displayName}
                    </label> :
                    <button key={i} type="button" onClick={() => handleButtonClicked(o.value)} name={o.name}
                      className={"sf-chooser-button sf-close-button btn btn-tertiary"} >
                      {o.displayName}
                    </button>)
                }
              </div>)
            }
          </div>
        </div>
      </div>

      {p.multiSelect && <div className="modal-footer">
        <button type="button" onClick={() => handleOkClicked()}
          className="btn btn-primary mt-2 sf-ok-button" disabled={
            p.multiSelect.minElements != null && selectedItems.length < p.multiSelect.minElements ||
            p.multiSelect.maxElements != null && selectedItems.length > p.multiSelect.maxElements}>
          {JavascriptMessage.ok.niceToString()}
        </button>
      </div>
      }
    </Modal>
  );
}

namespace SelectorModal {

  export function chooseElement<T extends Object>(options: T[], config?: SelectorConfig<T>): Promise<T | undefined> {
    const { buttonDisplay, buttonName, title, message, size, dialogClassName, buttonGroupKey } = config || {} as SelectorConfig<T>;

    if (!config || !config.forceShow) {
      if (options.length == 1)
        return Promise.resolve(options.single());

      if (options.length == 0)
        return Promise.resolve(undefined);
    }

    return openModal<T>(<SelectorModal
      options={options.map(a => ({
        value: a,
        displayName: buttonDisplay ? buttonDisplay(a) : a.toString(),
        name: buttonName ? buttonName(a) : a.toString(),
        htmlAttributes: config?.buttonHtmlAttributes && config.buttonHtmlAttributes(a),
        groupKey: buttonGroupKey?.(a) ?? undefined,
      }))}
      title={title || SelectorMessage.ChooseAValue.niceToString()}
      message={message ?? SelectorMessage.PleaseChooseAValueToContinue.niceToString()}
      size={size}
      dialogClassName={dialogClassName} />);
  };

  export function chooseManyElement<T extends Object>(options: T[], config?: MultiSelectorConfig<T>): Promise<T[] | undefined> {
    const { buttonDisplay, buttonName, title, message, size, dialogClassName, buttonGroupKey } = config || {} as SelectorConfig<T>;

    var minElements = config?.minElements === undefined ? 1 : config.minElements;

    if (!config || !config.forceShow) {
      if (options.length == 0)
        return Promise.resolve([]);

      if (options.length == minElements)
        return Promise.resolve(options)
    }

    return openModal<T[]>(<SelectorModal
      options={options.map(a => ({
        value: a,
        displayName: buttonDisplay ? buttonDisplay(a) : a.toString(),
        name: buttonName ? buttonName(a) : a.toString(),
        htmlAttributes: config?.buttonHtmlAttributes && config.buttonHtmlAttributes(a),
        groupKey: buttonGroupKey?.(a) ?? undefined,
      }))}
      title={title || SelectorMessage.ChooseValues.niceToString()}
      message={message ?? SelectorMessage.PleaseSelectAtLeastOneValueToContinue.niceToString()}
      size={size}
      dialogClassName={dialogClassName}
      multiSelect={{
        minElements: minElements,
        maxElements: config?.maxElements,
      }}
    />);
  }

  export function chooseType(options: TypeInfo[], config?: SelectorConfig<TypeInfo>): Promise<TypeInfo | undefined> {
    return SelectorModal.chooseElement(options,
      {
        buttonDisplay: a => a.niceName ?? "",
        buttonName: a => a.name,
        title: SelectorMessage.TypeSelector.niceToString(),
        message: SelectorMessage.PleaseSelectAType.niceToString(),
        ...config
      });
  }

  export function chooseEnum<T extends string>(enumType: EnumType<T>, values?: T[], config?: SelectorConfig<T>): Promise<T | undefined> {
    return SelectorModal.chooseElement(values ?? enumType.values(),
      {
        buttonDisplay: a => enumType.niceToString(a),
        buttonName: a => a,
        title: SelectorMessage._0Selector.niceToString(enumType.niceTypeName()),
        message: SelectorMessage.PleaseChooseA0ToContinue.niceToString(enumType.niceTypeName()),
        size: "md",
        ...config
      });
  }

  export function chooseLite<T extends Entity>(type: Type<T> | TypeInfo | string, values: Lite<T>[], config?: SelectorConfig<Lite<T>>): Promise<Lite<T> | undefined> {
    const ti = getTypeInfo(type);
    return SelectorModal.chooseElement<Lite<T>>(values,
      {
        buttonDisplay: a => getToString(a),
        buttonName: a => liteKey(a),
        title: SelectorMessage._0Selector.niceToString(ti.niceName),
        message: SelectorMessage.PleaseChooseA0ToContinue.niceToString(ti.niceName),
        size: "md",
        ...config
      });
  }
}

export default SelectorModal;

export interface SelectorConfig<T> {
  buttonName?: (val: T) => string; //For testing
  buttonDisplay?: (val: T) => React.ReactNode;
  buttonHtmlAttributes?: (val: T) => React.HTMLAttributes<HTMLButtonElement>; //For testing
  buttonGroupKey?: (val: T) => string | null | undefined;
  title?: React.ReactNode;
  message?: React.ReactNode;
  size?: BsSize;
  dialogClassName?: string;
  forceShow?: boolean;
}


export interface MultiSelectorConfig<T> extends SelectorConfig<T> {
  minElements?: number | null, /*Default 1*/
  maxElements?: number | null;
}


