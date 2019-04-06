//
//  SuperCombinerEditor.cs
//
//  Author:
//       Lunar Cats Studio <lunarcatsstudio@gmail.com>
//
//  Copyright (c) 2018 Lunar Cats Studio

using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LunarCatsStudio.SuperCombiner;

namespace LunarCatsStudio.SuperCombiner {
	
	/// <summary>
	/// Super combiner editor class, manage gui editor for interact with super combiner script
	/// </summary>
	[CustomEditor(typeof(SuperCombiner))]
	public class SuperCombinerEditor : Editor {
		
#region Inspector
		private enum CombineStatesList {Uncombined, Combining, Combined}
		// Reference to the SuperCombiner script
		private SuperCombiner _superCombiner;

		// Constants
		private const int MIN_VERITCES_COUNT = 32;
		private const int MAX_VERITCES_COUNT = 65534;
		private const string VERSION_NUMBER  = "1.5.2";
        private const int MAX_MULTI_MATERIAL_COUNT = 3;

		// Serialized
		private SerializedObject _serializedCombiner;
		private SerializedProperty _customShaderProperties;
		private List<SerializedProperty> _multiMaterialsSC = new List<SerializedProperty>();
        private List<int> _multiMaterialsOrder = new List<int>();

		// Scroll views
		private Vector2 _originalMaterialsPosition;
		private Vector2 _combinedMaterialsPosition;
		private Vector2 _combinedMeshsPosition;		


		/// <summary>
		/// Raises the enable event.
		/// </summary>
		private void OnEnable()
		{
			_superCombiner = (SuperCombiner)target;

			_serializedCombiner = new SerializedObject (_superCombiner);            
            _customShaderProperties = _serializedCombiner.FindProperty ("customTextureProperies");

            for (int i = 0; i < MAX_MULTI_MATERIAL_COUNT; i++) {
				_multiMaterialsSC.Add(_serializedCombiner.FindProperty ("multiMaterials" + i));
                _multiMaterialsOrder.Add(i);

            }
		}

		/// <summary>
		/// Raises the inspector GUI event.
		/// </summary>
		public override void OnInspectorGUI()
		{
			DisplayHelpSection ();

			// Display settings sections
			GUILayout.Label ("Combine Settings", EditorStyles.whiteBoldLabel);

			DisplayMainSettingsSection ();
			DisplayTextureSettingsSection ();
			DisplayMeshesSettingsSection ();
			DisplayCombineButton ();

			// Display results sections
			if ((CombineStatesList)_superCombiner.combiningState == CombineStatesList.Combined) {
				GUILayout.Label ("Combine results", EditorStyles.whiteBoldLabel);

				DisplayMeshStatsSection ();
				DisplayCombinedAtlasSection ();
				DisplayOriginalMaterialsSection ();
				DisplayCombinedMaterialsSection ();
				DisplayCombinedMeshSection ();

				DisplaySaveSection ();
			}

			_serializedCombiner.ApplyModifiedProperties ();
            _serializedCombiner.Update();
#if UNITY_2017_1_OR_NEWER
            EditorGUIUtility.ExitGUI();
#endif
        }


		/// <summary>
		/// Display the combined atlas.
		/// </summary>
		private void DisplayCombinedAtlasSection(){
            _superCombiner._showCombinedAtlas  = EditorGUILayout.Foldout (_superCombiner._showCombinedAtlas, "Combined Atlas textures:");
			if (_superCombiner._showCombinedAtlas) {
				foreach (KeyValuePair<string, Texture2D> keyValue in _superCombiner.texturePacker.packedTextures) {
					if (keyValue.Value != null) {
						string PropertyName = keyValue.Key;
						_superCombiner.texturePacker.TexturePropertyNames.TryGetValue (keyValue.Key, out PropertyName);
						GUILayout.Label (PropertyName + " AtlasTexture preview:");
						EditorGUILayout.ObjectField ("", keyValue.Value, typeof(Texture), false);			
						EditorGUILayout.Space ();
					}
				}
			}
		}


