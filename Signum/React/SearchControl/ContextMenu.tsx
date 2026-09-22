import * as React from 'react';
import ReactDOM from 'react-dom';
import { classes, DomUtils } from '../Globals'
import { Dropdown } from 'react-bootstrap';
import { useForceUpdate } from '../Hooks';


export interface ContextMenuPosition {
  top: number;
  left: number;
}

interface ContextMenuProps extends React.HTMLAttributes<HTMLUListElement> {
  position: ContextMenuPosition;
  onHide: () => void;
  alignRight?: boolean;
  children: React.ReactNode;
  itemsCount: number;
  /** Move focus into the menu as it opens. Set when it was opened from the keyboard: a menu that appears
   * next to a pointer is where the pointer already is, but one opened with Shift+F10 is unreachable unless
   * focus follows it there - and from the first item @restart's Dropdown takes over the arrow keys. */
  autoFocus?: boolean;
}

export default function ContextMenu({ position, onHide, children, alignRight, itemsCount, autoFocus, ...rest }: ContextMenuProps): React.ReactElement {

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
        // Flip up so the menu bottom stays inside the viewport, floored at the top edge.
        adjustedTop = Math.max(14, viewportHeight - menuHeight - 14);
      }

      if (adjustedLeft + menuWidth > viewportWidth) {
        adjustedLeft = Math.max(14, viewportWidth - menuWidth - 14);
  }

      setAdjustedPosition({ top: adjustedTop, left: adjustedLeft });
    }
  }, [itemsCount, left, top]);

  React.useEffect(() => {
    if (!autoFocus)
      return;

    // The search box of a row menu focuses itself through AutoFocus; this covers the menus that have none.
    const first = menuRef.current?.querySelector<HTMLElement>(".dropdown-menu .dropdown-item:not(.disabled):not([disabled])");
    first?.focus();
  }, [autoFocus]);

  React.useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        onHide();
  }
    };

    // Escape is handled on the document and not on the menu because the menu can be opened with the context
    // menu key while the focus is still on the element that opened it (a column header), and the handler that
    // was on the menu never saw the key, so the menu could not be closed at all (WCAG 2.1.2). Capture phase
    // and stopPropagation so that the menu, being the topmost layer, is the only thing that closes, instead
    // of also closing the modal or the page underneath it.
    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.preventDefault();
        event.stopPropagation();
        onHide();
      }
    };

    const oldResize = window.onresize;

    window.onresize = (e) => { oldResize?.call(window, e); forceUpdate(); };
    document.addEventListener('mousedown', handleClickOutside);
    document.addEventListener('keydown', handleEscape, true);

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      document.removeEventListener('keydown', handleEscape, true);
      window.onresize = oldResize;
    };
  }, [onHide]);

  // The menu renders after the table in the DOM, so opening it used to leave the focus behind on the element
  // that opened it: Tab went on to the next column and a keyboard user could open the menu without being able
  // to reach a single one of its items (WCAG 2.1.1). The focus moves into the menu when it opens and goes back
  // to where it came from when it closes (WCAG 2.4.3).
  const previouslyFocused = React.useRef<HTMLElement | null>(null);
  const focusWasInside = React.useRef(false);

  React.useEffect(() => {
    previouslyFocused.current = document.activeElement as HTMLElement | null;

    const handleFocusIn = (event: FocusEvent) => {
      focusWasInside.current = menuRef.current?.contains(event.target as Node) ?? false;
    };

    document.addEventListener('focusin', handleFocusIn);

    return () => {
      document.removeEventListener('focusin', handleFocusIn);

      // Only take the focus back when the menu still had it, otherwise whatever the chosen item opened
      // (a modal, another page) would lose it again right away.
      const active = document.activeElement;
      if (focusWasInside.current || active == null || active == document.body) {
        const previous = previouslyFocused.current;
        if (previous?.isConnected)
          previous.focus();
      }
    };
  }, []);

  // The items can arrive asynchronously, so focusing the first one is retried as they show up, but never once
  // the user has already moved the focus inside the menu.
  React.useEffect(() => {
    const menu = menuRef.current?.querySelector<HTMLElement>('.dropdown-menu');
    if (menu == null)
      return;

    const active = document.activeElement;
    if (active != null && active != menu && menu.contains(active))
      return;

    (getFocusableItems(menu).firstOrNull() ?? menu).focus();
  }, [itemsCount, children]);


  const handleMenuClick = (e: React.MouseEvent<HTMLElement>) => {
    (e.target as HTMLElement).matches(".dropdown-item:not(input, .disabled)") && onHide();
  }

  // Tab cycles inside the menu instead of walking out of it into the rest of the page, so that the menu
  // behaves like the transient overlay it is and the focus is never stranded behind it.
  const handleKeyDown = (event: React.KeyboardEvent<any>) => {
    if (event.key !== 'Tab')
      return;

    const menu = menuRef.current?.querySelector<HTMLElement>('.dropdown-menu');
    if (menu == null)
      return;

    const items = getFocusableItems(menu);
    if (items.length == 0)
      return;

    const active = document.activeElement as HTMLElement | null;
    const first = items[0];
    const last = items[items.length - 1];

    if (event.shiftKey ? (active == first || active == menu) : active == last) {
      event.preventDefault();
      (event.shiftKey ? last : first).focus();
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
      {/* tabIndex: the menu itself is the fallback focus target while its items are still loading, so that
          Escape and the Tab cycle work from the moment the menu opens. */}
      <Dropdown.Menu onClick={handleMenuClick} onKeyDown={handleKeyDown} className="sf-context-menu" tabIndex={-1}>
        {children}
      </Dropdown.Menu>
    </Dropdown>
  );
};

function getFocusableItems(menu: HTMLElement): HTMLElement[] {
  return Array.from(menu.querySelectorAll<HTMLElement>('a[href], button, input, select, textarea, [tabindex]'))
    .filter(e => e.tabIndex >= 0 &&
      !e.classList.contains("disabled") &&
      (e as HTMLInputElement).disabled != true &&
      e.getAttribute("aria-disabled") != "true" &&
      e.getClientRects().length > 0);
}

export function getMouseEventPosition(e: React.MouseEvent<HTMLTableElement>): ContextMenuPosition {

  const op = DomUtils.offsetParent(e.currentTarget);

  const rec = op?.getBoundingClientRect();

  var result = ({
    left: rec == null ? e.pageX : e.clientX - rec.left,
    top: rec == null ? e.pageY : e.clientY - rec.top,
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
