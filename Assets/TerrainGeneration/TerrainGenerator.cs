using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class TerrainGenerator : MonoBehaviour {

    public bool isMouseOver = false;

	public ToolType ToolType = ToolType.Normal;
	public float ToolSize = 25f;
	public float ToolStrength = 250f;
	public Color ToolColor = Color.white;
	
	public Vector3 MousePosition = Vector3.zero;

	public TerrainSystem TerrainSystem = null;

	//Terrain Generator
	public int Seed = 13;
	public float Scale = 10f;
	public int Octaves = 10;
	public float Persistance = 0.25f;
	public float Lacunarity = 3.0f;
	public float FalloffStrength = 1.0f;
	public float FalloffRamp = 3.0f;
	public float FalloffRange = 2.0f;
	public Vector2 Offset = Vector2.zero;
	public float HeightMultiplier = 50f;
    public AnimationCurve HeightCurve = new AnimationCurve(new Keyframe[] {
        new Keyframe(0,0,0,0,0,0),
        new Keyframe(1, 1, 2, 2, 0, 0)
    });
    
    [MenuItem("GameObject/3D Object/Low Poly Terrain")]
    private static void CreateTerrainObject()
    {
        GameObject go = new GameObject("Terrain Generator");
        go.transform.position = Vector3.zero;
        go.AddComponent<TerrainGenerator>();
    }

    void OnDrawGizmosSelected() {
        if (isMouseOver)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.75f);
            Gizmos.DrawSphere(MousePosition, ToolSize);
        }
    }

	void Awake() {
		if(TerrainSystem != null && !TerrainSystem.isInitialized) {
			TerrainSystem.Initialise(gameObject.GetComponent<TerrainGenerator>());
		}
	}

	void Update() {
        if (TerrainSystem != null && !TerrainSystem.isInitialized)
        {
            TerrainSystem.Initialise(gameObject.GetComponent<TerrainGenerator>());
        }
        if (TerrainSystem != null)
            TerrainSystem.Update();
	}

	void OnDestroy() {
		#if UNITY_EDITOR
		if((EditorApplication.isPlayingOrWillChangePlaymode || !Application.isPlaying) && (!EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)) {
			DestroyImmediate(TerrainSystem.Terrain.gameObject);
		}
		#endif
	}

}