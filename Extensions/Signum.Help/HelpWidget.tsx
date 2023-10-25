
import * as React from 'react'
import { Popover, OverlayTrigger  } from 'react-bootstrap'
import { Entity, getToString } from '@framework/Signum.Entities'
import * as HelpClient from './HelpClient';
import * as AppContext from '@framework/AppContext';
import { WidgetContext } from '@framework/Frames/Widgets';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { HelpMessage, TypeHelpEntity } from './Signum.Help';
import { useAPI } from '@framework/Hooks';
import { TypeContext } from '@framework/Lines';
import { HtmlViewer } from './Pages/EditableText';
import './HelpWidget.css';

export interface HelpWidgetProps {
  wc: WidgetContext<Entity>
}

const cache: { [cleanName: string]: Promise<TypeHelpEntity> } = {}; 

export function HelpWidget(p: HelpWidgetProps) {

  const entity = p.wc.ctx.value;

  var typeHelp = useAPI(() => cache[entity.Type] ?? HelpClient.API.type(entity.Type), [entity.Type]);

  var hasContent = Boolean(typeHelp && !typeHelp!.isNew);

  React.useEffect(() => {
    if (hasContent) {
      p.wc.frame.pack.typeHelp = typeHelp;
      p.wc.frame.onReload();
    }
  }, [hasContent])

  return (
    <a href={AppContext.toAbsoluteUrl(HelpClient.Urls.typeUrl(entity.Type))} target="_blank" className={hasContent ? "sf-help-button active" : "sf-help-button"}>
      <FontAwesomeIcon icon="circle-question" />
    </a>
  );
}

export function HelpIcon(p: { ctx: TypeContext<any> }) {

  //debugger;

  if (p.ctx.propertyRoute == null)
    return undefined;

  var typeHelp = p.ctx.frame?.pack.typeHelp;

  const pr = p.ctx.propertyRoute;

  var rootType = pr.findRootType();

  if (rootType?.name != typeHelp?.type.cleanName)
    return false;

  var prop = typeHelp?.properties.firstOrNull(a => a.element.property.path == p.ctx.propertyPath)?.element;

  if (prop == null || prop.description == null)
    return null;

  const popover = (
    <Popover id="popover-basic">
      <Popover.Header as="h3">{HelpMessage.Help.niceToString()}</Popover.Header>
      <Popover.Body>
        <HtmlViewer text={prop.description} />
        <br/>
        <a href={AppContext.toAbsoluteUrl(HelpClient.Urls.propertyUrl(rootType, pr))} target="_blank">
          {HelpMessage.ViewMore.niceToString()}
        </a>
      </Popover.Body>
    </Popover>
  );

  return (
    <OverlayTrigger trigger="click" placement="right" overlay={popover}>
      <a href="#" onClick={e => e.preventDefault()} className="ms-1 sf-help-button" title={HelpMessage.Help.niceToString()}>
        <FontAwesomeIcon icon="circle-question" />
      </a>
    </OverlayTrigger>
  );
}
