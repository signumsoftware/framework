import * as React from 'react'
import { classes } from '../Globals'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { Lite, Entity, isEntity, EntityControlMessage, isLite } from '../Signum.Entities'
import { getQueryKey, getQueryNiceName, QueryTokenString } from '../Reflection'
import * as Navigator from '../Navigator'
import { StyleContext, TypeContext } from '../Typecontext'
import ValueSearchControl from './ValueSearchControl'
import { FormGroup } from '../Lines/FormGroup'
import { SearchControlProps } from "./SearchControl";
import { BsColor } from '../Components';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { TitleManager } from '../../Scripts/Lines/EntityBase';

export interface ValueSearchControlLineProps extends React.Props<ValueSearchControlLine> {
  ctx: StyleContext;
  findOptions?: FindOptions;
  valueToken?: string | QueryTokenString<any>;
  labelText?: React.ReactChild;
  labelHtmlAttributes?: React.HTMLAttributes<HTMLLabelElement>;
  formGroupHtmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
  initialValue?: any;
  isLink?: boolean;
  isBadge?: boolean | "MoreThanZero";
  badgeColor?: BsColor;
  isFormControl?: boolean;
  findButton?: boolean;
  onViewEntity?: (entity: Lite<Entity>) => void;
  onExplored?: () => void;
  viewEntityButton?: boolean;
  avoidAutoRefresh?: boolean;
  refreshKey?: string | number;
  extraButtons?: (valueSearchControl: ValueSearchControl) => React.ReactNode;
  searchControlProps?: Partial<SearchControlProps>;
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
        parentToken: new QueryTokenString("").entity(),
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

    let isQuery = this.props.valueToken == undefined || token && token.queryTokenType == "Aggregate";

    let isBadge = coallesce(this.props.isBadge, this.props.valueToken == undefined ? "MoreThanZero" as "MoreThanZero" : false);
    let isFormControl = coallesce(this.props.isFormControl, this.props.valueToken != undefined);

    let unit = isFormControl && token && token.unit && <span className="input-group-text">{token.unit}</span>;


    let value = this.valueSearchControl && this.valueSearchControl.state.value;
    let find = value != undefined && coallesce(this.props.findButton, isQuery) &&
      <a href="#" className={classes("sf-line-button", "sf-find", isFormControl ? "btn input-group-text" : undefined)}
        onClick={this.valueSearchControl!.handleClick}
        title={TitleManager.useTitle ? EntityControlMessage.Find.niceToString() : undefined}>
        <FontAwesomeIcon icon="search" />
      </a>;


    let view = value != undefined && coallesce(this.props.viewEntityButton, isLite(value) && Navigator.isViewable(value.EntityType)) &&
      <a href="#" className={classes("sf-line-button", "sf-view", isFormControl ? "btn input-group-text" : undefined)}
        onClick={this.handleViewEntityClick}
        title={TitleManager.useTitle ? EntityControlMessage.View.niceToString() : undefined}>
        <FontAwesomeIcon icon="arrow-right" />
      </a>

    let extra = this.valueSearchControl && this.props.extraButtons && this.props.extraButtons(this.valueSearchControl);

    return (
      <FormGroup ctx={this.props.ctx}
        labelText={this.props.labelText || (token ? token.niceName : getQueryNiceName(fo.queryName))}
        labelHtmlAttributes={this.props.labelHtmlAttributes}
        htmlAttributes={this.props.formGroupHtmlAttributes}>
        <div className={isFormControl ? ((unit || view || extra || find) ? this.props.ctx.inputGroupClass : undefined) : this.props.ctx.formControlPlainTextClass}>
          <ValueSearchControl
            ref={this.handleValueSearchControlLoaded}
            findOptions={fo}
            initialValue={this.props.initialValue}
            isBadge={isBadge}
            badgeColor={this.props.badgeColor}
            isLink={this.props.isLink}
            formControlClass={isFormControl ? this.props.ctx.formControlClass : undefined}
            valueToken={this.props.valueToken}
            onValueChange={() => this.forceUpdate()}
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
              {extra}
            </div> : <span>
              {unit}
              {view}
              {find}
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

    Navigator.navigate(entity);
  }
}

function coallesce<T>(propValue: T | undefined, defaultValue: T): T {
  if (propValue !== undefined)
    return propValue;

  return defaultValue;
}
