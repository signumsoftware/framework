import * as React from 'react'
import { BsColor } from '@framework/Components/Basic';
import { classes } from '@framework/Globals'
import numbro from 'numbro';

interface ProgressBarProps {
  value?: number | null; /*0...1*/
  showPercentageInMessage?: boolean | null;
  message?: string | null;
  color?: BsColor | null;
  striped?: boolean;
  active?: boolean;
}

export default function ProgressBar(p : ProgressBarProps){
  let { value, showPercentageInMessage, message, color, striped, active } = p;

  if (striped == null)
    striped = value == null;

  if (active == null)
    active = value == null;

  const progressContainerClass = value == null ? " active" : "";

  const progressStyle = color != null ? "bg-" + color : "";

  const fullMessage = [
    (value == null || showPercentageInMessage === false ? undefined : `${numbro(value * 100).format("0.00")}%`),
    (message ? message : undefined)
  ].filter(a => a != null).join(" - ");

  return (
    <div className={classes("progress")}>
      <div className={classes(
        "progress-bar",
        progressStyle,
        striped && "progress-bar-striped",
        active && "active"
      )}
        role="progressbar" id="progressBar"
        aria-valuenow={value == null ? undefined : value * 100}
        aria-valuemin={value == null ? undefined : 0}
        aria-valuemax={value == null ? undefined : 100}
        style={{ width: value == null ? "100%" : (value * 100) + "%" }}>
        <span style={{ color: (value != null && value < 0.5) ? "black" : undefined }}>{fullMessage}</span>
      </div>
    </div>
  );
}
