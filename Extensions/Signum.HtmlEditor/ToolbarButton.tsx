import React, { useCallback, useEffect, useMemo, useState } from "react";
import "./LexicalHtmlEditor.css";
import { classes } from "@framework/Globals";
import { FontAwesomeIcon } from "@framework/Lines";
import { IconProp } from "@fortawesome/fontawesome-svg-core";

type ToolbarButtonProps = {
  onClick?: () => void;
  content?: React.ReactNode;
  title?: string;
  isActive?: boolean;
  icon?: IconProp;
  renderMenu?: React.ReactNode;
};

export default function ToolbarButton({
  onClick,
  content,
  title,
  isActive,
  icon = "question",
  renderMenu,
}: ToolbarButtonProps): React.JSX.Element {
  const [showMenu, setShowMenu] = useState(false);

  const Button = useCallback(({ onClick }: { onClick?: () => void }) => ( 
        <button
            onClick={onClick}
            className={classes(
                "lex-toolbar-item",
                isActive && "lex-toolbar-item-active"
            )}
            title={title}
            aria-label={title}
        >
            {content ?? <FontAwesomeIcon icon={icon!} />}
        </button>
    ), [title, content, isActive, icon])

  if(!renderMenu) {
    return <Button onClick={onClick} />
  }

  return (
    <div className="lex-toolbar-item-wrapper">
        <Button onClick={() => setShowMenu((show) => !show)} />
        {showMenu && (
            <div className="lex-toolbar-item-menu" onClick={() => setShowMenu(false)}>
                {renderMenu}
            </div>
        )}
    </div>
  );
}
