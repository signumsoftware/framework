
import * as React from 'react'
import { Modal, ModalHeader, ModalBody, ModalFooter, ButtonToolbar} from 'reactstrap'
import { openModal, IModalProps } from './Modals';
import { SelectorMessage } from './Signum.Entities'
import { TypeInfo } from './Reflection'


interface SelectorModalProps extends React.Props<SelectorModal>, IModalProps {
    options: { value: any; displayName: React.ReactChild; name: string; htmlAttributes?: React.HTMLAttributes<HTMLButtonElement> }[];
    title: React.ReactChild;
    message: React.ReactChild;
    size?: string;
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
        return <Modal size={this.props.size ? this.props.size : "sm"} isOpen={this.state.show} onExit={this.handleOnExited} className="sf-selector-modal" modalClassName={this.props.dialogClassName}>
            <ModalHeader>
                {this.props.title &&
                    <h4 className="modal-title">
                        {this.props.title}
                    </h4>
                }
            </ModalHeader>

            <ModalBody>
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
            </ModalBody>
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
    buttonDisplay?: (val: T) => React.ReactChild;
    buttonHtmlAttributes?: (val: T) => React.HTMLAttributes<HTMLButtonElement>; //For testing
    title?: React.ReactChild;
    message?: React.ReactChild;
    size?: string;
    dialogClassName?: string;
    forceShow?: boolean;
}



