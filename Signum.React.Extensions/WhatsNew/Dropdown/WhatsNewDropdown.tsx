import * as React from 'react'
import * as Operations from '@framework/Operations'
import * as Finder from '@framework/Finder'
import { is, JavascriptMessage, toLite } from '@framework/Signum.Entities'
import { Toast, Button, ButtonGroup } from 'react-bootstrap'
import { DateTime } from 'luxon'
import { useAPI, useAPIWithReload, useForceUpdate, useUpdatedRef } from '@framework/Hooks';
import * as Navigator from '@framework/Navigator'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as WhatsNewClient from '../WhatsNewClient'
import "./WhatsNewDropdown.css"
import { Link } from 'react-router-dom';
import { classes, Dic } from '@framework/Globals'
import MessageModal from '@framework/Modals/MessageModal'
import { WhatsNewEntity, WhatsNewMessage, WhatsNewOperation } from '../Signum.Entities.WhatsNew'
import * as AppContext from "@framework/AppContext"
import { API, NumWhatsNews, WhatsNewFull, WhatsNewShort } from '../WhatsNewClient'

export default function WhatsNewDropdown(props: { keepRingingFor?: number }) {

  if (!Navigator.isViewable(WhatsNewEntity))
    return null;

  return <WhatsNewDropdownImp keepRingingFor={props.keepRingingFor ?? 10 * 1000} />;
}

function WhatsNewDropdownImp(props: { keepRingingFor: number }) {

  const forceUpdate = useForceUpdate();
  const [isOpen, setIsOpen] = React.useState<boolean>(false);
  
  const [showNews, setShowNews] = React.useState<number>(5);
  
  const isOpenRef = useUpdatedRef(isOpen);

  var [countResult, reloadCount] = useAPIWithReload<WhatsNewClient.NumWhatsNews>(() => WhatsNewClient.API.myNewsCount().then(res => {
    if (isOpenRef.current) {
      WhatsNewClient.API.myNews()
        .then(als => {
          setNews(als);
        })
        .done();
    }

    return res;
  }), [], { avoidReset: true });

  const [whatsNew, setNews] = React.useState<WhatsNewShort[] | undefined>(undefined);

  function handleOnToggle() {

    if (!isOpen) {
      WhatsNewClient.API.myNews()
        .then(wn => setNews(wn))
        .done();
    }

    setIsOpen(!isOpen);
  }

  function handleClickAll() {
    setIsOpen(false);
    AppContext.history.push("~/news/");
  }

  function handleOnCloseNews(toRemove: WhatsNewShort[]) {

    //Optimistic
    let wasClosed = false;
    if (whatsNew) {
      whatsNew.extract(a => toRemove.some(r => is(r.whatsNew, a.whatsNew)));
      if (whatsNew.length == 0) {
        setIsOpen(false);
        wasClosed = true;
      }
    }
    if (countResult)
      countResult.numWhatsNews -= 1;
    forceUpdate();

    API.setNewsLogRead(toRemove.map(r => r.whatsNew.id)).then(res => {
      if (!res) {
        MessageModal.showError(<div>The news couldn't be removed</div>).done();
      }
      // Pesimistic
      WhatsNewClient.API.myNews()
        .then(wn => {
          if (wasClosed && wn.length > 0)
            setIsOpen(true);

          setNews(wn);
        })
        .done();

      reloadCount();
    }).done(), [toRemove];
    }

  var newsGroups = whatsNew == null ? null : whatsNew.orderByDescending(w => w.creationDate);

  return (
    <>
      <div className="nav-link sf-bell-container" onClick={handleOnToggle}>
        <FontAwesomeIcon icon="bullhorn" className={classes("sf-newspaper", isOpen && "open", countResult && countResult.numWhatsNews > 0 && "active")} />
        {countResult && countResult.numWhatsNews > 0 && <span className="badge btn-danger badge-pill sf-news-badge">{countResult.numWhatsNews}</span>}
      </div>
      {isOpen && <div className="sf-news-toasts">
        {newsGroups == null ? <Toast> <Toast.Body>{JavascriptMessage.loading.niceToString()}</Toast.Body></Toast> :

          <>
            {newsGroups.length == 0 && <Toast><Toast.Body>{WhatsNewMessage.YouDoNotHaveAnyUnreadNews.niceToString()}</Toast.Body></Toast>}

            {
              newsGroups.filter((gr, i) => i < showNews)
                .map(a => <WhatsNewToast whatsnew={a} key={a.whatsNew.id} onClose={handleOnCloseNews} refresh={reloadCount} />)
            }
            {
              newsGroups.length > showNews &&
              <Toast onClose={() => handleOnCloseNews(whatsNew!.map(a => a))}>
                <Toast.Header>
                    <small>{WhatsNewMessage.CloseAll.niceToString()}</small>
                </Toast.Header>
              </Toast>
            }
            <Toast>
              <Toast.Body style={{ textAlign: "center" }}>
                <a style={{ cursor: "pointer", color: "#114177" }}  onClick={() => handleClickAll()}>{WhatsNewMessage.AllMyNews.niceToString()}</a>
              </Toast.Body>
            </Toast>
          </>
        }
      </div>}
    </>
  );
}

export function WhatsNewToast(p: { whatsnew: WhatsNewShort, onClose: (e: WhatsNewShort[]) => void, refresh: () => void, className?: string; })
{
  function handleClickSpecific(id: string | number) {
    AppContext.history.push("~/newspage/" + id);
  }

  return (
    <Toast onClose={() => p.onClose([p.whatsnew])} className={p.className}>
      <Toast.Header>
        <strong className="me-auto">{p.whatsnew.title}</strong>
        <small>{DateTime.fromISO(p.whatsnew.creationDate!).toRelative()}</small>
      </Toast.Header>
      <Toast.Body style={{ whiteSpace: "pre-wrap" }}>
        {p.whatsnew.description.substring(0, 100)}...
        <br />
        <a href="" onClick={() => handleClickSpecific(p.whatsnew.whatsNew.id!)}>{WhatsNewMessage.ReadFurther.niceToString()}</a>
      </Toast.Body>
    </Toast>
  );
}

WhatsNewToast.icons = {} as { [alertTypeKey: string]: React.ReactNode };
