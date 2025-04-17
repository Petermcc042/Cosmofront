using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleTreeDiagrams
{
    /// <summary>
    /// The core component of the tree diagram.
    /// <br/>
    /// Use it by adding the DefaultSimpleTreeDiagram prefab to your scene, or calling MakeTreeDiagram() to programatically create a new one.
    /// <br/>
    /// Always call SetTree() after instantiation to set the data it should render.
    /// </summary>
    public class SimpleTreeDiagramComponent : MonoBehaviour
    {
        public delegate void InsertNodeDelegate<T>(T data, Transform parent);

        [Description("The prefab to instantiate the first connector")]
        public GameObject FirstConnectorPrefab;

        [Description("The prefab to instantiate the middle connector")]
        public GameObject MiddleConnectorPrefab;

        [Description("The prefab to instantiate the last connector")]
        public GameObject LastConnectorPrefab;

        [Description("The prefab to instantiate the single connector")]
        public GameObject SingleConnectorPrefab;

        /// <summary>
        /// Makes a Tree Diagram in a Canvas.
        /// <br/>
        /// Use this method to programatically create Tree Diagrams.
        /// </summary>
        /// <returns>A new GameObject with the Tree Diagram</returns>
        public static SimpleTreeDiagramComponent MakeTreeDiagram(
            GameObject firstConnectorPrefab,
            GameObject middleConnectorPrefab,
            GameObject lastConnectorPrefab,
            GameObject singleConnectorPrefab
        )
        {
            var treeDiagram = new GameObject("TreeDiagram");
            var treeDiagramComponent = treeDiagram.AddComponent<SimpleTreeDiagramComponent>();

            treeDiagramComponent.FirstConnectorPrefab = firstConnectorPrefab;
            treeDiagramComponent.MiddleConnectorPrefab = middleConnectorPrefab;
            treeDiagramComponent.LastConnectorPrefab = lastConnectorPrefab;
            treeDiagramComponent.SingleConnectorPrefab = singleConnectorPrefab;

            var verticalLayoutGroup = treeDiagram.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.childControlWidth = true;
            verticalLayoutGroup.childControlHeight = false;

            var contentSizeFitter = treeDiagram.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return treeDiagramComponent;
        }

        /// <summary>
        /// Sets the data this Tree Diagram will render.
        /// <br/>
        /// This method may take some time in big diagrams as it needs to recalculate layouts; check with caution.
        /// </summary>
        /// <typeparam name="T">The type of the object provided to the insertNode delegate to create your node</typeparam>
        /// <param name="treeNode">The tree node this diagram will represent</param>
        /// <param name="insertNode">The delegate that will create the data of each element. It must be attached to the provided transform.</param>
        public void SetTree<T>(SimpleTreeDiagramNode<T> treeNode, InsertNodeDelegate<T> insertNode)
        {
            InternalSetTree(treeNode, insertNode, true);
        }

        private void InternalSetTree<T>(SimpleTreeDiagramNode<T> treeNode, InsertNodeDelegate<T> insertNode, bool isRoot)
        {
            // Clear containers
            foreach (Transform oldObject in transform)
            {
                Destroy(oldObject.gameObject);
            }

            if (treeNode == null)
            {
                return;
            }

            bool anyChildren = treeNode.children != null && treeNode.children.Count > 0;

            // Create basic containers
            var childrenContainer = new GameObject("ChildrenContainer");

            childrenContainer.transform.SetParent(transform, false);

            var childrenContainerHorizontalLayoutGroup = childrenContainer.AddComponent<HorizontalLayoutGroup>();

            childrenContainerHorizontalLayoutGroup.childAlignment = TextAnchor.LowerCenter;
            childrenContainerHorizontalLayoutGroup.childControlWidth = false;
            childrenContainerHorizontalLayoutGroup.childControlHeight = false;

            var childrenContainerContentSizeFitter = childrenContainer.AddComponent<ContentSizeFitter>();

            childrenContainerContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            childrenContainerContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (anyChildren)
            {
                Instantiate(SingleConnectorPrefab, transform);
            }

            var dataContainer = new GameObject("DataContainer");

            dataContainer.transform.SetParent(transform, false);

            var dataContainerGridLayoutGroup = dataContainer.AddComponent<GridLayoutGroup>();

            dataContainerGridLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            dataContainerGridLayoutGroup.padding = new RectOffset(2, 2, 0, 0);

            var dataContainerContentSizeFitter = dataContainer.AddComponent<ContentSizeFitter>();

            dataContainerContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            dataContainerContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Create root data
            insertNode(treeNode.data, dataContainer.transform);

            // Create children
            if (anyChildren)
            {
                for (int index = 0; index < treeNode.children.Count; index++)
                {
                    MakeChild(childrenContainer, treeNode, index, insertNode);
                }
            }

            if (isRoot)
            {
                foreach (var layoutGroup in transform.parent.GetComponentsInChildren<LayoutGroup>())
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
                }
            }
        }

        private void MakeChild<T>(GameObject childrenContainer, SimpleTreeDiagramNode<T> treeNode, int index, InsertNodeDelegate<T> insertNode)
        {
            // Child container
            GameObject childContainer = new GameObject($"Child{index}");

            childContainer.transform.SetParent(childrenContainer.transform, false);

            var childContainerVerticalLayoutGroup = childContainer.AddComponent<VerticalLayoutGroup>();

            childContainerVerticalLayoutGroup.childControlWidth = true;
            childContainerVerticalLayoutGroup.childControlHeight = false;

            var childContainerContentSizeFitter = childContainer.AddComponent<ContentSizeFitter>();

            childContainerContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            childContainerContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Data container
            var dataContainer = new GameObject("DataContainer");

            dataContainer.transform.SetParent(childContainer.transform, false);

            var dataContainerVerticalLayoutGroup = dataContainer.AddComponent<VerticalLayoutGroup>();

            dataContainerVerticalLayoutGroup.childControlWidth = false;
            dataContainerVerticalLayoutGroup.childControlHeight = false;

            var dataContainerContentSizeFitter = dataContainer.AddComponent<ContentSizeFitter>();

            //dataContainerContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            dataContainerContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Connector
            if (treeNode.children.Count  == 1)
            {
                Instantiate(SingleConnectorPrefab, childContainer.transform);
            }
            else if (index == 0)
            {
                Instantiate(FirstConnectorPrefab, childContainer.transform);
            }
            else if (index == treeNode.children.Count - 1)
            {
                Instantiate(LastConnectorPrefab, childContainer.transform);
            }
            else
            {
                Instantiate(MiddleConnectorPrefab, childContainer.transform);
            }

            // Create child tree diagram
            var childTreeDiagram = MakeTreeDiagram(
                FirstConnectorPrefab,
                MiddleConnectorPrefab,
                LastConnectorPrefab,
                SingleConnectorPrefab
            );

            childTreeDiagram.transform.SetParent(dataContainer.transform, false);

            childTreeDiagram.InternalSetTree(treeNode.children[index], insertNode, false);
        }
    }
}