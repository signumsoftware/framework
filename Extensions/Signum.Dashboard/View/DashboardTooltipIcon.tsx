import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Tooltip } from 'react-bootstrap'
import { Overlay } from 'react-bootstrap'
import HtmlViewer from '../../Signum.HtmlEditor/HtmlViewer';
import { DashboardMessage } from '../Signum.Dashboard';

export interface DashboardTooltipIconProps {
  tooltipHtml: string;
  placement?: 'auto' | 'top' | 'bottom' | 'left' | 'right';
  className?: string;
  iconClassName?: string;
}

export function DashboardTooltipIcon(p: DashboardTooltipIconProps): React.JSX.Element {
  const [show, setShow] = React.useState(false);
  // A <button>, not a <span>: the trigger is what opens the explanation, and as a span with an onClick it
  // could not be reached or activated from the keyboard at all (WCAG 2.1.1).
  const targetRef = React.useRef<HTMLButtonElement>(null);
  // Unique per instance: a dashboard renders one of these per part, and the tooltip id is referenced by
  // aria-describedby, so a fixed id would point every trigger at the same element.
  const tooltipId = React.useId();

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

  // Dismissing was mousedown-outside only, which leaves a keyboard user with no way to close it again.
  const handleKeyDown = React.useCallback((e: KeyboardEvent) => {
    if (e.key === 'Escape') {
      setShow(false);
      targetRef.current?.focus();
    }
  }, []);

  React.useEffect(() => {
    if (show) {
      document.addEventListener('mousedown', handleClickOutside);
      document.addEventListener('keydown', handleKeyDown);
      return () => {
        document.removeEventListener('mousedown', handleClickOutside);
        document.removeEventListener('keydown', handleKeyDown);
      };
    }
  }, [show, handleClickOutside, handleKeyDown]);

  return (
    <>
      <button
        type="button"
        ref={targetRef}
        className={p.className}
        onClick={handleClick}
        aria-label={DashboardMessage.MoreInformation.niceToString()}
        aria-expanded={show}
        aria-describedby={show ? tooltipId : undefined}
        style={{ cursor: 'pointer', background: 'none', border: 0, padding: 0, lineHeight: 1, color: 'inherit' }}
      >
        <FontAwesomeIcon
          aria-hidden={true}
          icon="circle-info"
          className={p.iconClassName}
        />
      </button>
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
        <Tooltip id={tooltipId} className="dashboard-tooltip-content">
          <HtmlViewer text={p.tooltipHtml} />
        </Tooltip>
      </Overlay>
    </>
  );
}


