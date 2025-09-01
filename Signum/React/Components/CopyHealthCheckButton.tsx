import * as React from 'react';
import * as AppContext from '../AppContext';
import { useInterval } from '../Hooks';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { Overlay, Tooltip } from 'react-bootstrap';
import { FrameMessage } from '../Signum.Entities';



export function CopyHealthCheckButton(p: { name: string, healthCheckUrl: string, clickUrl: string }): React.ReactElement | null {

  const supportsClipboard = (navigator.clipboard && window.isSecureContext);
  if (!supportsClipboard)
    return null;

  const link = React.useRef<HTMLAnchorElement>(null);
  const [showTooltip, setShowTooltip] = React.useState<boolean>(false);
  const elapsed = useInterval(showTooltip ? 1000 : null, 0, d => d + 1);

  React.useEffect(() => {
    setShowTooltip(false);
  }, [elapsed]);

  return (
    <span >
      <a ref={link} className="btn btn-sm btn-light sf-pointer mx-1" onClick={handleCopyLiteButton}
        title="Copy Health Check dashboard data" style={{color: "var(--bs-secondary)", backgroundColor: "var(--bs-body-bg)", border: "1px solid var(--bs-border-color)"}}>
        <FontAwesomeIcon icon="heart-pulse"  /> Health Check Link
      </a>
      <Overlay target={link.current} show={showTooltip} placement="bottom">
        <Tooltip>
          {FrameMessage.Copied.niceToString()}
        </Tooltip>
      </Overlay>
    </span>
  );

  function handleCopyLiteButton(e: React.MouseEvent<any>) {
    e.preventDefault();

    navigator.clipboard.writeText(p.name + '$#$' +  p.healthCheckUrl + "$#$" + p.clickUrl)
      .then(() => setShowTooltip(true));
  }
}
