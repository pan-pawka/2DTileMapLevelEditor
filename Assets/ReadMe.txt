2D Level Editor for Unity

This package allows the user to add a simple 2D Level Editor to their game. 
The 2D Level Editor allows users to create levels intuitively and fast while also allowing players to create and share their own levels.

Features:

- Add and remove prefabs to quickly and simply build a level
- Allow player to create and share their own levels
- Use different layers to create depth
- Show all layers or only the current layer
- Save and load the levels 
- Grid overlay to visualize the future tile placement
- Test and change at run-time for quick iterations 

---------------------------------------

CONTENT:

Materials:

- GridMaterial: material used to visualize the grid. Color can be changed in the LevelEditor script. 

Prefabs:

- ButtonPrefab: prefab used to represent the tiles in the editor. 
- LevelEditor: GameObject with the LevelEditor script attached to it.
- LevelEditorUI: the interface for the LevelEditor.

Scenes:

- Demo: a demo environment to demonstrate the tool

Scripts: 

- GridOverlay: the grid used to visualize the tiles (Credits: http://answers.unity3d.com/questions/482128/draw-grid-lines-in-game-view.html)
- LevelEditor: the logic that keeps track of changes and updates the interface (Credits: https://gist.github.com/JISyed/5017805)
- MoveCamera: allows the user to move the camera while using the level editor

Sprites:

- Sprites for testing. Credits to Kenney (www.kenney.nl).

---------------------------------------

SETUP:

Create an instance of the LevelEditor prefab in the Hierarchy (drag and drop)

Create an instance of Canvas using the Unity create in the Hierarchy (Create -> UI -> Canvas)
	My settings:
	- UI Scale Mode: Scale With Screen Size
	- Reference resolution: 1280 x 720
	Rest is default

Attach the GridOverlay and MoveCamera script to the main camera

Choose the GridMaterial as the Line Material in the GridOverlay script component of the main camera

Set the desired height, width and amount of layer in the LevelEditor prefab
Add the level tiles (Prefab GameObjects) to the Tiles array in the LevelEditor prefab
Attach the accompanied ButtonPrefab
Set the desired dimensions for the tiles in the interface
Specify a file extension to save and load the levels (default extension: lvl)
Attach the accompanied LevelEditorUI

---------------------------------------

CONTROLS:

- Left mouse to place tiles and select options
- Right mouse to delete tiles
- Right click and drag to move camera

Online documentation:

https://github.com/GracesGames/2DLevelEditor