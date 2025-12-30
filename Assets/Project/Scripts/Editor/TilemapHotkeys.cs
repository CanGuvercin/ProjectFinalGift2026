using UnityEditor;
using UnityEngine;

public static class TilemapHotkeys
{
    private static void Select(string name)
    {
        var obj = GameObject.Find(name);
        if (obj != null)
        {
            Selection.activeGameObject = obj;
            SceneView.lastActiveSceneView?.FrameSelected();
        }
        else
        {
            Debug.LogWarning($"Tilemap not found: {name}");
        }
    }

    [MenuItem("Tilemaps/Select Ground _1")]
    private static void Ground() => Select("TM_Ground");

    [MenuItem("Tilemaps/Select Path _2")]
    private static void Path() => Select("TM_Path");

    [MenuItem("Tilemaps/Select Walls _3")]
    private static void Walls() => Select("TM_Walls");

    [MenuItem("Tilemaps/Select Deco Low _4")]
    private static void DecoLow() => Select("TM_Deco_Low");

    [MenuItem("Tilemaps/Select Deco High _5")]
    private static void DecoHigh() => Select("TM_Deco_High");

    [MenuItem("Tilemaps/Select Collision _6")]
    private static void Collision() => Select("TM_Collision");
}
