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


        public string Visit<TActivity, TInput, TOutput>(GraphNode<TContext, TActivity, TInput, TOutput> node) where TActivity : IActivity<TInput, TOutput>
        {
            return Visit(node as IGraphNode<TContext>);
        }

        public string Visit(IGraphNode<TContext> node)
        {
            m_Visited.Add(node);
            string res = "";
            res += string.Format("\"{0}\" [label={0}, shape=box]", node.Name);
            res += Environment.NewLine;
            
            if (node.Edges.Count() > 1)
            {
                res += string.Format("\"{0}\"->\"{0} decision\"", node.Name);
                res += Environment.NewLine;
                res += string.Format("\"{0} decision\" [shape=diamond, label=\"\"]", node.Name);
                res += Environment.NewLine;
            }
            foreach (var edge in node.Edges)
            {
                var nextNode = m_Nodes[edge.Node];
                if (node.Edges.Count() > 1)
                    res += string.Format("\"{0} decision\"->\"{1}\"  [label=\"{2}\"]", node.Name, nextNode.Name, edge.Description);
                else
                    res += string.Format("\"{0}\"->\"{1}\"", node.Name, nextNode.Name);
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