		/// <summary>
		/// Display the stats.
		/// </summary>
		private void DisplayMeshStatsSection (){
            _superCombiner._showMeshResults  = EditorGUILayout.Foldout (_superCombiner._showMeshResults, "Meshes:");
			if (_superCombiner._showMeshResults) {
				GUILayout.Label ("Found " + _superCombiner.combinedResult.meshesCombinedCount + " different mesh(s)");
				if (_superCombiner.skinnedMeshList.Count > 0) {
					GUILayout.Label ("Found " + _superCombiner.combinedResult.skinnedMeshesCombinedCount + " different skinned mesh(es)");
				}
			}
		}


		/// <summary>
		/// Display the combine button.
		/// </summary>
		private void DisplayCombineButton (){
			EditorGUILayout.Space();

			if ((CombineStatesList)_superCombiner.combiningState == CombineStatesList.Uncombined) {
				if (GUILayout.Button ("Combine", GUILayout.MinHeight (30))) {
					_superCombiner.CombineChildren();
				}
			} else if ((CombineStatesList)_superCombiner.combiningState == CombineStatesList.Combining) {
				EditorGUILayout.Space();
				if (GUILayout.Button("Uncombine", GUILayout.MinHeight(30))) {
					_superCombiner.UnCombine();
				}
				Rect r = EditorGUILayout.BeginVertical ();
				EditorGUI.ProgressBar (r, 0.1f, "Combining in progress ... ");
				GUILayout.Space (20);
				EditorGUILayout.EndVertical ();
			} else {
				if (GUILayout.Button("Uncombine", GUILayout.MinHeight(30))) {
					_superCombiner.UnCombine();
				}
			}
		}


		/// <summary>
		/// Display the header (version number and instructions).
		/// </summary>
		private void DisplayHelpSection (){
			EditorGUILayout.Space();

            _superCombiner._showInstructions = EditorGUILayout.Foldout (_superCombiner._showInstructions, "Instructions for Super Combiner: " + VERSION_NUMBER);
			if (_superCombiner._showInstructions) {
				GUILayout.Label ("Put all you prefabs to combine as children of me. " +
					"Select your session name, the texture atlas size and whether or not to combine meshes. " +
					"When you are ready click 'Combine' button to start the process (it may take a while depending on the quantity of different assets). " +
					"When the process is finished you'll see the result on the scene (all original mesh renderers will be deactivated). " +
					"If you want to save the combined assets, select your saving options and click 'Save' button. " +
					"To revert the process just click 'Uncombine' button.", EditorStyles.helpBox);
			}

			EditorGUILayout.Space();
		}


		/// <summary>
		/// Display the main section.
		/// </summary>
		private void DisplayMainSettingsSection (){
            _superCombiner._showCombineSettings = EditorGUILayout.Foldout (_superCombiner._showCombineSettings, "General Settings:");
			if (_superCombiner._showCombineSettings) {
				if ((CombineStatesList)_superCombiner.combiningState == CombineStatesList.Uncombined) 
					GUI.enabled = true;
				else
					GUI.enabled = false;
		
				_superCombiner.sessionName = EditorGUILayout.TextField(new GUIContent("Session name", "Your session name should be different for every SuperCombiner instance. Avoid using special characters."), _superCombiner.sessionName, GUILayout.ExpandWidth (true));
				_superCombiner.combineAtRuntime = EditorGUILayout.Toggle (new GUIContent("Combine at runtime?", "Set to true if you want the process to combine at startup during runtime (beware that combining is a complex task that may takes some time to process)"), _superCombiner.combineAtRuntime);
			
				GUI.enabled = true;
			}
		}


