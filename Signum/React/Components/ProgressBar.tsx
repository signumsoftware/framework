import * as React from 'react'
import { classes } from '../Globals'
import { BsColor } from './Basic';
import { toNumberFormat } from '../Reflection';

interface ProgressBarProps {
  value?: number | null; /*0...1*/
  showPercentageInMessage?: boolean | string | null;
  message?: string | null;
  color?: BsColor | null;
  striped?: boolean;
  animated?: boolean;
  containerHtmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
  progressHtmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
}

export default function ProgressBar(p: ProgressBarProps): React.ReactElement {
  let { value, showPercentageInMessage, message, color, striped, animated } = p;

  if (striped == null)
    striped = value == null;

  if (animated == null)
    animated = value == null;

  const progressStyle = color != null ? "bg-" + color : "";

  var numberFormat = toNumberFormat(typeof showPercentageInMessage == "string" ? showPercentageInMessage : "P2");

  const fullMessage = [
    (value == null || showPercentageInMessage === false ? undefined : numberFormat.format(value)),
    (message ? message : undefined)
  ].filter(a => a != null).join(" - ");

  return (
    <div
      {...p.containerHtmlAttributes}
      className={classes("progress", p.containerHtmlAttributes?.className)}>
      <div
        {...p.progressHtmlAttributes}
        className={classes(
          "progress-bar",
          progressStyle,
          striped && "progress-bar-striped",
          animated && "progress-bar-animated",
          p.progressHtmlAttributes?.className,
        )}
        role="progressbar" id="progressBar"
        aria-valuenow={value == null ? undefined : value * 100}
        aria-valuemin={value == null ? undefined : 0}
        aria-valuemax={value == null ? undefined : 100}
        style={{ width: value == null ? "100%" : (value * 100) + "%", userSelect: "none", ...p.progressHtmlAttributes?.style }}>
        {value * 100 >= 30 ? <span>{fullMessage}</span> : null}
      </div>
      {value * 100 < 30 ? <span style={{ marginLeft: 5}}>{fullMessage}</span> : null}
    </div>
  );
}
