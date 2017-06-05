import * as Services from './Services';
import * as Navigator from './Navigator';

import * as React from 'react'

export interface IModalProps {
    onExited?: (val: any) => void;
}

export interface GlobalModalContainerState {
    modals: React.ReactElement<IModalProps>[];
    currentUrl: string;
}

let current: GlobalModalContainer;

export class GlobalModalContainer extends React.Component<{}, GlobalModalContainerState> {
    constructor(props: {}) {
        super(props);
        this.state = { modals: [], currentUrl: Navigator.history.location.pathname };
        current = this;
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

        cloned = React.cloneElement(modal, { onExited: onExited, key: current.state.modals.length } as any);

        current.state.modals.push(cloned);
        current.forceUpdate();
    });
}

