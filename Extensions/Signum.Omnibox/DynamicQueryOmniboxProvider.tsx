import * as React from 'react'
import { OmniboxMessage } from './Signum.Omnibox'
import { OmniboxClient, OmniboxResult, OmniboxMatch } from './OmniboxClient'
import { OmniboxProvider } from "./OmniboxProvider"
import { QueryToken, FilterOperation, FindOptions } from '@framework/FindOptions'
import { Finder } from '@framework/Finder'

const UNKNOWN = "??UNKNOWN??";

export default class DynamicQueryOmniboxProvider extends OmniboxProvider<DynamicQueryOmniboxResult>
{
  getProviderName() {
    return "DynamicQueryOmniboxResult";
  }

  icon(): React.ReactElement {
    return this.coloredIcon("search", "orange");
  }

  renderItem(result: DynamicQueryOmniboxResult): React.ReactElement[] {

    const array: React.ReactElement[] = [];

    array.push(this.icon());

    this.renderMatch(result.queryNameMatch, array);

    result.filters.forEach(f => {
      array.push(<span> </span>);

      if (f.queryTokenMatches)
        f.queryTokenMatches.map((m, i) => {
          if (i != 0)
            array.push(<span>.</span>);
          this.renderMatch(m, array);
        });


      var shown = f.queryTokenMatches?.map(a => a.text).join(".");

      if (shown == null || (shown != f.queryTokenOmniboxPascal && shown != f.queryTokenOmniboxPascal.tryAfterLast("."))) {
        if (f.queryTokenMatches && f.queryTokenMatches.length > 0)
          array.push(<span>.</span>);

        array.push(this.coloredSpan(f.queryTokenOmniboxPascal.tryAfterLast(".") ?? f.queryTokenOmniboxPascal, "gray"));
      }

      if (f.canFilter && f.canFilter.length)
        array.push(this.coloredSpan(f.canFilter, "red"));
      else if (f.operation != undefined) {

        array.push(<strong>{f.operationToString}</strong>);

        if (f.value == UNKNOWN)
          array.push(this.coloredSpan(OmniboxMessage.Unknown.niceToString(), "red"));
        else if (f.valueMatch != undefined)
          this.renderMatch(f.valueMatch, array);
        else if (f.syntax != undefined && f.syntax.completion == FilterSyntaxCompletion.Complete)
          array.push(<b>{f.valueToString}</b>);
        else
          array.push(this.coloredSpan(f.valueToString, "gray"));
      }
    });

    return array;
  }

  navigateTo(result: DynamicQueryOmniboxResult): Promise<string> {

    const fo: FindOptions = {
      queryName: result.queryName,
      filterOptions: []
    };

    result.filters.forEach(f => {
      fo.filterOptions!.push({
        token: f.queryToken.fullKey,
        operation: f.operation,
        value: f.value,
      });
    });

    return Promise.resolve(Finder.findOptionsPath(fo));
  }

  toString(result: DynamicQueryOmniboxResult): string {
    const queryName = result.queryNameMatch.text;

    const filters = result.filters.map(f => {

      const token = f.queryTokenOmniboxPascal;

      if (f.syntax == undefined || f.syntax.completion == FilterSyntaxCompletion.Token || f.canFilter && f.canFilter.length > 1)
        return token;

      const oper = f.operationToString;

      if (f.syntax.completion == FilterSyntaxCompletion.Operation && f.value == undefined ||
        (f.value == UNKNOWN))
        return token + oper;

      return token + oper + f.valueToString;
    }).join(" ");

    return filters.length ? queryName + " " + filters : queryName;
  }
}

interface DynamicQueryOmniboxResult extends OmniboxResult {
  queryName: string;
  queryNameMatch: OmniboxMatch;
  filters: OmniboxFilterResult[];
}

interface OmniboxFilterResult {

  distance: number;
  syntax: FilterSyntax;
  queryToken: QueryToken;
  queryTokenOmniboxPascal: string;
  queryTokenMatches: OmniboxMatch[]
  operation: FilterOperation;
  operationToString: string;
  value: any
  valueMatch: OmniboxMatch;
  valueToString: string;
  canFilter: string;
}

interface FilterSyntax {
  index: number;
  tokenLength: number;
  length: number;
  completion: FilterSyntaxCompletion;
}

enum FilterSyntaxCompletion {
  Token = "Token" as any,
  Operation = "Operation" as any,
  Complete = "Complete" as any,
}
