import * as Navigator from './Navigator';

import * as React from 'react'
import { FunctionalAdapter } from './Frames/FrameModal';
import { Modal } from 'react-overlays';

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

let current: GlobalModalContainer;
  
let modalInstances: (React.Component & IHandleKeyboard)[] = [];

export class GlobalModalContainer extends React.Component<{}, GlobalModalContainerState> {
  constructor(props: {}) {
    super(props);
    this.state = { modals: [], currentUrl: Navigator.history.location.pathname };
    current = this;
  }

  componentDidMount() {
    window.addEventListener("keydown", this.hanldleKeyDown);
  }

  componentWillUnmount() {
    window.removeEventListener("keydown", this.hanldleKeyDown);
  }

  hanldleKeyDown = (e: KeyboardEvent) => {
    if (modalInstances.length) {
      e.preventDefault();
      var topMost = modalInstances[modalInstances.length - 1];

      if (topMost.handleKeyDown) {
        topMost.handleKeyDown(e);
      }
    }
  }

  componentWillReceiveProps(nextProps: {}, nextContext: any): void {
    var newUrl = Navigator.history.location.pathname;

    if (newUrl != this.state.currentUrl)
      this.setState({ modals: [], currentUrl: newUrl });
  }

  render() {
    return <div className="sf-modal-container">{this.state.modals}</div>
  }
}

export function openModal<T>(modal: React.ReactElement<IModalProps>): Promise<T | undefined> {

  return new Promise<T>((resolve) => {
    let cloned: React.ReactElement<IModalProps>;
    const onExited = (val: T) => {
      current.state.modals.remove(cloned);
      current.forceUpdate();
      resolve(val);
    }

    cloned = FunctionalAdapter.withRef(React.cloneElement(modal, { onExited: onExited, key: current.state.modals.length } as any),
      c => c ? modalInstances.push(c) : modalInstances.pop());

    current.state.modals.push(cloned);
    current.forceUpdate();
  });
}

