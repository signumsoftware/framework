import * as React from 'react'
import { Link } from 'react-router-dom'
import { classes } from '@framework/Globals'
import * as AppContext from '@framework/AppContext'
import { ToolbarMenuEntity } from '../Signum.Entities.Toolbar'
import * as ToolbarClient from '../ToolbarClient'
import { ToolbarConfig } from "../ToolbarClient";
import '@framework/Frames/MenuIcons.css'
import './Toolbar.css'
import * as PropTypes from "prop-types";
import { Collapse, Modal } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useAPI } from '@framework/Hooks';
import * as Reflection from '@framework/Reflection';
import * as Finder from '@framework/Finder';
import { JavascriptMessage, getToString, SearchMessage } from '@framework/Signum.Entities';
import { IModalProps, openModal } from '@framework/Modals';
import { parseIcon } from '../../Basics/Templates/IconTypeahead'

export interface ToolbarMainRendererProps {
}

export default function ToolbarMainRenderer(p: ToolbarMainRendererProps) {
  var response = useAPI(signal => ToolbarClient.API.getCurrentToolbar("Main").then(t => t ?? null), []);

  if (response === undefined)
    return <span>{JavascriptMessage.loading.niceToString()}</span>;

  if (response === null)
    return <span>{SearchMessage.NoResultsFound.niceToString()}</span>;

  return (<ToolbarMainRendererPrivate response={response} />);
}

function ToolbarMainRendererPrivate({ response }: { response: ToolbarClient.ToolbarResponse<any> }) {
  return (
    <div>
      {
        response.elements!.groupWhen(a => a.type == "Divider" || a.type == "Header", false, true).map((gr, i) => <div key={i}>
          {gr.key && gr.key.type == "Divider" && <hr />}
          {gr.key && gr.key.type == "Header" && ToolbarMenuEntity.isLite(gr.key.content) && <CollapsableBlock r={gr.key} />}
          {gr.key && gr.key.type == "Header" && !ToolbarMenuEntity.isLite(gr.key.content) && <h4>{gr.key.label ?? getToString(gr.key.content!)}</h4>}
          {gr.elements.length > 0 && <div className="row">
            {gr.elements.map((tr, j) => <div key={j} className="toolbar-card-container">
              <ToolbarIconButton tr={tr} />
            </div>)}
          </div>}
        </div>
        )
      }
    </div>
  );
}

function CollapsableBlock({ r }: { r: ToolbarClient.ToolbarResponse<any> }) {
  const [isOpen, setIsOpen] = React.useState(false);
  return (
    <div>
      <h4 style={{ cursor: "pointer" }} onClick={e => { e.preventDefault(); setIsOpen(!isOpen); }}><FontAwesomeIcon icon={isOpen ? "chevron-down" : "chevron-right"} /> {r.label ?? getToString(r.content!)}</h4>
      <Collapse in={isOpen}>
        <div>
          <ToolbarMainRendererPrivate response={r} />
        </div>
      </Collapse>
    </div>
  );
}

function ToolbarIconButton({ tr }: { tr: ToolbarClient.ToolbarResponse<any> }) {

  if (tr.elements && tr.elements.length > 0) {
    return (
      <a href="#" onClick={e => { e.preventDefault(); ToolbarMainModalModal.show(tr); }}>
        <div className="card toolbar-card">
          <div className="card-img-top" style={{ fontSize: "60px" }}>
            {ToolbarConfig.coloredIcon(parseIcon(tr.iconName), tr.iconColor)}
          </div>
          <div className="card-body">
            <h5 className="card-title">{tr.label ?? getToString(tr.content!)}</h5>
          </div>
        </div>
      </a>
    );
  }


  if (tr.url) {
    return (
      <a href="#" onMouseDown={e => { e.preventDefault(); AppContext.pushOrOpenInTab(tr.url!, e); }}>
        <div className="card toolbar-card">
          <div className="card-img-top" style={{ fontSize: "60px" }}>
            {ToolbarConfig.coloredIcon(parseIcon(tr.iconName), tr.iconColor)}
          </div>
          <div className="card-body">
            <h5 className="card-title">{tr.label}</h5>
          </div>
        </div>
      </a>
    );
  }

  const config = ToolbarClient.getConfig(tr);
  if (config == null)
    return (
      <div className="card toolbar-card text-danger">
        {tr.content!.EntityType} ToolbarConfig not registered
      </div>
    );

  return (
    <a href="#" onMouseDown={e => { e.preventDefault(); config.handleNavigateClick(e, tr); }}>
      <div className="card toolbar-card">
        <div className="card-img-top" style={{ fontSize: "60px" }}>
          {config.getIcon(tr)}
        </div>
        <div className="card-body">
          <h5 className="card-title">{tr.label}</h5>
        </div>
      </div>
    </a>
  );
}



interface ToolbarMainModalModalProps extends IModalProps<undefined> {
  tr: ToolbarClient.ToolbarResponse<any>;
}

interface ToolbarMainModalModalState {
  show: boolean;
}

function ToolbarMainModalModal(p: ToolbarMainModalModalProps) {

  const [show, setShow] = React.useState<boolean>(true);

  function handleCloseClicked() {
    setShow(false);
  }

  function handleOnExited() {
    p.onExited!(undefined);
  }

  return (
    <Modal onHide={handleCloseClicked} show={show} className="message-modal" onExited={handleOnExited} size="xl">
      <div className="modal-header">
        <h5 className="modal-title">{p.tr.label ?? getToString(p.tr.content!)}</h5>
        <button type="button" className="btn-close" data-dismiss="modal" aria-label="Close" onClick={handleCloseClicked}/>
      </div>
      <div className="modal-body">
        <ToolbarMainRendererPrivate response={p.tr} />
      </div>
    </Modal>
  );
}


ToolbarMainModalModal.show = (tr: ToolbarClient.ToolbarResponse<any>): Promise<undefined> => {
  return openModal<undefined>(<ToolbarMainModalModal tr={tr} />);
}
