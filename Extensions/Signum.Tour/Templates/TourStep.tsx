import * as React from 'react'
import { CssStepType, TourEntity, TourMessage, TourStepEntity } from '../Signum.Tour'
import { AutoLine, EntityLine, EntityTable, EnumLine, TextAreaLine, TextBoxLine, TypeContext } from '@framework/Lines'
import HtmlCodeMirror from "@extensions/Signum.CodeMirror/HtmlCodeMirror"
import { getTypeInfos, getTypeInfo } from '@framework/Reflection';
import { PropertyRouteEntity, TypeEntity } from '@framework/Signum.Basics';
import { useForceUpdate } from '@framework/Hooks';
import PropertyRouteCombo from '@framework/Components/PropertyRouteCombo';
import { classes } from '@framework/Globals';

export default function TourStep(p: { ctx: TypeContext<TourStepEntity>, invalidate: () => void; type?: TypeEntity | null }) {
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

  const getAvailableCssStepTypes = (): CssStepType[] => {
    const allTypes: CssStepType[] = ["CSSSelector", "Property", "ToolbarContent"];
    if (!p.type) {
      return allTypes.filter(t => t !== "Property");
    }
    return allTypes;
  };

  return (
    <div>
      <AutoLine ctx={sc.subCtx(a => a.title)} onChange={p.invalidate} />
      <EntityTable ctx={sc.subCtx(a => a.cssSteps)} columns={[
        {
          property: a => a.type, 
          template: (ctx, row) => <EnumLine ctx={ctx.subCtx(a => a.type)} onChange={() => row.forceUpdate()} optionItems={getAvailableCssStepTypes()} />,
          headerHtmlAttributes: {style: {width: "20%"}}
        },
        {
          header: "CSS Step",
          template: (ctx) => {
            const type = ctx.value.type;
            if (type === "CSSSelector") {
              return <TextBoxLine ctx={ctx.subCtx(a => a.cssSelector)} valueHtmlAttributes={{ className: "font-monospace", placeholder: "#someId div.some-class" }} />;
            } else if (type === "Property") {
              return p.type && <PropertyRouteCombo ctx={ctx.subCtx(a => a.property)} type={p.type} />;
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
      <div className={classes("code-container", sc.value.description ? null : "sf-mandatory")}>
        <HtmlCodeMirror ctx={sc.subCtx(a => a.description)} onChange={forceUpdate}/>
      </div>
    </div>
  );
}
