using System;
using System.Collections.Generic;
using System.Linq;

namespace Inceptum.Workflow
{
    internal class GraphvizGenerator<TContext> : IWorkflowVisitor<TContext, string>
    {
        private readonly INodesResolver<TContext> m_Nodes;
        private readonly List<IGraphNode<TContext>> m_Visited = new List<IGraphNode<TContext>>();
      
        public GraphvizGenerator(INodesResolver<TContext> nodes)
        {
            m_Nodes = nodes;
        }



        public string Visit(IGraphNode<TContext> node)
        {
            m_Visited.Add(node);
            string res = "";
            if (node.Name != "fail")
            {
                res += string.Format("\"{0}\" [label=\"{0}\", shape={1}]", node.Name, node.Name == "end" || node.Name == "start" ? "ellipse, style=filled,fillcolor=\"yellow\"" : "box");
                res += Environment.NewLine;
            }
            if (node.Edges.Count() > 1 || (node.Edges.Count()==1 && node.Edges.First().Description != "Success"))
            {
                res += string.Format("\"{0}\"->\"{0} decision\"", node.Name);
                res += Environment.NewLine;
                res += string.Format("\"{0} decision\" [shape=diamond, label=\"\",style=filled,fillcolor=\"gray\"]", node.Name);
                res += Environment.NewLine;
            }
            foreach (var edge in node.Edges.OrderBy(e=>e.Description=="Fail"?1:0))
            {
                var nextNode = m_Nodes[edge.Node];

                var nextNodeName = nextNode.Name;
                if (nextNodeName == "fail")
                {
                    res += string.Format("\"{0} fail\" [label=\"fail\",style=filled,fillcolor=\"red\"]", node.Name);
                    res += Environment.NewLine;
                    nextNodeName = string.Format("{0} fail", node.Name);
                }
                var edgeDescription = edge.Description;
                if (edgeDescription == "Success")
                    edgeDescription = "";
                if (node.Edges.Count() > 1 || edge.Description != "Success")
                    res += string.Format("\"{0} decision\"->\"{1}\"  [label=\"{2}\"]", node.Name, nextNodeName, edgeDescription);
                else
                    res += string.Format("\"{0}\"->\"{1}\"  [label=\"{2}\"]", node.Name, nextNodeName, edgeDescription);
                res += Environment.NewLine;
              /*  if (res != "") res += ",";
                res += string.Format("{0}{2}->{1}",
                    nodeStringFrom(node),
                    nodeStringTo(nextNode),
                    string.IsNullOrEmpty(edge.Description) || node.Edges.Count() == 1 ? "" : "[" + edge.Description + "]");*/
                if (!m_Visited.Contains(nextNode))
                    res +=  nextNode.Accept(this);
            }
            return res;
        }

        private string nodeStringTo(IGraphNode<TContext> node)
        {
            return string.Format("({0})", node.Name);
        }
        private string nodeStringFrom(IGraphNode<TContext> node)
        {
            if (node.Edges.Count() > 1)
                return string.Format("<{0} decision>", node.Name);
            return string.Format("({0})", node.Name);
        }

    }
}