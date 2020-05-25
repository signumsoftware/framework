import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { openModal, IModalProps } from '../Modals';
import { FindOptions, FindMode, ResultRow, ModalFindOptions } from '../FindOptions'
import { getQueryNiceName, PseudoType, QueryKey, getTypeInfo } from '../Reflection'
import SearchControl, { SearchControlProps, SearchControlHandler } from './SearchControl'
import { AutoFocus } from '../Components/AutoFocus';
import { SearchMessage } from '../Signum.Entities';
import { ModalHeaderButtons } from '../Components/ModalHeaderButtons';
import { Modal, Dropdown } from 'react-bootstrap';
import { namespace } from 'd3';
import { useForceUpdate } from '../Hooks';
import SearchControlLoaded from './SearchControlLoaded';

interface SearchModalProps extends IModalProps<ResultRow[] | undefined> {
  findOptions: FindOptions;
  findMode: FindMode;
  isMany: boolean;
  title?: React.ReactNode;
  message?: React.ReactNode;
  searchControlProps?: Partial<SearchControlProps>;
  onOKClicked?: (sc: SearchControlLoaded) => Promise<boolean>;
}

function SearchModal(p: SearchModalProps) {

  const [show, setShow] = React.useState(true);

  const selectedRows = React.useRef<ResultRow[]>([]);
  const okPressed = React.useRef<boolean>(false);
  const forceUpdate = useForceUpdate();
  const searchControl = React.useRef<SearchControlHandler>(null);

  React.useEffect(() => {
    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  });

  function handleSelectionChanged(selected: ResultRow[]) {
    selectedRows.current = selected;
    forceUpdate();
  }

  function handleOkClicked() {
    if (!p.onOKClicked) {
      okPressed.current = true;
      setShow(false);
    }
    else
      p.onOKClicked(searchControl.current!.searchControlLoaded!).then(result => {
        if (result) {
          okPressed.current = true;
          setShow(false);
        };
      }).done();
  }

  function handleCancelClicked() {
    okPressed.current = false;
    setShow(false);
  }

  function handleOnExited() {
    p.onExited!(okPressed.current ? selectedRows.current : undefined);
  }

  function handleDoubleClick(e: React.MouseEvent<any>, row: ResultRow) {
    e.preventDefault();
    selectedRows.current = [row];
    searchControl.current!.searchControlLoaded!.state.selectedRows!.clear();
    searchControl.current!.searchControlLoaded!.state.selectedRows!.push(row);
    handleOkClicked();
  }

  function onResize() {
    var sc = searchControl.current;
    var scl = sc?.searchControlLoaded;
    var containerDiv = scl?.containerDiv;
    if (containerDiv) {
      var maxHeight = (window.innerHeight - SearchModal.marginVertical);
      containerDiv.style.maxHeight = Math.max(maxHeight, SearchModal.minHeight) + "px";
    }
  }

  const okEnabled = p.isMany ? selectedRows.current.length > 0 : selectedRows.current.length == 1;

  return (
    <Modal size="lg" show={show} onExited={handleOnExited} onHide={handleCancelClicked} className="sf-search-modal">
      <ModalHeaderButtons
        onClose={p.findMode == "Explore" ? handleCancelClicked : undefined}
        onOk={p.findMode == "Find" ? handleOkClicked : undefined}
        onCancel={p.findMode == "Find" ? handleCancelClicked : undefined}
        okDisabled={!okEnabled}>
        <span className="sf-entity-title">
          {p.title}
          &nbsp;
          </span>
        <a className="sf-popup-fullscreen pointer" onMouseUp={(e) => searchControl.current && searchControl.current.searchControlLoaded!.handleFullScreenClick(e)}>
          <FontAwesomeIcon icon="external-link-alt" />
        </a>
        {p.message && <>
          <br />
          <small className="sf-type-nice-name text-muted"> {p.message}</small>
        </>
        }
      </ModalHeaderButtons>
      <div className="modal-body">
        <SearchControl
          ref={searchControl}
          hideFullScreenButton={true}
          throwIfNotFindable={true}
          findOptions={p.findOptions}
          defaultIncludeDefaultFilters={true}
          onSelectionChanged={handleSelectionChanged}
          showGroupButton={p.findMode == "Explore"}
          largeToolbarButtons={true}
          maxResultsHeight={"none"}
          enableAutoFocus={true}
          onHeighChanged={onResize}
          onDoubleClick={p.findMode == "Find" ? handleDoubleClick : undefined}
          {...p.searchControlProps}
        />
      </div>
    </Modal>
  );
}


namespace SearchModal {
  export let marginVertical = 300;
  export let minHeight = 600;

  export function open(findOptions: FindOptions, modalOptions?: ModalFindOptions): Promise<ResultRow | undefined> {

    return openModal<ResultRow[]>(<SearchModal
      findOptions={findOptions}
      findMode={"Find"}
      isMany={false}
      title={modalOptions?.title ?? getQueryNiceName(findOptions.queryName)}
      message={modalOptions?.message ?? defaultSelectMessage(findOptions.queryName, false)}
      searchControlProps={modalOptions?.searchControlProps}
      onOKClicked={modalOptions?.onOKClicked}
    />)
      .then(a => a ? a[0] : undefined);
  }

  export function openMany(findOptions: FindOptions, modalOptions?: ModalFindOptions): Promise<ResultRow[] | undefined> {

    return openModal<ResultRow[]>(<SearchModal findOptions={findOptions}
      findMode={"Find"}
      isMany={true}
      title={modalOptions?.title ?? getQueryNiceName(findOptions.queryName)}
      message={modalOptions?.message ?? defaultSelectMessage(findOptions.queryName, true)}
      searchControlProps={modalOptions?.searchControlProps}
      onOKClicked={modalOptions?.onOKClicked}
    />);
  }

  export function explore(findOptions: FindOptions, modalOptions?: ModalFindOptions): Promise<void> {

    return openModal<void>(<SearchModal findOptions={findOptions}
      findMode={"Explore"}
      isMany={true}
      title={modalOptions?.title ?? getQueryNiceName(findOptions.queryName)}
      message={modalOptions?.message}
      searchControlProps={modalOptions?.searchControlProps}
    />);
  }
}

export default SearchModal;

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
