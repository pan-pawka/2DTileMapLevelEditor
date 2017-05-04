using UnityEngine;
using UnityEditor;
using System.Collections;

// Include for Lists and Dictionaries
using System.Collections.Generic;

//Include these namespaces to use BinaryFormatter
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

//Include for Unity UI
using UnityEngine.UI;

public class LevelEditor : MonoBehaviour {

	// The instance of the LevelEditor
	public static LevelEditor instance = null;
	// Whether this script is enabled (false, if the user closes the window)
	private bool scriptEnabled = true;
	// Counter for errors
	private int errorCounter = 0;

	// Define empty tile for map
	const int EMPTY = -1;

	// The X,Y and Z value of the map
	public int HEIGHT = 14;
	public int WIDTH = 16;
	public int LAYERS = 10;

	// The internal representation of the level (int values) and gameObjects (transforms)
	private int[, ,] level;
	private Transform[, ,] gameObjects;

	// The list of tiles the user can use to create maps
	// Public so the user can add all user-created prefabs
	public List<Transform> tiles;

	// Used to store the currently selected tile and layer
	private int selectedTile = 0;
	private int selectedLayer = 0;

	// GameObject as the parent for all the layers (to keep the Hierarchy window clean)
	private GameObject tileLevelParent;
	// Dictionary as the parent for all the GameObjects per layer
	private Dictionary<int, GameObject> layerParents = new Dictionary<int, GameObject>();

	// GameObject as the parent for all the GameObject in the tiles selection
	private GameObject prefabParent;
	// Button Prefab used to create tile selection buttons for each GameObjects.
	public GameObject buttonPrefab;
	// Dimensions used for the representation of the GameObject tile selection buttons
	public int buttonHeight = 100;
	public int buttonWidth = 100;
	public float buttonImageScale = 0.8f;

	// File extension used to save and load the levels
	public string fileExtension = "lvl";

	// Boolean to determine whether to show all layers or only the current one
	private bool onlyShowCurrentLayer = false;

	// The parent object of the Level Editor UI as prefab
	public GameObject levelEditorUIPrefab;
	// Text used to represent the currently selected layer
	private Text layerText;
	// Open button to reopen the level editor after closing it
	private GameObject openButton;
	// The UI panel used to store the Level Editor options
	private GameObject levelEditorPanel;

