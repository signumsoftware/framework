import * as React from 'react'
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
  children: React.ReactNode;
}

export default function ContextMenu({ position, onHide, children, ...rest }: ContextMenuProps): React.ReactElement {

  const { top, left } = position;

  const forceUpdate = useForceUpdate();
  const menuRef = React.useRef<HTMLDivElement>(null);
  const [adjustedPosition, setAdjustedPosition] = React.useState({ left, top });


  React.useEffect(() => {

    if (menuRef.current) {
      const menuWidth = menuRef.current.scrollWidth;
      const menuHeight = menuRef.current.scrollHeight;
      const viewportWidth = window.innerWidth;
      const viewportHeight = window.innerHeight;

      let adjustedTop = Math.min(top, adjustedPosition.top);
      let adjustedLeft = Math.min(left, adjustedPosition.left);

      if (adjustedTop + menuHeight > viewportHeight) {
        adjustedTop = Math.max(position.maxTop ?? 14, viewportHeight - menuHeight);
      }

      if (adjustedLeft + menuWidth > viewportWidth) {
        adjustedLeft = Math.max(14, viewportWidth - menuWidth - 14);
  }

      setAdjustedPosition({ top: adjustedTop, left: adjustedLeft });
    }
  }, [React.Children.count(children), left, top, menuRef.current?.scrollWidth, menuRef.current?.scrollHeight, window.innerWidth, window.innerHeight]);

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
    <Dropdown show={true}
      ref={menuRef}
      style={{
        position: 'absolute',
        top: `${adjustedPosition.top}px`,
        left: `${adjustedPosition.left}px`,
      }}
      {...rest as any}
    >
      <Dropdown.Menu onClick={handleMenuClick} onKeyDown={handleKeyDown}>
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
