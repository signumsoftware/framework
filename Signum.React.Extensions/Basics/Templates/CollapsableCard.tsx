import * as React from 'react'
import { classes } from '@framework/Globals'
import { BsColor } from '@framework/Components/Basic';
import { Collapse } from '@framework/Components';
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
}
interface CardStyle {
    border?: BsColor;
    text?: BsColor;
    background?: BsColor
}

function cardStyleClasses(style?: CardStyle) {
    return classes(
        style && style.text && "text-" + style.text,
        style && style.background && "bg-" + style.background,
        style && style.border && "border-" + style.border,
    )
}



export interface CollapsableCardState {
    isOpen: boolean,
    isRTL: boolean;
}

export default class CollapsableCard extends React.Component<CollapsableCardProps, CollapsableCardState> {

    constructor(props: CollapsableCardProps) {
        super(props);

        CollapsableCard.checkProps(props);

        this.state = {
            isOpen: this.props.defaultOpen == true,
            isRTL: document.body.classList.contains("rtl"),
        };
    }

    static checkProps(props: CollapsableCardProps) {
        if ((props.isOpen == null) != (props.toggle == null))
            throw new Error("isOpen and toggle should be set together");
    }

    componentWillReceiveProps(newProps: CollapsableCardProps) {
        CollapsableCard.checkProps(newProps);
    }

    handleToggle = () => {
        if (this.props.toggle)
            this.props.toggle(this.props.isOpen!);
        else
            this.setState({
                isOpen: !this.state.isOpen,
            });
    }

    render() {

        var isOpen = this.props.isOpen == undefined ?
            this.state.isOpen :
            this.props.isOpen;

        return (
            <div className={classes("card", cardStyleClasses(this.props.cardStyle))}>
                <div className={classes("card-header", cardStyleClasses(this.props.headerStyle))} style={{ cursor: "pointer" }} onClick={this.handleToggle}>
                    {(this.props.collapsable == undefined || this.props.collapsable == true) &&
                        <span
                            className={this.state.isRTL ? "float-left" : "float-right"}
                            style={{ cursor: "pointer", margin: "4px" }}
                            onClick={this.handleToggle}>
                            <FontAwesomeIcon icon={isOpen ? "chevron-up" : "chevron-down"} />
                        </span>
                    }
                    {this.props.header}
                </div>
                <Collapse isOpen={isOpen}>
                    <div className={classes("card-body", cardStyleClasses(this.props.bodyStyle))}>
                        {this.props.children}
                    </div>
                </Collapse>
            </div>
        );
    }
}