
import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar, Sizes } from 'react-bootstrap'
import { openModal, IModalProps } from './Modals';
import { SelectorMessage } from './Signum.Entities'
import { TypeInfo } from './Reflection'


interface SelectorModalProps extends React.Props<SelectorModal>, IModalProps {
    options: { value: any; displayName: React.ReactChild; name: string; }[];
    title: React.ReactChild;
    message: React.ReactChild;
    size?: Sizes;
}

export default class SelectorModal extends React.Component<SelectorModalProps, { show: boolean }>  {

    constructor(props: SelectorModalProps) {
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
        this.props.onExited!(this.selectedValue);
    }


    render() {
        return <Modal bsSize={this.props.size ? this.props.size : "sm"} onHide={this.handleCancelClicked} show={this.state.show} onExited={this.handleOnExited} className="sf-selector-modal" >
            <Modal.Header closeButton={true}>
                {this.props.title &&
                    <h4 className="modal-title">
                        {this.props.title}
                    </h4>
                }
            </Modal.Header>

            <Modal.Body>
                <div>
                    {this.props.message &&
                        <p>
                            {this.props.message}
                        </p>
                    }
                    {this.props.options.map((o, i) =>
                        <button key={i} type="button" onClick={() => this.handleButtonClicked(o.value)} name={o.value}
                            className="sf-chooser-button sf-close-button btn btn-default">
                            {o.displayName}
                        </button>)}
                </div>
            </Modal.Body>
        </Modal>;
    }

    static chooseElement<T>(options: T[], config?: SelectorConfig<T>): Promise<T | undefined> {

        const {display, name, title, message, size} = config!;

        if (options.length == 1)
            return Promise.resolve(options.single());

        if (options.length == 0)
            return Promise.resolve(undefined);

        return openModal<T>(<SelectorModal
            options={options.map(a => ({
                value: a,
                displayName: display ? display(a) : a.toString(),
                name: name ? name(a) : a.toString()
            }))}
            title={title || SelectorMessage.ChooseAValue.niceToString()}
            message={message || SelectorMessage.PleaseChooseAValueToContinue.niceToString()}
            size={size} />);
    }

    static chooseType(options: TypeInfo[]): Promise<TypeInfo> {
        return SelectorModal.chooseElement(options,
            {
                display: a => a.niceName || "",
                name: a => a.name,
                title: SelectorMessage.TypeSelector.niceToString(),
                message: SelectorMessage.PleaseSelectAType.niceToString()
            });
    }
}

export interface SelectorConfig<T> {
    display?: (val: T) => React.ReactChild;
    name?: (val: T) => string; //For testing
    title?: React.ReactChild;
    message?: React.ReactChild;
    size?: Sizes;
}



