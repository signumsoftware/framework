import * as React from "react";
// import * as draftjs from "draft-js";
import { IBinding } from "@framework/Reflection";
import { HtmlContentStateConverter } from "./HtmlContentStateConverter";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { IconProp } from "@fortawesome/fontawesome-svg-core";
import { classes } from "@framework/Globals";
import { HtmlEditorController } from "./HtmlEditorController";
import {
  $createParagraphNode,
  $getRoot,
  $getSelection,
  $isParagraphNode,
  $isRangeSelection,
  ElementNode,
  FORMAT_TEXT_COMMAND,
  LexicalEditor,
  LexicalNode,
  RangeSelection,
  TextFormatType,
} from "lexical";
import {
  $createListItemNode,
  $createListNode,
  $isListItemNode,
  $isListNode,
  INSERT_ORDERED_LIST_COMMAND,
  INSERT_UNORDERED_LIST_COMMAND,
  ListNode,
  ListType,
  REMOVE_LIST_COMMAND,
} from "@lexical/list";
import {
  $createHeadingNode,
  $createQuoteNode,
  $isHeadingNode,
  $isQuoteNode,
  HeadingTagType,
} from "@lexical/rich-text";
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";

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
};

export function BlockStyleButton({
  controller,
  blockType,
  icon,
  content,
  title,
}: BlockStyleButtonProps): React.JSX.Element {
  const { editor, editorState } = controller;

  const isActive = React.useMemo(() => {
    let active = false;

    editorState?.read(() => {
      const selection = $getSelection();

      if (!$isRangeSelection(selection)) return;

      const nodes = selection.getNodes();

      for (const node of nodes) {
        const parent = node.getParent();
        if (!parent) continue;
      
        const isHeading = $isHeadingNode(parent) && parent.getTag() === blockType;
        const isList =  (() => {
            if($isListNode(parent) && parent.getTag() === blockType) {
                return true;
            }

            if($isListItemNode(parent)) {
                const listNode = parent.getParent();
                return $isListNode(listNode) && listNode.getTag() === blockType;
            }

            return false;
        })()
        const isQuote = blockType === "blockquote" && $isQuoteNode(parent);
        
        if (isHeading || isList || isQuote) {
          active = true;
          break;
        }
      }
    });

    return active;
  }, [editorState, blockType]);

  function toggleBlockStyle() {
    if(["ul", "ol"].includes(blockType)) {
        const listCommand = (isActive) ? REMOVE_LIST_COMMAND : (blockType === "ul") ? INSERT_UNORDERED_LIST_COMMAND : INSERT_ORDERED_LIST_COMMAND;
        editor.dispatchCommand(listCommand, undefined);
        return;
    }

    if(["h1", "h2", "h3"].includes(blockType)) {
        replaceNodes(editor, node => {
            if($isHeadingNode(node) && node.getTag() === blockType ) {
                return $createParagraphNode();
            }
            
            return $createHeadingNode(blockType as HeadingTagType);
        });
        return;
    }

    if(blockType === "blockquote") {
        replaceNodes(editor, node => $isQuoteNode(node) ? $createParagraphNode() : $createQuoteNode());
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

/**
 * Replaces the editor's node of the selection.
 * @param editor Instance of the active editor.
 * @param replaceWith Callback that returns an element to replace the selection's node with.
 */
function replaceNodes(editor: LexicalEditor, replaceWith: (node: ElementNode) => ElementNode | undefined) {
    editor.update(() => {
        const selection = $getSelection();
        if(!$isRangeSelection(selection)) return;
        const nodes = selection.getNodes();
        for(const node of nodes) {
            const parent = node.getParent();
            if(!parent) continue;
            const replacementNode = replaceWith(parent);
            
            if(replacementNode) {
                parent.replace(replacementNode);
                parent.getChildren().forEach(child => replacementNode.append(child));
            }
        }
    })
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
