using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Utilities;
using Signum.Utilities.DataStructures;

namespace Signum.Engine
{
      [TestClass]
    public class DirectedGraphTest
    {
          [TestMethod]
          public void DirectedGraph()
          {
              var dg = DirectedGraph<int>.Generate(8, a => 0.To(a));

              Assert.IsTrue(dg.Connected(8, 7));
              Assert.IsTrue(dg.Connected(2, 1));
              Assert.IsFalse(dg.Connected(1, 2));
              Assert.IsFalse(dg.TryConnected(10, 8));

              var dgi = dg.Inverse();

              Assert.IsFalse(dgi.Connected(8, 7));
              Assert.IsFalse(dgi.Connected(2, 1));
              Assert.IsTrue(dgi.Connected(1, 2));
              Assert.IsFalse(dgi.TryConnected(10, 8));

              var dgn = new DirectedGraph<int>() { { 10, 8 } };

              dgi.Union(dgn);

              Assert.IsTrue(dgi.TryConnected(10, 8));

              var cadena = new DirectedGraph<int>() { { 10, 9 }, { 9, 8 }, { 8, 7 } };
              Assert.AreEqual(cadena.IndirectlyRelatedTo(10).OrderBy(a => a).ToString(","), "7,8,9");


              var ciclo = new DirectedGraph<int>() { { 1, 2 }, { 2, 3 }, { 3, 1 } };
              Assert.AreEqual(ciclo.IndirectlyRelatedTo(1).OrderBy(a => a).ToString(","), "1,2,3");
          }

          [TestMethod]
          public void TopologicalSortGroups()
          {
              //buscar topological sorting en la wikipedia
              DirectedGraph<int> dg = new DirectedGraph<int>()
              {
                  {7,11},{7,8},{5,11},{3,8},
                  {11,2},{11,9},{8,9},{3,10}
              };

              var grupos = dg.CompilationOrderGroups().ToList();
              string result = grupos.ToString(g => g.Order().ToString(","), "; ");
              Assert.AreEqual("2,9,10; 8,11; 3,5,7", result);
          }

          [TestMethod]
          public void Acyclic()
          {
              //buscar topological sorting en la wikipedia
              DirectedGraph<int> dg = new DirectedGraph<int>()
              {
                {1,2},{2,3},{3,4}
              };

              var split = dg.FeedbackEdgeSet();

              Assert.AreEqual(0, split.Count());
              
              dg.RemoveFullNode(4); 
              dg.Add(3, 1);

              split = dg.FeedbackEdgeSet();

              Assert.AreEqual(1, split.Edges.Count());

              DirectedGraph<int> fullgraph = new DirectedGraph<int>()
              {
                {1,2},{1,3},{1,4},
                {2,3},{2,4},{2,1},
                {3,4},{3,1},{3,2},
                {4,1},{4,2},{4,3}
              };

              split = fullgraph.FeedbackEdgeSet();

              Assert.AreEqual(6, split.Edges.Count());

              fullgraph.RemoveAll(split.Edges);
              Assert.AreEqual(4, fullgraph.CompilationOrderGroups().Count());

              DirectedGraph<int> spiral = new DirectedGraph<int>()
              {
                {1,2},{1,3},{1,4},{1,5},{1,6},
                {2,3},{3,4},{4,5},{5,6},{6,1},
                {5,1}
              };

              split = spiral.FeedbackEdgeSet();
              Assert.AreEqual(2, split.Edges.Count());


              DirectedGraph<int> dgDouble = new DirectedGraph<int>()
              {
                {1,2},{2,3},{3,1},
                {10,20},{20,30},{30,10},
              };

              var feeddgDouble = dgDouble.FeedbackEdgeSet();

              Assert.AreEqual(2, feeddgDouble.Edges.Count());
          
          }

          //[TestMethod]
          //public void ColapseGraph()
          //{
          //    //buscar topological sorting en la wikipedia
          //    DirectedGraph<object> dg = new DirectedGraph<object>()
          //    {
          //      {"a", 2,4,  "b", "s"},
          //      {"b", 3,"s"},
          //      {2,3},
          //      {3,"c"},
          //      {"c", "b", "j"}
          //    };

          //    DirectedGraph<string> colapsed = dg.ColapseTo<string>();
          //    Assert.AreEqual(5, colapsed.Count);
          //    Assert.AreEqual(7, colapsed.Edges.Count());
          //}
    }
}
