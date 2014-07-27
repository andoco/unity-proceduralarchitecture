﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Text;

public class Demo : MonoBehaviour
{	
	private IShapeProductionSystem system;
	private IShapeConfiguration shapeConfiguration;

	private const int numColors = 50;
	private Color[] faceColors = new Color[numColors];
	private TextAsset[] sourceFiles;
	private int currentSourceFileIndex;
	private GameObject rootGo;
	
	public Material material;
	public GUIText sourceGuiText;

	// Use this for initialization
	void Start () {
		for (int i=0; i < numColors; i++)
		{
			faceColors[i] = new Color(Random.value, Random.value, Random.value);
		}

		this.sourceFiles = Resources.LoadAll<TextAsset>("");

		this.ShowSystem();
	}

	void Update()
	{
		if (Input.GetKey(KeyCode.UpArrow))
		{
			this.Rotate(45f);
		}
		else if (Input.GetKey(KeyCode.DownArrow))
		{
			this.Rotate(-45f);
		}

		if (Input.GetKeyUp(KeyCode.RightArrow) & this.currentSourceFileIndex < this.sourceFiles.Length - 1)
		{
			this.ShowNext();
		}
		else if (Input.GetKeyUp(KeyCode.LeftArrow) && this.currentSourceFileIndex > 0)
		{
			this.ShowPrevious();
		}
	}

	void OnGUI()
	{
		var screenScale = Screen.width / 960.0f;
		var scaledMatrix = Matrix4x4.Scale(new Vector3(screenScale,screenScale,screenScale));
		GUI.matrix = scaledMatrix;

		if (GUILayout.Button("Previous"))
		{
			this.ShowPrevious();
		}
		else if (GUILayout.Button("Next"))
		{
			this.ShowNext();
		}
		else if (GUILayout.RepeatButton("Rotate Clockwise"))
		{
			this.Rotate(45f);
		}
		else if (GUILayout.RepeatButton("Rotate Anticlockwise"))
		{
			this.Rotate(-45f);
		}
	}

	private void ShowNext()
	{
		if (this.currentSourceFileIndex < this.sourceFiles.Length - 1)
		{
			this.currentSourceFileIndex += 1;
			ShowSystem();
		}
	}

	private void ShowPrevious()
	{
		if (this.currentSourceFileIndex > 0)
		{
			this.currentSourceFileIndex -= 1;
			ShowSystem();
		}
	}

	private void Rotate(float amount)
	{
		Camera.main.transform.RotateAround(Vector3.zero, Vector3.up, amount * Time.deltaTime);
	}

	private void ShowSystem()
	{
		var asset = this.sourceFiles[this.currentSourceFileIndex];
		var source = asset.text;

		if (this.sourceGuiText != null)
			this.sourceGuiText.text = asset.name;

		if (this.rootGo != null)
		{
			GameObject.Destroy(this.rootGo);
		}

		this.rootGo = new GameObject("Architecture");

		try
		{
			BuildProductionSystem(source);
			BuildProductionConfiguration();

			var mesh = this.BuildMesh(this.shapeConfiguration);
			BuildGameObject(mesh);
		}
		catch (System.Exception e)
		{
			Debug.Log(e);
		}
	}
	
	void OnDrawGizmos()
	{
		if (Application.isPlaying && this.shapeConfiguration != null)
		{
			this.shapeConfiguration.RootNode.TraverseBreadthFirst(node => {
				var shapeNode = (ShapeNode)node;
				
				if (node.IsLeaf)
				{
					Gizmos.color = Color.white;
				}
				else
                {
                    Gizmos.color = Color.grey;
                }
                
				var vol = shapeNode.Value.Volume;

				if (vol != null)
				{
					vol.DrawGizmos();
                }
            });
        }
    }
	    
    private void BuildProductionSystem(string sourceFile)
	{
		var builder = new IronyShapeProductionSystemBuilder();
		this.system = builder.Build(sourceFile);
		this.system.Axiom = "root";
		
		foreach (var item in this.system.Rules)
		{
			Debug.Log(string.Format("RULE: {0} = {1}", item.Key, item.Value));
		}
	}

	private void BuildProductionConfiguration()
	{
		Debug.Log("======= Building System ========");

		var beige = new Color(208f/255f, 197f/255f, 133f/255f);
		var grey = new Color(110f/255f, 110f/255f, 110f/255f);

		var styles = new Dictionary<string, IDictionary<string, object>> {
			{ "facade", new Dictionary<string, object> { 
					{ "face-color", grey }
				} 
			},
			{ "roof", new Dictionary<string, object> { 
					{ "top-color", new Color(255f/255f, 195f/255f, 0) },
					{ "side-color", grey }
				}
			}
		};

		var styleConfig = new StyleConfig(styles);
		
		this.shapeConfiguration = new ShapeConfiguration(this.system.Rules, styleConfig);
		this.system.Run(this.shapeConfiguration, new List<string> { "2", "3", "1" });
		
		Debug.Log("======= Finished Building System ========");
	}

	private Mesh BuildMesh(IShapeConfiguration configuration)
	{
		var meshBuilder = new MeshBuilder();

		configuration.RootNode.TraverseBreadthFirst(node => {
			if (node.IsLeaf)
			{
				var shapeNode = (ShapeNode)node;
				var vol = shapeNode.Value.Volume;
				if (vol != null)
				{
					vol.BuildMesh(meshBuilder);
				}
			}
		});

		var mesh = meshBuilder.BuildMesh();
		
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		mesh.Optimize();

		return mesh;
	}

	private void BuildGameObject(Mesh mesh)
	{
		var go = new GameObject();
		go.transform.parent = this.rootGo.transform;
		var meshFilter = go.AddComponent<MeshFilter>();
		var meshRenderer = go.AddComponent<MeshRenderer>();
		meshFilter.sharedMesh = mesh;
		meshRenderer.material = this.material;
	}
}