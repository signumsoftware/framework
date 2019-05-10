
using Signum.Engine.Chart;
using Signum.Entities.Chart;
using System.Collections.Generic;

namespace Signum.Engine.Chart.Scripts 
{
    public class LineChartScript : ChartScript                
    {
        public LineChartScript() : base(D3ChartScript.Line)
        {
            this.Icon = ChartScriptLogic.LoadIcon("lines.png");
            this.Columns = new List<ChartScriptColumn>
            {
                new ChartScriptColumn("Horizontal Axis", ChartColumnType.Groupable),
                new ChartScriptColumn("Height", ChartColumnType.Positionable) 
            };
            this.ParameterGroups = new List<ChartScriptParameterGroup>
            {
                new ChartScriptParameterGroup("Scale")
                {
                    new ChartScriptParameter("Scale", ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)") },
                    new ChartScriptParameter("CompleteValues", ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("Auto|Yes|No") },
                },
                new ChartScriptParameterGroup("Margins")
                {
                    new ChartScriptParameter("UnitMargin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 20m } },
                },
                new ChartScriptParameterGroup("Number")
                {
                    new ChartScriptParameter("NumberOpacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } }
                },
                new ChartScriptParameterGroup("Color")
                {
                    new ChartScriptParameter("Color", ChartParameterType.String) {  ValueDefinition = new StringValue("steelblue") },
                },
                new ChartScriptParameterGroup("Form")
                {
                    new ChartScriptParameter("Interpolate", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("linear|step-before|step-after|cardinal|monotone|basis|bundle|catmull-rom") },
                } 
            };
        }      
    }                
}
