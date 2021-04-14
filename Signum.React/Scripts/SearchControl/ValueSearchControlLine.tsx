import * as React from 'react'
import { classes } from '../Globals'
import * as Finder from '../Finder'
import * as Constructor from '../Constructor'
import { FindOptions, QueryDescription } from '../FindOptions'
import { Lite, Entity, isEntity, EntityControlMessage, isLite } from '../Signum.Entities'
import { getQueryKey, getQueryNiceName, QueryTokenString, tryGetTypeInfos, getTypeInfos } from '../Reflection'
import * as Navigator from '../Navigator'
import { StyleContext, TypeContext } from '../TypeContext'
import ValueSearchControl from './ValueSearchControl'
import { FormGroup } from '../Lines/FormGroup'
import { SearchControlProps } from "./SearchControl";
import { BsColor } from '../Components';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { EntityBaseController } from '../Lines/EntityBase'
import SelectorModal from '../SelectorModal'

export interface ValueSearchControlLineProps extends React.Props<ValueSearchControlLine> {
  ctx: StyleContext;
  findOptions?: FindOptions;
  valueToken?: string | QueryTokenString<any>;
  multipleValues?: { vertical?: boolean, showType?: boolean };
  labelText?: React.ReactChild;
  labelHtmlAttributes?: React.HTMLAttributes<HTMLLabelElement>;
  unitText?: React.ReactChild;
  formGroupHtmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
  helpText?: React.ReactChild;
  initialValue?: any;
  isLink?: boolean;
  isBadge?: boolean | "MoreThanZero";
  badgeColor?: BsColor;
  customClass?: string | ((value: any | undefined) => (string | undefined ));
  customStyle?: React.CSSProperties;
  isFormControl?: boolean;
  findButton?: boolean;
  viewEntityButton?: boolean;
  avoidAutoRefresh?: boolean;
  refreshKey?: any;
  extraButtons?: (valueSearchControl: ValueSearchControl) => React.ReactNode;
  create?: boolean;
  onCreate?: () => Promise<any>;
  getViewPromise?: (e: any /*Entity*/) => undefined | string | Navigator.ViewPromise<any /*Entity*/>;
  searchControlProps?: Partial<SearchControlProps>;
  onExplored?: () => void;
  onViewEntity?: (entity: Lite<Entity>) => void;
  onValueChanged?: (value: any) => void;
}

export default class ValueSearchControlLine extends React.Component<ValueSearchControlLineProps> {

  valueSearchControl?: ValueSearchControl | null;

  handleValueSearchControlLoaded = (vsc: ValueSearchControl | null) => {

    if (vsc != this.valueSearchControl)
      this.forceUpdate();

    this.valueSearchControl = vsc;
  }

  getFindOptions(props: ValueSearchControlLineProps): FindOptions {
    if (props.findOptions)
      return props.findOptions;

    var ctx = props.ctx as TypeContext<any>;

    if (isEntity(ctx.value))
      return {
        queryName: ctx.value.Type,
        parentToken: QueryTokenString.entity(),
        parentValue: ctx.value
      };

    if (isLite(ctx.value))
      return {
        queryName: ctx.value.EntityType,
        parentToken: QueryTokenString.entity(),
        parentValue: ctx.value
      };

    throw new Error("Impossible to determine 'findOptions' because 'ctx' is not a 'TypeContext<Entity>'. Set it explicitly");
  }

  refreshValue() {
    this.valueSearchControl && this.valueSearchControl.refreshValue()
  }

