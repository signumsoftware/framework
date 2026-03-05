import * as React from 'react'
import { TourEntity, TourMessage, TourStepEntity } from '../Signum.Tour'
import { AutoLine, EntityLine, EntityTable, EnumLine, TextAreaLine, TextBoxLine, TypeContext } from '@framework/Lines'
import HtmlCodeMirror from "@extensions/Signum.CodeMirror/HtmlCodeMirror"
import { getTypeInfos } from '@framework/Reflection';
import { PropertyRouteEntity } from '@framework/Signum.Basics';
import { useForceUpdate } from '@framework/Hooks';

export default function TourStep(p: { ctx: TypeContext<TourStepEntity>, invalidate: () => void; }) {
  const ctx = p.ctx;
  const sc = ctx.subCtx({ labelColumns: 2 });
  const sc4 = ctx.subCtx({ labelColumns: 6 });
  const forceUpdate = useForceUpdate();

  const handleSideChange = () => {
    const side = ctx.value.side;
    if (side === "Top" || side === "Bottom") {
      ctx.value.align = "Center";
    } else if (side === "Left" || side === "Right") {
      ctx.value.align = "Start";
    }
    forceUpdate();
    p.invalidate();
  };

  return (
    <div>
      <AutoLine ctx={sc.subCtx(a => a.title)} onChange={p.invalidate} />
      <EntityTable ctx={sc.subCtx(a => a.cssSteps)} columns={[
        {
          property: a => a.type, 
          template: (ctx, row) => <EnumLine ctx={ctx.subCtx(a => a.type)} onChange={() => row.forceUpdate()} />,
          headerHtmlAttributes: {style: {width: "20%"}}
        },
        {
          header: "CSS Step",
          template: (ctx) => {
            const type = ctx.value.type;
            if (type === "CSSSelector") {
              return <TextBoxLine ctx={ctx.subCtx(a => a.cssSelector)} valueHtmlAttributes={{ className: "font-monospace", placeholder: "#someId div.some-class" }} />;
            } else if (type === "Property") {
              return <EntityLine ctx={ctx.subCtx(a => a.property)}  />;
            } else if (type === "ToolbarContent") {
              return <EntityLine ctx={ctx.subCtx(a => a.toolbarContent)} create={false}/>;
            }
            return null;
          },
          headerHtmlAttributes: {style: {width: "80%"}}
        }
      ]} />
      <div className="row">
        <div className="col-sm-4">
          <AutoLine ctx={sc4.subCtx(a => a.side)} onChange={handleSideChange} mandatory/>
        </div>
        <div className="col-sm-4">
          <AutoLine ctx={sc4.subCtx(a => a.align)} />
        </div>
      </div>
      <div className="code-container">
        <HtmlCodeMirror ctx={sc.subCtx(a => a.description)}  />
      </div>
    </div>
  );
}
