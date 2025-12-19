
import * as React from 'react'
import { Popover, OverlayTrigger  } from 'react-bootstrap'
import { Entity, getToString } from '@framework/Signum.Entities'
import { HelpClient } from './HelpClient';
import * as AppContext from '@framework/AppContext';
import { WidgetContext } from '@framework/Frames/Widgets';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { HelpMessage, TypeHelpEntity } from './Signum.Help';
import { useAPI } from '@framework/Hooks';
import { TypeContext } from '@framework/Lines';
import { HtmlViewer } from './Editor/EditableHtml';
import './HelpWidget.css';
import { classes } from '@framework/Globals';
import { LinkButton } from '@framework/Basics/LinkButton';

export interface HelpWidgetProps {
  wc: WidgetContext<Entity>
}

export function HelpWidget(p: HelpWidgetProps): React.JSX.Element {

  const entity = p.wc.ctx.value;

  var typeHelp = useAPI(() => HelpClient.API.type(entity.Type), [entity.Type]);

  var hasContent = Boolean(typeHelp && !typeHelp!.isNew);

  React.useEffect(() => {
    if (hasContent) {
      p.wc.frame.pack.typeHelp = typeHelp;
      p.wc.frame.onReload();
    }
  }, [hasContent])

  return (
    <a href={AppContext.toAbsoluteUrl(HelpClient.Urls.typeUrl(entity.Type))} role="button" target="_blank" className={hasContent ? "sf-help-button active" : "sf-help-button"}>
      <FontAwesomeIcon aria-hidden={true} icon="circle-question" />
    </a>
  );
}

export function HelpIcon(p: { ctx: TypeContext<any>; typeHelp?: TypeHelpEntity }): React.JSX.Element | undefined | null | boolean {

  if (!p.ctx.propertyRoute) return undefined;

  const typeHelp = p.typeHelp ?? p.ctx.frame?.pack.typeHelp;
  const pr = p.ctx.propertyRoute;
  const rootType = pr.findRootType();
  if (rootType?.name !== typeHelp?.type.cleanName) return false;

  const prop = typeHelp?.properties.firstOrNull(a => a.element.property.path === p.ctx.propertyPath)?.element;
  if (!prop?.description) return null;

  const bodyRef = React.useRef<HTMLDivElement>(null);
  const uniqueId = `popover-viewmore-help-jump`;

  const popover = (
    <Popover id="popover-basic" role="dialog" tabIndex={-1} aria-labelledby="popover-header" aria-describedby="popover-body">
      <Popover.Header as="h3" id="popover-header">{HelpMessage.Help.niceToString()}</Popover.Header>

      <a href={`#${uniqueId}`} style={{ position: "absolute", left: "-9999px" }}>{HelpMessage.JumpToViewMore.niceToString()}</a>

      <Popover.Body id="popover-body" role="document" ref={bodyRef}>
        <HtmlViewer text={prop.description} />
        <br />
        <a id={uniqueId} href={AppContext.toAbsoluteUrl(HelpClient.Urls.propertyUrl(rootType, pr))} target="_blank">
          {HelpMessage.ViewMore.niceToString()}
        </a>
      </Popover.Body>
    </Popover>
  );

  const handleEntered = () => {
    bodyRef.current?.parentElement?.focus();
  };

  return (
    <OverlayTrigger
      trigger="click"
      rootClose
      placement="right"
      overlay={popover}
      onEntered={handleEntered}
    >
      <LinkButton onClick={e => { }} className="ms-1 sf-help-button" title={HelpMessage.Help.niceToString()}>
        <FontAwesomeIcon aria-hidden={true} icon="circle-question" />
      </LinkButton>
    </OverlayTrigger>
  );
}

interface TypeHelpIconProps extends React.HTMLAttributes<HTMLAnchorElement>{
  type: string
}

export function TypeHelpIcon({type, className, ...props} : TypeHelpIconProps): React.JSX.Element {

  return (
    <a href={AppContext.toAbsoluteUrl(HelpClient.Urls.typeUrl(type))} role="button" target="_blank" className={classes("sf-help-button", className)} {...props}>
      <FontAwesomeIcon aria-hidden={true} icon="circle-question" />
    </a>
  );
}
