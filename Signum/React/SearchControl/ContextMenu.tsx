import * as React from 'react';
import ReactDOM from 'react-dom';
import { classes, DomUtils } from '../Globals'
import { Dropdown } from 'react-bootstrap';
import { useForceUpdate } from '../Hooks';


export interface ContextMenuPosition {
  top: number;
  left: number;
  maxTop?: number;
}

interface ContextMenuProps extends React.HTMLAttributes<HTMLUListElement> {
  position: ContextMenuPosition;
  onHide: () => void;
  alignRight?: boolean;
  children: React.ReactNode;
  itemsCount: number;
}

export default function ContextMenu({ position, onHide, children, alignRight, itemsCount, ...rest }: ContextMenuProps): React.ReactElement {

  const { top, left } = position;

  const forceUpdate = useForceUpdate();
  const menuRef = React.useRef<HTMLDivElement>(null);
  const [adjustedPosition, setAdjustedPosition] = React.useState({ left, top });


  React.useEffect(() => {

    if (menuRef.current) {
      const menuElement = menuRef.current.querySelector('.dropdown-menu') as HTMLElement;
      if (!menuElement) return;

      const menuWidth = menuElement.scrollWidth;
      const menuHeight = menuElement.offsetHeight; 
      const viewportWidth = window.innerWidth;
      const viewportHeight = window.innerHeight;

      let adjustedTop = top;
      let adjustedLeft = left;

      if (adjustedTop + menuHeight > viewportHeight) {
        adjustedTop = Math.max(position.maxTop ?? 14, viewportHeight - menuHeight -14);
      }

      if (adjustedLeft + menuWidth > viewportWidth) {
        adjustedLeft = Math.max(14, viewportWidth - menuWidth - 14);
  }

      setAdjustedPosition({ top: adjustedTop, left: adjustedLeft });
    }
  }, [itemsCount, left, top, position.maxTop]);

  React.useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        onHide();
  }
    };

    const oldResize = window.onresize;

    window.onresize = (e) => { oldResize?.call(window, e); forceUpdate(); };
    document.addEventListener('mousedown', handleClickOutside);

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      window.onresize = oldResize;
    };
  }, [onHide]);


  const handleMenuClick = (e: React.MouseEvent<HTMLElement>) => {
    (e.target as HTMLElement).matches(".dropdown-item:not(input, .disabled)") && onHide();
  }

  const handleKeyDown = (event: React.KeyboardEvent<any>) => {
    if (event.key === 'Escape') {
      onHide();
    }
  }

  return (
    <Dropdown className="sf-context-menu" show={true}
      ref={menuRef}
      style={{
        position: 'absolute',
        top: `${adjustedPosition.top}px`,
        left: alignRight ? `${adjustedPosition.left - (menuRef.current?.querySelector('.dropdown-menu')?.scrollWidth ?? 100)}px` : `${adjustedPosition.left}px`,
        zIndex: 999999,
      }}
      {...rest as any}
    >
      <Dropdown.Menu onClick={handleMenuClick} onKeyDown={handleKeyDown} className="sf-context-menu">
        {children}
      </Dropdown.Menu>
    </Dropdown>
  );
};

export function getMouseEventPosition(e: React.MouseEvent<HTMLTableElement>, container?: Element | null): ContextMenuPosition {

  const op = DomUtils.offsetParent(e.currentTarget);

  const rec = op?.getBoundingClientRect();

  var result = ({
    left: rec == null ? e.pageX : e.clientX - rec.left,
    top: rec == null ? e.pageY : e.clientY - rec.top,
    maxTop: container?.getBoundingClientRect().top, //table's body top
  }) as ContextMenuPosition;
  return result;
};

export function getPositionElement(button: HTMLElement, alignRight?: boolean): ContextMenuPosition {
  //const op = DomUtils.offsetParent(button);
  //const recOp = op!.getBoundingClientRect();
  const recButton = button.getBoundingClientRect();
  // Since we're now using fixed positioning, calculate position relative to viewport
  var result = ({
    left: recButton.left + (alignRight ? recButton.width : 0),
    top: recButton.top + recButton.height,
  }) as ContextMenuPosition;

  return result;
}
