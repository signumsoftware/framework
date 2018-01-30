
import * as React from 'react'
import { Modal, ModalHeader, ModalBody } from 'reactstrap'
import * as Finder from '../Finder'
import { openModal, IModalProps } from '../Modals';
import { ResultTable, FindOptions, FindMode, FilterOption, QueryDescription, ResultRow, ModalFindOptions } from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, Entity } from '../Signum.Entities'
import { getQueryNiceName } from '../Reflection'
import SearchControl, { SearchControlProps} from './SearchControl'


interface SearchModalProps extends React.Props<SearchModal>, IModalProps {
    findOptions: FindOptions;
    findMode: FindMode;
    isMany: boolean;
    title?: string;

    searchControlProps?: Partial<SearchControlProps>;
}

export default class SearchModal extends React.Component<SearchModalProps, { show: boolean; }>  {

    constructor(props: SearchModalProps) {
        super(props);

        this.state = { show: true };
    }

    selectedRows: ResultRow[] = [];
    okPressed: boolean = false;

    handleSelectionChanged = (selected: ResultRow[]) => {
        this.selectedRows = selected;
        this.forceUpdate();
    }

    handleOkClicked = () => {
        this.okPressed = true;
        this.setState({ show: false });
    }

    handleCancelClicked = () => {
        this.okPressed = false;
        this.setState({ show: false });
    }

    handleOnClosed = () => {
        this.props.onExited!(this.okPressed ? this.selectedRows : undefined);
    }

    handleDoubleClick = (e: React.MouseEvent<any>, row: ResultRow) => {
        e.preventDefault();
        this.selectedRows = [row];
        this.okPressed = true;
        this.setState({ show: false });
    }

    searchControl?: SearchControl;

    render() {

        const okEnabled = this.props.isMany ? this.selectedRows.length > 0 : this.selectedRows.length == 1;

        return (
            <Modal size="lg" isOpen={this.state.show} onClosed={this.handleOnClosed} toggle={this.handleCancelClicked}>
                <ModalTitleButtons
                    onClose={this.props.findMode == "Explore" ? this.handleCancelClicked : undefined}
                    onOk={this.props.findMode == "Find" ? this.handleOkClicked : undefined}
                    onCancel={this.props.findMode == "Find" ? this.handleCancelClicked : undefined}
                    okDisabled={!okEnabled}
                >
                    <span className="sf-entity-title"> {this.props.title}</span>
                    &nbsp;
                        <a className="sf-popup-fullscreen pointer" onMouseUp={(e) => this.searchControl && this.searchControl.handleFullScreenClick(e)}>
                            <span className="fa fa-external-link"></span>
                        </a>
                </ModalTitleButtons>
                <ModalBody>
                    <SearchControl
                        hideFullScreenButton={true}
                        throwIfNotFindable={true}
                        ref={e => this.searchControl = e!}
                        findOptions={this.props.findOptions}
                        onSelectionChanged={this.handleSelectionChanged}
                        largeToolbarButtons={true}
                        onDoubleClick={this.props.findMode == "Find" ? this.handleDoubleClick : undefined}
                        {...this.props.searchControlProps}
                        />
                </ModalBody>
            </Modal>
        );
    }

    static open(findOptions: FindOptions, modalOptions?: ModalFindOptions): Promise<ResultRow | undefined> {

        return openModal<ResultRow[]>(<SearchModal
            findOptions={findOptions}
            findMode={"Find"}
            isMany={false}
            title={modalOptions && modalOptions.title || getQueryNiceName(findOptions.queryName)}
            searchControlProps={modalOptions && modalOptions.searchControlProps}
        />)
            .then(a => a ? a[0] : undefined);
    }

    static openMany(findOptions: FindOptions, modalOptions?: ModalFindOptions): Promise<ResultRow[] | undefined> {

        return openModal<ResultRow[]>(<SearchModal findOptions={findOptions}
            findMode={"Find"}
            isMany={true}
            title={modalOptions && modalOptions.title || getQueryNiceName(findOptions.queryName)}
            searchControlProps={modalOptions && modalOptions.searchControlProps}
        />);
    }

    static explore(findOptions: FindOptions, modalOptions?: ModalFindOptions): Promise<void> {

        return openModal<void>(<SearchModal findOptions={findOptions}
            findMode={"Explore"}
            isMany={true}
            title={modalOptions && modalOptions.title || getQueryNiceName(findOptions.queryName) } />);
    }
}


interface ModalTitleButtonsProps {
    onClose?: () => void;
    onOk?: () => void;
    okDisabled?: boolean;
    onCancel?: () => void;
}

export class ModalTitleButtons extends React.Component<ModalTitleButtonsProps> {
    render() {
        const p = this.props;
        return (
            <div className="modal-header">
                <h4 className="modal-title">
                    {this.props.children}
                </h4>
                {this.props.onClose &&
                    <button type="button" className="close" aria-label="Close" onClick={this.props.onClose}>
                        <span aria-hidden="true">×</span>
                    </button>
                }
                {(this.props.onCancel || this.props.onOk) &&
                    <div className="btn-toolbar" style={{ flexWrap: "nowrap" }}>
                        {this.props.onOk && <button className="btn btn-primary sf-entity-button sf-close-button sf-ok-button" disabled={this.props.okDisabled} onClick={this.props.onOk}>
                            {JavascriptMessage.ok.niceToString()}
                        </button>
                        }
                        {this.props.onCancel && <button className="btn btn-light sf-entity-button sf-close-button sf-cancel-button" onClick={this.props.onCancel}>
                            {JavascriptMessage.cancel.niceToString()}
                        </button>
                        }
                    </div>
                }
            </div>
        );
    }
}



