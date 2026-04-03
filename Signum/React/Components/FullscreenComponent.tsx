import * as React from 'react'
import "./FullscreenComponent.css"
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { EntityControlMessage } from '../Signum.Entities';
import { LinkButton } from '../Basics/LinkButton';


interface FullscreenComponentProps {
  children: (fullScreen: boolean) =>  React.ReactNode;
  onReload?: (e: React.MouseEvent<any>) => void;
}

export function FullscreenComponent(p: FullscreenComponentProps): React.ReactElement {

  const [isFullScreen, setIsFullScreen] = React.useState(false);

  function handleExpandToggle(e: React.MouseEvent<any>) {
    setIsFullScreen(!isFullScreen);
  }

  return (
    <div className="sf-fullscreen-component" style={!isFullScreen ? { display: "flex", flex: 1 } : ({
      display: "flex",
      position: "fixed",
      background: "var(--bs-body-bg)",
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      height: "auto",
      zIndex: 9999,
    })}>

      <div style={{ flexGrow: 1, display: "flex", width: "0px" }}>
        {p.children(isFullScreen)}
      </div>
      <div style={{ display: "flex", flexDirection: "column", marginLeft: "5px" }}>
        <LinkButton onClick={handleExpandToggle} tabIndex={0} className="sf-chart-mini-icon" title={isFullScreen ? EntityControlMessage.Minimize.niceToString() : EntityControlMessage.Maximize.niceToString()}  >
          <FontAwesomeIcon aria-hidden={true} icon={isFullScreen ? "compress" : "expand"} />
        </LinkButton>
        {p.onReload &&
          <LinkButton onClick={e => { p.onReload!(e); }} className="sf-chart-mini-icon" title={EntityControlMessage.Reload.niceToString()} >
            <FontAwesomeIcon aria-hidden={true} icon={"arrow-rotate-right"} />
          </LinkButton>
        }
      </div>

    </div>
  );
}



