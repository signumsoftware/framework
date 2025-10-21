import React, { ReactElement, useEffect, useRef } from "react";
import { OverlayTrigger } from "react-bootstrap";
import { VisualTipSymbol } from "../Signum.Basics";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { VisualTipClient } from "./VisualTipClient";
import { useAPIWithReload } from "../Hooks";
import { classes } from "../Globals";
import "./VisualTipIcon.css";
import { OverlayInjectedProps } from "react-bootstrap/esm/Overlay";
import { VisualTipMessage } from "../Signum.Basics";

function AccessibleOverlay(p: { id: string; children: ReactElement; onClose: () => void }) {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const node = ref.current;
    if (!node) return;

    const popover = node.querySelector<HTMLElement>(".popover");
    if (popover) {
      popover.setAttribute("tabindex", "-1");
      popover.focus();
    } else {
      node.focus(); // Fallback
    }

    const header = node.querySelector(".popover-header");
    if (header) {
      const id = `${p.id}-header`;
      header.id = id;
      node.setAttribute("aria-labelledby", id);
    }

    const body = node.querySelector(".popover-body");
    if (body) {
      const bodyId = `${p.id}-body`;
      body.id = bodyId;
      node.setAttribute("aria-describedby", bodyId);

      body.setAttribute("role", "document"); //for long content, we guess that this will be the case for help content
    }

    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === "Escape") {
        e.preventDefault();
        p.onClose();
      }
    }

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [p]);

  return (
    <div
      ref={ref}
      id={p.id}
      role="dialog"
      aria-modal="true"
      tabIndex={-1}
      className="visual-tip-overlay"
    >
      {p.children}
    </div>
  );
}

export function VisualTipIcon(p: {
  visualTip: VisualTipSymbol;
  className?: string;
  content: (injected: OverlayInjectedProps) => ReactElement;
}): React.ReactElement {
  const [visualTipSymbols, reload] = useAPIWithReload(() => VisualTipClient.API.getConsumed(), []);
  const buttonRef = useRef<HTMLButtonElement>(null);

  return (
    <OverlayTrigger
      trigger="click"
      rootClose
      placement="bottom"
      overlay={(injected: OverlayInjectedProps) => (
        <AccessibleOverlay
          id="visual-tip-popover"
          onClose={() => {
            // Schließt Popover über rootClose
            const refEl = injected.popper?.state?.elements?.reference;
            if (refEl instanceof HTMLElement) refEl.click();
            buttonRef.current?.focus();
          }}
        >
          {p.content(injected)}
        </AccessibleOverlay>
      )}
    >
      <button
        ref={buttonRef}
        type="button"
        style={{ border: "none", background: "transparent" }}
        className={classes(
          "sf-line-button align-self-center",
          Boolean(visualTipSymbols && !visualTipSymbols.contains(p.visualTip.key)) && "sf-beat",
          p.className
        )}
        title={VisualTipMessage.Help.niceToString()}
        onClick={async () => {
          if (
            visualTipSymbols === null ||
            (visualTipSymbols != null && !visualTipSymbols.contains(p.visualTip.key))
          ) {
            await VisualTipClient.API.consume(p.visualTip.key);
            reload();
          }
        }}
      >
        <FontAwesomeIcon aria-hidden={true} icon="question-circle" />
      </button>
    </OverlayTrigger>
  );
}
