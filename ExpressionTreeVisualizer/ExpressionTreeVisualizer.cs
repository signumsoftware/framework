using System;
using System.Text;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.CSharp;
using System.Collections;
using System.Linq.Expressions;
using Microsoft.VisualStudio.DebuggerVisualizers;
using System.Diagnostics;
using System.Runtime.Serialization;
using ExpressionVisualizer;
using Signum.Utilities; 

[assembly: DebuggerVisualizer(typeof(ExpressionTreeVisualizer), typeof(ExpressionTreeVisualizerObjectSource), Target = typeof(Expression), Description = "Expression Tree Visualizer")]

namespace ExpressionVisualizer
{

    public class ExpressionTreeVisualizer : DialogDebuggerVisualizer
    {
        private IDialogVisualizerService modalService;

        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            try
            {
                modalService = windowService;
                if (modalService == null)
                    throw new NotSupportedException("This debugger does not support modal visualizers");

                object obj = objectProvider.GetObject();

                if (obj is string)
                    MostrarExcepcion((string)obj);
                else
                {
                    ExpressionTreeNode node = (ExpressionTreeNode)obj;
                    TreeWindow treeForm = new TreeWindow();
                    treeForm.browser.Nodes.Add(node);
                    treeForm.browser.ExpandAll();

                    modalService.ShowDialog(treeForm);
                }
            }
            catch (Exception e)
            {
                MostrarExcepcion(GetString(e));
            }
        }

        private static void MostrarExcepcion(string str)
        {
            MessageBox.Show(str, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static string GetString(Exception ex)
        {
            return @"Type:{0}
Message:{1}
CallStack:
{2}".Formato(ex.GetType().Name, ex.Message, ex.StackTrace); 
        }

        public static void ShowInVisualizer(Expression expr)
        {
            //VisualizerDevelopmentHost host = new VisualizerDevelopmentHost(expr,
            //                                   typeof(ExpressionTreeVisualizer),
            //                                   typeof(ExpressionTreeVisualizerObjectSource));
            //host.ShowVisualizer();   

            ExpressionTreeNode node = ExpressionTreeNodeBuilder.Build("Start", expr);
            TreeWindow treeForm = new TreeWindow();
            treeForm.browser.Nodes.Add(node);
            treeForm.browser.ExpandAll();

            Application.Run(treeForm);

        }
    }
}