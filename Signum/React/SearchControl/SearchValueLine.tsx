import * as React from 'react'
import { classes } from '../Globals'
import { Finder } from '../Finder'
import { Constructor } from '../Constructor'
import { FindOptions, FindOptionsParsed, QueryDescription, QueryToken, QueryValueRequest } from '../FindOptions'
import { Lite, Entity, isEntity, EntityControlMessage, isLite } from '../Signum.Entities'
import { getQueryKey, getQueryNiceName, QueryTokenString, tryGetTypeInfos, getTypeInfos } from '../Reflection'
import { Navigator, ViewPromise } from '../Navigator'
import { StyleContext, TypeContext } from '../TypeContext'
import SearchValue, { renderTimeMachineIcon, SearchValueController } from './SearchValue'
import { FormGroup } from '../Lines/FormGroup'
import { SearchControlProps } from "./SearchControl";
import { BsColor, BsSize } from '../Components';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { EntityBaseController } from '../Lines/EntityBase'
import SelectorModal from '../SelectorModal'
import { useForceUpdate } from '../Hooks'
import { toAbsoluteUrl } from '../AppContext'

export interface SearchValueLineProps {
  ctx: StyleContext;
  findOptions?: FindOptions | Lite<Entity> | Entity;
  valueToken?: string | QueryTokenString<any>;
  multipleValues?: { vertical?: boolean, showType?: boolean };
  label?: React.ReactNode | (() => React.ReactNode);
  labelHtmlAttributes?: React.HTMLAttributes<HTMLLabelElement>;
  unit?: React.ReactNode;
  format?: string;
  formGroupHtmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
  helpText?: (vscc: SearchValueController) => React.ReactNode | undefined;
  initialValue?: any;
  isLink?: boolean;
  isBadge?: boolean | "MoreThanZero";
  badgeColor?: BsColor;
  customClass?: string | ((value: any | undefined) => (string | undefined));
  customStyle?: React.CSSProperties;
  isFormControl?: boolean;
  findButton?: boolean;
  viewEntityButton?: boolean;
  avoidAutoRefresh?: boolean;
  deps?: React.DependencyList;
  extraButtons?: (vscc: SearchValueController) => React.ReactNode;
  create?: boolean | "ifNull" ;
  onCreate?: () => Promise<any>;
  getViewPromise?: (e: any /*Entity*/) => undefined | string | ViewPromise<any /*Entity*/>;
  searchControlProps?: Partial<SearchControlProps>;
  modalSize?: BsSize;
  onExplored?: () => void;
  onViewEntity?: (entity: Lite<Entity>) => void;
  onValueChanged?: (value: any) => void;
  customRequest?: (req: QueryValueRequest, fop: FindOptionsParsed, token: QueryToken | null, signal: AbortSignal) => Promise<any>,
  onRender?: (value: any | undefined, vsc: SearchValueController) => React.ReactElement | null | undefined | false,
  onExplore?: (vsc: SearchValueController) => Promise<boolean>;
}

export interface SearchValueLineController {
  searchValue: SearchValueController | null | undefined;
  refreshValue(): void;
}

