import * as Navigator from './Navigator';

import * as React from 'react'
import { FunctionalAdapter } from './Frames/FrameModal';

declare global {
  interface KeyboardEvent {
    openedModals?: boolean;
  }
}

export interface IModalProps {
  onExited?: (val: any) => void;
}

export interface IHandleKeyboard {
  handleKeyDown?: (e: KeyboardEvent) => void;
}

export interface GlobalModalContainerState {
  modals: React.ReactElement<IModalProps>[];
  currentUrl: string;
}
let current: GlobalModalContainerHandles;
  
const modalInstances: (React.Component & IHandleKeyboard)[] = [];

interface GlobalModalContainerHandles {
  pushModal(element: React.ReactElement<any>) : void;
  popModal(element: React.ReactElement<any>): void;
  getCount(): number;
}

export function GlobalModalContainer() {
  React.useEffect(() => {
    window.addEventListener("keydown", hanldleKeyDown);
    return () => window.removeEventListener("keydown", hanldleKeyDown);
  }, []);

  React.useEffect(() => {
    current = {
      pushModal: e => {
        modals.push(e);
        setModals(modals);
      },
      popModal: e => {
        modals.remove(e);
        setModals(modals);
      },
      getCount: () => modals.length
    };
    return () => { current = null!; };
  }, []);

  var [modals, setModals] = React.useState<React.ReactElement<IModalProps>[]>([]);


  function hanldleKeyDown(e: KeyboardEvent){
    if (modalInstances.length) {
      e.openedModals = true;
      var topMost = modalInstances[modalInstances.length - 1];

      if (topMost.handleKeyDown) {
        topMost.handleKeyDown(e);
      }
    }
  }


  React.useEffect(() => {
    setModals([]);
  }, [Navigator.history.location.pathname])


  return React.createElement("div", { className: "sf-modal-container" }, ...modals);
}

export function openModal<T>(modal: React.ReactElement<IModalProps>): Promise<T | undefined> {

  return new Promise<T>((resolve) => {
    let cloned: React.ReactElement<IModalProps>;
    const onExited = (val: T) => {
      current.popModal(cloned);
      resolve(val);
    }

    cloned = FunctionalAdapter.withRef(React.cloneElement(modal, { onExited: onExited, key: current.getCount() } as any),
      c => c ? modalInstances.push(c) : modalInstances.pop());

    current.pushModal(cloned);
  });
}

