using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{

    private TerrainGenerator Target;

    private bool LeftMouseDown = false;
    private RaycastHit Hit;

    private System.DateTime Time;
    private float DeltaTime;

    private Vector2i resolution;

    private Biome[] oldBiomes;

    void Awake()
    {
        Target = (TerrainGenerator)target;
        if (Target.terrainSystem != null)
            resolution = Target.terrainSystem.resolution;
    }

    void OnEnable()
    {
        Tools.hidden = true;
    }

    void OnDisable()
    {
        Tools.hidden = false;
    }

    void OnSceneGUI()
    {
        DeltaTime = Mathf.Min(0.01f, (float)(System.DateTime.Now - Time).Duration().TotalSeconds); //100Hz Baseline
        Time = System.DateTime.Now;
        if (Event.current.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(0);
        }
        DrawCursor();
        HandleInteraction();
    }

    public override void OnInspectorGUI()
    {
        Undo.RecordObject(Target, Target.name);
        Inspect();
        if (GUI.changed)
        {
            EditorUtility.SetDirty(Target);
            if (oldBiomes != Target.terrainSystem.biomes)
            {
                oldBiomes = Target.terrainSystem.biomes;
                Target.terrainSystem.SetColorMap(Target.terrainSystem.CreateColorMap());
            }
        }
    }

    private void Inspect()
    {
        InspectWorld(Target.terrainSystem);
        InspectTerrain();
        InspectTools();
        if (GUILayout.Button("Reset"))
        {
            Target.terrainSystem.Reinitialise();
        }
    }

    private void InspectWorld(TerrainSystem terrainSystem)
    {
        if (terrainSystem == null)
        {
            return;
        }
        using (new EditorGUILayout.VerticalScope("Button"))
        {
            GUI.backgroundColor = Color.white;
            EditorGUILayout.HelpBox("World", MessageType.None);

            terrainSystem.SetSize(EditorGUILayout.Vector2Field("Size", terrainSystem.size));
            Vector2 _resolution = EditorGUILayout.Vector2Field("Resolution", new Vector2(resolution.x, resolution.y));
            resolution = new Vector2i((int)_resolution.x, (int)_resolution.y);
            if (resolution.x != terrainSystem.resolution.x || resolution.y != terrainSystem.resolution.y)
            {
                EditorGUILayout.HelpBox("Changing the resolution will reset the world.", MessageType.Warning);
                if (GUILayout.Button("Apply"))
                {
                    terrainSystem.SetResolution(resolution);
                }
            }
        }
    }

    private void InspectTerrain()
    {
        using (new EditorGUILayout.VerticalScope("Button"))
        {
            GUI.backgroundColor = Color.white;
            EditorGUILayout.HelpBox("Terrain", MessageType.None);

            Target.Seed = EditorGUILayout.IntField("Seed", Target.Seed);
            Target.Scale = EditorGUILayout.FloatField("Scale", Target.Scale);
            Target.Octaves = EditorGUILayout.IntField("Octaves", Target.Octaves);
            Target.Persistance = EditorGUILayout.FloatField("Persistance", Target.Persistance);
            Target.Lacunarity = EditorGUILayout.FloatField("Lacunarity", Target.Lacunarity);
            Target.FalloffStrength = EditorGUILayout.FloatField("FalloffStrength", Target.FalloffStrength);
            Target.FalloffRamp = EditorGUILayout.FloatField("FalloffRamp", Target.FalloffRamp);
            Target.FalloffRange = EditorGUILayout.FloatField("FalloffRange", Target.FalloffRange);
            Target.Offset = EditorGUILayout.Vector2Field("Offset", Target.Offset);
            Target.HeightMultiplier = EditorGUILayout.FloatField("HeightMultiplier", Target.HeightMultiplier);
            Target.HeightCurve = EditorGUILayout.CurveField("HeightCurve", Target.HeightCurve);
            Target.terrainSystem = (TerrainSystem)EditorGUILayout.ObjectField("TerrainSystem", Target.terrainSystem, typeof(TerrainSystem), true, null);

            if (Target.terrainSystem == null)
                return;

            using (new EditorGUILayout.VerticalScope("Button"))
            {
                GUI.backgroundColor = Color.white;
                EditorGUILayout.HelpBox("Biomes", MessageType.None);

                Target.terrainSystem.interpolation = EditorGUILayout.Slider("Interpolation", Target.terrainSystem.interpolation, 0f, 1f);
                Target.terrainSystem.filterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", Target.terrainSystem.filterMode);
                for (int i = 0; i < Target.terrainSystem.biomes.Length; i++)
                {
                    Target.terrainSystem.biomes[i].color = EditorGUILayout.ColorField(Target.terrainSystem.biomes[i].color);
                    float start = Target.terrainSystem.biomes[i].startHeight;
                    float end = Target.terrainSystem.biomes[i].endHeight;
                    EditorGUILayout.MinMaxSlider(ref start, ref end, 0f, 1f);
                    Target.terrainSystem.SetBiomeStartHeight(i, start);
                    Target.terrainSystem.SetBiomeEndHeight(i, end);
                }

                if (GUILayout.Button("Add Biome"))
                {
                    System.Array.Resize(ref Target.terrainSystem.biomes, Target.terrainSystem.biomes.Length + 1);
                    Target.terrainSystem.biomes[Target.terrainSystem.biomes.Length - 1] = new Biome();
                }
                if (GUILayout.Button("Remove Biome"))
                {
                    if (Target.terrainSystem.biomes.Length > 0)
                    {
                        System.Array.Resize(ref Target.terrainSystem.biomes, Target.terrainSystem.biomes.Length - 1);
                    }
                }
                if (GUILayout.Button("Generate Biomes"))
                {
                    if (Target.terrainSystem.biomes.Length > 0)
                    {
                        Target.terrainSystem.SetColorMap(Target.terrainSystem.CreateColorMap());
                    }
                }
            }
            GUILayout.Space(10);
            if (GUILayout.Button("Generate"))
            {
                Target.terrainSystem.SetHeightMap(Target.terrainSystem.CreateHeightMap(
                    Target.Seed, Target.Scale, Target.Octaves, Target.Persistance, Target.Lacunarity, Target.FalloffStrength, Target.FalloffRamp, Target.FalloffRange, Target.Offset, Target.HeightMultiplier, Target.HeightCurve
                ));
                Target.terrainSystem.SetColorMap(Target.terrainSystem.CreateColorMap());
            }
            GUILayout.Space(2);
        }
    }

    private void InspectTools()
    {
        using (new EditorGUILayout.VerticalScope("Button"))
        {
            GUI.backgroundColor = Color.white;
            EditorGUILayout.HelpBox("Tools", MessageType.None);

            Target.ToolType = (ToolType)EditorGUILayout.EnumPopup("Type", Target.ToolType);
            Target.ToolSize = EditorGUILayout.FloatField("Size", Target.ToolSize);
            Target.ToolStrength = EditorGUILayout.FloatField("Strength", Target.ToolStrength);
            if (Target.ToolType == ToolType.Brush)
                Target.ToolColor = EditorGUILayout.ColorField("Color", Target.ToolColor);
        }
    }

    private void DrawCursor()
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y));
        Target.isMouseOver = Physics.Raycast(ray.origin, ray.direction, out Hit);
        Target.MousePosition = Hit.point;
        EditorUtility.SetDirty(target);
    }

    private void HandleInteraction()
    {
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            LeftMouseDown = true;
        }
        if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
        {
            LeftMouseDown = false;
        }
        if (LeftMouseDown && Target.isMouseOver)
        {
            if (Target.ToolType == ToolType.Brush)
            {
                Target.terrainSystem.ModifyTexture(new Vector2(Target.MousePosition.x, Target.MousePosition.z), Target.ToolSize, Target.ToolStrength * DeltaTime, Target.ToolColor);
            }
            else
            {
                Target.terrainSystem.ModifyTerrain(new Vector2(Target.MousePosition.x, Target.MousePosition.z), Target.ToolSize, Target.ToolStrength * DeltaTime, Target.ToolType);
            }
        }
    }

}