  render() {

    var fo = this.getFindOptions(this.props);

    if (!Finder.isFindable(fo.queryName, false))
      return null;

    var errorMessage = Finder.validateNewEntities(fo);
    if (errorMessage) {
      return (
        <div className="alert alert-danger" role="alert">
          <strong>Error in ValueSearchControlLine ({getQueryKey(fo.queryName)}): </strong>
          {errorMessage}
        </div>
      );
    }

    var token = this.valueSearchControl && this.valueSearchControl.state.token;

    const isQuery = this.props.valueToken == undefined || token?.queryTokenType == "Aggregate";

    const isBadge = (this.props.isBadge ?? this.props.valueToken == undefined ? "MoreThanZero" as "MoreThanZero" : false);
    const isFormControl = (this.props.isFormControl ?? this.props.valueToken != undefined);

    const unit = isFormControl && (this.props.unitText ?? (token?.unit && <span className="input-group-text">{token.unit}</span>));

    const ctx = this.props.ctx;

    const value = this.valueSearchControl && this.valueSearchControl.state.value;
    const find = value != undefined && (this.props.findButton ?? isQuery) &&
      <a href="#" className={classes("sf-line-button sf-find", isFormControl ? "btn input-group-text" : undefined)}
        onClick={this.valueSearchControl!.handleClick}
        title={ctx.titleLabels ? EntityControlMessage.Find.niceToString() : undefined}>
        {EntityBaseController.findIcon}
      </a>;

    const create = (this.props.create ?? false) &&
      <a href="#" className={classes("sf-line-button sf-create", isFormControl ? "btn input-group-text" : undefined)}
        onClick={this.handleCreateClick}
        title={ctx.titleLabels ? EntityControlMessage.Create.niceToString() : undefined}>
        {EntityBaseController.createIcon}
      </a>;

    const view = value != undefined && (this.props.viewEntityButton ?? (isLite(value) && Navigator.isViewable(value.EntityType))) &&
      <a href="#" className={classes("sf-line-button sf-view", isFormControl ? "btn input-group-text" : undefined)}
        onClick={this.handleViewEntityClick}
        title={ctx.titleLabels ? EntityControlMessage.View.niceToString() : undefined}>
        {EntityBaseController.viewIcon}
      </a>

    let extra = this.valueSearchControl && this.props.extraButtons && this.props.extraButtons(this.valueSearchControl);

    return (
      <FormGroup ctx={this.props.ctx}
        labelText={this.props.labelText ?? token?.niceName ?? getQueryNiceName(fo.queryName)}
        labelHtmlAttributes={this.props.labelHtmlAttributes}
        htmlAttributes={{ ...this.props.formGroupHtmlAttributes, ...{ "data-value-query-key": getQueryKey(fo.queryName) } }}
        helpText={this.props.helpText}
      >
        <div className={isFormControl ? ((unit || view || extra || find || create) ? this.props.ctx.inputGroupClass : undefined) : this.props.ctx.formControlPlainTextClass}>
          <ValueSearchControl
            ref={this.handleValueSearchControlLoaded}
            findOptions={fo}
            initialValue={this.props.initialValue}
            multipleValues={this.props.multipleValues}
            isBadge={isBadge}
            customClass={this.props.customClass ?? (this.props.multipleValues ? this.props.ctx.labelClass : undefined)}
            customStyle={this.props.customStyle}
            badgeColor={this.props.badgeColor}
            isLink={this.props.isLink ?? Boolean(this.props.multipleValues)}
            formControlClass={isFormControl && !this.props.multipleValues ? this.props.ctx.formControlClass + " readonly" : undefined}
            valueToken={this.props.valueToken}
            onValueChange={v => { this.forceUpdate(); this.props.onValueChanged && this.props.onValueChanged(v); }}
            onTokenLoaded={() => this.forceUpdate()}
            onExplored={this.props.onExplored}
            searchControlProps={this.props.searchControlProps}
            refreshKey={this.props.refreshKey}
          />

          {(view || extra || find || unit) && (isFormControl ?
            <div className="input-group-append">
              {unit}
              {view}
              {find}
              {create}
              {extra}
            </div> : <span>
              {unit}
              {view}
              {find}
              {create}
              {extra}
            </span>)}
        </div>
      </FormGroup>
    );
  }

  handleViewEntityClick = (e: React.MouseEvent<any>) => {
    e.preventDefault();

    var entity = this.valueSearchControl!.state.value as Lite<Entity>;
    if (this.props.onViewEntity)
      this.props.onViewEntity(entity);

    Navigator.view(entity)
      .then(() => {
        this.refreshValue();
        this.props.onExplored && this.props.onExplored();
      }).done();
  }

  handleCreateClick = (e: React.MouseEvent<any>) => {
    e.preventDefault();

    if (this.props.onCreate) {
      this.props.onCreate().then(() => {
        if (!this.props.avoidAutoRefresh)
          this.valueSearchControl?.refreshValue();
      }).done();
    } else {

      var fo = this.props.findOptions!;
      const isWindowsOpen = e.button == 1 || e.ctrlKey;
      Finder.getQueryDescription(fo.queryName).then(qd => {
        this.chooseType(qd).then(tn => {
          if (tn == null)
            return;

          var s = Navigator.getSettings(tn);
          var qs = Finder.getSettings(fo.queryName);
          var getViewPromise = this.props.getViewPromise ?? qs?.getViewPromise;

          if (isWindowsOpen || (s != null && s.avoidPopup)) {
            var vp = getViewPromise && getViewPromise(null)

            window.open(Navigator.createRoute(tn, vp && typeof vp == "string" ? vp : undefined));
          } else {
            Finder.parseFilterOptions(fo.filterOptions || [], false, qd)
              .then(fos => Finder.getPropsFromFilters(tn, fos)
                .then(props => Constructor.constructPack(tn, props))
                .then(pack => pack && Navigator.view(pack!, {
                  getViewPromise: getViewPromise as any,
                  createNew: () => Finder.getPropsFromFilters(tn, fos)
                    .then(props => Constructor.constructPack(tn, props)!),
                })))
              .then(() => this.props.avoidAutoRefresh ? undefined : this.valueSearchControl!.refreshValue())
              .done();
          }
        }).done();
      }).done();
    }
  }

  chooseType(qd: QueryDescription): Promise<string | undefined> {

    const tis = getTypeInfos(qd.columns["Entity"].type)
      .filter(ti => Navigator.isCreable(ti, { isSearch: true }));

    return SelectorModal.chooseType(tis)
      .then(ti => ti ? ti.name : undefined);
  }
}
