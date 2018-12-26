import * as React from 'react'
import * as moment from 'moment'
import { classes, Dic } from '@framework/Globals'
import { ValueLine, EntityLine, EntityCombo } from '@framework/Lines'
import { FilterOptionParsed } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import * as Finder from '@framework/Finder'
import { Binding, IsByAll, getTypeInfos, TypeReference } from '@framework/Reflection'
import { QueryTokenEmbedded, UserAssetMessage } from '../Signum.Entities.UserAssets'
import { QueryFilterEmbedded, PinnedQueryFilterEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryDescription, SubTokensOptions, isFilterGroupOptionParsed, FilterConditionOptionParsed, isList, FilterType, FilterGroupOptionParsed, PinnedFilter } from '@framework/FindOptions'
import { Lite, Entity, parseLite, liteKey } from "@framework/Signum.Entities";
import * as Navigator from "@framework/Navigator";
import FilterBuilder, { MultiValue, FilterConditionComponent, FilterGroupComponent } from '@framework/SearchControl/FilterBuilder';
import { MList, newMListElement } from '@framework/Signum.Entities';
import { TokenCompleter } from '@framework/Finder';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

interface FilterBuilderEmbeddedProps {
  ctx: TypeContext<MList<QueryFilterEmbedded>>;
  queryKey: string;
  subTokenOptions: SubTokensOptions;
  onChanged?: () => void;
  showUserFilters: boolean

}

interface FilterBuilderEmbeddedState {
  filterOptions?: FilterOptionParsed[];
  queryDescription?: QueryDescription;
}

export default class FilterBuilderEmbedded extends React.Component<FilterBuilderEmbeddedProps, FilterBuilderEmbeddedState> {

  constructor(props: FilterBuilderEmbeddedProps) {
    super(props);
    this.state = {
      filterOptions: undefined,
      queryDescription: undefined
    };
  }

  componentWillMount() {
    this.loadData(this.props).done()
  }

  componentWillReceiveProps(newProps: FilterBuilderEmbeddedProps) {
    if (newProps.ctx.value != this.props.ctx.value || newProps.queryKey != this.props.queryKey)
      this.setState({ queryDescription: undefined, filterOptions: undefined }, () => {
        this.loadData(newProps).done();
      });
  }

  async loadData(props: FilterBuilderEmbeddedProps): Promise<void> {

    var qd = await Finder.getQueryDescription(props.queryKey);

    var filterOptions = await FilterBuilderEmbedded.toFilterOptionParsed(qd, props.ctx.value, props.subTokenOptions);

    this.setState({ queryDescription: qd, filterOptions: filterOptions });
  }

  static async toFilterOptionParsed(qd: QueryDescription, allFilters: MList<QueryFilterEmbedded>, subTokenOptions: SubTokensOptions): Promise<FilterOptionParsed[]> {
    const completer = new TokenCompleter(qd);

    allFilters.forEach(mle => {
      if (mle.element.token && mle.element.token.tokenString)
        completer.request(mle.element.token.tokenString, subTokenOptions);
    });

    await completer.finished();

    function toFilterList(filters: QueryFilterEmbedded[], indent: number): FilterOptionParsed[] {
      return filters.groupWhen(f => f.indentation == indent).map(gr => {
        if (!gr.key.isGroup) {
          if (gr.elements.length != 0)
            throw new Error("Unexpected childrens of condition");

          const pinned = gr.key.pinned;

          return {
            token: completer.get(gr.key.token!.tokenString),
            operation: gr.key.operation,
            value: gr.key.valueString,
            frozen: false,
            pinned: !pinned ? undefined : toPinnedFilter(pinned),
          } as FilterConditionOptionParsed;
        }
        else {

          const pinned = gr.key.pinned;

          return {
            token: gr.key.token ? completer.get(gr.key.token.tokenString) : null,
            groupOperation: gr.key.groupOperation!,
            filters: toFilterList(gr.elements, indent + 1),
            value: gr.key.valueString,
            frozen: false,
            pinned: !pinned ? undefined : toPinnedFilter(pinned),
          } as FilterGroupOptionParsed;
        }
      });

      function toPinnedFilter(pinned: PinnedQueryFilterEmbedded): PinnedFilter {
        return {
          label: pinned.label || undefined,
          column: pinned.column || undefined,
          row: pinned.row || undefined,
          disableOnNull: pinned.disableOnNull || undefined,
          splitText: pinned.splitText || undefined,
        };
      }
    }

    return toFilterList(allFilters.map(a => a.element), 0);
  }

