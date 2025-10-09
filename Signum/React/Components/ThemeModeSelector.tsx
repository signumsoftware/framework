import React, { useEffect } from "react";
import { NavDropdown } from "react-bootstrap";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faSun, faMoon, faCircleHalfStroke } from "@fortawesome/free-solid-svg-icons";
import { JSX } from "react/jsx-runtime";
import { useWindowEvent } from "../Hooks";

type BootstrapThemeModes = "light" | "dark" | "auto";

const BOOTSTRAP_MODES: BootstrapThemeModes[] = ["light", "dark", "auto"];

const ICONS: Record<BootstrapThemeModes, any> = {
  light: faSun,
  dark: faMoon,
  auto: faCircleHalfStroke,
};

export function useAuto(theme: BootstrapThemeModes): "dark" | "light" {
  const query = window.matchMedia("(prefers-color-scheme: dark)");
  const get = () => (query.matches ? "dark" : "light");

  const [mode, setMode] = React.useState<"dark" | "light">(theme === "auto" ? get() : theme);

  useEffect(() => {
    if (theme !== "auto")
      return setMode(theme);
    const fn = () => setMode(get());
    query.addEventListener("change", fn);
    fn();
    return () => query.removeEventListener("change", fn);
  }, [theme]);

  return mode;
}
export const STORAGE_KEY = "bootstrap-theme-mode";
export function ThemeModeSelector(p: { onSetMode?: (mode: "dark" | "light") => void }): JSX.Element {

  const getDefaultTheme = (): BootstrapThemeModes => {
    const stored = localStorage.getItem(STORAGE_KEY) as BootstrapThemeModes | null;
    if (stored) return stored;
    return "auto"; // fallback to system preference
  };

  const [bootstrapMode, setBootstrapMode] = React.useState<BootstrapThemeModes>(getDefaultTheme());

  const finalMode = useAuto(bootstrapMode);

  useEffect(() => {
    document.body.dataset.bsTheme = finalMode;
    p.onSetMode?.(finalMode)
    localStorage.setItem(STORAGE_KEY, finalMode);
  }, [finalMode]);

  useWindowEvent("change-theme-mode", (e) => {
    setBootstrapMode((e as CustomEvent).detail as BootstrapThemeModes);
  }, []);

  return (
    <div style={{ display: "flex", alignItems: "center" }}>
      <NavDropdown
        title={
          <>
            <FontAwesomeIcon icon={ICONS[bootstrapMode]} title={(bootstrapMode.firstUpper())} />
          </>
        }
      >
        {BOOTSTRAP_MODES.map((theme) => (
          <NavDropdown.Item
            key={theme}
            active={bootstrapMode === theme}
            onClick={() => setBootstrapMode(theme)}
          >
            <FontAwesomeIcon icon={ICONS[theme]} className="me-2" />
            {(theme.firstUpper())}
          </NavDropdown.Item>
        ))}
      </NavDropdown>
    </div>
  );
};
