import * as React from 'react';
import { Overlay, Tooltip } from 'react-bootstrap';
import { FrameMessage } from '../Signum.Entities';
import { LinkButton } from '../Basics/LinkButton';

interface CopyButtonProps {
  getText: () => string;
  className?: string;
  title?: string;
  children: React.ReactNode;
}

export default function CopyButton(p: CopyButtonProps): React.ReactElement | null {
  const supportsClipboard = (navigator.clipboard && window.isSecureContext);
  const link = React.useRef<HTMLAnchorElement>(null);
  const [showTooltip, setShowTooltip] = React.useState<boolean>(false);
  const [elapsed, setElapsed] = React.useState(0);

  React.useEffect(() => {
    if (!showTooltip) return;
    const timer = setTimeout(() => {
      setShowTooltip(false);
      setElapsed(e => e + 1);
    }, 1000);
    return () => clearTimeout(timer);
  }, [showTooltip, elapsed]);

  if (!supportsClipboard)
    return null;

  function handleCopy(e: React.MouseEvent<any>) {
    e.preventDefault();
    const text = p.getText();
    if (!text) return;
    navigator.clipboard.writeText(text)
      .then(() => setShowTooltip(true));
  }

  return (
    <span className={p.className}>
      <LinkButton ref={link} className="btn btn-sm btn-tertiary sf-pointer mx-1" onClick={handleCopy} title={p.title}>
        {p.children}
      </LinkButton>
      <Overlay target={link.current} show={showTooltip} placement="bottom">
        <Tooltip>
          {FrameMessage.Copied.niceToString()}
        </Tooltip>
      </Overlay>
    </span>
  );
}
