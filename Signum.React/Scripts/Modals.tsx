import * as AppContext from './AppContext';
import * as React from 'react'
import { useStateWithPromise, useHistoryListen } from './Hooks';

declare global {
  interface KeyboardEvent {
    openedModals?: boolean;
  }
}

export interface IModalProps<T> {
  onExited?: (val: T) => void;
}

export interface IHandleKeyboard {
  handleKeyDown?: (e: KeyboardEvent) => void;
}

export interface GlobalModalContainerState {
  modals: React.ReactElement<IModalProps<any>>[];
  currentUrl: string;
}
let current: GlobalModalContainerHandles;
  
const modalInstances: (React.Component & IHandleKeyboard)[] = [];

export function isStarted() {
  return current != null;
}


interface GlobalModalContainerHandles {
  pushModal(element: React.ReactElement<any>) : Promise<any>;
  popModal(element: React.ReactElement<any>): Promise<any>;
  getCount(): number;
}

export function GlobalModalContainer() {
  React.useEffect(() => {
    window.addEventListener("keydown", hanldleKeyDown);
    return () => window.removeEventListener("keydown", hanldleKeyDown);
  }, []);

  var [modals, setModals] = useStateWithPromise<React.ReactElement<IModalProps<any>>[]>([]);

  useHistoryListen(() => setModals([]), true);

  React.useEffect(() => {
    current = {
      pushModal: e => setModals([...modals, e]),
      popModal: e => setModals(modals.filter(a=>a != e)),
      getCount: () => modals.length
    };
    return () => { current = null!; };
  }, [modals.length]);

  function hanldleKeyDown(e: KeyboardEvent){
    if (modalInstances.length) {
      e.openedModals = true;
      var topMost = modalInstances[modalInstances.length - 1];
      topMost = FunctionalAdapter.innerRef(topMost);
      if (topMost && topMost.handleKeyDown) {
        topMost.handleKeyDown(e);
      }
    }
  }

  React.useEffect(() => {
    setModals([]);
  }, [AppContext.history.location.pathname])

  return React.createElement("div", { className: "sf-modal-container" }, ...modals);
}

export function openModal<T>(modal: React.ReactElement<IModalProps<T>>): Promise<T> {

  return new Promise<T>((resolve) => {
    let cloned: React.ReactElement<IModalProps<T>>;
    const onExited = (val: T) => {
      current.popModal(cloned)
        .then(() => resolve(val));
    }

    cloned = FunctionalAdapter.withRef(React.cloneElement(modal, { onExited: onExited, key: current.getCount() } as any),
      c => {
        c ? modalInstances.push(c) : modalInstances.pop();
      });

    return current.pushModal(cloned);
  });
}




export interface FunctionalAdapterProps {
  children: React.ReactNode;
}

export class FunctionalAdapter extends React.Component<FunctionalAdapterProps> {

  innerRef?: any | null;

  render(): React.ReactNode {
    var only = React.Children.only(this.props.children);
    if (!React.isValidElement(only))
      throw new Error("Not a valid react element: " + only);

    if (isForwardRef(only.type)) {
      return React.cloneElement(only, { ref: (a: any) => { this.innerRef = a; } } as any);
    }

    return this.props.children;
  }

  static withRef(element: React.ReactElement<any>, ref: React.Ref<React.Component>) {
    var type = element.type as React.ComponentClass | React.FunctionComponent | string;
    if (typeof type == "string" || type.prototype?.render) {
      return React.cloneElement(element, { ref: ref });
    } else {
      return <FunctionalAdapter ref={ref as React.Ref<FunctionalAdapter>}>{element}</FunctionalAdapter>
    }
  }

  static isInstanceOf(component: React.Component | null | undefined, type: React.ComponentType) {

    if (component instanceof type)
      return true;

    if (component instanceof FunctionalAdapter) {
      var only = React.Children.only(component.props.children);
      return React.isValidElement(only) && only.type == type;
    }

    return false
  }

  static innerRef(component: React.Component | null | undefined) {

    if (component instanceof FunctionalAdapter) {
      return component.innerRef;
    }
    return component;
  }
}

function isForwardRef(type: any) {
  return type.$$typeof == Symbol.for("react.forward_ref");
}

