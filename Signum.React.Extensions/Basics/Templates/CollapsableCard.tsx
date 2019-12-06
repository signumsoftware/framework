import * as React from 'react'
import Collapse from 'react-bootstrap/Collapse'
import { classes } from '@framework/Globals'
import { BsColor } from '@framework/Components/Basic';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

export interface CollapsableCardProps {
  color?: BsColor;
  header?: React.ReactNode;
  defaultOpen?: boolean;
  collapsable?: boolean;
  isOpen?: boolean;
  toggle?: (isOpen: boolean) => void;
  cardId?: string | number;
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

function isControlled(p: CollapsableCardProps): [boolean, (isOpen: boolean) => void] {
  if ((p.isOpen != null) && (p.toggle == null))
    throw new Error("isOpen and toggle should be set together");

  if (p.isOpen != null) {
    return [p.isOpen, p.toggle!];
  }

  const [openState, setOpenState] = React.useState(p.defaultOpen == true);

  React.useEffect(() => {
    setOpenState(p.defaultOpen == true);
  }, [p.defaultOpen]);

  return [openState, (isOpen: boolean) => { setOpenState(isOpen); p.toggle && p.toggle(isOpen); }];
}

export default function CollapsableCard(p: CollapsableCardProps) {

  const [isOpen, setIsOpen] = isControlled(p);
  const isRTL = React.useMemo(() => document.body.classList.contains("rtl"), []);
  return (
    <div className={classes("card", cardStyleClasses(p.cardStyle), p.size && ("card-" + p.size))}>
      <div className={classes("card-header", cardStyleClasses(p.headerStyle))} style={{ cursor: "pointer" }} onClick={() => setIsOpen(!isOpen)}>
        {(p.collapsable == undefined || p.collapsable == true) &&
          <span
            className={isRTL ? "float-left" : "float-right"}
            style={{ cursor: "pointer" }}
            onClick={() => setIsOpen(!isOpen)}>
            <FontAwesomeIcon icon={isOpen ? "chevron-up" : "chevron-down"} />
          </span>
        }
        {p.header}
      </div>
      <Collapse in={isOpen}>
        <div className={classes("card-body", cardStyleClasses(p.bodyStyle))}>
          {p.children}
        </div>
      </Collapse>
    </div>
  );
}
