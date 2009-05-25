using System;
using System.Text;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.CSharp;
using System.Collections;
using Microsoft.VisualStudio.DebuggerVisualizers;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Linq.Expressions;
using ExpressionVisualizer;

namespace ExpressionVisualizer
{

    public class ExpressionTreeVisualizerObjectSource : VisualizerObjectSource
    {
        public override void GetData(object target, Stream outgoingData)
        {
            try
            {
                Expression expr = (Expression)target;
                ExpressionTreeNode browser = ExpressionTreeNodeBuilder.Build("Start", expr);
                //ExpressionTreeContainer container = new ExpressionTreeContainer(browser);
                VisualizerObjectSource.Serialize(outgoingData, browser);
            }
            catch (Exception e)
            {
                VisualizerObjectSource.Serialize(outgoingData, "GetData:\r\n" + ExpressionTreeVisualizer.GetString(e));
            }
        }
    }

  
}