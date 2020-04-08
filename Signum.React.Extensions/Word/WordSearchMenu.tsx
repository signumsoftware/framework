import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Lite } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import SearchControlLoaded from '@framework/SearchControl/SearchControlLoaded'
import { WordTemplateEntity, WordTemplateMessage } from './Signum.Entities.Word'
import * as WordClient from './WordClient'
import { saveFile } from "@framework/Services";
import { DropdownButton, Dropdown } from 'react-bootstrap';

export interface WordSearchMenuProps {
  searchControl: SearchControlLoaded;
}

export default function WordSearchMenu(p : WordSearchMenuProps){
  function handleOnClick(wt: Lite<WordTemplateEntity>) {
    Navigator.API.fetchAndForget(wt)
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
      .then(response => response && saveFile(response))
      .done();
  }

  var wordReports = p.searchControl.props.queryDescription.wordTemplates;

  if (!wordReports || !wordReports.length ||
    (p.searchControl.props.showBarExtensionOption && p.searchControl.props.showBarExtensionOption.showWordReport == false))
    return null;

  const label = <span><FontAwesomeIcon icon={["far", "file-word"]} />&nbsp;{p.searchControl.props.largeToolbarButtons == true ? " " + WordTemplateMessage.WordReport.niceToString() : undefined}</span>;

  return (
    <DropdownButton id="wordTemplateDropDown" className="sf-word-dropdown" title={label}>
      {
        wordReports.map((wt, i) =>
          <Dropdown.Item key={i}
            onClick={() => handleOnClick(wt)}>
            {wt.toStr}
          </Dropdown.Item>)
      }
    </DropdownButton>
  );
}



