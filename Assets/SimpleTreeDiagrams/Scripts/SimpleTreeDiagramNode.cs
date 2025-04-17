using System.Collections.Generic;

namespace SimpleTreeDiagrams
{
    public class SimpleTreeDiagramNode<T>
    {
        public T data;
        public List<SimpleTreeDiagramNode<T>> children;
    }

}