		/// <summary>
		/// Display the texture section.
		/// </summary>
		private void DisplayTextureSettingsSection (){
            _superCombiner._showTextureSettings = EditorGUILayout.Foldout (_superCombiner._showTextureSettings, "Texture Atlas Settings:");
			if (_superCombiner._showTextureSettings) {
				if ((CombineStatesList)_superCombiner.combiningState == CombineStatesList.Uncombined) 
					GUI.enabled = true;
				else
					GUI.enabled = false;
				
				//GUILayout.Label ("Texture Atlas", EditorStyles.boldLabel);
				EditorGUILayout.BeginVertical (EditorStyles.helpBox);
				GUILayout.Label ("The first material found in all game objects to combine will be used as a reference for the combined material.", EditorStyles.wordWrappedMiniLabel);

				// Atlas Texture Size choice
				_superCombiner.textureAtlasSize = EditorGUILayout.IntPopup("Texture Atlas size", _superCombiner.textureAtlasSize, _superCombiner.TextureAtlasSizesNames.ToArray(), _superCombiner.TextureAtlasSizes.ToArray(), GUILayout.ExpandWidth(true));


                _superCombiner._showAdditionalParameters = EditorGUILayout.Foldout(_superCombiner._showAdditionalParameters, "Additional parameters");
				if (_superCombiner._showAdditionalParameters) {
					EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    // Multi materials group
                    //DisplayMultiMaterialSettingsSection();

                    // Custom Shader propertues
                    EditorGUILayout.PropertyField(_customShaderProperties, new GUIContent("Custom shader properties", "Super Combiner uses the list of texture properties from standard shader. If you are using custom shader with different texture properties, add their exact name in the list."), true);

                    // Tiling factor
					_superCombiner.tilingFactor = EditorGUILayout.Slider(new GUIContent("tiling factor", "Apply a tiling factor on the textures. This may be helpfull if you observe strange artifacts after combining materials with heightmap"), _superCombiner.tilingFactor, 1, 2, GUILayout.ExpandWidth(true));
					EditorGUILayout.EndVertical();
				}

				EditorGUILayout.EndVertical ();
				GUI.enabled = true;
			}
		}

        /// <summary>
        /// Display the multi material section
        /// </summary>
        private void DisplayMultiMaterialSettingsSection()
        {
            _superCombiner.multipeMaterials = EditorGUILayout.Toggle("Multiple materials", _superCombiner.multipeMaterials);
            if (_superCombiner.multipeMaterials)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField("Source Material To Combined Mapping " + _superCombiner._multiMaterialsCount, EditorStyles.boldLabel);

                // Choose between submeshes or separate meshes
                _superCombiner.makeSubmeshes = EditorGUILayout.Toggle("Make submeshes", _superCombiner.makeSubmeshes);

                // Foldout
                EditorGUILayout.BeginHorizontal();
                _superCombiner._showMultiMaterials = EditorGUILayout.Foldout(_superCombiner._showMultiMaterials, "combined materials");

                // Add new material group button
                EditorGUI.BeginDisabledGroup(_superCombiner._multiMaterialsCount >= MAX_MULTI_MATERIAL_COUNT);
                if (GUILayout.Button("+", EditorStyles.miniButtonLeft, GUILayout.MaxWidth(20f)))
                {
                    _superCombiner._multiMaterialsCount++;
                    Debug.Log("Adding group, count is " + _superCombiner._multiMaterialsCount);
                }
                EditorGUI.EndDisabledGroup();

                // Remove new material group button
                EditorGUI.BeginDisabledGroup(_superCombiner._multiMaterialsCount == 0);
                if (GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.MaxWidth(20f)))
                {
                    Debug.Log("before, count was: " + _superCombiner._multiMaterialsCount);
                    _superCombiner._multiMaterialsCount--;
                    _serializedCombiner.Update();

                    Debug.Log("Removing at " + _superCombiner._multiMaterialsCount + " -> " + _multiMaterialsOrder[_superCombiner._multiMaterialsCount]);
                    _multiMaterialsSC[_multiMaterialsOrder[_superCombiner._multiMaterialsCount]].ClearArray();
                    Debug.Log("now count is: " + _superCombiner._multiMaterialsCount);
                }
                EditorGUI.EndDisabledGroup();

