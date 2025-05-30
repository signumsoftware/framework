import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { AutoLine, TextAreaLine, TypeContext } from '@framework/Lines'
import AutoLineModal from '@framework/AutoLineModal';
import { useForceUpdate } from '@framework/Hooks'
import { DynamicApiEntity } from '../Signum.Dynamic.Controllers'
import CSharpCodeMirror from '../../Signum.CodeMirror/CSharpCodeMirror'
import TypeHelpComponent from '../../Signum.Eval/TypeHelp/TypeHelpComponent'
import TextArea from '@framework/Components/TextArea';

interface DynamicApiProps {
  ctx: TypeContext<DynamicApiEntity>;
}

export default function DynamicApi(p: DynamicApiProps): React.JSX.Element {
  const forceUpdate = useForceUpdate();

  function handleCodeChange(newScript: string) {
    const evalEntity = p.ctx.value.eval;
    evalEntity.modified = true;
    evalEntity.script = newScript;
    forceUpdate();
  }


  function handleGetClick() {
    var code =
      `[HttpGet("/api/pizza/{id}")]
public PizzaEntity GetPizza(string id) 
{
    var lite = Lite.ParsePrimaryKey<PizzaEntity>(id);
    return lite.Retrieve();
}`;

    AutoLineModal.show({
      type: { name: "string" },
       initialValue: code,
      customComponent:  a => <TextAreaLine {...a}/>,
      title: `GET Code Snippet`,
      message: "Copy to clipboard: Ctrl+C, ESC",
      valueHtmlAttributes: { style: { height: 150 } },
    });
  }

  function handlePostClick() {
    var code =
      `[HttpPost("/api/pizza")]
public List<PizzaEntity>> GetPizza([Required, FromBody]PizzaDTO request) 
{
    return Database.Query<PizzaEntity>()
              .Where(p => request.ingredients.All(ing => p.Ingredients.Any(pi => pi.Name == ing)))
              .Where(p => p.Price >= request.minPrice && p.Price <= request.maxPrice)
              .ToList();
}

public class PizzaDTO 
{
    public string[] ingredients { get; set; }
    public int minPrice { get; set; }
    public int maxPrice { get; set; }
}`;

    AutoLineModal.show({
      type: { name: "string" },
      initialValue: code,
      customComponent: p => <TextAreaLine {...p}/>,
      title: `POST Code Snippet`,
      message: "Copy to clipboard: Ctrl+C, ESC",
      valueHtmlAttributes: { style: { height: 150 } },
    });
  }

  var ctx = p.ctx;
  return (
    <div>
      <AutoLine ctx={ctx.subCtx(d => d.name)} />
      <br />
      <div className="row">
        <div className="col-sm-7">
          <div className="btn-group" style={{ marginBottom: "3px" }}>
            <input type="button" className="btn btn-success btn-sm sf-button" value="GET" onClick={handleGetClick} />
            <input type="button" className="btn btn-warning btn-sm sf-button" value="POST" onClick={handlePostClick} />
          </div>
          <div className="code-container">
            <pre style={{ border: "0px", margin: "0px" }} />
            <CSharpCodeMirror script={ctx.value.eval.script ?? ""} onChange={handleCodeChange} />
            <pre style={{ border: "0px", margin: "0px" }} />
          </div>
        </div>
        <div className="col-sm-5">
          <TypeHelpComponent mode="CSharp" />
        </div>
      </div>
    </div>
  );
}
