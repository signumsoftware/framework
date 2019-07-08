import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { openModal, IModalProps } from '../Modals';
import { FindOptions, FindMode, ResultRow, ModalFindOptions } from '../FindOptions'
import { getQueryNiceName, PseudoType, QueryKey, getTypeInfo } from '../Reflection'
import SearchControl, { SearchControlProps } from './SearchControl'
import { Modal } from '../Components';
import { ModalHeaderButtons } from '../Components/Modal';
import { AutoFocus } from '../Components/AutoFocus';
import { SearchMessage } from '../Signum.Entities';

interface SearchModalProps extends React.Props<SearchModal>, IModalProps {
  findOptions: FindOptions;
  findMode: FindMode;
  isMany: boolean;
  title?: React.ReactNode;
  message?: React.ReactNode;
  searchControlProps?: Partial<SearchControlProps>;
}

export default class SearchModal extends React.Component<SearchModalProps, { show: boolean; }>  {
  static marginVertical = 300;
  static minHeight = 600;

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

  componentWillMount() {
    window.addEventListener('resize', this.onResize);
  }

  componentWillUnmount() {
    window.removeEventListener('resize', this.onResize);
  }

  onResize = () => {
    var sc = this.searchControl;
    var scl = sc && sc.searchControlLoaded;
    var containerDiv = scl && scl.containerDiv;
    if (containerDiv) {

      var maxHeight = (window.innerHeight - SearchModal.marginVertical);

      containerDiv.style.maxHeight = Math.max(maxHeight, SearchModal.minHeight) + "px";
    }
  }

  render() {
    const okEnabled = this.props.isMany ? this.selectedRows.length > 0 : this.selectedRows.length == 1;

    return (
      <Modal size="lg" show={this.state.show} onExited={this.handleOnExisted} onHide={this.handleCancelClicked}>
        <ModalHeaderButtons
          onClose={this.props.findMode == "Explore" ? this.handleCancelClicked : undefined}
          onOk={this.props.findMode == "Find" ? this.handleOkClicked : undefined}
          onCancel={this.props.findMode == "Find" ? this.handleCancelClicked : undefined}
          okDisabled={!okEnabled}>
          <span className="sf-entity-title">
            {this.props.title}
          &nbsp;
          </span>
          <a className="sf-popup-fullscreen pointer" onMouseUp={(e) => this.searchControl && this.searchControl.handleFullScreenClick(e)}>
            <FontAwesomeIcon icon="external-link-alt" />
          </a>
          {this.props.message && <>
            <br />
            <small className="sf-type-nice-name text-muted"> {this.props.message}</small>
          </>
          }
        </ModalHeaderButtons>
        <div className="modal-body">
          <SearchControl
            hideFullScreenButton={true}
            throwIfNotFindable={true}
            ref={e => this.searchControl = e!}
            create={this.props.findMode == "Explore"}
            findOptions={this.props.findOptions}
            onSelectionChanged={this.handleSelectionChanged}
            showGroupButton={this.props.findMode == "Explore"}
            largeToolbarButtons={true}
            maxResultsHeight={"none"}
            enableAutoFocus={true}
            onHeighChanged={this.onResize}
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
      message={modalOptions && modalOptions.message || defaultSelectMessage(findOptions.queryName, false)}
      searchControlProps={modalOptions && modalOptions.searchControlProps}
    />)
      .then(a => a ? a[0] : undefined);
  }

  static openMany(findOptions: FindOptions, modalOptions?: ModalFindOptions): Promise<ResultRow[] | undefined> {

    return openModal<ResultRow[]>(<SearchModal findOptions={findOptions}
      findMode={"Find"}
      isMany={true}
      title={modalOptions && modalOptions.title || getQueryNiceName(findOptions.queryName)}
      message={modalOptions && modalOptions.message || defaultSelectMessage(findOptions.queryName, true)}
      searchControlProps={modalOptions && modalOptions.searchControlProps}
    />);
  }

  static explore(findOptions: FindOptions, modalOptions?: ModalFindOptions): Promise<void> {

    return openModal<void>(<SearchModal findOptions={findOptions}
      findMode={"Explore"}
      isMany={true}
      title={modalOptions && modalOptions.title || getQueryNiceName(findOptions.queryName)}
      message={modalOptions && modalOptions.message}
      searchControlProps={modalOptions && modalOptions.searchControlProps}
    />);
  }
}

export function defaultSelectMessage(queryName: PseudoType | QueryKey, plural: boolean) {

  var type = queryName instanceof QueryKey ? null : getTypeInfo(queryName);

  if (plural) {
    return type ?
      SearchMessage.PleaseSelectOneOrMore0_G.niceToString().forGenderAndNumber(type.gender, 2).formatWith(type.nicePluralName) :
      SearchMessage.PleaseSelectOneOrSeveralEntities.niceToString();
  } else {
    return type ?
      SearchMessage.PleaseSelectA0_G.niceToString().forGenderAndNumber(type.gender, 2).formatWith(type.niceName) :
      SearchMessage.PleaseSelectAnEntity.niceToString();
  }
}
