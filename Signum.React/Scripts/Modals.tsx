/// <reference path="globals.ts" />

import * as Services from 'Framework/Signum.React/Scripts/Services';

import * as React from 'react'
import {Modal} from 'react-bootstrap'

var singeltone: GlobalModalsContainer;

export class GlobalModalsContainer extends React.Component<{}, { modals: Modal[] }> {

    constructor() {
        super({});
        this.state = { modals: [] };
        singeltone = this;
    }

    render() {
        return (<div className="sf-global-modals-container">{this.state.modals}</div>);
    }
}

export function openPopup(modal: Modal): Promise<void> {

    var newModal: Modal;

    var oldHide = modal.props.onHide;

    return new Promise<void>((resolve, reject) => {

        var hide = () => {
            if (oldHide)
                oldHide();
            singeltone.state.modals.pop();
            singeltone.forceUpdate(() => resolve(null));
        };

        newModal = React.cloneElement(modal, { show: true, onHide: hide });

        singeltone.state.modals.push(newModal);
        singeltone.forceUpdate();
    });
}

export function errorModal(error: any): Promise<void> {

    var modal = <Modal onHide={null}>
          <Modal.Header closeButton>
            <Modal.Title>Modal heading</Modal.Title>
              </Modal.Header>
          <Modal.Body>


            <h4>Overflowing text to show scroll behavior</h4>
            <p>Cras mattis consectetur purus sit amet fermentum.Cras justo odio, dapibus ac facilisis in, egestas eget quam.Morbi leo risus, porta ac consectetur ac, vestibulum at eros.</p>
              </Modal.Body>
        </Modal>;
    
    return openPopup(modal);
}