	// Method to Instantiate the LevelEditor instance and keep it from destroying
	void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if(instance != this){
			Destroy(gameObject);
		}
		DontDestroyOnLoad(gameObject);
	}

	// Method to instantiate the dependencies and variables
	public void Start()
	{
		// Check the start values to prevent errors
		CheckStartValues();

		// Define the level sizes as the sizes for the grid
		GridOverlay.instance.SetGridSizeX (WIDTH);
		GridOverlay.instance.SetGridSizeY (HEIGHT);

		// Find the camera and position it in the middle of our level
		GameObject camera = GameObject.FindGameObjectWithTag ("MainCamera");
		if (camera != null) {
			camera.transform.position = new Vector3 (WIDTH / 2, HEIGHT / 2, camera.transform.position.z);
		} else {
			errorCounter++;
			Debug.LogError ("Object with tag MainCamera not found");
		}

		// Get or create the tileLevelParent object so we can make it our newly created objects' parent
		tileLevelParent = GameObject.Find("TileLevel");
		if (tileLevelParent == null) {
			tileLevelParent = new GameObject ("TileLevel");
		}

		// Instantiate the level and gameObject to an empty level and empty Transform
		level = CreateEmptyLevel ();
		gameObjects = new Transform[WIDTH, HEIGHT, LAYERS];

		//------ UI ---------

		// Instantiate the LevelEditorUI
		GameObject canvas = GameObject.Find("Canvas");
		if (canvas == null) {
			errorCounter++;
			Debug.LogError ("Make sure there is a canvas GameObject present in the Hierary (Create UI/Canvas)");
		}
		Instantiate (levelEditorUIPrefab, canvas.transform);

		// Hook up SaveLevel method to SaveButton
		GameObject saveButton = GameObject.Find ("SaveButton");
		if (saveButton == null) {
			errorCounter++;
			Debug.LogError ("Make sure SaveButton is present");
		}
		saveButton.GetComponent<Button>().onClick.AddListener (SaveLevel);

		// Hook up LoadLevel method to LoadButton
		GameObject loadButton = GameObject.Find ("LoadButton");
		if (loadButton == null) {
			errorCounter++;
			Debug.LogError ("Make sure LoadButton is present");
		}
		loadButton.GetComponent<Button>().onClick.AddListener (LoadLevel);

		// Hook up ToggleGrid method to GridToggle
		GameObject gridToggle = GameObject.Find ("GridToggle");
		if (gridToggle == null) {
			errorCounter++;
			Debug.LogError ("Make sure GridToggle is present");
		}
		gridToggle.GetComponent<Toggle>().onValueChanged.AddListener (ToggleGrid);

		// Hook up LayerUp method to +LayerButton
		GameObject plusLayerButton = GameObject.Find ("+LayerButton");
		if (plusLayerButton == null) {
			errorCounter++;
			Debug.LogError ("Make sure +LayerButton is present");
		}
		plusLayerButton.GetComponent<Button>().onClick.AddListener (LayerUp);

		// Hook up LayerDown method to -LayerButton
		GameObject minusLayerButton = GameObject.Find ("-LayerButton");
		if (minusLayerButton == null) {
			errorCounter++;
			Debug.LogError ("Make sure -LayerButton is present");
		}
		minusLayerButton.GetComponent<Button>().onClick.AddListener (LayerDown);

		// Hook up ToggleOnlyShowCurrentLayer method to OnlyShowCurrentLayerToggle
		GameObject onlyShowCurrentLayerToggle = GameObject.Find ("OnlyShowCurrentLayerToggle");
		if (onlyShowCurrentLayerToggle == null) {
			errorCounter++;
			Debug.LogError ("Make sure OnlyShowCurrentLayerToggle is present");
		}
		onlyShowCurrentLayerToggle.GetComponent<Toggle>().onValueChanged.AddListener (ToggleOnlyShowCurrentLayer);

		// Hook up CloseLevelEditorPanel method to CloseButton
		GameObject closeButton = GameObject.Find ("CloseButton");
		if (closeButton == null) {
			errorCounter++;
			Debug.LogError ("Make sure CloseButton is present");
		}
		closeButton.GetComponent<Button>().onClick.AddListener (CloseLevelEditorPanel);

		// Find the prefabParent object and set the cellSize for the tile selection buttons
		prefabParent = GameObject.Find ("Prefabs");
		if (prefabParent == null || prefabParent.GetComponent<GridLayoutGroup> () == null) {
			errorCounter++;
			Debug.LogError ("Make sure prefabParent is present and has a GridLayoutGroup component");
		} else {
			prefabParent.GetComponent<GridLayoutGroup> ().cellSize = new Vector2 (buttonHeight, buttonWidth);
		}

		// Counter to determine which tile button is pressed
		int tileCounter = 0;
		//Create a button for each tile in tiles
		foreach (Transform tile in tiles) {
			int index = tileCounter;
			GameObject button = Instantiate (buttonPrefab, Vector3.zero, Quaternion.identity) as GameObject;
			button.name = tile.name;
			button.GetComponent<Image> ().sprite = tile.gameObject.GetComponent<SpriteRenderer> ().sprite;
			button.transform.SetParent (prefabParent.transform, false);
			button.transform.localScale = new Vector3 (buttonImageScale, buttonImageScale, buttonImageScale);
			// Add a click handler to the button
			button.GetComponent<Button> ().onClick.AddListener (() => {
				ButtonClick (index);
			});
			tileCounter++;
		}

		// Instantiate the LayerText
		layerText = GameObject.Find ("LayerText").GetComponent<Text> ();
		if (layerText == null) {
			errorCounter++;
			Debug.LogError ("Make sure LevelEditorPrefab is present");
		}
		// Instantiate the OpenButton
		openButton = GameObject.Find ("OpenButton");
		if (openButton == null) {
			errorCounter++;
			Debug.LogError ("Make sure OpenButton is present");
		} else {
			openButton.SetActive(false);
			openButton.GetComponent<Button>().onClick.AddListener (OpenLevelEditorPanel);
		}

		// Instantiate the LevelEditorPanel
		levelEditorPanel = GameObject.Find ("LevelEditorPanel");
		if (levelEditorPanel == null) {
			errorCounter++;
			Debug.LogError ("Make sure LevelEditorPanel is present");
		}
	}

	// Method that checks public variables values and sets them to valid defaults when necessary
	private void CheckStartValues()
	{
		WIDTH = Mathf.Clamp (WIDTH, 1, WIDTH);
		HEIGHT = Mathf.Clamp (HEIGHT, 1, HEIGHT);
		LAYERS = Mathf.Clamp (LAYERS, 1, LAYERS);
		buttonHeight = Mathf.Clamp (buttonHeight, 1, buttonHeight);
		buttonWidth = Mathf.Clamp (buttonHeight, 1, buttonHeight);
		buttonImageScale = Mathf.Clamp01 (buttonImageScale);
		fileExtension = fileExtension.Trim () == "" ? "lvl" : fileExtension;
	}

	// Method to switch selectedTile on tile selection
	private void ButtonClick (int tileIndex)
	{
		selectedTile = tileIndex;
	}

	// Method to create an empty level by looping through the Height, Width and Layers 
	// and setting the value to EMPTY (-1)
	int[, ,] CreateEmptyLevel()
	{
		int[,,] level = new int[WIDTH, HEIGHT, LAYERS];
		for (int x = 0; x < WIDTH; x++) {
			for (int y = 0; y < HEIGHT; y++) {
				for (int z = 0; z < LAYERS; z++) {
					level [x, y, z] = EMPTY;
				}
			}
		}
		return level;
	}

	// Method to determine for a given x, y, z, whether the position is valid (within Width, Heigh and Layers)
	private bool ValidPosition(int x, int y, int z)
	{
		if (x < 0 || x >= WIDTH || y < 0 || y >= HEIGHT || z < 0 || z >= LAYERS) {
			return false;
		} else {
			return true;
		}
	}

	// Method that creates a GameObject on click
	public void CreateBlock(int value, int xPos, int yPos, int zPos)
	{
		// The transform to create
		Transform toCreate = null;
		// Return on invalid positions
		if (!ValidPosition (xPos, yPos, zPos)) {
			return;
		}
		// Set the value for the internal level representation
		level [xPos, yPos, zPos] = value;
		// If the value is not empty, set it to the correct tile
		if (value != EMPTY) {
			toCreate = tiles [value];
		}
		if (toCreate != null) {
			//Create the object we want to create
			Transform newObject = Instantiate (toCreate, new Vector3 (xPos, yPos, toCreate.position.z), Quaternion.identity) as Transform;
			//Give the new object the same name as our tile prefab
			newObject.name = toCreate.name;
			// Set the object's parent to the layer parent variable so it doesn't clutter our Hierarchy
			newObject.parent = GetLayerParent (zPos).transform;
			// Add the new object to the gameObjects array for correct administration
			gameObjects [xPos, yPos, zPos] = newObject;
		}
	}

	// Method that updates layer text and handles creation and deletion on click
	void Update()
	{
		// Only continue if the script is enabled (level editor is open) and there are no errors
		if (scriptEnabled && errorCounter == 0) {
			// Update the layer text
			SetLayerText ();
			// Get the mouse position before click (abstraction)
			Vector3 mousePos = Input.mousePosition;
			//Set the position in the z axis to the opposite of the
			// camera's so that the position is on the world so
			// ScreenToWorldPoint will give us valid values.
			mousePos.z = Camera.main.transform.position.z * -1;
			Vector3 pos = Camera.main.ScreenToWorldPoint (mousePos);
			// Deal with the mouse being not exactly on a block
			int posX = Mathf.FloorToInt (pos.x + .5f);
			int posY = Mathf.FloorToInt (pos.y + .5f);
			// Return if the value is not valid
			if (!ValidPosition (posX, posY, selectedLayer)) {
				return;
			}
			// Left click - Create object
			if (Input.GetMouseButton (0) && GUIUtility.hotControl == 0) {
				// If it's the same, just keep the previous one and do nothing
				if (level [posX, posY, selectedLayer] == selectedTile) {
				}
				// If the position is empty, create a new block
				else if (level [posX, posY, selectedLayer] == EMPTY) {
					CreateBlock (selectedTile, posX, posY, selectedLayer);
				}
				// Else destroy the current element (using gameObjects array) and create a new block
				else {
					DestroyImmediate (gameObjects [posX, posY, selectedLayer].gameObject);
					CreateBlock (selectedTile, posX, posY, selectedLayer);
				}
			}
			// Right clicking - Delete object
			if (Input.GetMouseButton (1) && GUIUtility.hotControl == 0) {
				// If we hit something (!= EMPTY), we want to destroy the object and update the gameObject array and level array
				if (level [posX, posY, selectedLayer] != EMPTY) {
					DestroyImmediate (gameObjects [posX, posY, selectedLayer].gameObject);
					level [posX, posY, selectedLayer] = EMPTY;
				}
			}
		}
	}

	// Method that toggles the grid
	public void ToggleGrid(bool enabled)
	{
		GridOverlay.instance.enabled = enabled;
	}

	// Method that updates the LayerText
	void SetLayerText()
	{
		layerText.text = "Layer: " + selectedLayer;
	}

	// Method that increments the selected layer
	public void LayerUp()
	{
		selectedLayer = Mathf.Clamp (selectedLayer + 1, 0, 100);
		UpdateLayerVisibility ();
	}

	// Method that decrements the selected layer
	public void LayerDown()
	{
		selectedLayer = Mathf.Clamp (selectedLayer - 1, 0, 100);
		UpdateLayerVisibility ();
	}

	// Method that handles the UI toggle to only show the current layer
	public void ToggleOnlyShowCurrentLayer(bool onlyShow){
		onlyShowCurrentLayer = onlyShow;
		UpdateLayerVisibility ();
	}

	// Method that updates which layers should be shown
	public void UpdateLayerVisibility(){
		if (onlyShowCurrentLayer) {
			OnlyShowCurrentLayer ();
		} else {
			ShowAllLayers ();
		}
	}

	// Method that enables/disables all layerParents
	public void ToggleLayerParents(bool show){
		foreach (GameObject layerParent in layerParents.Values) {
			layerParent.SetActive (show);
		}
	}

	// Method that enables all layers
	public void ShowAllLayers(){
		ToggleLayerParents (true);
	}

	// Method that disables all layers except the current one
	public void OnlyShowCurrentLayer(){
		ToggleLayerParents (false);
		GetLayerParent (selectedLayer).SetActive (true);
	}

	// Method to determine whether a layer is empty
	private bool EmptyLayer(int layer)
	{
		bool result = true;
		for (int x = 0; x < WIDTH; x++) {
			for (int y = 0; y < HEIGHT; y++) {
				if (level [x, y, layer] != -1) {
					result = false;
				}
			}
		}
		return result;
	}

	// Method that return the parent GameObject for a layer
	private GameObject GetLayerParent(int layer)
	{
		if (!layerParents.ContainsKey (layer)) {
			GameObject layerParent = new GameObject ("Layer " + layer);
			layerParent.transform.parent = tileLevelParent.transform;
			layerParents.Add (layer, layerParent);
		}
		return layerParents [layer];
	}

	// Close the level editor panel, test level mode
	public void CloseLevelEditorPanel ()
	{
		scriptEnabled = false;
		levelEditorPanel.SetActive (false);
		openButton.SetActive(true);
	}

	// Open the level editor panel, level editor mode
	public void OpenLevelEditorPanel()
	{
		levelEditorPanel.SetActive (true);
		openButton.SetActive(false);
		scriptEnabled = true;
	}

	// Save the level to a file
	public void SaveLevel()
	{
		List<string> newLevel = new List<string> ();
		// Loop through the layers
		for (int layer = 0; layer < LAYERS; layer++) {
			// If the layer is not empty, add it and add \t at the end"
			if (!EmptyLayer (layer)) {
				// Loop through the rows and add \n at the end"
				for (int y = 0; y < HEIGHT; y++) {
					string newRow = "";
					for (int x = 0; x < WIDTH; x++) {
						newRow += +level [x, y, layer] + ",";
					}
					if (y != 0) {
						newRow += "\n";
					}
					newLevel.Add (newRow);
				}
				newLevel.Add ("\t" + layer);
			}
		}

		// Reverse the rows to make the final version rightside up
		newLevel.Reverse ();
		string levelComplete = "";
		foreach (string level in newLevel) {
			levelComplete += level;
		}
		//Save to a file
		BinaryFormatter bFormatter = new BinaryFormatter ();
		string path = EditorUtility.SaveFilePanel ("Save level", "", "LevelName", fileExtension);
		if (path.Length != 0) {
			FileStream file = File.Create (path);
			bFormatter.Serialize (file, levelComplete);
			file.Close ();
		} else {
			Debug.Log ("Failed to save level");
		}
	}

	// Method that resets the level and GameObject before a load
	void ResetBeforeLoad()
	{
		// Destroy everything inside our currently level that's created
		// dynamically
		foreach (Transform child in tileLevelParent.transform) {
			Destroy (child.gameObject);
		}
		level = CreateEmptyLevel ();
		layerParents = new Dictionary<int, GameObject> ();
	}

	public void LoadLevel()
	{
		BinaryFormatter bFormatter = new BinaryFormatter ();
		string path = EditorUtility.OpenFilePanel ("Open level", "", fileExtension);
		if (path.Length != 0) {
			// Reset the level
			ResetBeforeLoad ();
			FileStream file = File.OpenRead (path);
			// Convert the file from a byte array into a string
			string levelData = bFormatter.Deserialize (file) as string;
			// We're done working with the file so we can close it
			file.Close ();
			LoadLevelFromStringLayers (levelData);
		} else {
			print ("Failed to open level");
		}
	}

	// Method that loads the layers
	void LoadLevelFromStringLayers(string content)
	{
		// Split our level on layers by the new tabs (\t)
		List <string> layers = new List <string> (content.Split ('\t'));
		int layerCounter = 0;
		foreach (string layer in layers) {
			if (layer.Trim () != "") {
				LoadLevelFromString (int.Parse (layer [0].ToString ()), layer.Substring (1));
				layerCounter++;
			}
		}
	}

	// Method that loads one layer
	void LoadLevelFromString(int layer, string content)
	{
		// Split our layer on rows by the new lines (\n)
		List <string> lines = new List <string> (content.Split ('\n'));
		// Place each block in order in the correct x and y position
		for (int i = 0; i < lines.Count; i++) {
			string[] blockIDs = lines [i].Split (',');
			for (int j = 0; j < blockIDs.Length - 1; j++) {
				CreateBlock (int.Parse (blockIDs [j]), j, lines.Count - i - 1, layer);
			}
		}
		// Update to only show the correct layer(s)
		UpdateLayerVisibility ();
	}
}
