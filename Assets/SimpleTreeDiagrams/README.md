# Simple Tree Diagrams

A simple tool to generate simple tree diagrams in a `Canvas`.

It's fully based on layout groups, the tree nodes can have any size, and they may be irregular.

## How to use

### Quick start

Use the `DefaultSimpleTreeDiagram` prefab. Instantiate it, and call `SetTree()` on it to fill it with data.

Check the `TreeDiagramComponent` methods documentations for more details.

### Customization

You can customize the connectors prefab. Check the default ones to see how they work, and how to configure their anchors or layouts.

The nodes themselves are fully customizable. This tool will just generate the layout for you.

See the `DefaultSimpleTreeDiagram` prefab as the base, and override the connector prefabs there.
Don't use the `SimpleTreeDiagramComponent` directly, unless you fully understand which layouts are required in the GameObject, as it depends on other components to work.

## Example

There's a `ExampleSimpleTreeDiagramScene` you can check as an example.
It has an object with a `TreeDiagramExampleComponent` that programatically initializes a diagram on start.