import * as React from 'react'
import { SearchMessage } from '@framework/Signum.Entities'
import { ChartMessage } from '../Signum.Entities.Chart'

import "../Chart.css"

import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { TypeInfo } from '@framework/Reflection'

interface FullscreenComponentProps {
  children: React.ReactNode;
  onReload?: (e: React.MouseEvent<any>) => void;
}

export function FullscreenComponent(p: FullscreenComponentProps) {

  const [isFullScreen, setIsFullScreen] = React.useState(false);

  function handleExpandToggle(e: React.MouseEvent<any>) {
    e.preventDefault();
    setIsFullScreen(!isFullScreen);
  }

  return (
    <div className="sf-fullscreen-component" style={!isFullScreen ? { display: "flex", flex: 1 } : ({
      display: "flex",
      position: "fixed",
      background: "white",
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      height: "auto",
      zIndex: 9,
    })}>

      <div key={isFullScreen ? "A" : "B"} style={{ flexGrow: 1, display: "flex", width: "0px" }}>
        {p.children}
      </div>
      <div style={{ display: "flex", flexDirection: "column", marginLeft: "5px" }}>
        <a onClick={handleExpandToggle} href="#" className="sf-chart-mini-icon" title={isFullScreen ? ChartMessage.Minimize.niceToString() : ChartMessage.Maximize.niceToString()}  >
          <FontAwesomeIcon icon={isFullScreen ? "compress" : "expand"} />
        </a>
        {p.onReload &&
          <a onClick={p.onReload} href="#" className="sf-chart-mini-icon" title={ChartMessage.Reload.niceToString()} >
            <FontAwesomeIcon icon={"arrow-rotate-right"} />
          </a>
        }
      </div>

    </div>
  );
}



