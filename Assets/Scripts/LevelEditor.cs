using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic; // Lists
//You must include these namespaces
//to use BinaryFormatter
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class LevelEditor : MonoBehaviour {

	public static LevelEditor instance = null;

	const int EMPTY = -1;

	private int[][] level = new int[][]
	{
		new int[]{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
		new int[]{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
		new int[]{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
		new int[]{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
		new int[]{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
		new int[]{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
		new int[]{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
		new int[]{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
		new int[]{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
		new int[]{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
		new int[]{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
		new int[]{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
		new int[]{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
		new int[]{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
	};

	int xMin = 0;
	static int xMax = 15;
	int yMin = 0;
	static int yMax = 13;

	private Transform[,] gameObjects = new Transform[xMax, yMax];

	public List<Transform> tiles;

	private int selectedTile = 0;
	private bool toggleGrid = false;
	private bool deleteMode = false;

	GameObject tileLevelParent;

	//The object we are currently looking to spawn
	Transform toCreate;

	string levelName = "Temp";

	public Texture2D deleteTexture;

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

	public void Start()
	{
		// Get the DynamicObjects object so we can make it our
		// newly created objects' parent
		tileLevelParent = GameObject.Find("TileLevel");
		if (tileLevelParent == null) {
			tileLevelParent = new GameObject ("TileLevel");
		}
		BuildLevel();
		enabled = true;

		toCreate = tiles[0];
	}

	private void toggleDeleteCursor(){
		if (deleteMode) {
			Cursor.SetCursor (ScaleTexture (deleteTexture, 16, 16), Vector2.zero, CursorMode.Auto);
		} else {
			Cursor.SetCursor (null, Vector2.zero, CursorMode.Auto);
		}
	}


	private Texture2D ScaleTexture(Texture2D source,int targetWidth,int targetHeight) {
		Texture2D result = new Texture2D (targetWidth, targetHeight, source.format, false);
//		float incX = (1.0f / (float)targetWidth);
//		float incY = (1.0f / (float)targetHeight);
		for (int i = 0; i < result.height; ++i) {
			for (int j = 0; j < result.width; ++j) {
				Color newColor = source.GetPixelBilinear ((float)j / (float)result.width, (float)i / (float)result.height);
				result.SetPixel (j, i, newColor);
			}
		}
		result.Apply ();
		return result;
	}

	void BuildLevel()
	{
		//Go through each element inside our level variable
		for (int yPos = 0; yPos < level.Length; yPos++)
		{
			for (int xPos = 0; xPos < (level[yPos]).Length; xPos++)
			{
				CreateBlock(level[yPos][xPos], xPos, level.Length - yPos - 1);
			}
		}
	}

	public void CreateBlock(int value, int xPos, int yPos)
	{
		Transform toCreate = null;
		// We need to know the size of our level to save later
		if (xPos < xMin || xPos > xMax || yPos < yMin || yPos > yMax) {
			return;
		}
		level [yPos] [xPos] = value;
		// If the value is not -1 (empty) we 
		if (value != -1) {
			toCreate = tiles [value];
		}
		if (toCreate != null) {
			print ("Creating " + tiles [value].name + " on: (" + xPos + "," + yPos + ")");
			//Create the object we want to create
			Transform newObject = Instantiate (toCreate, new Vector3 (xPos, yPos, 0), Quaternion.identity) as Transform;
			//Give the new object the same name as ours
			newObject.name = toCreate.name;
			// Set the object's parent to the DynamicObjects
			// variable so it doesn't clutter our Hierarchy
			newObject.parent = tileLevelParent.transform;

			gameObjects [yPos, xPos] = newObject;
		}
	}

	void Update()
	{
		// Left click - Create object
		if (Input.GetMouseButton (0) && GUIUtility.hotControl == 0) {
			Vector3 mousePos = Input.mousePosition;
			//Set the position in the z axis to the opposite of the
			// camera's so that the position is on the world so
			// ScreenToWorldPoint will give us valid values.
			mousePos.z = Camera.main.transform.position.z * -1;
			Vector3 pos = Camera.main.ScreenToWorldPoint (mousePos);
			// Deal with the mouse being not exactly on a block
			int posX = Mathf.FloorToInt (pos.x + .5f);
			int posY = Mathf.FloorToInt (pos.y + .5f);
			if (!deleteMode) {
				print ("Selected tile: " + selectedTile);
				print ("Currently on position " + level [posY] [posX]);
				if (level [posY] [posX] == -1) {
					CreateBlock (tiles.IndexOf (toCreate), posX, posY);
				}
				//If it's the same, just keep the previous one
				else if (level [posY] [posX] == selectedTile) {
					print ("Already there, yo");
				} else {
					DestroyImmediate (gameObjects [posY, posX].gameObject);
					CreateBlock (tiles.IndexOf (toCreate), posX, posY);
				}
			} else {
				// Right clicking - Delete object
				//if ((Input.GetMouseButton (1) || Input.GetKeyDown (KeyCode.T)) && GUIUtility.hotControl == 0) {

				// If we hit something (!= -1), we want to destroy it!
				if (level [posY] [posX] != -1) {
					DestroyImmediate (gameObjects [posY, posX].gameObject);
					level [posY] [posX] = -1;
				}
			}
		}
	}

	void OnGUI()
	{
		GUILayout.BeginArea (new Rect (Screen.width - 210, 20, 200, 800));
		Texture[] GUItiles = new Texture[tiles.Count];
		int i = 0;
		foreach (Transform item in tiles) {
			GUItiles [i] = item.gameObject.GetComponent<SpriteRenderer> ().sprite.texture;
			i++;
		}
		selectedTile = GUILayout.SelectionGrid (selectedTile, (Texture[])GUItiles, 3);
		toCreate = tiles [selectedTile];
		GUILayout.EndArea ();

		GUILayout.BeginArea (new Rect (10, 120, 100, 200));
		levelName = GUILayout.TextField (levelName);
		if (GUILayout.Button ("Save")) {
			SaveLevel ();
		}
		if (GUILayout.Button ("Load")) {
			LoadLevelFile (levelName);

		}
		if (GUILayout.Button ("Quit")) {
			enabled = false;
		}

		toggleGrid = GUILayout.Toggle (toggleGrid, "Show Grid");
		deleteMode = GUILayout.Toggle (deleteMode, "Delete Mode");
		toggleDeleteCursor ();
		GridOverlay.instance.enabled = toggleGrid;
		GUILayout.EndArea ();
	}

	void SaveLevel()
	{
		List<string> newLevel = new List<string> ();
		for (int layer = 8; layer < 31; layer++) {
			if (LayerMask.LayerToName (layer).Length > 0) {
				for (int i = yMin; i <= yMax; i++) {
					string newRow = "";
					for (int j = xMin; j <= xMax; j++) {
						Vector3 pos = new Vector3 (j, i, 0);
						Ray ray = Camera.main.ScreenPointToRay (pos);
						RaycastHit hit = new RaycastHit ();
						Physics.Raycast (ray, out hit, 100);


						//int l = 0;
						// Will check if there is something hitting us within
						// a distance of .1
						//Collider[] hitColliders = Physics.OverlapSphere (pos, 0.1f);
						Collider2D[] hitColliders2D = Physics2D.OverlapCircleAll (pos, 0.1f, (1 << layer));
						if (hitColliders2D.Length > 0) {
							// Do we have a tile with the same name as this object?
							for (int k = 0; k < tiles.Count; k++) {
								// If so, let's save that to the string
								if (tiles [k].name == hitColliders2D [0].GetComponent<Collider2D> ().gameObject.name) {
									newRow += (k + 1).ToString () + ",";
								}
							}
						} else {
							newRow += "0,";
						}
					}
					if (i != yMin) {
						newRow += "\n";
					}
					newLevel.Add (newRow);
				}
				newLevel.Add ("\t");
			}
		}

		// Reverse the rows to make the final version rightside up
		newLevel.Reverse ();
		// Remove the first \n after the reversal (layers addition)
		newLevel [0] = newLevel [0].Substring (1);
		string levelComplete = "";
		foreach (string level in newLevel) {
			levelComplete += level;
		}
		// This is the data we're going to be saving
		print (levelComplete);
		//Save to a file
		BinaryFormatter bFormatter = new BinaryFormatter ();
		string path = EditorUtility.SaveFilePanel("Save level", "", levelName, "lvl" );
		if (path.Length != 0) {
			FileStream file = File.Create (path);
			bFormatter.Serialize (file, levelComplete);
			file.Close ();
		} else {
			print ("Failed to save level");
		}
	}

	void LoadLevelFile(string level)
	{
		// Destroy everything inside our currently level that's created
		// dynamically
		foreach(Transform child in tileLevelParent.transform) {
			Destroy(child.gameObject);
		}
		BinaryFormatter bFormatter = new BinaryFormatter();
		string path = EditorUtility.OpenFilePanel("Open level", "", "lvl" );
		if (path.Length != 0) {
			FileStream file = File.OpenRead (path);
			// Convert the file from a byte array into a string
			string levelData = bFormatter.Deserialize (file) as string;
			// We're done working with the file so we can close it
			file.Close ();
			LoadLevelFromStringLayers (levelData);
		}else {
			print ("Failed to open level");
		}
		// Set our text object to the current level.
		levelName = level;
	}

	public void LoadLevelFromStringLayers(string content){
		List <string> layers = new List <string> (content.Split('\t'));
		foreach (string layer in layers) {
			print ("Loaded Layer:");
			print (layer);
			LoadLevelFromString(layer);
		}
	}

	public void LoadLevelFromString(string content)
	{
		print ("Load content:\n" + content);
		// Split our string by the new lines (enter)
		List <string> lines = new List <string> (content.Split('\n'));
		// Place each block in order in the correct x and y position
		for(int i = 0; i < lines.Count; i++)
		{
			string[] blockIDs = lines[i].Split (',');
			for(int j = 0; j < blockIDs.Length - 1; j++)
			{
				CreateBlock(int.Parse(blockIDs[j]), j, lines.Count - i - 1);
			}
		}
	}
}
