import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Tooltip } from 'react-bootstrap'
import { Overlay } from 'react-bootstrap'
import { HtmlViewer } from '../Admin/TextPart'

export interface DashboardTooltipIconProps {
  tooltipHtml: string;
  placement?: 'auto' | 'top' | 'bottom' | 'left' | 'right';
  className?: string;
  iconClassName?: string;
}

export function DashboardTooltipIcon(p: DashboardTooltipIconProps): React.JSX.Element {
  const [show, setShow] = React.useState(false);
  const targetRef = React.useRef<HTMLSpanElement>(null);

  const handleClick = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setShow(!show);
  };

  const handleClickOutside = React.useCallback((e: MouseEvent) => {
    if (targetRef.current && !targetRef.current.contains(e.target as Node)) {
      setShow(false);
    }
  }, []);

  React.useEffect(() => {
    if (show) {
      document.addEventListener('mousedown', handleClickOutside);
      return () => {
        document.removeEventListener('mousedown', handleClickOutside);
      };
    }
  }, [show, handleClickOutside]);

  return (
    <>
      <span
        ref={targetRef}
        className={p.className}
        onClick={handleClick}
        style={{ cursor: 'pointer' }}
      >
        <FontAwesomeIcon
          aria-hidden={true}
          icon="circle-info"
          className={p.iconClassName}
        />
      </span>
      <Overlay
        show={show}
        target={targetRef.current}
        placement="auto-start"
        rootClose={false}
        container={document.body}
        popperConfig={{
          strategy: 'fixed',
          modifiers: [
            {
              name: 'preventOverflow',
              options: {
                boundary: 'clippingParents',
                rootBoundary: 'viewport',
                padding: 16,
                altAxis: true,
                tether: false,
              },
            },
            {
              name: 'flip',
              enabled: true,
              options: {
                fallbackPlacements: ['left', 'top', 'bottom', 'right'],
                padding: 16,
              },
            },
            {
              name: 'offset',
              options: {
                offset: [0, 8],
              },
            },
          ],
        }}
      >
        <Tooltip id="dashboard-tooltip-popover" className="dashboard-tooltip-content">
          <HtmlViewer text={p.tooltipHtml} />
        </Tooltip>
      </Overlay>
    </>
  );
}
