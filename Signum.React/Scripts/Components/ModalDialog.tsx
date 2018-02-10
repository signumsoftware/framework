
import * as React from 'react';
import { classes } from '../Globals';
import { BsColor, BsSize } from './Basic';



export interface ModelDialogProps {
    /**
     * A css class to apply to the Modal dialog DOM node.
     */
    dialogClassName?: string;
    className?: string;
    style?: React.CSSProperties;

    size?: BsSize;
    color?: BsColor;
}

export class ModalDialog extends React.Component<ModelDialogProps> {


    render() {
        const {
            dialogClassName,
            className,
            style,
            children,
            size,
            color,
            ...elementProps
        } = this.props;

        return (
            <div
                {...elementProps}
                tabIndex={-1}
                role="dialog"
                style={{ display: 'block', ...style }}
                className={classes(className, "modal")}>
                <div className={classes(
                    dialogClassName,
                    "modal-dialog",
                    color && "modal-" + color,
                    size && "modal-" + size)}>
                    <div className="modal-content" role="document">
                        {children}
                    </div>
                </div>
            </div>
        );
    }
}