const SearchValueLine: React.ForwardRefExoticComponent<SearchValueLineProps & React.RefAttributes<SearchValueLineController>> =
  React.forwardRef(function SearchValueLine(p: SearchValueLineProps, ref?: React.Ref<SearchValueLineController>) {

  var svRef = React.useRef<SearchValueController>(null);

  React.useImperativeHandle(ref, () => ({
    searchValue: svRef.current,
    refreshValue: () => svRef.current?.refreshValue(),
  }), [svRef.current]);


  const forceUpdate = useForceUpdate();

  var handleSearchValueLoaded = React.useCallback((vsc: SearchValueController | null) => {
    if (vsc != svRef.current) {
      forceUpdate();
    }
    svRef.current = vsc;
  }, []);

  function getFindOptions(props: SearchValueLineProps): FindOptions {
    if (props.findOptions) {
      const fo = props.findOptions;
      if (isEntity(fo))
        return {
          queryName: fo.Type,
          filterOptions: [{ token: QueryTokenString.entity(), value: fo }]
        };

      if (isLite(fo))
        return {
          queryName: fo.EntityType,
          filterOptions: [{ token: QueryTokenString.entity(), value: fo }]
        };

      return fo;
    }

    var ctx = props.ctx as TypeContext<any>;

    if (isEntity(ctx.value))
      return {
        queryName: ctx.value.Type,
        filterOptions: [{ token: QueryTokenString.entity(), value: ctx.value }]
      };

    if (isLite(ctx.value))
      return {
        queryName: ctx.value.EntityType,
        filterOptions: [{ token: QueryTokenString.entity(), value: ctx.value }]
      };

    throw new Error("Impossible to determine 'findOptions' because 'ctx' is not a 'TypeContext<Entity>'. Set it explicitly");
  }

  var fo = getFindOptions(p);

  if (!Finder.isFindable(fo.queryName, false))
    return null;

  var errorMessage = Finder.validateNewEntities(fo);
  if (errorMessage) {
    return (
      <div className="alert alert-danger" role="alert">
        <strong>Error in SearchValueLine ({getQueryKey(fo.queryName)}): </strong>
        {errorMessage}
      </div>
    );
  }
  
  var token = svRef.current?.valueToken;

  const isQuery = p.valueToken == undefined || token?.queryTokenType == "Aggregate";

  const isBadge = (p.isBadge ?? p.valueToken == undefined ? "MoreThanZero" as "MoreThanZero" : false);
  const isFormControl = (p.isFormControl ?? p.valueToken != undefined);

  const unit = isFormControl && (p.unit ?? token?.unit) && <span className="input-group-text">{p.unit ?? token?.unit}</span>;

  const ctx = p.ctx;

  const value = svRef.current?.value;
  const find = value != undefined && (p.findButton ?? isQuery) &&
    <a href="#" className={classes("sf-line-button sf-find", isFormControl ? "btn input-group-text" : undefined)}
      onClick={svRef.current!.handleClick}
      title={ctx.titleLabels ? EntityControlMessage.Find.niceToString() : undefined}>
      {EntityBaseController.getFindIcon()}
    </a>;
  
  const create = !p.ctx.frame?.currentDate && (p.create == true || p.create == "ifNull" && value === null) &&
    <a href="#" className={classes("sf-line-button sf-create", isFormControl ? "btn input-group-text" : undefined)}
      onClick={handleCreateClick}
      title={ctx.titleLabels ? EntityControlMessage.Create.niceToString() : undefined}>
      {EntityBaseController.getCreateIcon()}
    </a>;

  const view = value != undefined && (p.viewEntityButton ?? (isLite(value) && Navigator.isViewable(value.EntityType))) &&
    <a href="#" className={classes("sf-line-button sf-view", isFormControl ? "btn input-group-text" : undefined)}
      onClick={handleViewEntityClick}
      title={ctx.titleLabels ? EntityControlMessage.View.niceToString() : undefined}>
      {EntityBaseController.getViewIcon()}
    </a>

  let extra = svRef.current && p.extraButtons && p.extraButtons(svRef.current);

  var label = p.label == undefined ? undefined :
    typeof p.label == "function" ? p.label() :
      p.label;

  return (
    <FormGroup ctx={p.ctx}
      label={label ?? token?.niceName ?? getQueryNiceName(fo.queryName)}
      labelHtmlAttributes={p.labelHtmlAttributes}
      htmlAttributes={{ ...p.formGroupHtmlAttributes, ...{ "data-value-query-key": getQueryKey(fo.queryName) } }}
      helpText={p.helpText && svRef.current && p.helpText(svRef.current)}>
      {inputId => <div className={isFormControl ? ((unit || view || extra || find || create) ? p.ctx.inputGroupClass : undefined) : p.ctx.formControlPlainTextClass}>
        {svRef.current && renderTimeMachineIcon(svRef.current.hasHistoryChanges, `translate(-40%, -40%)`)}
        <SearchValue
          ctx={p.ctx}
          id={inputId}
          ref={handleSearchValueLoaded}
          findOptions={fo}
          format={p.format}
          initialValue={p.initialValue}
          onInitialValueLoaded={() => forceUpdate()}
          multipleValues={p.multipleValues}
          isBadge={isBadge}
          customClass={p.customClass ?? (p.multipleValues ? p.ctx.labelClass : undefined)}
          customStyle={p.customStyle}
          badgeColor={p.badgeColor}
          isLink={p.isLink ?? Boolean(p.multipleValues)}
          formControlClass={isFormControl && !p.multipleValues ? p.ctx.formControlClass + " readonly" : undefined}
          valueToken={p.valueToken}
          onValueChange={v => { forceUpdate(); p.onValueChanged && p.onValueChanged(v); }}
          onTokenLoaded={() => forceUpdate()}
          onExplored={p.onExplored}
          searchControlProps={p.searchControlProps}
          modalSize={p.modalSize}
          unit={null}
          deps={p.deps}
          onRender={p.onRender}
          onExplore={p.onExplore}
          customRequest={p.customRequest}
          avoidRenderTimeMachineIcon
        />
        {unit}
        {view}
        {find}
        {create}
        {extra}
      </div>}
    </FormGroup>
    
  );


  function handleViewEntityClick(e: React.MouseEvent<any>) {
    e.preventDefault();

    var entity = svRef.current!.value as Lite<Entity>;
    if (p.onViewEntity)
      p.onViewEntity(entity);

    Navigator.view(entity)
      .then(() => {
        svRef.current!.refreshValue();
        p.onExplored && p.onExplored();
      });
  }

  function handleCreateClick(e: React.MouseEvent<any>) {
    e.preventDefault();

    if (p.onCreate) {
      p.onCreate().then(() => {
        if (!p.avoidAutoRefresh)
          svRef.current!.refreshValue();
      });
    } else {

      var fo = p.findOptions as FindOptions;
      const isWindowsOpen = e.button == 1 || e.ctrlKey;
      Finder.getQueryDescription(fo.queryName).then(qd => {
        chooseType(qd).then(tn => {
          if (tn == null)
            return;

          var s = Navigator.getSettings(tn);
          var qs = Finder.getSettings(fo.queryName);
          var getViewPromise = p.getViewPromise ?? qs?.getViewPromise;

          if (isWindowsOpen || (s != null && s.avoidPopup)) {
            var vp = getViewPromise && getViewPromise(null)

            window.open(toAbsoluteUrl(Navigator.createRoute(tn, vp && typeof vp == "string" ? vp : undefined)));
          } else {
            Finder.parseFilterOptions(fo.filterOptions || [], false, qd)
              .then(fos => Finder.getPropsFromFilters(tn, fos)
                .then(props => Constructor.constructPack(tn, props))
                .then(pack => pack && Navigator.view(pack!, {
                  getViewPromise: getViewPromise as any,
                  createNew: () => Finder.getPropsFromFilters(tn, fos)
                    .then(props => Constructor.constructPack(tn, props)!),
                })))
              .then(() => p.avoidAutoRefresh ? undefined : svRef.current!.refreshValue());
          }
        });
      });
    }
  }

  function chooseType(qd: QueryDescription): Promise<string | undefined> {

    const tis = getTypeInfos(qd.columns["Entity"].type)
      .filter(ti => Navigator.isCreable(ti, { isSearch: true }));

    return SelectorModal.chooseType(tis)
      .then(ti => ti ? ti.name : undefined);
  }

});


export default SearchValueLine; 
