import * as AppContext from './AppContext';
import * as React from 'react'
import { useForceUpdatePromise, useStateWithPromise, useUpdatedRef} from './Hooks';
import { useLocation } from 'react-router';

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

export function isStarted(): boolean {
  return current != null;
}


interface GlobalModalContainerHandles {
  pushModal(element: React.ReactElement<IModalProps<any>>): number;
  popModal(index: number): Promise<void>;
  getCount(): number;
}

export function GlobalModalContainer(): React.DetailedReactHTMLElement<{
    className: string;
}, HTMLElement> {


  React.useEffect(() => {
    window.addEventListener("keydown", hanldleKeyDown);
    return () => window.removeEventListener("keydown", hanldleKeyDown);
  }, []);

  const forceUpdatePromise = useForceUpdatePromise();

  const location = useLocation();


  const modals = React.useMemo<React.ReactElement<IModalProps<any>>[]>(() => [], []);

  React.useEffect(() => {
    modals.clear();
    forceUpdatePromise();
  }, [location]);

  React.useEffect(() => {
    current = {
      pushModal: e => {
        modals.push(e);
        forceUpdatePromise();
        return modals.length;
      },
      popModal: async index => {
        if (index != modals.length)
          throw new Error("Unexpected index");
        modals.pop();
        await forceUpdatePromise();
      },
      getCount: () => modals.length
    };
    return () => { current = null!; };
  }, []);

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

  return React.createElement("div", { className: "sf-modal-container", id: "modal-container" }, ...modals);
}

export function openModal<T>(modal: React.ReactElement<IModalProps<T>>): Promise<T> {

  return new Promise<T>((resolve) => {
    let cloned: React.ReactElement<IModalProps<T>>;

    cloned = FunctionalAdapter.withRef(React.cloneElement(modal, {
      key: current.getCount(),
      onExited: (val: T) => {
        current.popModal(index!).then(() => resolve(val));
      },
    } as any),
      c => {
        c ? modalInstances.push(c) : modalInstances.pop();
      });

    let index: number = current.pushModal(cloned);;
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
      return React.cloneElement(only, { ref: (a: any) => { this.innerRef = a; } } as any);//TODO: while forwardRef exists
    }

    return React.cloneElement(only, { innerRef: (a: any) => { this.innerRef = a; } } as any); //TODO: To avoid having to use forward Ref until is removed
  }

  static withRef<P>(element: React.ReactElement<P>, ref: React.Ref<any>): React.ReactElement<P> {
    var type = element.type as React.ComponentClass | React.FunctionComponent | string;
    if (typeof type == "string" || type.prototype?.render) {
      return React.cloneElement(element, { ref: ref } as any);
    } else {
      return <FunctionalAdapter ref={ref as React.Ref<FunctionalAdapter>}>{element}</FunctionalAdapter>
    }
  }

  static isInstanceOf(component: React.Component | null | undefined, type: React.ComponentType): boolean {

    if (component instanceof type)
      return true;

    if (component instanceof FunctionalAdapter) {
      var only = React.Children.only(component.props.children);
      return React.isValidElement(only) && only.type == type;
    }

    return false
  }

  static innerRef(component: React.Component | null | undefined): any {

    if (component instanceof FunctionalAdapter) {
      return component.innerRef;
    }
    return component;
  }
}

function isForwardRef(type: any) {
  return type.$$typeof == Symbol.for("react.forward_ref");
}

