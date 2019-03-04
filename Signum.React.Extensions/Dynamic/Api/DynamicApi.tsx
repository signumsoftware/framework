import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { DynamicApiEntity } from '../Signum.Entities.Dynamic'
import { ValueLine, TypeContext } from '@framework/Lines'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import ValueLineModal from '@framework/ValueLineModal';

interface DynamicApiProps {
  ctx: TypeContext<DynamicApiEntity>;
}

export default class DynamicApi extends React.Component<DynamicApiProps> {

  handleCodeChange = (newScript: string) => {
    const evalEntity = this.props.ctx.value.eval;
    evalEntity.modified = true;
    evalEntity.script = newScript;
    this.forceUpdate();
  }

  render() {
    var ctx = this.props.ctx;
    return (
      <div>
        <ValueLine ctx={ctx.subCtx(d => d.name)} />
        <br />
        <div className="row">
          <div className="col-sm-7">
            <div className="btn-group" style={{ marginBottom: "3px" }}>
              <input type="button" className="btn btn-success btn-sm sf-button" value="GET" onClick={this.handleGetClick} />
              <input type="button" className="btn btn-warning btn-sm sf-button" value="POST" onClick={this.handlePostClick} />
            </div>
            <div className="code-container">
              <pre style={{ border: "0px", margin: "0px" }} />
              <CSharpCodeMirror script={ctx.value.eval.script || ""} onChange={this.handleCodeChange} />
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

  handleGetClick = () => {
    var code =
`[HttpGet("/api/pizza/{id}")]
public PizzaEntity GetPizza(string id) 
{
    var lite = Lite.ParsePrimaryKey<PizzaEntity>(id);
    return lite.Retrieve();
}`;

    ValueLineModal.show({
      type: { name: "string" },
      initialValue: code,
      valueLineType: "TextArea",
      title: `GET Code Snippet`,
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
      valueHtmlAttributes: { style: { height: 150 } },
    }).done();
  }

  handlePostClick = () => {
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

    ValueLineModal.show({
      type: { name: "string" },
      initialValue: code,
      valueLineType: "TextArea",
      title: `POST Code Snippet`,
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
      valueHtmlAttributes: { style: { height: 150 } },
    }).done();
  }
}