  render() {
    return (
      <div>
        {
          this.state.queryDescription != null &&
          <FilterBuilder
            title={this.props.ctx.niceName()}
            queryDescription={this.state.queryDescription}
            filterOptions={this.state.filterOptions || []}
            subTokensOptions={this.props.subTokenOptions}
            readOnly={this.props.ctx.readOnly}
            onFiltersChanged={this.handleFiltersChanged}
            renderValue={this.handleRenderValue}
            showPinnedFilters={this.props.showUserFilters} />
        }
      </div>
    );
  }

  handleFiltersChanged = () => {

    var ctx = this.props.ctx;

    ctx.value.clear();


    function pushFilter(fo: FilterOptionParsed, indent: number) {
      if (isFilterGroupOptionParsed(fo)) {
        ctx.value.push(newMListElement(QueryFilterEmbedded.New({
          isGroup: true,
          indentation: indent,
          groupOperation: fo.groupOperation,
          token: fo.token && QueryTokenEmbedded.New({ token: fo.token, tokenString: fo.token.fullKey }),
          valueString: fo.value,
          pinned: !fo.pinned ? undefined : toPinnedQueryFilterEmbedded(fo.pinned)
        })));

        fo.filters.forEach(f => pushFilter(f, indent + 1));
      } else {

        ctx.value.push(newMListElement(QueryFilterEmbedded.New({
          token: fo.token && QueryTokenEmbedded.New({ token: fo.token, tokenString: fo.token.fullKey }),
          operation: fo.operation,
          valueString: fo.value,
          indentation: indent,
          pinned: !fo.pinned ? undefined : toPinnedQueryFilterEmbedded(fo.pinned)
        })));
      }

      function toPinnedQueryFilterEmbedded(pinned: PinnedFilter): PinnedQueryFilterEmbedded {
            return PinnedQueryFilterEmbedded.New({
                label: pinned.label,
                column: pinned.column,
                row: pinned.row,
                disableOnNull: pinned.disableOnNull,
                splitText: pinned.splitText,
            });
        }
    }

    this.state.filterOptions!.forEach(fo => pushFilter(fo, 0))

    ctx.binding.setValue(ctx.value); //force change

    if (this.props.onChanged)
      this.props.onChanged();

    this.forceUpdate();
  }

  handleRenderValue = (fc: FilterConditionComponent | FilterGroupComponent) => {

    if (fc instanceof FilterGroupComponent) {

      const f = fc.props.filterGroup;

      const readOnly = fc.props.readOnly || f.frozen;

      const ctx = new TypeContext<any>(undefined, { formGroupStyle: "None", readOnly: readOnly, formSize: "ExtraSmall" }, undefined as any, Binding.create(f, a => a.value));

      return <ValueLineOrExpression ctx={ctx} onChange={fc.handleValueChange} filterType={"String"} type={{ name: "string" }} />

    } else {

      const f = fc.props.filter;

      const readOnly = fc.props.readOnly || f.frozen;

      const ctx = new TypeContext<any>(undefined, { formGroupStyle: "None", readOnly: readOnly, formSize: "ExtraSmall" }, undefined as any, Binding.create(f, a => a.value));

      if (isList(f.operation!))
        return <MultiLineOrExpression ctx={ctx} onRenderItem={ctx => this.handleCreateAppropiateControl(ctx, fc)} onChange={fc.handleValueChange} />;

      return this.handleCreateAppropiateControl(ctx, fc);
    }
  }

  handleCreateAppropiateControl = (ctx: TypeContext<any>, fc: FilterConditionComponent): React.ReactElement<any> => {

    const token = fc.props.filter.token!;

    switch (token.filterType) {
      case "Lite":
      case "Embedded":
        return <EntityLineOrExpression ctx={ctx} onChange={fc.handleValueChange} filterType={token.filterType} type={token.type} />;
      default:
        return <ValueLineOrExpression ctx={ctx} onChange={fc.handleValueChange} filterType={token.filterType} type={token.type} />

    }
  }
}


