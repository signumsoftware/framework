import * as React from 'react'
import { Link } from 'react-router-dom'
import { classes } from '@framework/Globals'
import { Modal } from '@framework/Components/Modal'
import * as Navigator from '@framework/Navigator'
import { ToolbarLocation, ToolbarMenuEntity } from '../Signum.Entities.Toolbar'
import * as ToolbarClient from '../ToolbarClient'
import { ToolbarConfig } from "../ToolbarClient";
import '@framework/Frames/MenuIcons.css'
import './Toolbar.css'
import * as PropTypes from "prop-types";
import { Dropdown, DropdownToggle, DropdownMenu, DropdownItem, NavItem, Collapse } from '@framework/Components';
import { NavLink } from '@framework/Components/NavItem';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { parseIcon } from '../../Dashboard/Admin/Dashboard';
import { coalesceIcon } from '@framework/Operations/ContextualOperations';
import { useAPI } from '@framework/Hooks';
import * as Reflection from '@framework/Reflection';
import * as Finder from '@framework/Finder';
import { JavascriptMessage, getToString } from '@framework/Signum.Entities';
import { IModalProps, openModal } from '../../../../Framework/Signum.React/Scripts/Modals';

export interface ToolbarMainRendererProps {
}

export default function ToolbarMainRenderer(p: ToolbarMainRendererProps) {
  var response = useAPI(undefined, [], signal => ToolbarClient.API.getCurrentToolbar("Main"));

  if (response == null)
    return <span>{JavascriptMessage.loading.niceToString()}</span>;

  return (<ToolbarMainRendererPrivate response={response} />);
}

function ToolbarMainRendererPrivate({ response }: { response: ToolbarClient.ToolbarResponse<any> }) {
  debugger;
  return (
    <div>
      {
        response.elements!.groupWhen(a => a.type == "Divider" || a.type == "Header", false, true).map((gr, i) => <div key={i}>
          {gr.key && gr.key.type == "Divider" && <hr />}
          {gr.key && gr.key.type == "Header" && ToolbarMenuEntity.isLite(gr.key.content) && <CollapsableBlock r={gr.key} />}
          {gr.key && gr.key.type == "Header" && !ToolbarMenuEntity.isLite(gr.key.content) && <h4>{gr.key.label || getToString(gr.key.content!)}</h4>}
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
      <h4 style={{ cursor: "pointer" }} onClick={e => { e.preventDefault(); setIsOpen(!isOpen); }}><FontAwesomeIcon icon={isOpen ? "chevron-down" : "chevron-right"} /> {r.label || getToString(r.content!)}</h4>
      <Collapse isOpen={isOpen}>
        <ToolbarMainRendererPrivate response={r} />
      </Collapse>
    </div>
  );
}

function ToolbarIconButton({ tr  }: { tr: ToolbarClient.ToolbarResponse<any> }) {

  if (tr.elements && tr.elements.length > 0) {
    return (
      <a href="#" onClick={e => { e.preventDefault(); ToolbarMainModalModal.show(tr).done(); }}>
        <div className="card toolbar-card">
          <div className="card-img-top" style={{ fontSize: "60px" }}>
            {ToolbarConfig.coloredIcon(parseIcon(tr.iconName), tr.iconColor)}
          </div>
          <div className="card-body">
            <h5 className="card-title">{tr.label || getToString(tr.content!)}</h5>
          </div>
        </div>
      </a>
    );
  }


  if (tr.url) {
    return (
      <a href="#" onMouseDown={e => { e.preventDefault(); Navigator.pushOrOpenInTab(tr.url!, e); }}>
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

  const config = ToolbarClient.configs[tr.content!.EntityType];
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
          <h5 className="card-title">{config.getLabel(tr)}</h5>
        </div>
      </div>
    </a>
  );
}



interface ToolbarMainModalModalProps extends IModalProps {
  tr: ToolbarClient.ToolbarResponse<any>;
}

interface ToolbarMainModalModalState {
  show: boolean;
}

class ToolbarMainModalModal extends React.Component<ToolbarMainModalModalProps, ToolbarMainModalModalState> {

  constructor(props: ToolbarMainModalModalProps) {
    super(props);
    this.state = { show: true };
  }

  handleCloseClicked = () => {
    this.setState({ show: false });
  }

  handleOnExited = () => {
    this.props.onExited!(undefined);
  }

  render() {
    return (
      <Modal onHide={this.handleCloseClicked} show={this.state.show} className="message-modal" onExited={this.handleOnExited} size="xl">
        <div className="modal-header">
          <h5 className="modal-title">{this.props.tr.label || getToString(this.props.tr.content!)}</h5>
          <button type="button" className="close" data-dismiss="modal" aria-label="Close" onClick={this.handleCloseClicked}>
            <span aria-hidden="true">&times;</span>
          </button>
        </div>
        <div className="modal-body">
          <ToolbarMainRendererPrivate response={this.props.tr} />
        </div>
      </Modal>
    );
  }

  static show(tr: ToolbarClient.ToolbarResponse<any>): Promise<undefined> {
    return openModal<undefined>(<ToolbarMainModalModal tr={tr} />);
  }
}
