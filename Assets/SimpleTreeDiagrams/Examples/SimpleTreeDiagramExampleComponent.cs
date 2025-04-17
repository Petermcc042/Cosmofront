using System.Collections.Generic;
using UnityEngine;

namespace SimpleTreeDiagrams.Examples
{
    public class SimpleTreeDiagramExampleComponent : MonoBehaviour
    {
        public SimpleTreeDiagramComponent SimpleTreeDiagramPrefab;

        public GameObject Container;

        public Font FontAsset;

        void Start()
        {
            var treeStructure = new SimpleTreeDiagramNode<string>
            {
                data = "Node",
                children = new List<SimpleTreeDiagramNode<string>>
                {
                   new SimpleTreeDiagramNode<string>
                    {
                        data = "Node",
                        children = new List<SimpleTreeDiagramNode<string>>
                        {
                            new SimpleTreeDiagramNode<string>
                            {
                                data = "Node",
                                children = new List<SimpleTreeDiagramNode<string>>
                                {
                                    new SimpleTreeDiagramNode<string>
                                    {
                                        data = "Node",
                                        children = new List<SimpleTreeDiagramNode<string>>
                                        {
                                            new SimpleTreeDiagramNode<string>
                                            {
                                                data = "Node",
                                                children = new List<SimpleTreeDiagramNode<string>>
                                                {
                                                    new SimpleTreeDiagramNode<string>
                                                    {
                                                        data = "Node",
                                                        children = new List<SimpleTreeDiagramNode<string>>
                                                        {
                                                            new SimpleTreeDiagramNode<string>
                                                            {
                                                                data = "Node",
                                                                children = new List<SimpleTreeDiagramNode<string>>
                                                                {
                                                                    new SimpleTreeDiagramNode<string>
                                                                    {
                                                                        data = "Node"
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            },
                                            new SimpleTreeDiagramNode<string>
                                            {
                                                data = "Node"
                                            }
                                        }
                                    },
                                    new SimpleTreeDiagramNode<string>
                                    {
                                        data = "Node"
                                    }
                                }
                            },
                            new SimpleTreeDiagramNode<string>
                            {
                                data = "Node",
                                children = new List<SimpleTreeDiagramNode<string>>
                                {
                                    new SimpleTreeDiagramNode<string>
                                    {
                                        data = "Node"
                                    },
                                    new SimpleTreeDiagramNode<string>
                                    {
                                        data = "Node"
                                    },
                                    new SimpleTreeDiagramNode<string>
                                    {
                                        data = "Node"
                                    },
                                    new SimpleTreeDiagramNode<string>
                                    {
                                        data = "Node"
                                    }
                                }
                            }
                        }
                    },
                    new SimpleTreeDiagramNode<string>
                    {
                        data = "Node"
                    },
                    new SimpleTreeDiagramNode<string>
                    {
                        data = "Node",
                        children = new List<SimpleTreeDiagramNode<string>>
                        {
                            new SimpleTreeDiagramNode<string>
                            {
                                data = "Node",
                                children = new List<SimpleTreeDiagramNode<string>>
                                {
                                    new SimpleTreeDiagramNode<string>
                                    {
                                        data = "Node"
                                    },
                                    new SimpleTreeDiagramNode<string>
                                    {
                                        data = "Node"
                                    },
                                    new SimpleTreeDiagramNode<string>
                                    {
                                        data = "Node",
                                        children = new List<SimpleTreeDiagramNode<string>>
                                        {
                                            new SimpleTreeDiagramNode<string>
                                            {
                                                data = "Node"
                                            },
                                            new SimpleTreeDiagramNode<string>
                                            {
                                                data = "Node"
                                            },
                                            new SimpleTreeDiagramNode<string>
                                            {
                                                data = "Node"
                                            },
                                            new SimpleTreeDiagramNode<string>
                                            {
                                                data = "Node"
                                            }
                                        }
                                    },
                                    new SimpleTreeDiagramNode<string>
                                    {
                                        data = "Node"
                                    },
                                    new SimpleTreeDiagramNode<string>
                                    {
                                        data = "Node"
                                    }
                                }
                            },
                            new SimpleTreeDiagramNode<string>
                            {
                                data = "Node"
                            },
                            new SimpleTreeDiagramNode<string>
                            {
                                data = "Node"
                            },
                            new SimpleTreeDiagramNode<string>
                            {
                                data = "Node"
                            },
                            new SimpleTreeDiagramNode<string>
                            {
                                data = "Node",
                                children = new List<SimpleTreeDiagramNode<string>>
                                {
                                    new SimpleTreeDiagramNode<string>
                                    {
                                        data = "Node"
                                    },
                                    new SimpleTreeDiagramNode<string>
                                    {
                                        data = "Node"
                                    },
                                    new SimpleTreeDiagramNode<string>
                                    {
                                        data = "Node"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var treeDiagram = Instantiate(SimpleTreeDiagramPrefab, Container.transform);

            treeDiagram.SetTree(treeStructure, (data, parent) =>
            {
                var node = new GameObject(data);

                node.transform.SetParent(parent, false);

                var image = node.AddComponent<UnityEngine.UI.Image>();

                image.color = new Color(Random.value / 2 + 0.5f, Random.value / 2 + 0.5f, Random.value / 2 + 0.5f);

                var rectTransform = node.GetComponent<RectTransform>();

                rectTransform.sizeDelta = new Vector2(50, 50);

                var text = new GameObject("Text");

                text.transform.SetParent(node.transform, false);

                var textComponent = text.AddComponent<UnityEngine.UI.Text>();

                textComponent.text = data;
                textComponent.alignment = TextAnchor.MiddleCenter;
                textComponent.color = Color.black;
                textComponent.font = FontAsset;
                textComponent.fontSize = 20;
                textComponent.rectTransform.sizeDelta = new Vector2(50, 50);
            });
        }
    }
}