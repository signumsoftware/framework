import * as React from 'react'
import { classes } from '../Globals'
import * as Navigator from '../Navigator'
import { ErrorBoundary } from "../Components/ErrorBoundary";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

export default function ContainerToggleComponent(p: { children: React.ReactNode }) {

  const [fluid, setFluid] = React.useState(false);

  React.useEffect(() => {
    Navigator.Expander.onGetExpanded = () => fluid;
    Navigator.Expander.onSetExpanded = (isExpanded: boolean) => setFluid(isExpanded);
  });

  function handleExpandToggle(e: React.MouseEvent<any>){
    e.preventDefault();
    setFluid(!fluid);
  }

  return (
    <div className={classes(fluid ? "container-fluid" : "container", "mt-3", "d-flex")}>
      <a className="expand-window d-none d-md-block" onClick={handleExpandToggle} href="#" >
        <FontAwesomeIcon icon={fluid ? "compress" : "expand"} />
      </a>
      <ErrorBoundary>
        {p.children}
      </ErrorBoundary>
    </div>
  );
}

