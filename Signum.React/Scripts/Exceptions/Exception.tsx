import * as React from 'react'
import { DateTime } from 'luxon'
import { BigStringEmbedded, ExceptionEntity } from '../Signum.Entities.Basics'
import { ValueLine, EntityLine, TypeContext } from '../Lines'
import { Tab, Tabs } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { classes } from '../Globals';

export default function Exception(p: { ctx: TypeContext<ExceptionEntity> }) {
  const ctx = p.ctx;
  const sc = p.ctx.subCtx({ labelColumns: { sm: 4 } });
  return (
    <div>
      <div className="row">
        <div className="col-sm-6">
          <ValueLine ctx={sc.subCtx(f => f.environment)} />
          <ValueLine ctx={sc.subCtx(f => f.creationDate)} unit={DateTime.fromISO(sc.value.creationDate!).toRelative() ?? undefined} />
          <EntityLine ctx={sc.subCtx(f => f.user)} />
          <ValueLine ctx={sc.subCtx(f => f.version)} />
          <ValueLine ctx={sc.subCtx(f => f.threadId)} />
          <ValueLine ctx={sc.subCtx(f => f.machineName)} />
          <ValueLine ctx={sc.subCtx(f => f.applicationName)} />
        </div>
        <div className="col-sm-6">
          <ValueLine ctx={sc.subCtx(f => f.actionName)} />
          <ValueLine ctx={sc.subCtx(f => f.controllerName)} />
          <ValueLine ctx={sc.subCtx(f => f.userHostAddress)} />
          <ValueLine ctx={sc.subCtx(f => f.userHostName)} />
          <ValueLine ctx={sc.subCtx(f => f.userAgent)} valueLineType="TextArea" />
          <ValueLine ctx={sc.subCtx(f => f.origin)}  />
        </div>
      </div>
      <ValueLine ctx={ctx.subCtx(f => f.requestUrl)} />
      <ValueLine ctx={ctx.subCtx(f => f.urlReferer)} />
      <h3 style={{ color: "rgb(139, 0, 0)" }}>{ctx.value.exceptionType} <small>(HResult = {ctx.value.hResult})</small></h3>

      <pre style={{ whiteSpace: "pre-wrap" }}><code>{ctx.value.exceptionMessage}</code></pre>
    
      <Tabs id="exceptionTabs">
        {codeTab("stackTrace", a => a.stackTrace)}
        {codeTab("data", a => a.data)}
        {codeTab("queryString", a => a.queryString)}
        {codeTab("form", a => a.form, true)}
        {codeTab("session", a => a.session)}
      </Tabs>
    </div>
  );

  function codeTab(tabId: string, property: (ex: ExceptionEntity) => BigStringEmbedded, formatJson?: boolean) {
    const tc = p.ctx.subCtx(property);

    if (tc.propertyRoute == null || !tc.value.text || tc.value.text == "")
      return undefined;

    return (
      <Tab title={tc.propertyRoute.member!.niceName} eventKey={tabId}>
        {formatJson ?
          <FormatJson code={tc.value.text} /> :
          <pre style={{ whiteSpace: "pre-wrap" }}>
            <code>{tc.value.text}</code>
          </pre>
        }
      </Tab>
    );
  }
}

export function FormatJson(p: { code: string | undefined | null }) {

  const [formatJson, setFormatJson] = React.useState<boolean>(false);

  const formattedJson = React.useMemo(() => {
    if (formatJson == false || p.code == undefined)
      return null;

    try {
      return JSON.stringify(JSON.parse(p.code), undefined, 2);
    } catch{
      return "Invalid Json"
    }
  }, [formatJson, p.code])

  return (
    <div>
      <button className={classes("btn btn-sm btn-light", formatJson && "active")} onClick={() => setFormatJson(!formatJson)}>
        <FontAwesomeIcon icon="code" /> Format JSON 
      </button>
      <pre style={{ whiteSpace: "pre-wrap" }}>
        <code>{formatJson ? formattedJson : p.code}</code>
      </pre>
    </div>
  );
}
