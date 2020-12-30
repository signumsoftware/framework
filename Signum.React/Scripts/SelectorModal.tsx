import * as React from 'react'
import { openModal, IModalProps } from './Modals';
import { SelectorMessage, Lite, getToString, liteKey, Entity } from './Signum.Entities'
import { TypeInfo, EnumType, Type } from './Reflection'
import * as Finder from './Finder'
import { BsSize } from './Components';
import { Modal } from 'react-bootstrap';

interface SelectorModalProps extends IModalProps<any> {
  options: { value: any; displayName: React.ReactNode; name: string; htmlAttributes?: React.HTMLAttributes<HTMLButtonElement> }[];
  title: React.ReactNode;
  message: React.ReactNode;
  size?: BsSize;
  dialogClassName?: string;
}

export default function SelectorModal(p: SelectorModalProps) {

  const [show, setShow] = React.useState(true);
  const selectedValue = React.useRef<any>(undefined);

  function handleButtonClicked(val: any) {
    selectedValue.current = val;
    setShow(false);
  }

  function handleCancelClicked() {
    setShow(false);
  }

  function handleOnExited() {
    p.onExited!(selectedValue.current);
  }

  return (
    <Modal size={p.size || "sm" as any} show={show} onExited={handleOnExited}
      className="sf-selector-modal" dialogClassName={p.dialogClassName} onHide={handleCancelClicked}>
      <div className="modal-header">
        {p.title &&
          <h4 className="modal-title">
            {p.title}
          </h4>
        }
        <button type="button" className="close" data-dismiss="modal" aria-label="Close" onClick={handleCancelClicked}>
          <span aria-hidden="true">&times;</span>
        </button>
      </div>

      <div className="modal-body">
        <div>
          {p.message && (typeof p.message == "string" ? <p>{p.message}</p> : p.message)}
          {p.options.map((o, i) =>
            <button key={i} type="button" onClick={() => handleButtonClicked(o.value)} name={o.name}
              className="sf-chooser-button sf-close-button btn btn-light" {...o.htmlAttributes}>
              {o.displayName}
            </button>)}
        </div>
      </div>
    </Modal>
  );
}

SelectorModal.chooseElement = <T extends Object>(options: T[], config?: SelectorConfig<T>): Promise<T | undefined> => {
  const { buttonDisplay, buttonName, title, message, size, dialogClassName } = config || {} as SelectorConfig<T>;

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
      htmlAttributes: config?.buttonHtmlAttributes && config.buttonHtmlAttributes(a)
    }))}
    title={title || SelectorMessage.ChooseAValue.niceToString()}
    message={message ?? SelectorMessage.PleaseChooseAValueToContinue.niceToString()}
    size={size}
    dialogClassName={dialogClassName} />);
};

SelectorModal.chooseType = (options: TypeInfo[]): Promise<TypeInfo | undefined> => {
  return SelectorModal.chooseElement(options,
    {
      buttonDisplay: a => a.niceName ?? "",
      buttonName: a => a.name,
      title: SelectorMessage.TypeSelector.niceToString(),
      message: SelectorMessage.PleaseSelectAType.niceToString()
    });
};

SelectorModal.chooseEnum = <T extends string>(enumType: EnumType<T>, title?: React.ReactNode, message?: React.ReactNode, values?: T[]): Promise<T | undefined> => {
    return SelectorModal.chooseElement(values ?? enumType.values(),
      {
        buttonDisplay: a => enumType.niceToString(a),
        buttonName: a => a,
        title: title ?? SelectorMessage._0Selector.niceToString(enumType.niceTypeName()),
        message: message ?? SelectorMessage.PleaseChooseA0ToContinue.niceToString(enumType.niceTypeName()),
        size: "md",
      });
};

SelectorModal.chooseLite = <T extends Entity>(type: Type<T>): Promise<Lite<T> | undefined> => {
  return Finder.API.fetchAllLites({ types: type.typeName })
    .then(lites => SelectorModal.chooseElement<Lite<T>>(lites as Lite<T>[],
      {
        buttonDisplay: a => getToString(a),
        buttonName: a => liteKey(a),
        title: SelectorMessage._0Selector.niceToString(type.niceName()),
        message: SelectorMessage.PleaseChooseA0ToContinue.niceToString(type.niceName()),
        size: "md",
      }));
};


export interface SelectorConfig<T> {
  buttonName?: (val: T) => string; //For testing
  buttonDisplay?: (val: T) => React.ReactNode;
  buttonHtmlAttributes?: (val: T) => React.HTMLAttributes<HTMLButtonElement>; //For testing
  title?: React.ReactNode;
  message?: React.ReactNode;
  size?: BsSize;
  dialogClassName?: string;
  forceShow?: boolean;
}



