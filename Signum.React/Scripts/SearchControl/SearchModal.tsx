
import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar } from 'react-bootstrap'
import * as Finder from '../Finder'
import { openModal, IModalProps } from '../Modals';
import { ResultTable, FindOptions, FindMode, FilterOption, QueryDescription, ResultRow } from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, Entity } from '../Signum.Entities'
import { getQueryNiceName } from '../Reflection'
import SearchControl, { SearchControlProps} from './SearchControl'


interface SearchModalProps extends React.Props<SearchModal>, IModalProps {
    findOptions: FindOptions;
    findMode: FindMode;
    isMany: boolean;
    title?: string;
}

export default class SearchModal extends React.Component<SearchModalProps, { show: boolean; }>  {

    constructor(props: SearchModalProps) {
        super(props);

        this.state = { show: true };
    }

    selectedEntites: Lite<Entity>[] = [];
    okPressed: boolean;

    handleSelectionChanged = (selected: Lite<Entity>[]) => {
        this.selectedEntites = selected;
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

    handleOnExited = () => {
        this.props.onExited(this.okPressed ? this.selectedEntites : null);
    }

    handleDoubleClick = (e: React.MouseEvent, row: ResultRow) => {
        e.preventDefault();
        this.selectedEntites = [row.entity];
        this.okPressed = true;
        this.setState({ show: false });
    }

    searchControl: SearchControl;

    render() {

        const okEnabled = this.props.isMany ? this.selectedEntites.length > 0 : this.selectedEntites.length == 1;

        return (
            <Modal bsSize="lg" onHide={this.handleCancelClicked} show={this.state.show} onExited={this.handleOnExited}>
                <Modal.Header closeButton={this.props.findMode == FindMode.Explore}>
                    { this.props.findMode == FindMode.Find &&
                        <div className="btn-toolbar" style={{ float: "right" }}>
                            <button className ="btn btn-primary sf-entity-button sf-close-button sf-ok-button" disabled={!okEnabled} onClick={this.handleOkClicked}>
                                {JavascriptMessage.ok.niceToString() }
                            </button>

                            <button className ="btn btn-default sf-entity-button sf-close-button sf-cancel-button" onClick={this.handleCancelClicked}>{JavascriptMessage.cancel.niceToString() }</button>
                        </div>}
                    <h4>
                        <span className="sf-entity-title"> {this.props.title}</span>&nbsp;
                        <a className ="sf-popup-fullscreen" href="#" onClick={(e) => this.searchControl.handleFullScreenClick(e) }>
                            <span className="glyphicon glyphicon-new-window"></span>
                        </a>
                    </h4>
                </Modal.Header>

                <Modal.Body>
                    <SearchControl hideExternalButton={true}
                        ref={(e: SearchControl) => this.searchControl = e}
                        findOptions={this.props.findOptions}
                        onSelectionChanged={this.handleSelectionChanged}
                        onDoubleClick={this.props.findMode == FindMode.Find ? this.handleDoubleClick : null}
                        />
                </Modal.Body>
            </Modal>
        );
    }

    static open(findOptions: FindOptions, title?: string): Promise<Lite<Entity>> {

        return openModal<Lite<Entity>[]>(<SearchModal
            findOptions={findOptions}
            findMode={FindMode.Find}
            isMany={false}
            title={title || getQueryNiceName(findOptions.queryName) } />)
            .then(a => a ? a[0] : null);
    }

    static openMany(findOptions: FindOptions, title?: string): Promise<Lite<Entity>[]> {

        return openModal<Lite<Entity>[]>(<SearchModal findOptions={findOptions}
            findMode={FindMode.Find}
            isMany={true}
            title={title || getQueryNiceName(findOptions.queryName) } />);
    }

    static explore(findOptions: FindOptions, title?: string): Promise<void> {

        return openModal<void>(<SearchModal findOptions={findOptions}
            findMode={FindMode.Explore}
            isMany={true}
            title={title || getQueryNiceName(findOptions.queryName) } />);
    }
}



