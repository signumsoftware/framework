import * as React from "react";
// import * as draftjs from "draft-js";
import { IconProp } from "@fortawesome/fontawesome-svg-core";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { classes } from "@framework/Globals";
import {
  $isListItemNode,
  $isListNode
} from "@lexical/list";
import {
  $isHeadingNode,
  $isQuoteNode,
  HeadingTagType
} from "@lexical/rich-text";
import {
  $getSelection,
  $isRangeSelection,
  FORMAT_TEXT_COMMAND,
  LexicalEditor,
  RangeSelection,
  TextFormatType
} from "lexical";
import { HtmlEditorController } from "./HtmlEditorController";
import { formatHeading, formatList, formatQuote } from "./Utilities/format";
import { isNodeType } from "./Utilities/node";

export function Separator(): React.JSX.Element {
  return <div className="sf-html-separator" />;
}

export function HtmlEditorButton(p: {
  icon?: IconProp;
  content?: React.ReactNode;
  isActive?: boolean;
  title?: string;
  onClick: (e: React.MouseEvent) => void;
}): React.JSX.Element {
  return (
    <div
      className="sf-draft-button-wrapper"
      onMouseDown={(e) => e.preventDefault()}
    >
      <button
        className={classes("sf-draft-button", p.isActive && "sf-draft-active ")}
        onClick={p.onClick}
        title={p.title}
      >
        {p.content ?? <FontAwesomeIcon icon={p.icon!} />}
      </button>
    </div>
  );
}

export namespace HtmlEditorButton {
  export const defaultProps = { icon: "question" };
}

type InlineStyleButtonProps = {
  controller: HtmlEditorController;
  style: TextFormatType;
  icon?: IconProp;
  content?: React.ReactChild;
  title?: string;
};

export function InlineStyleButton({
  controller,
  style,
  icon,
  content,
  title,
}: InlineStyleButtonProps): React.JSX.Element {
  const { editor, editorState } = controller;

  const isActive = React.useMemo(() => {
    let active = false;
    editorState?.read(() => {
      const selection = $getSelection();

      if ($isRangeSelection(selection)) {
        active = selection.hasFormat(style);
      }
    });

    return active;
  }, [editorState, style]);

  const toggleStyle = () => {
    editor?.update(() => {
      const selection = $getSelection();
      if (!$isRangeSelection(selection)) return;
      editor.dispatchCommand(FORMAT_TEXT_COMMAND, style)
    });
  };

  return (
    <HtmlEditorButton
      isActive={isActive}
      onClick={toggleStyle}
      icon={icon}
      content={content}
      title={title}
    />
  );
}

type BlockStyleButtonProps = {
  controller: HtmlEditorController;
  blockType: string;
  icon?: IconProp;
  content?: React.ReactChild;
  title?: string;
  checkActive?: (selection: RangeSelection) => boolean;
  onClick?: (editor: LexicalEditor) => void;
};

export function BlockStyleButton({
  controller,
  blockType,
  icon,
  content,
  title,
  checkActive,
  onClick
}: BlockStyleButtonProps): React.JSX.Element {
  const { editor, editorState } = controller;

  const isActive = React.useMemo(() => {
    let active = false;

    editorState?.read(() => {
      const selection = $getSelection();

      if (!$isRangeSelection(selection)) return;

      if(checkActive) {
        active = checkActive(selection);
        return;
      }

      active = isNodeType(selection, node => {
        if($isHeadingNode(node) || $isListNode(node)) {
          return blockType === node.getTag();
        }

        if($isListItemNode(node)) {
          const parentNode = node.getParent();
          return $isListNode(parentNode) && blockType === parentNode.getTag();
        }

        if($isQuoteNode(node)) {
          return blockType === "blockquote"
        }

        return false;
      })
    });

    return active;
  }, [editorState, blockType]);

  function toggleBlockStyle() {
    if(onClick) {
      onClick(editor);
      return
    }

    if(blockType === "ul" || blockType === "ol") {
      formatList(editor, blockType);
      return;
    }

    if(blockType === "h1" || blockType === "h2" || blockType === "h3") {
      formatHeading(editor, blockType as HeadingTagType); 
      return;
    }

    if(blockType === "blockquote") {
      formatQuote(editor);
      return;
    }
  }

  return (
    <HtmlEditorButton
      isActive={isActive}
      onClick={toggleBlockStyle}
      icon={icon}
      content={content}
      title={title}
    />
  );
}



export function SubMenuButton(p: {
  controller: HtmlEditorController;
  icon?: IconProp;
  content?: React.ReactChild;
  title?: string;
  children: React.ReactNode;
}): React.JSX.Element {
  function handleOnClick() {
    p.controller.setOverrideToolbar(
      <SubMenu controller={p.controller}>{p.children}</SubMenu>
    );
  }

  return (
    <HtmlEditorButton
      onClick={handleOnClick}
      icon={p.icon}
      content={p.content}
      title={p.title}
    />
  );
}

export function SubMenu(p: {
  controller: HtmlEditorController;
  children: React.ReactNode;
}): React.JSX.Element {
  React.useEffect(() => {
    function onWindowClick() {
      p.controller.setOverrideToolbar(undefined);
    }
    window.setTimeout(() => {
      window.addEventListener("click", onWindowClick);
    });
    return () => window.removeEventListener("click", onWindowClick);
  }, []);

  return p.children as React.ReactElement;
}
