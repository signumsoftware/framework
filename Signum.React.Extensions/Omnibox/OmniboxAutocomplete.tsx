import * as React from 'react'
import { AbortableRequest } from '@framework/Services';
import * as Navigator from '@framework/Navigator'
import { Typeahead, ErrorBoundary } from '@framework/Components'
import * as OmniboxClient from './OmniboxClient'
import { OmniboxMessage } from './Signum.Entities.Omnibox'
import '@framework/Frames/MenuIcons.css'
import { TypeaheadHandle } from '../../../Framework/Signum.React/Scripts/Components/Typeahead';

export interface OmniboxAutocompleteProps {
  inputAttrs?: React.HTMLAttributes<HTMLInputElement>;
}

export default function OmniboxAutocomplete(p: OmniboxAutocompleteProps) {

  const typeahead = React.useRef<TypeaheadHandle>(null);
  const abortRequest = React.useMemo(() => new AbortableRequest((ac, query: string) => OmniboxClient.API.getResults(query, ac)), []);

  function handleOnSelect(result: OmniboxClient.OmniboxResult, e: React.KeyboardEvent<any> | React.MouseEvent<any>) {
    abortRequest.abort();

    const ke = e as React.KeyboardEvent<any>;
    if (ke.keyCode && ke.keyCode == 9) {
      return OmniboxClient.toString(result);
    }
    e.persist();

    const promise = OmniboxClient.navigateTo(result);
    if (promise) {
      promise
        .then(url => {
          if (url)
            Navigator.pushOrOpenInTab(url, e);
        }).done();
    }
    typeahead.current!.blur();

    return null;
  }

  let inputAttr = { tabIndex: -1, placeholder: OmniboxMessage.Search.niceToString(), ...p.inputAttrs };

  return (
    <ErrorBoundary>
      <Typeahead ref={typeahead } getItems={str => abortRequest.getData(str)}
        renderItem={item => OmniboxClient.renderItem(item as OmniboxClient.OmniboxResult)}
        onSelect={(item, e) => handleOnSelect(item as OmniboxClient.OmniboxResult, e)}
        inputAttrs={inputAttr}
        minLength={0} />
    </ErrorBoundary>
  );
}






