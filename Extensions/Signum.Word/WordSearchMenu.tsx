import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { getToString, Lite } from '@framework/Signum.Entities'
import { Navigator } from '@framework/Navigator'
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded'
import { WordTemplateEntity, WordTemplateMessage } from './Signum.Word'
import { WordClient } from './WordClient'
import { saveFile } from "@framework/Services";
import { DropdownButton, Dropdown } from 'react-bootstrap';

export interface WordSearchMenuProps {
  searchControl: SearchControlLoaded;
}

export default function WordSearchMenu(p : WordSearchMenuProps): React.JSX.Element | null {
  function handleOnClick(wt: Lite<WordTemplateEntity>) {
    Navigator.API.fetch(wt)
      .then(wordTemplate => WordClient.API.getConstructorType(wordTemplate.model!))
      .then(async ct => {
        var s = WordClient.settings[ct];
        if (!s)
          throw new Error("No 'WordModelSettings' defined for '" + ct + "'");

        if (!s.createFromQuery)
          throw new Error("No 'createFromQuery' defined in the WordModelSettings of '" + ct + "'");

        const m = await s.createFromQuery(wt, p.searchControl.getQueryRequest());
        return m && WordClient.API.createAndDownloadReport({ template: wt, entity: m });
      })
      .then(response => response && saveFile(response));
  }

  var wordReports = p.searchControl.props.queryDescription.wordTemplates;

  if (!wordReports || !wordReports.length)
    return null;

  const label = <span><FontAwesomeIcon aria-hidden={true} icon={"file-word"} />&nbsp;{p.searchControl.props.largeToolbarButtons == true ? " " + WordTemplateMessage.WordReport.niceToString() : undefined}</span>;

  return (
    <DropdownButton id="wordTemplateDropDown" className="sf-word-dropdown" title={label}>
      {
        wordReports == "error" ? <Dropdown.Item className="text-danger">Error</Dropdown.Item> : 
        wordReports.map((wt, i) =>
          <Dropdown.Item key={i}
            onClick={() => handleOnClick(wt)}>
            {getToString(wt)}
          </Dropdown.Item>)
      }
    </DropdownButton>
  );
}



