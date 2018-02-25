
import * as React from 'react'
import * as Finder from '../Finder'
import { openModal, IModalProps } from '../Modals';
import { ResultTable, FindOptions, FindMode, FilterOption, QueryDescription, ResultRow, ModalFindOptions } from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, Entity } from '../Signum.Entities'
import { getQueryNiceName } from '../Reflection'
import SearchControl, { SearchControlProps} from './SearchControl'
import { Modal } from '../Components';
import { ModalHeaderButtons } from '../Components/Modal';


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

    handleOnExisted = () => {
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
            <Modal size="lg" show={this.state.show} onExited={this.handleOnExisted} onHide={this.handleCancelClicked}>
                <ModalHeaderButtons
                    onClose={this.props.findMode == "Explore" ? this.handleCancelClicked : undefined}
                    onOk={this.props.findMode == "Find" ? this.handleOkClicked : undefined}
                    onCancel={this.props.findMode == "Find" ? this.handleCancelClicked : undefined}
                    okDisabled={!okEnabled}>
                    <span className="sf-entity-title"> {this.props.title}</span>
                    &nbsp;
                        <a className="sf-popup-fullscreen pointer" onMouseUp={(e) => this.searchControl && this.searchControl.handleFullScreenClick(e)}>
                        <span className="fa fa-external-link"></span>
                        </a>
                </ModalHeaderButtons>
                <div className="modal-body">
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
                </div>
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
