import * as React from 'react'
import { Link } from 'react-router-dom'
import { classes } from '@framework/Globals'
import * as AppContext from '@framework/AppContext'
import { ToolbarMenuEntity } from '../Signum.Toolbar'
import { ToolbarClient, ToolbarResponse } from '../ToolbarClient'
import { ToolbarConfig } from "../ToolbarConfig"
import '@framework/Frames/MenuIcons.css'
import './Toolbar.css'
import * as PropTypes from "prop-types";
import { Collapse, Modal } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useAPI } from '@framework/Hooks';
import * as Reflection from '@framework/Reflection';
import { Finder } from '@framework/Finder';
import { JavascriptMessage, getToString, SearchMessage } from '@framework/Signum.Entities';
import { IModalProps, openModal } from '@framework/Modals';
import { parseIcon } from '@framework/Components/IconTypeahead'

export interface ToolbarMainRendererProps {
}

export default function ToolbarMainRenderer(p: ToolbarMainRendererProps): React.JSX.Element {
  var response = useAPI(signal => ToolbarClient.API.getCurrentToolbar("Main").then(t => t ?? null), []);

  if (response === undefined)
    return <span>{JavascriptMessage.loading.niceToString()}</span>;

  if (response === null)
    return <span>{SearchMessage.NoResultsFound.niceToString()}</span>;

  return (<ToolbarMainRendererPrivate response={response} />);
}

function ToolbarMainRendererPrivate({ response }: { response: ToolbarResponse<any> }) {
  return (
    <div>
      {
        response.elements!.groupWhen(a => a.type == "Divider" || a.type == "Header", false, "defaultGroup").map((gr, i) => <div key={i}>
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

function CollapsableBlock({ r }: { r: ToolbarResponse<any> }) {
  const [isOpen, setIsOpen] = React.useState(false);
  return (
    <div>
      <h4 style={{ cursor: "pointer" }} onClick={e => { e.preventDefault(); setIsOpen(!isOpen); }}><FontAwesomeIcon aria-hidden={true} icon={isOpen ? "chevron-down" : "chevron-right"} /> {r.label ?? getToString(r.content!)}</h4>
      <Collapse in={isOpen}>
        <div>
          <ToolbarMainRendererPrivate response={r} />
        </div>
      </Collapse>
    </div>
  );
}

function ToolbarIconButton({ tr }: { tr: ToolbarResponse<any> }) {

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
    <a href="#" onMouseDown={e => { e.preventDefault(); config.handleNavigateClick(e, tr, null); }}>
      <div className="card toolbar-card">
        <div className="card-img-top" style={{ fontSize: "60px" }}>
          {config.getIcon(tr, null)}
        </div>
        <div className="card-body">
          <h5 className="card-title">{tr.label}</h5>
        </div>
      </div>
    </a>
  );
}



interface ToolbarMainModalModalProps extends IModalProps<undefined> {
  tr: ToolbarResponse<any>;
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


ToolbarMainModalModal.show = (tr: ToolbarResponse<any>): Promise<undefined> => {
  return openModal<undefined>(<ToolbarMainModalModal tr={tr} />);
}
