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

	private int[][] level = new int[][]
	{
		new int[]{1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
		new int[]{1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
		new int[]{1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
		new int[]{1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
		new int[]{1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
		new int[]{1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
		new int[]{1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
		new int[]{1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
		new int[]{1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
		new int[]{1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
		new int[]{1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
		new int[]{1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
		new int[]{1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
		new int[]{1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1}
	};

	int xMin = 0;
	int xMax = 15;
	int yMin = 0;
	int yMax = 13;

	public List<Transform> tiles;

	private int selectedTile = 0;
	private bool toggleGrid = false;
	private bool deleteMode = false;

	GameObject dynamicParent;

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
		print (deleteTexture.GetType ());
		// Get the DynamicObjects object so we can make it our
		// newly created objects' parent
		dynamicParent = GameObject.Find("DynamicObjects");
		if (dynamicParent == null) {
			dynamicParent = new GameObject ("DynamicObjects");
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
		if(xPos < xMin || xPos > xMax || yPos < yMin || yPos > yMax){
			return;
		}
		if(xPos < xMin)
		{
			xMin = xPos;
		}
		if(xPos > xMax)
		{
			xMax = xPos;
		}
		if(yPos < yMin)
		{
			yMin = yPos;
		}
		if(yPos > yMax)
		{
			yMax = yPos;
		}
		//If value is set to 0, we don't want to spawn anything
		if(value != 0)
		{
			toCreate = tiles[value-1];
		}
		if(toCreate != null)
		{
			//Create the object we want to create
			Transform newObject = Instantiate(toCreate, new Vector3(xPos, yPos, 0), Quaternion.identity) as Transform;
			//Give the new object the same name as ours
			newObject.name = toCreate.name;
			// Set the object's parent to the DynamicObjects
			// variable so it doesn't clutter our Hierarchy
			newObject.parent = dynamicParent.transform;
		}
	}

	void Update()
	{
		// Left click - Create object
		if (Input.GetMouseButton (0) && GUIUtility.hotControl == 0) {
			if (!deleteMode) {
				Vector3 mousePos = Input.mousePosition;
				//Set the position in the z axis to the opposite of the
				// camera's so that the position is on the world so
				// ScreenToWorldPoint will give us valid values.
				mousePos.z = Camera.main.transform.position.z * -1;
				Vector3 pos = Camera.main.ScreenToWorldPoint (mousePos);
				// Deal with the mouse being not exactly on a block
				int posX = Mathf.FloorToInt (pos.x + .5f);
				int posY = Mathf.FloorToInt (pos.y + .5f);
				// Convert from screenspace to worldspace using a Ray
				Ray ray = Camera.main.ScreenPointToRay (mousePos);
				// We need to check if there is an object already at
				// the position we're trying to create at
				RaycastHit hit = new RaycastHit ();

				RaycastHit2D hit2d = Physics2D.Raycast (Camera.main.ScreenToWorldPoint (mousePos), Vector2.zero);
				// If something within a distance of 100 in the
				// direction hits something hit will get the data of
				//the hit object.
				Physics.Raycast (ray, out hit, 100);
				if ((hit2d.collider != null) && (hit2d.collider.name != "Player")) {
					//If it's the same, just keep the previous one
					if (toCreate.name != hit2d.collider.gameObject.name) {
						if (hit2d.collider.gameObject.GetComponent<SpriteRenderer> ().sortingOrder == toCreate.GetComponent<SpriteRenderer> ().sortingOrder) {
							//print (hit2d.collider.gameObject);
							CreateBlock (tiles.IndexOf (toCreate) + 1,
								Mathf.FloorToInt (hit2d.collider.gameObject.transform.position.x),
								Mathf.FloorToInt (hit2d.collider.gameObject.transform.position.y));
							DestroyImmediate (hit2d.collider.gameObject);
						} else {
							CreateBlock (tiles.IndexOf (toCreate) + 1, posX, posY);
						}
					}
				
				} else {
					CreateBlock (tiles.IndexOf (toCreate) + 1, posX, posY);
				}
			} else {
				// Right clicking - Delete object
				//if ((Input.GetMouseButton (1) || Input.GetKeyDown (KeyCode.T)) && GUIUtility.hotControl == 0) {
				Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
				RaycastHit hit = new RaycastHit ();
				RaycastHit2D hit2d = Physics2D.Raycast (Camera.main.ScreenToWorldPoint (Input.mousePosition), Vector2.zero);
				Physics.Raycast (ray, out hit, 100);
				// If we hit something other than the player, we
				// want to destroy it!
				if ((hit2d.collider != null) && (hit2d.collider.name != "Player")) {
					//print (hit2d.collider.gameObject);
					Destroy (hit2d.collider.gameObject);
				}
			}
		}
	}

	void OnGUI()
	{
		GUILayout.BeginArea(new Rect(Screen.width - 210, 20, 200, 800));
		Texture[] GUItiles = new Texture[tiles.Count];
		int i = 0;
		foreach(Transform item in tiles)
		{
			GUItiles [i] = item.gameObject.GetComponent<SpriteRenderer> ().sprite.texture;
			i++;
		}
		selectedTile = GUILayout.SelectionGrid (selectedTile, (Texture[])GUItiles, 3);
		toCreate = tiles [selectedTile];
		GUILayout.EndArea();

		GUILayout.BeginArea(new Rect(10, 120, 100, 200));
		levelName = GUILayout.TextField(levelName);
		if (GUILayout.Button ("Save"))
		{
			SaveLevel();
		}
		if (GUILayout.Button ("Load"))
		{
			//If we have a file with the name typed in, load it!
			if(File.Exists(Application.persistentDataPath + "/" + levelName
				+ ".lvl"))
			{
				LoadLevelFile(levelName);
				//PlayerStart.spawned = false;
				// We need to wait one frame before UpdateOrbTotals
				// will work (Orbs need to have Tag assigned)
				//StartCoroutine(LoadedUpdate());
			}
			else
			{
				levelName = "Error";
			}
		}
		if (GUILayout.Button ("Quit"))
		{
			enabled = false;
		}

		toggleGrid = GUILayout.Toggle (toggleGrid, "Show Grid");
		deleteMode = GUILayout.Toggle (deleteMode, "Delete Mode");
		toggleDeleteCursor ();
		GridOverlay.instance.enabled = toggleGrid;
		GUILayout.EndArea();
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
		foreach(Transform child in dynamicParent.transform) {
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