interface MultiLineOrExpressionProps {
  ctx: TypeContext<string | null | undefined>;
  onChange: () => void;
  onRenderItem: (ctx: TypeContext<any>) => React.ReactElement<any>
}

export class MultiLineOrExpression extends React.Component<MultiLineOrExpressionProps, { values: string[] }> {

  constructor(props: MultiLineOrExpressionProps) {
    super(props);
    this.state = {
      values: (props.ctx.value || "").split("|")
    }
  }

  componentWillReceiveProps(newProps: MultiLineOrExpressionProps) {
    if (this.props.ctx.value != newProps.ctx.value)
      this.setState({ values: (newProps.ctx.value || "").split("|") });
  }


  render() {

    const handleChangeValue = () => {
      this.props.ctx.value = this.state.values.join("|");
      if (this.props.onChange)
        this.props.onChange();
    }

    return <MultiValue values={this.state.values} onChange={handleChangeValue} readOnly={this.props.ctx.readOnly} onRenderItem={this.props.onRenderItem} />;
  }
}

interface EntityLineOrExpressionProps {
  ctx: TypeContext<string | null | undefined>;
  onChange: () => void;
  type: TypeReference;
  filterType: FilterType;
}

export class EntityLineOrExpression extends React.Component<EntityLineOrExpressionProps, { lite: Lite<Entity> | null | undefined }> {

  constructor(props: EntityLineOrExpressionProps) {
    super(props);
    this.state = {
      lite: undefined
    }
  }

  componentWillMount() {
    this.load(this.props);
  }

  componentWillReceiveProps(newProps: EntityLineOrExpressionProps) {
    if (this.props.ctx.value != newProps.ctx.value)
      this.load(newProps);
  }

  load(props: EntityLineOrExpressionProps) {

    var lite = props.ctx.value == null ? null :
      props.ctx.value.contains(";") ? parseLite(props.ctx.value) :
        undefined;

    this.setState({ lite: lite });

    if (lite != null) {
      Navigator.API.fillToStrings(lite)
        .then(() => this.forceUpdate())
        .done();
    }
  }

  render() {

    if (this.state.lite === undefined)
      return <ValueLine ctx={this.props.ctx} type={{ name: "string" }} onChange={this.props.onChange} extraButtons={() => this.getSwitchModelButton(false)} />;

    const ctx = new TypeContext<any>(undefined, { formGroupStyle: "None", readOnly: this.props.ctx.readOnly, formSize: "ExtraSmall" }, undefined as any, Binding.create(this.state, a => a.lite));

    const handleChangeValue = () => {
      this.props.ctx.value = ctx.value ? liteKey(ctx.value) : null;
      if (this.props.onChange)
        this.props.onChange();

    }

    const type = this.props.type;

    if (this.props.filterType == "Lite") {
      if (type.name == IsByAll || getTypeInfos(type).some(ti => !ti.isLowPopulation))
        return <EntityLine ctx={ctx} type={type} create={false} onChange={handleChangeValue} extraButtons={() => this.getSwitchModelButton(true)} />;
      else
        return <EntityCombo ctx={ctx} type={type} create={false} onChange={handleChangeValue} extraButtons={() => this.getSwitchModelButton(true)} />;
    }
    else if (this.props.filterType == "Embedded") {
      return <EntityLine ctx={ctx} type={type} create={false} autocomplete={null} onChange={handleChangeValue} extraButtons={() => this.getSwitchModelButton(true)} />;
    }
    else
      throw new Error("Unexpected Filter Type");
  }

  getSwitchModelButton(isValue: boolean): React.ReactElement<any> {
    return (<a href="#" className={classes("sf-line-button", "sf-remove", "btn input-group-text")}
      onClick={e => { e.preventDefault(); this.setState({ lite: isValue ? undefined : null }); }}
      title={isValue ? UserAssetMessage.SwitchToExpression.niceToString() : UserAssetMessage.SwitchToValue.niceToString()}>
      <FontAwesomeIcon icon={[isValue ? "far" : "fas", "edit"]} />
    </a>)
  }
}


