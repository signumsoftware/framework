import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import { mergeRegister } from "@lexical/utils";
import {
    $createParagraphNode,
  $getSelection,
  $isRangeSelection,
  $isRootNode,
  FORMAT_ELEMENT_COMMAND,
  FORMAT_TEXT_COMMAND,
  SELECTION_CHANGE_COMMAND,
} from "lexical";
import React, { useCallback, useEffect, useRef, useState } from "react";
import ToolbarButton from "../ToolbarButton";
import { $insertList, $isListItemNode, $isListNode, $removeList, ListNode, ListType } from "@lexical/list";
import { getHeadingTag, getListType } from "../Utiliites/lexical";
import { $createHeadingNode, $isHeadingNode, HeadingNode, HeadingTagType } from "@lexical/rich-text";

const LowPriority = 1;

export default function ToolbarPlugin(): React.JSX.Element {
  const [editor] = useLexicalComposerContext();
  const [isBold, setIsBold] = useState(false);
  const [isItalic, setIsItalic] = useState(false);
  const [isUnderline, setIsUnderline] = useState(false);
  const [isCode, setIsCode] = useState(false);
  const [isHeading1, setIsHeading1] = useState(false);
  const [isHeading2, setIsHeading2] = useState(false);
  const [isHeading3, setIsHeading3] = useState(false);
  const [isUList, setIsUList] = useState(false);
  const [isOList, setIsOList] = useState(false);

  const $updateToolbar = useCallback(() => {
    const selection = $getSelection();
    if ($isRangeSelection(selection)) {
      setIsBold(selection.hasFormat("bold"));
      setIsItalic(selection.hasFormat("italic"));
      setIsUnderline(selection.hasFormat("underline"));
      setIsCode(selection.hasFormat("code"));

      const listType = getListType(selection);
      setIsUList(listType === "bullet");
      setIsOList(listType === "number");

      const headingTag = getHeadingTag(selection);
      setIsHeading1(headingTag === "h1");
      setIsHeading2(headingTag === "h2");
      setIsHeading3(headingTag === "h3");
    }
  }, []);

  useEffect(() => {
    return mergeRegister(
      editor.registerUpdateListener(({ editorState }) => {
        editorState.read(() => {
          $updateToolbar();
        });
      }),
      editor.registerCommand(
        SELECTION_CHANGE_COMMAND,
        (_payload, _newEditor) => {
          $updateToolbar();
          return false;
        },
        LowPriority
      )
    );
  }, [editor, $updateToolbar]);

  const handleToggleList = useCallback((type: ListType) => {
    return () => {
        editor.update(() => {
            const selection = $getSelection();
            if(!$isRangeSelection(selection)) return ;
            const anchorNode = selection.anchor.getNode()
            const parentNode = anchorNode.getParent();

            if(parentNode && $isListNode(parentNode) || $isListItemNode(parentNode)) {
                $removeList();
                return;
            } 
    
            $insertList(type);
        })
    }
  }, [editor])

  const handleToggleHeading = useCallback((headingType: HeadingTagType) => {
    return () => {
        console.log('handleToggleHeading', headingType)
        editor.update(() => {
            const selection = $getSelection();
            if(!$isRangeSelection(selection)) return;

            const anchorNode = selection.anchor.getNode();
            let parentNode = anchorNode.getParent();

            if($isHeadingNode(parentNode)) {
                const currentType = (parentNode as HeadingNode).getTag();
                if(currentType === headingType) {
                    const paragraphNode = $createParagraphNode();
                    paragraphNode.append(...parentNode.getChildren());
                    parentNode.replace(paragraphNode);
                    paragraphNode.select();
                    return;
                }

            }

            if(parentNode && !$isRootNode(parentNode)) {
                const headingNode = $createHeadingNode(headingType);
                headingNode.append(...parentNode.getChildren());
                parentNode.replace(headingNode);
                headingNode.select();
            }
        })
    }
  }, [editor])

  return (
    <div className="lex-toolbar">
      <ToolbarButton
        isActive={isBold}
        onClick={() => editor.dispatchCommand(FORMAT_TEXT_COMMAND, "bold")}
        icon="bold"
        title="Bold (Ctrl + B)"
      />
      <ToolbarButton
        isActive={isItalic}
        onClick={() => editor.dispatchCommand(FORMAT_TEXT_COMMAND, "italic")}
        icon="italic"
        title="Italic (Ctrl + I)"
      />
      <ToolbarButton
        isActive={isUnderline}
        onClick={() =>
          editor.dispatchCommand(FORMAT_TEXT_COMMAND, "underline")
        }
        icon="underline"
        title="Underline (Ctrl + U)"
      />
      <ToolbarButton
        isActive={isCode}
        onClick={() => editor.dispatchCommand(FORMAT_TEXT_COMMAND, "code")}
        icon="code"
        title="Code"
      />
      <Separator />
      <ToolbarButton
        icon="heading"
        title="Headings..."
        renderMenu={(
          <>
            <ToolbarButton
              isActive={isHeading1}
              content="H1"
              onClick={handleToggleHeading("h1")}
            />
            <ToolbarButton
              isActive={isHeading2}
              content="H2"
              onClick={handleToggleHeading("h2")}
            />
            <ToolbarButton
              isActive={isHeading3}
              content="H3"
              onClick={handleToggleHeading("h3")}
            />
          </>
        )}
      />
      <ToolbarButton isActive={isUList} icon="list-ul" onClick={handleToggleList("bullet")}/>
      <ToolbarButton isActive={isOList} icon="list-ol" onClick={handleToggleList("number")}/>
    </div>
  );
}

function Separator(): React.JSX.Element {
  return <div className="lex-toolbar-separator" />;
}
