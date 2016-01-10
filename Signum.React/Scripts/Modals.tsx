/// <reference path="globals.ts" />

import * as Services from 'Framework/Signum.React/Scripts/Services';

import * as React from 'react'



export interface IModalProps {
    onExited?: (val: any) => void;
}


var singletone: GlobalModalContainer;
export class GlobalModalContainer extends React.Component<{}, { modals: React.ReactElement<IModalProps>[]
}> {
    constructor(props) {
        super(props);
        this.state = { modals: [] };
        singletone = this;
    }

    render() {
        return <div className="sf-modal-container">{this.state.modals}</div>
    }
}


export function openModal<T>(modal: React.ReactElement<IModalProps>): Promise<T> {

    return new Promise<T>((resolve) => {

        var cloned;
        var onExited = (val : T) => {
            singletone.state.modals.remove(cloned);
            singletone.forceUpdate();
            resolve(val);
        }

        cloned = React.cloneElement(modal, { onExited: onExited, key: singletone.state.modals.length } as any);

        singletone.state.modals.push(cloned);
        singletone.forceUpdate();
    });
}

//export function errorModal(error: any): Promise<void> {

//    var modal = <Modal onHide={null}>
//          <Modal.Header closeButton>
//            <Modal.Title>Modal heading</Modal.Title>
//              </Modal.Header>
//          <Modal.Body>


//            <h4>Overflowing text to show scroll behavior</h4>
//            <p>Cras mattis consectetur purus sit amet fermentum.Cras justo odio, dapibus ac facilisis in, egestas eget quam.Morbi leo risus, porta ac consectetur ac, vestibulum at eros.</p>
//              </Modal.Body>
//        </Modal>;
    
//    return openModal(modal);
//}



export interface SelectValueProps extends IModalProps {
    values: any[];
}


export function selectValue<T>(values: T[], toString: (val: T) => string) {
    return null;
}
