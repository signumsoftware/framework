import * as React from 'react'
import Collapse from 'react-bootstrap/Collapse'
import { classes } from '../Globals'
import { BsColor } from './Basic';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';
import { CollapsableCardMessage } from '../Signum.Basics';

export interface CollapsableCardProps {
  color?: BsColor;
  header?: React.ReactNode;
  defaultOpen?: boolean;
  collapsable?: boolean;
  isOpen?: boolean;
  toggle?: (isOpen: boolean, e: React.MouseEvent) => void;
  cardId?: string | number;
  expandIcon?: IconProp;
  collapseIcon?: IconProp;
  cardStyle?: CardStyle;
  headerStyle?: CardStyle;
  bodyStyle?: CardStyle;
  size?: "sm" | "xs";
  children?: React.ReactNode;
}
interface CardStyle {
  border?: BsColor;
  text?: BsColor;
  background?: BsColor;
}

function cardStyleClasses(style?: CardStyle) {
  return classes(
    style?.text && "text-" + style.text,
    style?.background && "bg-" + style.background,
    style?.border && "border-" + style.border,
  )
}

export interface CollapsableCardState {
  isOpen: boolean,
  isRTL: boolean;
}

function isControlled(p: CollapsableCardProps): [boolean, (isOpen: boolean, e: React.MouseEvent) => void] {
  if ((p.isOpen != null) && (p.toggle == null))
    throw new Error("isOpen and toggle should be set together");

  if (p.isOpen != null) {
    return [p.isOpen, p.toggle!];
  }

  const [openState, setOpenState] = React.useState(p.defaultOpen == true);

  React.useEffect(() => {
    setOpenState(p.defaultOpen == true);
  }, [p.defaultOpen]);

  return [openState, (isOpen: boolean, e: React.MouseEvent) => { setOpenState(isOpen); p.toggle && p.toggle(isOpen, e); }];
}

export default function CollapsableCard(p: CollapsableCardProps): React.ReactElement {

  const [isOpen, setIsOpen] = isControlled(p);
  const collapsable = (p.collapsable == undefined || p.collapsable == true);
  return (
    <div className={classes("card", cardStyleClasses(p.cardStyle), p.size && ("card-" + p.size))}>
      <div className={classes("card-header", cardStyleClasses(p.headerStyle))} style={{ cursor: "pointer" }} onClick={collapsable ? e => setIsOpen(!isOpen, e) : undefined}>
        {collapsable &&
          <span
            className={"float-end"}
            style={{ cursor: "pointer" }}            
            onClick={e => setIsOpen(!isOpen, e)}
            title={isOpen ? CollapsableCardMessage.Collapse.niceToString() : CollapsableCardMessage.Expand.niceToString()}>
            <FontAwesomeIcon aria-hidden={true} icon={isOpen ? (p.collapseIcon ?? "chevron-up") : (p.expandIcon ?? "chevron-down")} />
          </span>
        }
        {p.header}
      </div>
      <Collapse in={isOpen}>
        <div>
          <div className={classes("card-body", cardStyleClasses(p.bodyStyle))}>
            {p.children}
          </div>
        </div>
      </Collapse>
    </div>
  );
}
