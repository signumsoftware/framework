import * as React from "react";
import { Driver,
driver } from "driver.js";
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



export function TourButton(p: { trigger: PseudoType | TourTriggerSymbol }) {
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
        title={hasViewed ? "Replay tour" : "Start tour (new!)"}
      >
        <FontAwesomeIcon icon={faCompass} className={classes(!hasViewed && 'text-warning fa-beat')} />
      </LinkButton>
      {startTour && <TourComponent key={startTour.toString()} tour={tour} autoStart={true} ref={driverRef} />}
    </>
  );
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

    const steps = tour.steps.map(step => ({
      element: step.cssSelector || undefined,
      popover: step.cssSelector ? {
        title: step.title ?? undefined,
        description: step.description ?? undefined,
        side: step.side as any,
        align: step.align as any,
      } : {
        title: step.title ?? undefined,
        description: step.description ?? undefined,
      }
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
      overlayOpacity: 0.75,
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