                //Debug
                if (GUILayout.Button("PLot", EditorStyles.miniButtonRight, GUILayout.MaxWidth(20f)))
                {
                    Debug.Log("Count " + _superCombiner._multiMaterialsCount);
                }
                EditorGUILayout.EndHorizontal();

                if (_superCombiner._showMultiMaterials)
                {
                    for (int i = 0; i < _superCombiner._multiMaterialsCount; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("-- submesh: " + i + " --", EditorStyles.boldLabel);

                        // Remove a material group button
                        if (GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.MaxWidth(20f)))
                        {
                            _superCombiner._multiMaterialsCount--;
                            _serializedCombiner.Update();
                            Debug.Log("Remove at " + i + " -> " + _multiMaterialsOrder[i]);
                            _multiMaterialsSC[_multiMaterialsOrder[i]].ClearArray();

                            int tmp = _multiMaterialsOrder[i];
                            _multiMaterialsOrder.RemoveAt(i);
                            _multiMaterialsOrder.Add(tmp);
                        }
                        EditorGUILayout.EndHorizontal();

                        // Source materials
                        EditorGUILayout.PropertyField(_multiMaterialsSC[_multiMaterialsOrder[i]], new GUIContent("source materials " + _multiMaterialsOrder[i] + ", i: " + i, "blabla"), true);
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// Display the meshes section.
        /// </summary>
        private void DisplayMeshesSettingsSection (){
            _superCombiner._showMeshSettings = EditorGUILayout.Foldout (_superCombiner._showMeshSettings, "Meshes Settings:");
			if (_superCombiner._showMeshSettings) {
				if ((CombineStatesList)_superCombiner.combiningState == CombineStatesList.Uncombined) 
					GUI.enabled = true;
				else
					GUI.enabled = false;

				EditorGUILayout.BeginVertical (EditorStyles.helpBox);
				_superCombiner.combineMeshes = EditorGUILayout.Toggle (new GUIContent ("Combine meshes?", "If set to false, only materials and textures will be combined, all meshes will remain separated. If set to true, all meshes will be combined into a unique combined mesh."), _superCombiner.combineMeshes);
				if (_superCombiner.combineMeshes) {
					_superCombiner.meshOutput = EditorGUILayout.IntPopup (new GUIContent ("Mesh output", "Chose to combine into a Mesh or a SkinnedMesh. Combining into SkinnedMesh is in alpha release, it will only works properly if there are only SkinnedMeshes as input. Combining Meshes and SkinnedMeshes into a SkinnedMesh is not supported yet."), _superCombiner.meshOutput, new GUIContent[] {
						new GUIContent ("Mesh"),
						new GUIContent ("SkinnedMesh (alpha)")
					}, new int[] {
						0,
						1
					}, GUILayout.ExpandWidth (true));
					_superCombiner.maxVerticesCount = EditorGUILayout.IntSlider (new GUIContent ("Max vertices count", "If the combined mesh has more vertices than this parameter, it will be split into various meshes with 'Max vertices count' vertices"), _superCombiner.maxVerticesCount, MIN_VERITCES_COUNT, MAX_VERITCES_COUNT, GUILayout.ExpandWidth (true));
				}

				_superCombiner.targetGameObject = (GameObject)EditorGUILayout.ObjectField (new GUIContent ("Target GameObject", "The GameObject into which the combined GameObject(s) will be created. If you leave it empty, a new GameObject will be created under this GameObject with the name of you session name."), _superCombiner.targetGameObject, typeof(GameObject), true);

				EditorGUILayout.EndVertical ();
				GUI.enabled = true;
			}
		}


		/// <summary>
		/// Display the original material(s) section.
		/// </summary>
		private void DisplayOriginalMaterialsSection (){
            _superCombiner._showOriginalMaterials = EditorGUILayout.Foldout(_superCombiner._showOriginalMaterials, "Original Material(s): " + _superCombiner.combinedResult.originalMaterialList.Count);
			if (_superCombiner._showOriginalMaterials)
			{
				_originalMaterialsPosition = EditorGUILayout.BeginScrollView (_originalMaterialsPosition, GUILayout.MinHeight(150));
				for (int i=0; i<_superCombiner.combinedResult.originalMaterialList.Count; i++) {
					EditorGUILayout.ObjectField ("Material n°" +  i + ":", _superCombiner.combinedResult.originalMaterialList [i], typeof(Material), false);
				}
				EditorGUILayout.EndScrollView ();
			}
			if(!Selection.activeTransform) {
                _superCombiner._showOriginalMaterials = false;
			}
		}


		/// <summary>
		/// Display the combined material section.
		/// </summary>
		private void DisplayCombinedMaterialsSection (){
            _superCombiner._showCombinedMaterials = EditorGUILayout.Foldout(_superCombiner._showCombinedMaterials, "Combined Material(s): 1");

			if (_superCombiner._showCombinedMaterials)
			{
                EditorGUILayout.ObjectField("Combined material: ", _superCombiner.combinedResult.material, typeof(Material), false);
			}
			if(!Selection.activeTransform) {
                _superCombiner._showCombinedMaterials = false;	
			}
			EditorGUILayout.Space();
		}


		/// <summary>
		/// Display the combined mesh(es) section.
		/// </summary>
		private void DisplayCombinedMeshSection (){
			// Display created meshes
			if (_superCombiner.combineMeshes) {
                _superCombiner._showCombinedMesh = EditorGUILayout.Foldout(_superCombiner._showCombinedMesh, "Combined Meshs");
				if (_superCombiner._showCombinedMesh) {
					_combinedMeshsPosition = EditorGUILayout.BeginScrollView (_combinedMeshsPosition, GUILayout.MinHeight (100));
					for (int i = 0; i < _superCombiner.modifiedObjectList.Count; i++) {
						if (_superCombiner.modifiedObjectList [i] != null) {
							if (_superCombiner.meshOutput == 0) {
								EditorGUILayout.ObjectField ("Mesh n°" + i + ":", _superCombiner.modifiedObjectList [i].GetComponent<MeshFilter> ().sharedMesh, typeof(MeshFilter), false);
							} else if (_superCombiner.meshOutput == 1) {
								EditorGUILayout.ObjectField ("SkinnedMesh n°" + i + ":", _superCombiner.modifiedObjectList [i].GetComponent<SkinnedMeshRenderer> ().sharedMesh, typeof(SkinnedMeshRenderer), false);
							}
						}
					}
					EditorGUILayout.EndScrollView ();
				}
			}
		}
			

		/// <summary>
		/// Display the save section.
		/// </summary>
		private void DisplaySaveSection (){
			// Saving settings
			_superCombiner._showSaveOptions = EditorGUILayout.Foldout (_superCombiner._showSaveOptions, "Saving settings");
			if(_superCombiner._showSaveOptions) {

				_superCombiner.saveMaterials = EditorGUILayout.Toggle ("Save materials", _superCombiner.saveMaterials);
				_superCombiner.saveTextures = EditorGUILayout.Toggle ("Save textures", _superCombiner.saveTextures);
				_superCombiner.savePrefabs = EditorGUILayout.Toggle ("Save prefabs", _superCombiner.savePrefabs);
				_superCombiner.saveMeshObj = EditorGUILayout.Toggle ("Save meshes as Obj", _superCombiner.saveMeshObj);
				//this.SuperCombiner.saveMeshFbx = EditorGUILayout.Toggle ("Save meshes as Fbx", this.SuperCombiner.saveMeshFbx);

				if (GUILayout.Button ("Save in: " + _superCombiner.folderDestination + " ...", GUILayout.MinHeight (20))) {
					//this.SuperCombiner.folderDestination = EditorUtility.OpenFolderPanel("Destination Directory", "", "");
					string folderPath = EditorUtility.SaveFolderPanel("Destination Directory", "", "combined");
                    if (folderPath != null)
                    {
                        string relativePath = folderPath.Substring(folderPath.IndexOf("Assets/"));
                        _superCombiner.folderDestination = relativePath;
                    }
				}
			}
			EditorGUILayout.Space();

			if (GUILayout.Button("Save", GUILayout.MinHeight(30))) {
				_superCombiner.Save();
			}
		}

#endregion //Inspector


#region Menus
		/// <summary>
		/// Launch combine command for all SuperCombiner in current scene
		/// </summary>
		[MenuItem("SuperCombiner/Combine All")]
		static void CombineAll()
		{
			SuperCombiner[] sc_list= FindObjectsOfType<SuperCombiner>();
			foreach (SuperCombiner sc in sc_list) 
			{
				if ((CombineStatesList)sc.combiningState == CombineStatesList.Uncombined)
					sc.CombineChildren();
			}
		}


		/// <summary>
		/// Launch save command for all SuperCombiner in current scene
		/// </summary>
		[MenuItem("SuperCombiner/Save All")]
		static void SaveAll()
		{
			SuperCombiner[] sc_list= FindObjectsOfType<SuperCombiner>();
			foreach (SuperCombiner sc in sc_list) 
			{
				if ((CombineStatesList)sc.combiningState != CombineStatesList.Uncombined)
					sc.Save ();
			}	
		}


		/// <summary>
		/// Launch uncombine command for all SuperCombiner in current scene
		/// </summary>
		[MenuItem("SuperCombiner/UnCombine All")]
		static void UnCombineAll()
		{
			SuperCombiner[] sc_list= FindObjectsOfType<SuperCombiner>();
			foreach (SuperCombiner sc in sc_list) 
			{
				if ((CombineStatesList)sc.combiningState != CombineStatesList.Uncombined)
					sc.UnCombine ();
			}
		}


		/// <summary>
		/// Launch combine command for each SuperCombiner seleted in editor
		/// </summary>
		[MenuItem("SuperCombiner/Combine selected")]
		static void CombineSelected()
		{
			foreach (GameObject obj in Selection.gameObjects) 
			{
				SuperCombiner sc = obj.GetComponent<SuperCombiner> ();
				if (sc != null) 
				{
					if ((CombineStatesList)sc.combiningState == CombineStatesList.Uncombined)
						sc.CombineChildren();
				}
			}
		}


		/// <summary>
		/// activativate "combine selected" item menu when objects with SuperCombiner component are selected 
		/// </summary>
		/// <returns><c>true</c>, if SuperCombiner components are selected, <c>false</c> otherwise.</returns>
		[MenuItem("SuperCombiner/Combine selected", true)]
		static bool ValidateCombineSelected()
		{
			bool valide = false;
			foreach (GameObject obj in Selection.gameObjects) 
			{
				if (obj.GetComponent<SuperCombiner> () != null) 
				{
					valide = true;
				}
			}

			return valide;
		}


		/// <summary>
		/// Launch save command for each SuperCombiner seleted in editor
		/// </summary>
		[MenuItem("SuperCombiner/Save selected")]
		static void SaveSelected()
		{
			foreach (GameObject obj in Selection.gameObjects) 
			{
				SuperCombiner sc = obj.GetComponent<SuperCombiner> ();
				if (sc != null) 
				{
					if ((CombineStatesList)sc.combiningState != CombineStatesList.Uncombined)
						sc.Save ();
				}
			}
		}

		/// <summary>
		/// activativate "save selected" item menu when objects with SuperCombiner component are selected 
		/// </summary>
		/// <returns><c>true</c>, if SuperCombiner components are selected, <c>false</c> otherwise.</returns>
		[MenuItem("SuperCombiner/Save selected", true)]
		static bool ValidateSaveSelected()
		{
			bool valide = false;
			foreach (GameObject obj in Selection.gameObjects) 
			{
				if (obj.GetComponent<SuperCombiner> () != null) 
				{
					valide = true;
				}
			}

			return valide;
		}


		/// <summary>
		/// Launch uncombine command for each SuperCombiner seleted in editor
		/// </summary>
		[MenuItem("SuperCombiner/UnCombine selected")]
		static void UnCombineSelected()
		{
			foreach (GameObject obj in Selection.gameObjects) 
			{
				SuperCombiner sc = obj.GetComponent<SuperCombiner> ();
				if (sc != null) 
				{
					if ((CombineStatesList)sc.combiningState != CombineStatesList.Uncombined)
						sc.UnCombine ();
				}
			}
		}

		/// <summary>
		/// activativate "uncombine selected" item menu when objects with SuperCombiner component are selected 
		/// </summary>
		/// <returns><c>true</c>, if SuperCombiner components are selected, <c>false</c> otherwise.</returns>
		[MenuItem("SuperCombiner/UnCombine selected", true)]
		static bool ValidateUnCombineSelected()
		{
			bool valide = false;
			foreach (GameObject obj in Selection.gameObjects) 
			{
				if (obj.GetComponent<SuperCombiner> () != null) 
				{
					valide = true;
				}
			}

			return valide;
		}



		// Add a menu item called "Combine" to a superCombiner's context menu.
		/// <summary>
		/// Create contextual for Launch combine process
		/// </summary>
		/// <param name="command">Command.</param>
		[MenuItem("CONTEXT/SuperCombiner/Combine")]
		static void Combine(MenuCommand command)
		{
			Debug.Log("SuperCombiner Combine All...");

			SuperCombiner sc = (SuperCombiner)command.context;
			sc.CombineChildren();
		}


		/// <summary>
		/// Create contextual menu for uncombine result
		/// </summary>
		/// <param name="command">Command.</param>
		[MenuItem("CONTEXT/SuperCombiner/UnCombine")]
		static void UnCombine(MenuCommand command)
		{
			SuperCombiner sc = (SuperCombiner)command.context;
			sc.UnCombine ();
		}


		/// <summary>
		/// Create contextual menu for save combine result
		/// </summary>
		/// <param name="command">Command.</param>
		[MenuItem("CONTEXT/SuperCombiner/Save")]
		static void Save(MenuCommand command)
		{
			SuperCombiner sc = (SuperCombiner)command.context;
			sc.Save ();
		}


		/// <summary>
		/// Determines if we have combine result
		/// </summary>
		/// <returns><c>true</c> if is combined the specified command; otherwise, <c>false</c>.</returns>
		/// <param name="command">Command.</param>
		[MenuItem("CONTEXT/SuperCombiner/UnCombine", true)]
		[MenuItem("CONTEXT/SuperCombiner/Save", true)]
		static bool IsCombined(MenuCommand command)
		{
			SuperCombiner sc = (SuperCombiner)command.context;
			if ((CombineStatesList)sc.combiningState == CombineStatesList.Uncombined)
				return false;
			else
				return true;
		}


		/// <summary>
		/// Determines if is uncombined the specified command.
		/// </summary>
		/// <returns><c>true</c> if is uncombined the specified command; otherwise, <c>false</c>.</returns>
		/// <param name="command">Command.</param>
		[MenuItem("CONTEXT/SuperCombiner/Combine", true)]
		static bool IsUnCombined(MenuCommand command)
		{
			SuperCombiner sc = (SuperCombiner)command.context;
			if ((CombineStatesList)sc.combiningState == CombineStatesList.Uncombined)
				return true;
			else
				return false;
		}


		/// <summary>
		/// Add a menu item to create game object with a SuperCombiner component.
		/// Priority 1 ensures it is grouped with the other menu items of the same kind
		/// and propagated to the hierarchy dropdown and hierarch context menus.	
		/// </summary>
		/// <param name="menuCommand">Menu command.</param>
		[MenuItem("GameObject/SuperCombiner/SuperCombiner", false, 10)]
		static void CreateSuperCombinerGameObject(MenuCommand menuCommand)
		{
			// Create a empty game object
			GameObject go = new GameObject("SuperCombiner");
			//add supercombiner componant
			go.AddComponent<SuperCombiner> ();
			// Ensure it gets reparented if this was a context click (otherwise does nothing)
			GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
			// Register the creation in the undo system
			Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
			Selection.activeObject = go;
		}
#endregion //Menus
	}
}
