import * as React from 'react';
import { Overlay, Tooltip } from "react-bootstrap";
import { Entity, FrameMessage, liteKey, NormalControlMessage, toLite } from '../Signum.Entities';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useInterval } from '../Hooks';

interface CopyLiteButtonProps {
  entity: Entity;
  className?: string;
}

export default function CopyLiteButton(p: CopyLiteButtonProps): React.ReactElement | null {

  const supportsClipboard = (navigator.clipboard && window.isSecureContext);
  if (p.entity.isNew || !supportsClipboard)
    return null;

  const link = React.useRef<HTMLAnchorElement>(null);
  const [showTooltip, setShowTooltip] = React.useState<boolean>(false);
  const elapsed = useInterval(showTooltip ? 1000 : null, 0, d => d + 1);

  React.useEffect(() => {
    setShowTooltip(false);
  }, [elapsed]);

  return (
    <span className={p.className}>
      <a ref={link} className="btn btn-sm btn-tertiary sf-pointer mx-1" onClick={handleCopyLiteButton} title={NormalControlMessage.CopyEntityTypeAndIdForAutocomplete.niceToString()} >
        <FontAwesomeIcon icon="copy" color="gray" />
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
    const lk = liteKey(toLite(p.entity as Entity));

    navigator.clipboard.writeText(lk)
      .then(() => setShowTooltip(true));
  }
}
