import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { openModal, IModalProps } from '../Modals';
import { Finder } from '../Finder';
import { FindOptions, FindMode, ResultRow, ModalFindOptions, ModalFindOptionsMany, FindOptionsParsed, ResultTable } from '../FindOptions'
import { getQueryNiceName, PseudoType, QueryKey, getTypeInfo } from '../Reflection'
import SearchControl, { SearchControlProps, SearchControlHandler } from './SearchControl'
import { AutoFocus } from '../Components/AutoFocus';
import { Entity, EntityPack, FrameMessage, getToString, isEntityPack, isLite, Lite, ModifiableEntity, SearchMessage, toLite, is } from '../Signum.Entities';
import { ModalFooterButtons, ModalHeaderButtons } from '../Components/ModalHeaderButtons';
import { Modal, Dropdown } from 'react-bootstrap';
import { useForceUpdate } from '../Hooks';
import SearchControlLoaded from './SearchControlLoaded';
import MessageModal from '../Modals/MessageModal';
import { BsSize } from '../Components';
import { DomUtils } from '../Globals';

interface SearchModalProps extends IModalProps<{ rows: ResultRow[], searchControl: SearchControlLoaded } | undefined> {
  findOptions: FindOptions;
  findMode: FindMode;
  isMany: boolean;
  allowNoSelection?: boolean;
  title?: React.ReactNode;
  message?: React.ReactNode;
  size?: BsSize;
  avoidReturnCreate?: boolean;
  searchControlProps?: Partial<SearchControlProps>;
  autoCheckSingleRowResult?: boolean;
  onOKClicked?: (sc: SearchControlLoaded) => Promise<boolean>;
}

function SearchModal(p: SearchModalProps): React.ReactElement {

  const [show, setShow] = React.useState(true);

  const selectedRows = React.useRef<ResultRow[]>([]);
  const [singleResultRow, setSingleResultRow] = React.useState<ResultRow | null>(null);
  const okPressed = React.useRef<boolean>(false);
  const forceUpdate = useForceUpdate();
  const searchControl = React.useRef<SearchControlHandler>(null);

  React.useEffect(() => {
    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  });

  if (p.autoCheckSingleRowResult)
    React.useEffect(() => {

      let scl = searchControl.current?.searchControlLoaded;
      if (scl && singleResultRow) {       
        scl.setState({ selectedRows: [singleResultRow] }, () => handleSelectionChanged([singleResultRow]));
      }
    }, [singleResultRow]);

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
      });
  }

  function handleCancelClicked() {
    okPressed.current = false;
    setShow(false);
  }

  function handleOnExited() {
    p.onExited!(okPressed.current ? { rows: selectedRows.current, searchControl: searchControl.current!.searchControlLoaded! } : undefined);
  }

  function handleDoubleClick(e: React.MouseEvent<any>, row: ResultRow) {
    e.preventDefault();
    selectedRows.current = [row];
    searchControl.current!.searchControlLoaded!.state.selectedRows!.clear();
    searchControl.current!.searchControlLoaded!.state.selectedRows!.push(row);
    handleOkClicked();
  }

  function handleOnResult(table: ResultTable, dataChange: boolean, scl: SearchControlLoaded) {
    if (!p.autoCheckSingleRowResult)
      return;

    if (table.rows.length == 1)
      setSingleResultRow(table.rows[0])
    else
      setSingleResultRow(null)
  }

  function handleCreateFinished(entity: EntityPack<Entity> | ModifiableEntity | Lite<Entity> | undefined | void) {

    const scl = searchControl.current!.searchControlLoaded!;
    if (p.findMode == "Find" && entity != null && !p.avoidReturnCreate && !p.findOptions.groupResults && !p.onOKClicked) {
      const e = isEntityPack(entity) ? entity.entity : entity;
      const ti = getTypeInfo(isLite(e) ? e.EntityType : e.Type);
      MessageModal.show({
        buttons: "yes_no",
        style: "success",
        customIcon: "check-square",
        title: SearchMessage.ReturnNewEntity.niceToString(),
        message: SearchMessage.DoYouWantToSelectTheNew01_G.niceToString().forGenderAndNumber(ti.gender).formatHtml(ti.niceName, <strong>{getToString(e)}</strong>)
      }).then(b => {
        if (b == "yes") {
          selectedRows.current = [{ entity: isLite(e) ? e : toLite(e as Entity), columns: [] }];
          okPressed.current = true;
          setShow(false);
        } else
          scl.dataChanged();
      });

    } else 
      scl.dataChanged();
  }


  function handleInputsKeyUp(e: React.KeyboardEvent<HTMLDivElement>) {
    if (e.key == "Enter") {

      (e.target as HTMLElement).blur()

      window.setTimeout(() => {
        handleOkClicked();
      }, 100);
    }
  }

  function onResize() {
    var sc = searchControl.current;
    var scl = sc?.searchControlLoaded;
    var containerDiv = scl?.containerDiv;
    if (containerDiv) {
      var rect = containerDiv.getBoundingClientRect();
      var modalContent = DomUtils.closest(containerDiv.offsetParent as HTMLElement, ".modal-content")!;
      const marginModal = 28; 
      const marginTop = rect.top + marginModal;
      const marginButton = (modalContent!.offsetHeight - rect.bottom) + marginModal;
      const maxHeight = window.innerHeight - marginTop - marginButton;
      containerDiv.style.maxHeight = Math.max(maxHeight, SearchModal.minHeight) + "px";
    }
  }

  var qs = Finder.getSettings(p.findOptions.queryName);

  const okEnabled = p.isMany ? (p.allowNoSelection || selectedRows.current.length > 0) : selectedRows.current.length == 1;

  return (
    <Modal size={(p.size ?? qs?.modalSize ?? "lg") as any} show={show} onExited={handleOnExited} onHide={handleCancelClicked} className="sf-search-modal">
      <ModalHeaderButtons onClose={p.findMode == "Explore" ? handleCancelClicked : undefined}>
        <span className="sf-entity-title">
          {p.title}
          &nbsp;
        </span>
        <a className="sf-popup-fullscreen" href="#" onClick={(e) => { e.preventDefault(); searchControl.current && searchControl.current.searchControlLoaded!.handleFullScreenClick(e); } }>
          <FontAwesomeIcon icon="up-right-from-square" title={FrameMessage.Fullscreen.niceToString()} />
        </a>
        {p.message && <>
          <br />
          <small className="sf-type-nice-name text-muted"> {p.message}</small>
        </>
        }
      </ModalHeaderButtons>
      <div className="modal-body" onKeyUp={handleInputsKeyUp} >
        <SearchControl 
          ref={searchControl}
          hideFullScreenButton={true}
          throwIfNotFindable={true}
          findOptions={p.findOptions}
          defaultIncludeDefaultFilters={true}
          onSelectionChanged={handleSelectionChanged}
          showGroupButton={p.findMode == "Explore"}
          showSystemTimeButton={p.findMode == "Explore"}
          largeToolbarButtons={true}
          showFooter={true}
          maxResultsHeight={"none"}
          enableAutoFocus={true}
          onCreateFinished={handleCreateFinished}
          onHeighChanged={onResize}
          onDoubleClick={p.findMode == "Find" ? handleDoubleClick : undefined}
          onResult={handleOnResult}
          {...p.searchControlProps}
        />
      </div>
      {p.findMode == "Find" &&
        <ModalFooterButtons
          onOk={handleOkClicked}
          onCancel={handleCancelClicked}
          okDisabled={!okEnabled} />
      }
    </Modal>
  );
}


