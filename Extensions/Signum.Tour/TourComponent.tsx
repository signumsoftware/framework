import * as React from "react";
import { driver } from "driver.js";
import "driver.js/dist/driver.css";
import { TourEntity, TourMessage } from "./Signum.Tour";
import { useAPI } from "@framework/Hooks";
import { Finder } from "@framework/Finder";

export interface TourHelpers {
  start: () => void;
  destroy: () => void;
  next: () => void;
  previous: () => void;
  moveTo: (index: number) => void;
}

interface TourComponentProps {
  tour: TourEntity | string;
  autoStart?: boolean;
}

export const TourComponent = React.forwardRef<TourHelpers, TourComponentProps>(
  function TourComponent({ tour, autoStart = true }, ref) {
    const driverRef = React.useRef<any>(null);

    const tourEntity = useAPI(() => {
      if (typeof tour === "string") {
        return Finder.fetchEntities({
          queryName: TourEntity,
          filterOptions: [{ token: TourEntity.token(e => e.name), value: tour }],
          count: 1,
        }).then(r => r[0] as TourEntity | undefined);
      }
      return Promise.resolve(tour);
    }, [tour]);

    React.useImperativeHandle(ref, () => ({
      start: () => driverRef.current?.drive(),
      destroy: () => driverRef.current?.destroy(),
      next: () => driverRef.current?.moveNext(),
      previous: () => driverRef.current?.movePrevious(),
      moveTo: (index: number) => driverRef.current?.moveTo(index),
    }), []);

    React.useEffect(() => {
      if (!tourEntity) return;

      const steps = tourEntity.steps.map(step => ({
        element: step.element.element || undefined,
        popover: step.element.element ? {
          title: step.element.title ?? undefined,
          description: step.element.description ?? undefined,
          side: step.element.side?.toLowerCase() as any,
          align: step.element.align?.toLowerCase() as any,
        } : {
          title: step.element.title ?? undefined,
          description: step.element.description ?? undefined,
        }
      }));

      const driverObj = driver({
        steps,
        showProgress: tourEntity.showProgress,
        animate: tourEntity.animate,
        showButtons: [
          "next",
          "previous",
          tourEntity.showCloseButton ? "close" : null
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

      return () => driverObj.destroy();
    }, [tourEntity, autoStart]);

    return null;
  }
);

export default TourComponent;
