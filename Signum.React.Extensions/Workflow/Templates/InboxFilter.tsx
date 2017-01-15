import * as React from 'react'
import * as moment from 'moment'
import { Button } from "react-bootstrap"
import { Binding, LambdaMemberType } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import { newMListElement } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { InboxFilterModel, InboxFilterModelMessage, CaseNotificationState } from '../Signum.Entities.Workflow'
import { TypeContext, ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater, EnumCheckboxList, FormGroup, FormGroupStyle, FormGroupSize } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl, ValueSearchControl, FilterOperation, OrderType, PaginationMode, ISimpleFilterBuilder, FilterOption, FindOptionsParsed } from '../../../../Framework/Signum.React/Scripts/Search'

export default class InboxFilter extends React.Component<{ ctx: TypeContext<InboxFilterModel> }, void> implements ISimpleFilterBuilder {

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

        return (
            <div className="form-vertical" style={{ marginBottom: 15 }}>
                <div className="row">
                    <div className="col-sm-4">
                        <ValueLine ctx={ctx.subCtx(o => o.range) } />
                    </div>
                    <div className="col-sm-4">
                        <ValueLine ctx={ctx.subCtx(o => o.fromDate) } />
                    </div>
                </div>
                <div className="row">
                    <div className="col-sm-4">
                        <EnumCheckboxList ctx={ctx.subCtx(o => o.states) } />
                    </div>
                    <div className="col-sm-4">
                        <ValueLine ctx={ctx.subCtx(o => o.toDate) } />
                    </div>
                </div>
                <Button bsStyle="warning" style={{ marginTop: 15, marginLeft: 15 }} onClick={ this.handleOnClearFiltersClick } > { InboxFilterModelMessage.Clear.niceToString() } </Button>
            </div>);
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

            startOfYear = moment(now).startOf(yearUnit).toDate();
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
                result.push({ columnName: "StartDate", operation: "GreaterThanOrEqual", value: fromDate });

            if (toDate && toDate.length > 0)
                result.push({ columnName: "StartDate", operation: "LessThanOrEqual", value: toDate });
        }

        if (val.states)
            result.push({ columnName: "State", operation: "IsIn", value: val.states.map(elm => elm.element) });

        if (val.fromDate)
            result.push({ columnName: "StartDate", value: val.fromDate, operation: "GreaterThanOrEqual" });

        if (val.toDate)
            result.push({ columnName: "StartDate", value: val.toDate, operation: "LessThanOrEqual" });

        return result;
    }

    static extract(fo: FindOptionsParsed): InboxFilterModel | null {
        var filters = fo.filterOptions.clone();

        var extract = (columnName: string, operation: FilterOperation) => {
            var f = filters.filter(f => f.token!.fullKey == columnName && f.operation == operation).firstOrNull();
            if (!f)
                return null;

            filters.remove(f);
            return f.value;
        }

        var result: InboxFilterModel;
        
        if (filters.length == 0) {
            result = InboxFilterModel.New();
            InboxFilter.resetModel(result);
            return result;

        } else {
            result = InboxFilterModel.New({
                range: extract("Range", "EqualTo"),
                states: (extract("State", "IsIn") as CaseNotificationState[] || []).map(b => newMListElement(b)),
                fromDate: extract("StartDate", "GreaterThanOrEqual"),
                toDate: extract("StartDate", "LessThanOrEqual"),
            });
        }

        if (filters.length)
            return null;

        return result;
    }
}