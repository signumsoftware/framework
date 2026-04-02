import * as React from "react";
import {
  Alignment,
  Driver,
  driver, 
  DriveStep,
  Side } from "driver.js";
import "driver.js/dist/driver.css";
import { TourEntity, TourMessage, TourTriggerSymbol } from "./Signum.Tour";
import { useAPI } from "@framework/Hooks";
import { TourClient, TourDTO } from "./TourClient";
import { Entity,
  Lite } from "@framework/Signum.Entities";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faCompass } from "@fortawesome/free-solid-svg-icons";
import { getTypeName,
  PseudoType } from "@framework/Reflection";
import { LinkButton } from "@framework/Basics/LinkButton";
import { classes } from "@framework/Globals";
import { JSX } from "react/jsx-runtime";
import { micromark } from "micromark";

export function TourButton(p: { trigger: PseudoType | TourTriggerSymbol }): JSX.Element | null {
  const storageKey = TourTriggerSymbol.isInstance(p.trigger)
    ? `tour-viewed-${p.trigger.key}` 
    : `tour-viewed-${getTypeName(p.trigger)}`;

  const [hasViewed, setHasViewed] = React.useState(() => {
    return localStorage.getItem(storageKey) === "true";
  });

  const [startTour, setStartTour] = React.useState(false);

  const tour = useAPI(() => {
    if (TourTriggerSymbol.isInstance(p.trigger)) {
      return TourClient.API.getTourBySymbol(p.trigger.key);
    } else {
      return TourClient.API.getTourByEntity(getTypeName(p.trigger));
    }
  }, [p.trigger]);

  const driverRef = React.useRef<Driver | null>(null);

  const handleClick = () => {
    if (!hasViewed) {
      localStorage.setItem(storageKey, "true");
      setHasViewed(true);
    }

    // Toggle startTour to force recreation and auto-start
    setStartTour(prev => !prev);
  };

  if (!tour) {
    return null;
  }

  return (
    <>
      <LinkButton
        className={'sf-pointer nav-link'}
        onClick={handleClick}
        title={hasViewed ? TourMessage.ReplayTour.niceToString() : TourMessage.StartTour.niceToString()}
      >
        <FontAwesomeIcon icon={faCompass} className={classes(!hasViewed && 'text-warning fa-beat')} />
      </LinkButton>
      {startTour && <TourComponent key={startTour.toString()} tour={tour} autoStart={true} ref={driverRef} />}
    </>
  );
}

function waitForElement(selector: string, timeout: number = 5000): Promise<Element> {
  return new Promise((resolve, reject) => {
    if (document.querySelector(selector)) {
      return resolve(document.querySelector(selector)!);
    }
    
    const observer = new MutationObserver(() => {
      if (document.querySelector(selector)) {
        observer.disconnect();
        resolve(document.querySelector(selector)!);
      }
    });
    
    observer.observe(document.body, {
      childList: true,
      subtree: true
    });
    
    setTimeout(() => {
      observer.disconnect();
      reject(new Error('Element not found: ' + selector));
    }, timeout);
  });
}

export function TourComponent({ tour, autoStart = true, ref }: {
  tour: TourDTO;
  autoStart?: boolean;
  ref?: React.Ref<Driver | null>;
}) {
  const driverRef = React.useRef<Driver | null>(null);

  React.useImperativeHandle(ref, () => driverRef.current!);

  React.useEffect(() => {
    if (!tour) return;

    // Check if it's a TourDTO or TourEntity

    const steps = tour.steps.map<DriveStep>((step, i, steps) => ({
      element: step.cssSelector || undefined,
      popover: step.cssSelector ? {
        title: step.title ?? undefined,
        description: step.description ? micromark(step.description) : undefined,
        side: step.side as Side,
        align: step.align as Alignment,
        onPopoverRender: async (popover, opts) => {
          if (step.click === "OnLoad") {
            if (step.cssSelector) {
              var elem = await waitForElement(step.cssSelector);
              (elem as HTMLButtonElement).click();
            }
          }
        },
        onNextClick: async e => {
          if (step.click === "OnNext") {
            (e as HTMLButtonElement).click();
            var nextStep = steps[i + 1];
            if (nextStep?.cssSelector)
              await waitForElement(nextStep?.cssSelector);
          }

          driverObj.moveNext();
        },
      } : {
        title: step.title ?? undefined,
        description: step.description ? micromark(step.description) : undefined,
      },
    }));

    const driverObj = driver({
      steps,
      showProgress: tour.showProgress,
      animate: tour.animate,
      showButtons: [
        "next",
        "previous",
        tour.showCloseButton ? "close" : null
      ].filter(Boolean) as any,

      nextBtnText: TourMessage.Next.niceToString(),
      prevBtnText: TourMessage.Previous.niceToString(),
      doneBtnText: TourMessage.Done.niceToString(),

      overlayColor: "black",
      overlayOpacity: 0.3,
      stagePadding: 10,
      stageRadius: 5,
      popoverOffset: 10,
      allowClose: true,
    });

    driverRef.current = driverObj;

    if (autoStart) {
      driverObj.drive();
    }

    return () => {
      driverObj.destroy();
      driverRef.current = null;
    };
  }, [tour, autoStart]);

  return null;
}

export default TourComponent;