namespace SearchModal {
  export let minHeight = 400;

  export function open(findOptions: FindOptions, modalOptions?: ModalFindOptions): Promise<{ row: ResultRow, searchControl: SearchControlLoaded } | undefined> {

    return openModal<{ rows: ResultRow[], searchControl: SearchControlLoaded } | undefined>(<SearchModal
      findOptions={findOptions}
      findMode={"Find"}
      isMany={false}
      title={modalOptions?.title ?? getQueryNiceName(findOptions.queryName)}
      message={modalOptions?.message ?? defaultSelectMessage(findOptions.queryName, false, modalOptions?.forProperty)}
      size={modalOptions?.modalSize}
      searchControlProps={modalOptions?.searchControlProps}
      autoCheckSingleRowResult={modalOptions?.autoCheckSingleRowResult}
      onOKClicked={modalOptions?.onOKClicked}
    />)
      .then(a => a && { row: a.rows[0], searchControl: a.searchControl });
  }

  export function openMany(findOptions: FindOptions, modalOptions?: ModalFindOptionsMany): Promise<{ rows: ResultRow[], searchControl: SearchControlLoaded } | undefined> {
    return openModal<{ rows: ResultRow[], searchControl: SearchControlLoaded } | undefined>(<SearchModal findOptions={findOptions}
      findMode={"Find"}
      isMany={true}
      allowNoSelection={modalOptions?.allowNoSelection}
      title={modalOptions?.title ?? getQueryNiceName(findOptions.queryName)}
      message={modalOptions?.message ?? defaultSelectMessage(findOptions.queryName, true, modalOptions?.forProperty)}
      size={modalOptions?.modalSize}
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
      size={modalOptions?.modalSize}
      searchControlProps={modalOptions?.searchControlProps}
    />);
  }
}

export default SearchModal;

export function defaultSelectMessage(queryName: PseudoType | QueryKey, plural: boolean, forProperty?: string): string {

  var type = queryName instanceof QueryKey ? null : getTypeInfo(queryName);

  if (plural) {
    return type ?
      SearchMessage.PleaseSelectOneOrMore0_G.niceToString().forGenderAndNumber(type.gender, 2).formatWith(forProperty ?? type.nicePluralName) :
      SearchMessage.PleaseSelectOneOrSeveralEntities.niceToString();
  } else {
    return type ?
      SearchMessage.PleaseSelectA0_G.niceToString().forGenderAndNumber(type.gender, 2).formatWith(forProperty ?? type.niceName) :
      SearchMessage.PleaseSelectAnEntity.niceToString();
  }
}
