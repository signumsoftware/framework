import * as React from 'react';
import { Overlay, Tooltip } from "react-bootstrap";
import { Entity, FrameMessage, liteKey, toLite } from '../Signum.Entities';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useInterval } from '../Hooks';

interface CopyLiteButtonProps {
  entity: Entity;
  className?: string;
}

export default function CopyLiteButton(p: CopyLiteButtonProps) {

  const supportsClipboard = (navigator.clipboard && window.isSecureContext);
  if (p.entity.isNew || !supportsClipboard)
    return null;

  const lk = liteKey(toLite(p.entity as Entity));
  const link = React.useRef<HTMLAnchorElement>(null);
  const [showTooltip, setShowTooltip] = React.useState<boolean>(false);
  const elapsed = useInterval(showTooltip ? 1000 : null, 0, d => d + 1);

  React.useEffect(() => {
    setShowTooltip(false);
  }, [elapsed]);

  return (
    <span className={p.className}>
      <a ref={link} className="btn btn-sm btn-light text-dark sf-pointer mx-2" onClick={handleCopyLiteButton} >
        <FontAwesomeIcon icon="copy" color="gray" />
      </a>
      <Overlay target={link.current} show={showTooltip} placement="bottom">
        <Tooltip id={lk + "_tooltip"}>
          {FrameMessage.Copied.niceToString()}
        </Tooltip>
      </Overlay>
    </span>
  );

  function handleCopyLiteButton(e: React.MouseEvent<any>) {
    e.preventDefault();
    navigator.clipboard.writeText(lk)
      .then(() => setShowTooltip(true))
      .done();
  }
}
