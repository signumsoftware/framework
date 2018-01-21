
import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar, Sizes } from 'react-bootstrap'
import { openModal, IModalProps } from './Modals';
import { SelectorMessage } from './Signum.Entities'
import { TypeInfo } from './Reflection'


interface SelectorModalProps extends React.Props<SelectorModal>, IModalProps {
    options: { value: any; displayName: React.ReactNode; name: string; htmlAttributes?: React.HTMLAttributes<HTMLButtonElement> }[];
    title: React.ReactNode;
    message: React.ReactNode;
    size?: Sizes;
    dialogClassName?: string;
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
        return <Modal bsSize={this.props.size ? this.props.size : "sm"} onHide={this.handleCancelClicked} show={this.state.show} onExited={this.handleOnExited} className="sf-selector-modal" dialogClassName={this.props.dialogClassName}>
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
                            className="sf-chooser-button sf-close-button btn btn-default" {...o.htmlAttributes}>
                            {o.displayName}
                        </button>)}
                </div>
            </Modal.Body>
        </Modal>;
    }

    static chooseElement<T>(options: T[], config?: SelectorConfig<T>): Promise<T | undefined> {

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
                htmlAttributes: config && config.buttonHtmlAttributes && config.buttonHtmlAttributes(a)
            }))}
            title={title || SelectorMessage.ChooseAValue.niceToString()}
            message={message || SelectorMessage.PleaseChooseAValueToContinue.niceToString()}
            size={size}
            dialogClassName={dialogClassName} />);
    }

    static chooseType(options: TypeInfo[]): Promise<TypeInfo | undefined> {
        return SelectorModal.chooseElement(options,
            {
                buttonDisplay: a => a.niceName || "",
                buttonName: a => a.name,
                title: SelectorMessage.TypeSelector.niceToString(),
                message: SelectorMessage.PleaseSelectAType.niceToString()
            });
    }
}

export interface SelectorConfig<T> {
    buttonName?: (val: T) => string; //For testing
    buttonDisplay?: (val: T) => React.ReactNode;
    buttonHtmlAttributes?: (val: T) => React.HTMLAttributes<HTMLButtonElement>; //For testing
    title?: React.ReactNode;
    message?: React.ReactNode;
    size?: Sizes;
    dialogClassName?: string;
    forceShow?: boolean;
}



