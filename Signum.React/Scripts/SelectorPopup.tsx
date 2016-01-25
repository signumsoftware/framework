
import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar } from 'react-bootstrap'
import { openModal, IModalProps } from './Modals';
import { SelectorMessage } from './Signum.Entities'
import * as Reflection from './Reflection'


interface SelectorPopupProps extends React.Props<SelectorPopup>, IModalProps {
    options: { value: any;   displayName: React.ReactChild }[];
    title: string;
}

export default class SelectorPopup extends React.Component<SelectorPopupProps, { show: boolean }>  {

    constructor(props) {
        super(props);

        this.state = { show: true };
    }
    

    selectedValue: any;
    handleButtonClicked = (val: any) => {
        this.selectedValue = val;
        this.setState({ show: false });

    }

    handleCancelClicked = () => {
        this.setState({ show: false });
    }

    handleOnExited = () => {
        this.props.onExited(this.selectedValue);
    }

    render() {
        return <Modal bsSize="lg" onHide={this.handleCancelClicked} show={this.state.show} onExited={this.handleOnExited}>
            <Modal.Header closeButton={true}>
                <h4 className="modal-title">
                    {this.props.title}
                    </h4>
            </Modal.Header>
            
            <Modal.Body>
                <div>
                {this.props.options.map((o, i) =>
                    <button key={i} type="button" onClick={() => this.handleButtonClicked(o.value)}
                        className="sf-chooser-button sf-close-button btn btn-default">
                        {o.displayName}
                        </button>) }
                    </div>
                </Modal.Body>
            </Modal>;
    }

    static chooseElement<T>(options: T[], display?: (val: T) => React.ReactChild, title?: string): Promise<T> {

        if (options.length == 1)
            return Promise.resolve(options.single());

        return openModal<T>(<SelectorPopup
            options={options.map(a=> ({ value: a, displayName: display(a) }))}
            title={title || SelectorMessage.PleaseSelectAnElement.niceToString() } />);
    }

    static chooseType<T>(options: Reflection.TypeInfo[], title?: string): Promise<Reflection.TypeInfo> {

        return SelectorPopup.chooseElement(options,
            a => a.niceName,
            title || SelectorMessage.PleaseSelectAType.niceToString());
    }
}



