
import * as React from 'react'
import { Binding, EntityBaseController, EntityCombo, EntityLine, EntityTable, ValueLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ColorPaletteEntity, ColorPaletteMessage, SpecificColorEmbedded } from '../Signum.Entities.Chart';
import { colorSchemes } from './ColorUtils';
import { classes, Dic } from '@framework/Globals';
import * as Navigator from '@framework/Navigator';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as Finder from '@framework/Finder';
import { useAPI, useForceUpdate } from '@framework/Hooks';
import { ColorTypeaheadLine } from '../../Basics/Templates/ColorTypeahead';
import { getTypeInfo, IBinding, tryGetTypeInfo } from '@framework/Reflection';
import { Entity, EntityControlMessage, Lite, newMListElement, toLite } from '@framework/Signum.Entities';
import { UserEntity } from '../../Authorization/Signum.Entities.Authorization';
import { EntityLink } from '@framework/Search';
import { calculateColor, ColorScheme } from './ColorPaletteClient';

export default function ColorPalette(p: { ctx: TypeContext<ColorPaletteEntity> }) {
  const ctx = p.ctx;
  const forceUpdate = useForceUpdate();
  const ctx4 = ctx.subCtx({ formGroupStyle: "Basic" });

  const ti = ctx4.value.type && tryGetTypeInfo(ctx4.value.type.cleanName);

  const enumConverter = useAPI(() => ti?.kind == "Enum" ? Navigator.API.getEnumEntities(ti.name) : Promise.resolve(null), [ti?.name]);

  //const count = useAPI(() => ti?.queryDefined ? Finder.getQueryValue(ti, []) : Promise.resolve(null), [ti?.name]);

  function withConverter(ctx: TypeContext<Lite<Entity>>): TypeContext<string | null> {
    return new TypeContext<string | null>(ctx, undefined, ctx.propertyRoute, new ConvertBinding(ctx.binding, enumConverter!));
  }

  const colors = ctx4.value.categoryName ? colorSchemes[ctx4.value.categoryName] : null;


  async function handleMagicWand(e: React.MouseEvent) {

    e.preventDefault();

    if (ti == null)
      return;

    var fewEntities: Lite<Entity>[] | null  | undefined =
      ti.kind == "Enum" ? Dic.getValues(enumConverter!.enumToEntity).map(a => toLite(a)) :
        ti.isLowPopulation || await Finder.getQueryValue(ti.name, []) < 20 ? await Finder.API.fetchAllLites({ types: ti.name }) :
          null;

    if (fewEntities != null) {

      var step = fewEntities.length == 0 ? 1 : Math.floor(colors!.length / fewEntities.length);
      if (step == 0)
        step = 1;


      ctx.value.specificColors = [...fewEntities.map((e, i) => newMListElement(SpecificColorEmbedded.New({
        entity: e,
        color: colors![i * step % colors!.length]
      })))];

      forceUpdate();

    }
    else {

      var chooseEntities = await Finder.findMany({ queryName: ti.name }, {
        message: ColorPaletteMessage.Select0OnlyIfYouWantToOverrideTheAutomaticColor.niceToString().formatHtml(<strong>{ti.nicePluralName}</strong>),
        searchControlProps: {
          entityFormatter: new Finder.EntityFormatter((row, cols, sc) => !row.entity || !Navigator.isViewable(row.entity.EntityType, { isSearch: true }) ? undefined :
            <EntityLink lite={row.entity}
              inSearch={true}
              onNavigated={sc?.handleOnNavigated}
              getViewPromise={sc && (sc.props.getViewPromise ?? sc.props.querySettings?.getViewPromise)}
              inPlaceNavigation={sc?.props.view == "InPlace"} className="sf-line-button sf-view">
              <div title={EntityControlMessage.View.niceToString()} className="d-inline-flex align-items-center">
                <span style={{ backgroundColor: !colors ? undefined : calculateColor(row.entity.id!.toString(), colors, ctx.value.seed ?? 0), height: "20px", width: "20px", display: "inline-block", marginBottom: "-6px" }} className="me-2" />
                {EntityBaseController.viewIcon}
              </div>
            </EntityLink>)
        }
      });

      if (chooseEntities != null) {

        ctx.value.specificColors = [...chooseEntities.map(e => newMListElement(SpecificColorEmbedded.New({
          entity: e,
          color: !colors ? undefined : calculateColor(e.id!.toString(), colors!, ctx.value.seed ?? 0)
        })))];

        forceUpdate();
      }
    }
  }

  return (
    <div>

      <div className="row">
        <div className="col-sm-4">
          <EntityLine ctx={ctx4.subCtx(n => n.type)} readOnly={!ctx.value.isNew || ctx.value.specificColors.length > 0} onChange={forceUpdate} />
        </div>
        <div className="col-sm-4">
          <ValueLine ctx={ctx4.subCtx(n => n.categoryName)} onChange={forceUpdate}
            valueLineType="DropDownList"
            optionItems={Dic.getKeys(colorSchemes)}
            onRenderDropDownListItem={oi => <div style={{ display: "flex", alignItems: "center", userSelect: "none" }}>
              <ColorScheme colorScheme={oi.value} />
              {oi.label}
            </div>} />
        </div>
        <div className="col-sm-4">
          <ValueLine ctx={ctx4.subCtx(n => n.seed)} />
        </div>
      </div>

      {ti != null && (ti.kind != "Enum" || enumConverter != null) &&
        <EntityTable ctx={ctx.subCtx(p => p.specificColors)}
          extraButtonsAfter={() => <a href="#" className={classes("sf-line-button", "sf-create")}
            title={ColorPaletteMessage.FillAutomatically.niceToString()}
            onClick={handleMagicWand}>
            <FontAwesomeIcon icon="wand-magic-sparkles" />
          </a>}
          columns={EntityTable.typedColumns<SpecificColorEmbedded>([
            {
              property: p => p.entity,
              template: (ectx) =>
                ti.kind == "Enum" ? <ValueLine type={{ name: ctx4.value.type.cleanName }} optionItems={Dic.getKeys(enumConverter!.enumToEntity)} ctx={withConverter(ectx.subCtx(p => p.entity))} /> :
                  ti?.isLowPopulation ? <EntityCombo ctx={ectx.subCtx(p => p.entity)} type={{ name: ctx4.value.type.cleanName, isLite: true }} /> :
                    <EntityLine ctx={ectx.subCtx(p => p.entity)} type={{ name: ctx4.value.type.cleanName, isLite: true }} />,
              headerHtmlAttributes: { style: { width: "40%" } },
            },
            {
              property: p => p.color,
              template: (ectx) => <ColorSelector ctx={ectx.subCtx(a => a.color)} colors={colors as (string[] | null)} />,
              headerHtmlAttributes: { style: { width: "40%" } },
            },
          ])}
        />
      }
    </div>
  );
}

