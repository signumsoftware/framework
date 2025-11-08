import React, { useEffect, useState } from "react";
import { NavDropdown, Button } from "react-bootstrap";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faPlus, faMinus, faRotateRight, faTextHeight } from "@fortawesome/free-solid-svg-icons";
import { FontSizeMessage } from "../Signum.Entities";

export const FONT_SCALE_STORAGE_KEY = "bootstrap-font-scale";

// Constants
const DEFAULT_SCALE = 1;
const MIN_SCALE = 0.8; // 2 levels smaller
const MAX_SCALE = 1.5; // 5 levels larger
const STEP = 0.1;

export function FontSizeSelector({ isMobile }: { isMobile: boolean }): JSX.Element {
  const getDefaultScale = (): number => {
    const stored = localStorage.getItem(FONT_SCALE_STORAGE_KEY);
    return stored ? parseFloat(stored) : DEFAULT_SCALE;
  };

  const [scale, setScale] = useState<number>(getDefaultScale);

  useEffect(() => {
    document.documentElement.style.setProperty("--font-size-scale", scale.toString());
    localStorage.setItem(FONT_SCALE_STORAGE_KEY, scale.toString());
  }, [scale]);

  const increaseFont = () => setScale(prev => Math.min(prev + STEP, MAX_SCALE));
  const decreaseFont = () => setScale(prev => Math.max(prev - STEP, MIN_SCALE));
  const resetFont = () => setScale(DEFAULT_SCALE);
  return (
    <NavDropdown
      title={isMobile ? <FontAwesomeIcon icon={"font"} title={FontSizeMessage.FontSize.niceToString()} aria-label={FontSizeMessage.FontSize.niceToString()} /> : FontSizeMessage.FontSize.niceToString()}
      id="nav-fontsize-dropdown"
      align="end"
    >
      <div className="d-flex justify-content-between align-items-center px-3 py-1" style={{ minWidth: "130px" }}>
        <Button
          type="button"
          variant="outline-primary"
          size="sm"
          onClick={decreaseFont}
          title={FontSizeMessage.ReduceFontSize.niceToString()}
          aria-label={FontSizeMessage.ReduceFontSize.niceToString()}
          disabled={scale <= MIN_SCALE}
        >
          <FontAwesomeIcon aria-hidden={true} icon={faMinus} />
        </Button>
        <Button
          type="button"
          variant="outline-secondary"
          size="sm"
          onClick={resetFont}
          title={FontSizeMessage.ResetFontSize.niceToString()}
          aria-label={FontSizeMessage.ResetFontSize.niceToString()}
          disabled={scale === DEFAULT_SCALE}
        >
          <FontAwesomeIcon aria-hidden={true} icon={faRotateRight} />
        </Button>
        <Button
          type="button"
          variant="outline-primary"
          size="sm"
          onClick={increaseFont}
          title={FontSizeMessage.IncreaseFontSize.niceToString()}
          aria-label={FontSizeMessage.IncreaseFontSize.niceToString()}
          disabled={scale >= MAX_SCALE}
        >
          <FontAwesomeIcon aria-hidden={true} icon={faPlus} />
        </Button>
      </div>
    </NavDropdown>
  );
}
