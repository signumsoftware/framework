import * as React from 'react'
import * as ReactDOM from 'react-dom'
import { classes, combineFunction, DomUtils } from '../Globals'
import * as PropTypes from "prop-types";
import { DropdownItemProps } from 'react-bootstrap/DropdownItem';

export interface ContextMenuPosition {
  left: number;
  top: number;
  width: number; //Necessary for RTL
  children: React.ReactElement<any>[]
}

export interface ContextMenuProps extends React.Props<ContextMenu>, React.HTMLAttributes<HTMLUListElement> {
  position: ContextMenuPosition;
  onHide: (e: MouseEvent | TouchEvent) => void;
  alignRight?: boolean;
}

export default class ContextMenu extends React.Component<ContextMenuProps> {
  handleToggle = () => {

  }

  getChildContext() {
    return { toggle: this.handleToggle };
  }

  static childContextTypes = { "toggle": PropTypes.func };

  static getPositionEvent(e: React.MouseEvent<any>): ContextMenuPosition {

    const op = DomUtils.offsetParent(e.currentTarget);

    const rec = op?.getBoundingClientRect();

    var result = ({
      left: e.pageX - (rec ? rec.left : 0),
      top: e.pageY - (rec ? rec.top : 0),
      width: (op ? op.offsetWidth : window.innerWidth)
    }) as ContextMenuPosition;

    return result;
  }

  static getPositionElement(button: HTMLElement, alignRight?: boolean): ContextMenuPosition {
    const op = DomUtils.offsetParent(button);

    const recOp = op!.getBoundingClientRect();
    const recButton = button.getBoundingClientRect();

    var result = ({
      left: recButton.left + (alignRight ? recButton.width : 0) - recOp.left,
      top: recButton.top + recButton.height - recOp.top,
      width: (op ? op.offsetWidth : window.innerWidth)
    }) as ContextMenuPosition;

    return result;
  }

  render() {

    const { position, onHide, ref, alignRight, ...props } = this.props;

    const style: React.CSSProperties = { zIndex: 9999, display: "block", position: "absolute" };

    style.top = position.top + "px";
    if (document.body.className.contains("rtl-mode") !== Boolean(alignRight))
      style.right = (position.width - position.left) + "px";
    else
      style.left = position.left + "px";

    const childrens = React.Children.map(this.props.children,
      (rc) => {
        let c = rc as React.ReactElement<DropdownItemProps>;
        return c && React.cloneElement(c, { onClick: e => { c.props.onClick && c.props.onClick(e); onHide(e.nativeEvent); } } as Partial<DropdownItemProps>);
      });

    const ul = (
      <div {...props as any} className={classes(props.className, "dropdown-menu sf-context-menu")} style={style}>
        {childrens}
      </div>
    );

    return ul;
  }



  componentDidMount() {

    document.addEventListener('click', this.handleDocumentClick, true);
    document.addEventListener('touchstart', this.handleDocumentClick, true);
  }

  componentWillUnmount() {
    document.removeEventListener('click', this.handleDocumentClick, true);
    document.removeEventListener('touchstart', this.handleDocumentClick, true);
  }

  handleDocumentClick = (e: MouseEvent | TouchEvent) => {
    if ((e as MouseEvent).which === 3)
      return;

    const container = ReactDOM.findDOMNode(this) as HTMLElement;
    if (container.contains(e.target as Node) &&
      container !== e.target) {
      return;
    }

    this.props.onHide(e);
  }
}
