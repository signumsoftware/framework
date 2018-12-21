import * as React from 'react'
import * as moment from 'moment'
import { newMListElement } from '@framework/Signum.Entities'
import { InboxFilterModel, InboxMessage, CaseNotificationState, CaseActivityEntity, CaseNotificationEntity } from '../Signum.Entities.Workflow'
import { TypeContext, ValueLine, EnumCheckboxList } from '@framework/Lines'
import { ISimpleFilterBuilder, extractFilterValue, FilterOption } from '@framework/Search'
import { FilterOptionParsed } from "@framework/FindOptions";
import CollapsableCard from "../../Basics/Templates/CollapsableCard";
import { Button } from '@framework/Components';

export default class InboxFilter extends React.Component<{ ctx: TypeContext<InboxFilterModel> }> implements ISimpleFilterBuilder {
  handleOnClearFiltersClick = () => {
    //this.props.ctx.value = CaseNotificationFilterModel.New();
    InboxFilter.resetModel(this.props.ctx.value);
    this.forceUpdate();
  };

  static resetModel(model: InboxFilterModel) {
    model.range = "All";
    model.states = [
      newMListElement("New" as CaseNotificationState),
      newMListElement("Opened" as CaseNotificationState),
      newMListElement("InProgress" as CaseNotificationState)
    ];
    model.fromDate = null;
    model.toDate = null;
  }

  render() {
    var ctx = this.props.ctx;
    var ctx4 = this.props.ctx.subCtx({ labelColumns: 4 });

    return (
      <div style={{ marginBottom: "5px" }}>
        <CollapsableCard
          header={InboxMessage.Filters.niceToString()}
          cardStyle={{ background: "success" }}
          headerStyle={{ text: "light" }}
          bodyStyle={{ background: "light" }}>
          <div className="sf-main-control">
            <div className="row">
              <div className="col-sm-3">
                <EnumCheckboxList ctx={ctx.subCtx(o => o.states)} columnCount={2} formGroupHtmlAttributes={{ style: { marginTop: -15, marginBottom: -15 } }} />
              </div>
              <div className="col-sm-3">
                <ValueLine ctx={ctx4.subCtx(o => o.range)} />
                <ValueLine ctx={ctx4.subCtx(o => o.fromDate)} />
                <ValueLine ctx={ctx4.subCtx(o => o.toDate)} />
              </div>
              <div className="col-sm-1">
                <Button color="warning" className="btn" onClick={this.handleOnClearFiltersClick}>{InboxMessage.Clear.niceToString()}</Button>
              </div>
            </div>
          </div>
        </CollapsableCard>
      </div>
    );
  }

  getFilters(): FilterOption[] {
    var result: FilterOption[] = [];

    var val = this.props.ctx.value;

    if (val.range) {
      var fromDate: string | undefined;
      var toDate: string | undefined;
      var startOfYear: Date;
      var isPersian = moment.locale() == "fa";
      var monthUnit = isPersian ? "jMonth" : "month";
      var yearUnit = isPersian ? "jYear" : "year";
      var now: Date = moment(Date.now()).toDate();

      startOfYear = moment(now).startOf(yearUnit as any).toDate();
      switch (val.range) {
        case "All":
          break;

        case "LastWeek":
          fromDate = moment(now).add(-7, "day").toDate().toISOString();
          break;

        case "LastMonth":
          fromDate = moment(now).add(-30, "day").toDate().toISOString();
          break;

        case "CurrentYear":
          {
            fromDate = startOfYear.toISOString();
            break;
          }
      }

      if (fromDate && fromDate.length > 0)
        result.push({ token: CaseActivityEntity.token(e => e.startDate), operation: "GreaterThanOrEqual", value: fromDate });

      if (toDate && toDate.length > 0)
        result.push({ token: CaseActivityEntity.token(e => e.startDate), operation: "LessThanOrEqual", value: toDate });
    }

    if (val.states)
      result.push({ token: CaseNotificationEntity.token(e => e.state), operation: "IsIn", value: val.states.map(elm => elm.element) });

    if (val.fromDate)
      result.push({ token: CaseActivityEntity.token(e => e.startDate), value: val.fromDate, operation: "GreaterThanOrEqual" });

    if (val.toDate)
      result.push({ token: CaseActivityEntity.token(e => e.startDate), value: val.toDate, operation: "LessThanOrEqual" });

    return result;
  }

  static extract(fos: FilterOptionParsed[]): InboxFilterModel | null {
    var filters = fos.clone();

    var result = InboxFilterModel.New({
      range: extractFilterValue(filters, "Range", "EqualTo"),
      states: (extractFilterValue(filters, "State", "IsIn") as CaseNotificationState[] || []).map(b => newMListElement(b)),
      fromDate: extractFilterValue(filters, "StartDate", "GreaterThanOrEqual"),
      toDate: extractFilterValue(filters, "StartDate", "LessThanOrEqual"),
    });

    if (filters.length)
      return null;

    return result;
  }
}
