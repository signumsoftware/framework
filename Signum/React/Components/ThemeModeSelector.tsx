import React, { useEffect } from "react";
import { NavDropdown } from "react-bootstrap";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faSun, faMoon, faCircleHalfStroke } from "@fortawesome/free-solid-svg-icons";

type BootstrapThemeModes = "light" | "dark" | "auto";

const BOOTSTRAP_MODES: BootstrapThemeModes[] = ["light", "dark", "auto"];

const ICONS: Record<BootstrapThemeModes, any> = {
  light: faSun,
  dark: faMoon,
  auto: faCircleHalfStroke,
};

function applyThemeMode(theme: BootstrapThemeModes) {
  if (theme === "auto") {
    const darkQuery = window.matchMedia("(prefers-color-scheme: dark)");
    const updateTheme = () => {
      document.body.dataset.bsTheme = darkQuery.matches ? "dark" : "light";
    };
    updateTheme();
    darkQuery.addEventListener("change", updateTheme);
    return () => darkQuery.removeEventListener("change", updateTheme);
  } else {
    document.body.dataset.bsTheme = theme;
    return undefined;
  }
}

export function ThemeModeSelector() {

    const getDefaultTheme = (): BootstrapThemeModes => {
    const stored = localStorage.getItem("bootstrap-theme-mode") as BootstrapThemeModes | null;
    if (stored) return stored;
    return "auto"; // fallback to system preference
  };

  const [bootstrapTheme, setBootstrapThemeState] = React.useState<BootstrapThemeModes>(
    getDefaultTheme()
  );

  useEffect(() => {
    const cleanup = applyThemeMode(bootstrapTheme);
    localStorage.setItem("bootstrap-theme-mode", bootstrapTheme);
    return cleanup;
  }, [bootstrapTheme]);

  return (
    <div style={{ display: "flex", alignItems: "center" }}>
      <NavDropdown
        title={
          <>
            <FontAwesomeIcon icon={ICONS[bootstrapTheme]} /> {(bootstrapTheme.firstUpper())}
          </>
        }
      >
        {BOOTSTRAP_MODES.map((theme) => (
          <NavDropdown.Item
            key={theme}
            disabled={bootstrapTheme === theme}
            onClick={() => setBootstrapThemeState(theme)}
          >
            <FontAwesomeIcon icon={ICONS[theme]} className="me-2" />
            {(theme.firstUpper())}
          </NavDropdown.Item>
        ))}
      </NavDropdown>
    </div>
  );
};
