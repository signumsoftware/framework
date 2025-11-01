import * as React from 'react'
import { AbortableRequest } from '@framework/Services';
import * as AppContext from '@framework/AppContext'
import { Navigator } from '@framework/Navigator'
import { Typeahead, ErrorBoundary } from '@framework/Components'
import { OmniboxClient, OmniboxResult } from './OmniboxClient'
import { OmniboxMessage } from './Signum.Omnibox'
import '@framework/Frames/MenuIcons.css'
import { TypeaheadController } from '@framework/Components/Typeahead';

export interface OmniboxAutocompleteProps {
  inputAttrs?: React.HTMLAttributes<HTMLInputElement>;
}

export default function OmniboxAutocomplete(p: OmniboxAutocompleteProps): React.JSX.Element {

  const typeahead = React.useRef<TypeaheadController>(null);
  const abortRequest = React.useMemo(() => new AbortableRequest((ac, query: string) => OmniboxClient.API.getResults(query, ac)), []);

  function handleOnSelect(result: OmniboxResult, e: React.KeyboardEvent<any> | React.MouseEvent<any>) {
    abortRequest.abort();

    const ke = e as React.KeyboardEvent<any>;
    if (ke.key == "Tab") {
      if (result.resultTypeName == "HelpOmniboxResult")
        return "";

      return OmniboxClient.toString(result);
    }

    const promise = OmniboxClient.navigateTo(result);
    if (promise) {
      promise
        .then(url => {
          if (url)
            AppContext.pushOrOpenInTab(url, e);
        });
    }
    typeahead.current!.blur();

    return null;
  }

  let inputAttr = { placeholder: OmniboxMessage.Search.niceToString(), ...p.inputAttrs };

  return (
    <ErrorBoundary>
      <Typeahead ref={typeahead } getItems={str => abortRequest.getData(str)}
        renderItem={item => OmniboxClient.renderItem(item as OmniboxResult)}
        isHeader={item => (item as OmniboxResult).resultTypeName == "HelpOmniboxResult" && (item as OmniboxClient.HelpOmniboxResult).referencedTypeName == null}
        isDisabled={item => (item as OmniboxResult).resultTypeName == "HelpOmniboxResult"}
        onSelect={(item, e) => handleOnSelect(item as OmniboxResult, e)}
        inputAttrs={inputAttr}
        minLength={0}
        noResultsMessage={OmniboxMessage.NotFound.niceToString()}
        />
    </ErrorBoundary>
  );
}






