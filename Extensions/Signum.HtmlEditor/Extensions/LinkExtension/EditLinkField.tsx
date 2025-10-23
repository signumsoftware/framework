import { LinkButton } from "@framework/Basics/LinkButton";
import { classes } from "@framework/Globals";
import { useForceUpdate } from "@framework/Hooks";
import { EntityBaseController, TextBoxLine } from "@framework/Lines";
import { AutoLineProps } from "@framework/Lines/AutoLine";
import { EntityControlMessage, HtmlEditorMessage } from "@framework/Signum.Entities";
import React, { ReactNode } from "react";

export default function EditLinkField(p: AutoLineProps): ReactNode {
  const forceUpdate = useForceUpdate();
  return (
    <TextBoxLine {...p} valueHtmlAttributes={{ placeholder: HtmlEditorMessage.EnterYourUrlHere.niceToString() }} extraButtons={() =>
      <LinkButton className={classes("sf-line-button", "sf-remove", "input-group-text")}
        onClick={() => {
          p.ctx.value = null;
          forceUpdate();
        }}
        title={EntityControlMessage.Remove.niceToString()}>
        {EntityBaseController.getRemoveIcon()}
      </LinkButton>}
    />
  );
}
