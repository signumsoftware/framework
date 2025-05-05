import * as React from 'react'
import { DateTime } from 'luxon'
import { Navigator } from '@framework/Navigator'
import { JavascriptMessage, is, getToString } from '@framework/Signum.Entities'
import { CaseActivityEntity, CaseActivityMessage } from '../Signum.Workflow'

interface CaseFromSenderInfoProps {
  current: CaseActivityEntity;
}

interface CaseFromSenderInfoState {
  prev?: CaseActivityEntity;
}

export default function CaseFromSenderInfo(p: CaseFromSenderInfoProps): React.JSX.Element {

  const prev = Navigator.useFetchInState(p.current.previous);

  const c = p.current;

  return (
    <div>
      {
        c.previous == null || (prev != null && prev.doneType == null) ? null :
          <div className="alert alert-info case-alert">
            {prev == null ? JavascriptMessage.loading.niceToString() :
              CaseActivityMessage.From0On1.niceToString().formatHtml(
                <strong>{getToString(prev.doneBy)}</strong>,
                <strong>{DateTime.fromISO(prev.doneDate!).toFormat("FFF")} ({DateTime.fromISO(prev.doneDate!).toRelative()})</strong>)
            }
          </div>
      }
      {
        prev?.note && <div className="alert alert-warning case-alert">
          <strong>{CaseActivityEntity.nicePropertyName(a => a.note)}:</strong>
          {prev.note.contains("\n") ? "\n" : null}
          {prev.note}
        </div>
      }
    </div>
  );
}