interface ValueLineOrExpressionProps {
  ctx: TypeContext<string | null | undefined>;
  onChange: () => void;
  type: TypeReference;
  formatText?: string;
  unitText?: string;
  filterType?: FilterType;
}

export class ValueLineOrExpression extends React.Component<ValueLineOrExpressionProps, { value: string | number | boolean | null | undefined }> {

  constructor(props: ValueLineOrExpressionProps) {
    super(props);
    this.state = {
      value: undefined
    };
  }

  componentWillMount() {
    this.load(this.props);
  }

  componentWillReceiveProps(newProps: ValueLineOrExpressionProps) {
    if (this.props.ctx.value != newProps.ctx.value)
      this.load(newProps);
  }

  load(props: ValueLineOrExpressionProps) {

    var value = parseValue(props.ctx.value, props.filterType);

    this.setState({ value: value });
  }

  render() {

    if (this.state.value === undefined)
      return <ValueLine ctx={this.props.ctx} type={{ name: "string" }} onChange={this.props.onChange} extraButtons={() => this.getSwitchModelButton(false)} />;

    const ctx = new TypeContext<any>(undefined, { formGroupStyle: "None", readOnly: this.props.ctx.readOnly, formSize: "ExtraSmall" }, undefined as any, Binding.create(this.state, a => a.value));

    const handleChangeValue = () => {
      this.props.ctx.value = toStringValue(ctx.value, this.props.filterType);
      if (this.props.onChange)
        this.props.onChange();
    }

    const type = this.props.type;

    if (this.props.filterType == "Enum") {
      const ti = getTypeInfos(type).single();
      if (!ti)
        throw new Error(`EnumType ${type.name} not found`);
      const members = Dic.getValues(ti.members).filter(a => !a.isIgnoredEnum);
      return <ValueLine ctx={ctx} type={type} formatText={this.props.formatText} unitText={this.props.unitText} onChange={handleChangeValue} extraButtons={() => this.getSwitchModelButton(true)} comboBoxItems={members} />;
    } else {
      return <ValueLine ctx={ctx} type={type} formatText={this.props.formatText} unitText={this.props.unitText} onChange={handleChangeValue} extraButtons={() => this.getSwitchModelButton(true)} />;
    }
  }

  getSwitchModelButton(isValue: boolean): React.ReactElement<any> {
    return (
      <a href="#" className={classes("sf-line-button", "sf-remove", "btn input-group-text")}
        onClick={e => {
          e.preventDefault();
          if (this.props.filterType == "DateTime")
            this.props.ctx.value = "yyyy/mm/dd hh:mm:ss";

          if (this.props.filterType == "Lite")
            this.props.ctx.value = "[CurrentEntity]";

          this.setState({ value: isValue ? undefined : null });
        }}
        title={isValue ? UserAssetMessage.SwitchToExpression.niceToString() : UserAssetMessage.SwitchToValue.niceToString()}>
        <FontAwesomeIcon icon={[isValue ? "far" : "fas", "edit"]} />
      </a>
    );
  }
}

const serverFormat = "YYYY/MM/DD hh:mm:ss";

function parseValue(str: string | null | undefined, filterType: FilterType | undefined): string | number | boolean | null | undefined {
  return str == null ? null :
    filterType == "Integer" ? parseInt(str) :
      filterType == "Decimal" ? parseFloat(str) :
        filterType == "Boolean" ? (str == "True" ? true : str == "False" ? false : undefined) :
          filterType == "DateTime" ? parseDate(str) :
            filterType == "Enum" || filterType == "Guid" || filterType == "String" ? str :
              undefined;
}

function parseDate(str: string) {
  const parsed = moment(str, serverFormat, true).format();

  return parsed == "Invalid date" ? undefined : parsed;
}

function toStringValue(value: string | number | boolean | null | undefined, filterType: FilterType | undefined): string | null {
  return value == null ? null :
    filterType == "Integer" ? value.toString() :
      filterType == "Decimal" ? value.toString() :
        filterType == "Boolean" ? (value ? "True" : "False") :
          filterType == "DateTime" ? moment(value as string).format(serverFormat) :
            filterType == "Enum" || filterType == "Guid" || filterType == "String" ? value as string :
              null;

}
