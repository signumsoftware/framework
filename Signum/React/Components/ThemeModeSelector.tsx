import React, { useEffect } from "react";
import { NavDropdown } from "react-bootstrap";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faSun, faMoon, faCircleHalfStroke } from "@fortawesome/free-solid-svg-icons";
import { JSX } from "react/jsx-runtime";
import { useWindowEvent } from "../Hooks";
import { ThemeModeMessage } from "../Signum.Entities";
import { MessageKey } from "../Reflection";
import { dropdownActive } from "./DropdownActive";

type BootstrapThemeModes = "light" | "dark" | "auto";

const BOOTSTRAP_MODES: BootstrapThemeModes[] = ["light", "dark", "auto"];

const ICONS: Record<BootstrapThemeModes, any> = {
  light: faSun,
  dark: faMoon,
  auto: faCircleHalfStroke,
};

// The mode keys are internal; what the reader sees has to come from the translations, otherwise every
// non-English installation gets an English word in the middle of its toolbar.
const LABELS: Record<BootstrapThemeModes, MessageKey> = {
  light: ThemeModeMessage.Light,
  dark: ThemeModeMessage.Dark,
  auto: ThemeModeMessage.Auto,
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
// extraItems lets an application add its own presentation choices to this menu - a high contrast mode,
// for instance - without a second dropdown competing with this one for the same corner, and without
// their wording having to live in the framework.
export function ThemeModeSelector(p: { onSetMode?: (mode: "dark" | "light") => void, extraItems?: React.ReactNode }): JSX.Element {

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
        id="changeTheme"
        title={
          <>
            {/* The icon is the only visible content of the toggle, so the toggle's name has to say what it does,
                not just repeat the mode. As the icon's own SVG title it was read twice ("Theme: Auto Theme:
                Auto"): once as the toggle's name, once as the graphic inside it. Visually hidden text names
                the toggle once; the span's title keeps the tooltip. */}
            <span title={ThemeModeMessage.Theme.niceToString() + ": " + LABELS[bootstrapMode].niceToString()}>
              <FontAwesomeIcon aria-hidden={true} icon={ICONS[bootstrapMode]} />
              <span className="visually-hidden">{ThemeModeMessage.Theme.niceToString() + ": " + LABELS[bootstrapMode].niceToString()}</span>
            </span>
          </>
        }
      >
        {BOOTSTRAP_MODES.map((theme) => (
          <NavDropdown.Item
            key={theme}
            {...dropdownActive(bootstrapMode === theme)}
            onClick={() => setBootstrapMode(theme)}
          >
            <FontAwesomeIcon aria-hidden={true} icon={ICONS[theme]} className="me-2" />
            {LABELS[theme].niceToString()}
          </NavDropdown.Item>
        ))}
        {p.extraItems && <NavDropdown.Divider />}
        {p.extraItems}
      </NavDropdown>
    </div>
  );
};