function ColorSelector(p: { ctx: TypeContext<string>, colors: string[] | null }) {

  const [custom, setCustom] = React.useState<boolean>(false);

  React.useEffect(() => {
    setCustom(p.colors == null || p.ctx.value != null && !p.colors.contains(p.ctx.value));
  }, [p.colors])

  if (custom || p.colors == null)
    return <ColorTypeaheadLine ctx={p.ctx} extraButtons={() => getSwitchModelButton()} />

  return <ValueLine ctx={p.ctx}
    valueLineType="DropDownList"
    optionItems={p.colors!}
    onRenderDropDownListItem={oi => <span>
      <span style={{ backgroundColor: oi.value, height: "20px", width: "20px", display: "inline-block", marginBottom: "-6px" }} className="me-2" />
      {oi.label}
    </span>
    }
    extraButtons={vl => getSwitchModelButton()}
  />;

  function getSwitchModelButton(): React.ReactElement<any> {
    return (
      <a href="#" className={classes("sf-line-button", "sf-find", "btn input-group-text")}
        onClick={e => {
          e.preventDefault();
          setCustom(!custom);
        }}>
        <FontAwesomeIcon icon={custom ? "palette" : "list"} />
      </a>
    );
  }
}



class ConvertBinding implements IBinding<string | null> {

  parent: IBinding<Lite<Entity> | null>;
  converter: Navigator.EnumConverter<string>;

  constructor(binding: IBinding<Lite<Entity>>, enumEntities: Navigator.EnumConverter<string>) {
    this.parent = binding;
    this.suffix = this.parent.suffix;
    this.converter = enumEntities;
  }

  getValue(): string | null {
    var val = this.parent.getValue();
    return val && this.converter.idToEnum[val.id!];
  }

  setValue(val: string): void {
    this.parent.setValue(val == null ? null : toLite(this.converter.enumToEntity[val]));
  }
  suffix: string;
  getIsReadonly(): boolean {
    return this.parent.getIsReadonly();
  }
  getError(): string | undefined {
    return this.parent.getError()
  }
  setError(value: string | undefined): void {
    return this.parent.setError(value);
  }
}



