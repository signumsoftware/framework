import React, { ReactElement } from "react";
import { OverlayTrigger } from "react-bootstrap";
import { VisualTipSymbol } from "../Signum.Basics";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { API } from "./VisualTipClient";
import { useAPI, useAPIWithReload } from "../Hooks";
import { classes } from "../Globals";
import "./VisualTipIcon.css"
import { OverlayInjectedProps } from "react-bootstrap/esm/Overlay";
import createUtilityClassName from "react-bootstrap/esm/createUtilityClasses";

export function VisualTipIcon(p: { visualTip: VisualTipSymbol, className?: string, content: (injected: OverlayInjectedProps) => ReactElement }) {

  var [visualTipSymbols, reload] = useAPIWithReload(() => API.getConsumed(), []);

  return (
    <OverlayTrigger trigger="click" rootClose placement="bottom" overlay={p.content}>
      <a className={classes("sf-line-button align-self-center")} onClick={async e => {
        if (visualTipSymbols === null || (visualTipSymbols != null && !visualTipSymbols.contains(p.visualTip.key))) {
          await API.consume(p.visualTip.key);
          reload();
        }
      }}>

        <FontAwesomeIcon icon="question-circle" title="help" className={classes(p.className, Boolean(visualTipSymbols && !visualTipSymbols.contains(p.visualTip.key)) && "sf-beat")}
        /></a>
    </OverlayTrigger>
  );
}
