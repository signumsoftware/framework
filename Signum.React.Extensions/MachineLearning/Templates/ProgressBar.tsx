import * as React from 'react'
import { BsColor } from '../../../../Framework/Signum.React/Scripts/Operations';
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as numbro from 'numbro';

interface ProgressBarProps {
    value?: number | null; /*0...1*/
    showPercentageInMessage?: boolean | null;
    message?: string | null;
    color?: BsColor | null;
}

export default class ProgressBar extends React.Component<ProgressBarProps> {
    render() {

        const { value, showPercentageInMessage, message, color } = this.props;

        const progressContainerClass = value == null ? "progress-bar-striped active" : "";

        const progressStyle = color != null ? "progress-bar-" + color : "";

        const fullMessage = [
            (value == null || showPercentageInMessage === false ? undefined : `${numbro(value * 100).format("0.00")}%`),
            (message ? message : undefined)
        ].filter(a => a != null).join(" - ");

        return (
            <div className={classes("progress")}>
                <div className={classes("progress-bar", progressStyle, progressContainerClass)} role="progressbar" id="progressBar"
                    aria-valuenow={value == null ? undefined : value * 100}
                    aria-valuemin={value == null ? undefined : 0}
                    aria-valuemax={value == null ? undefined : 100}
                    style={{ width: value == null ? "100%" : (value * 100) + "%" }}>
                    <span style={{ color: (value != null && value < 0.5) ? "black" : undefined }}>{fullMessage}</span>
                </div>
            </div>
        );
    }
}