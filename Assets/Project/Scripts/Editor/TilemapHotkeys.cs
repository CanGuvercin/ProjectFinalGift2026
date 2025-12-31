using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using System.Reflection;

public static class TilemapHotkeys
{
    // Layer seçimi - ZOOM YOK
    private static void Select(string name)
    {
        var obj = GameObject.Find(name);
        if (obj != null)
        {
            Selection.activeGameObject = obj;
            // Zoom yapma, sadece seç
        }
        else
        {
            Debug.LogWarning($"Tilemap not found: {name}");
        }
    }

    // Tool değiştirme için reflection
    private static void SetTilemapTool(string toolName)
    {
        try
        {
            var gridPaintingStateType = Type.GetType("UnityEditor.Tilemaps.GridPaintingState, UnityEditor.Tilemaps");
            if (gridPaintingStateType != null)
            {
                var activeBrushToolProperty = gridPaintingStateType.GetProperty("activeBrushTool", 
                    BindingFlags.Public | BindingFlags.Static);
                
                if (activeBrushToolProperty != null)
                {
                    var brushToolEnum = Type.GetType("UnityEditor.Tilemaps.GridBrushBase+Tool, UnityEditor.Tilemaps");
                    if (brushToolEnum != null)
                    {
                        object toolValue = Enum.Parse(brushToolEnum, toolName);
                        activeBrushToolProperty.SetValue(null, toolValue);
                        Debug.Log($"[TILEMAP] {toolName} tool activated");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TILEMAP] Could not activate tool: {e.Message}");
        }
    }

    // ================= LAYER SEÇİMİ (ALT + 1-6) - ZOOM YOK =================
    
    [MenuItem("Tilemaps/Select Ground &1")]
    private static void Ground() => Select("TM_Ground");

    [MenuItem("Tilemaps/Select Path &2")]
    private static void Path() => Select("TM_Path");

    [MenuItem("Tilemaps/Select Walls &3")]
    private static void Walls() => Select("TM_Walls");

    [MenuItem("Tilemaps/Select Deco Low &4")]
    private static void DecoLow() => Select("TM_Deco_Low");

    [MenuItem("Tilemaps/Select Deco High &5")]
    private static void DecoHigh() => Select("TM_Deco_High");

    [MenuItem("Tilemaps/Select Collision &6")]
    private static void Collision() => Select("TM_Collision");

    // ================= MANUEL ZOOM (CTRL + F) =================
    
    [MenuItem("Tilemaps/Focus on Selected %f")]
    private static void FocusSelected()
    {
        if (Selection.activeGameObject != null)
        {
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log($"[TILEMAP] Focused on: {Selection.activeGameObject.name}");
        }
    }

    // ================= TOOL DEĞİŞTİRME (ALT + HARF) =================
    
    [MenuItem("Tilemaps/Tool: Brush (Paint) &b")]
    private static void ActivateBrush() => SetTilemapTool("Paint");

    [MenuItem("Tilemaps/Tool: Eraser &e")]
    private static void ActivateEraser() => SetTilemapTool("Erase");

    [MenuItem("Tilemaps/Tool: Fill (Bucket) &f")]
    private static void ActivateFill() => SetTilemapTool("FloodFill");

    [MenuItem("Tilemaps/Tool: Pick (Eyedropper) &l")]
    private static void ActivatePick() => SetTilemapTool("Pick");

    [MenuItem("Tilemaps/Tool: Select (Box) &s")]
    private static void ActivateSelect() => SetTilemapTool("Select");

    [MenuItem("Tilemaps/Tool: Move &m")]
    private static void ActivateMove() => SetTilemapTool("Move");

    // ================= EK FONKSİYONLAR =================

    [MenuItem("Tilemaps/Clear Selected Tilemap &c")]
    private static void ClearTilemap()
    {
        if (Selection.activeGameObject != null)
        {
            Tilemap tilemap = Selection.activeGameObject.GetComponent<Tilemap>();
            if (tilemap != null)
            {
                if (EditorUtility.DisplayDialog("Clear Tilemap",
                    $"Are you sure you want to clear all tiles from {tilemap.name}?",
                    "Yes", "Cancel"))
                {
                    Undo.RecordObject(tilemap, "Clear Tilemap");
                    tilemap.ClearAllTiles();
                    Debug.Log($"[TILEMAP] Cleared: {tilemap.name}");
                }
            }
            else
            {
                Debug.LogWarning("[TILEMAP] Selected object is not a Tilemap!");
            }
        }
    }

    [MenuItem("Tilemaps/Refresh All Tilemaps &r")]
    private static void RefreshAllTilemaps()
    {
        Tilemap[] tilemaps = GameObject.FindObjectsOfType<Tilemap>();
        foreach (var tilemap in tilemaps)
        {
            tilemap.RefreshAllTiles();
        }
        Debug.Log($"[TILEMAP] Refreshed {tilemaps.Length} tilemaps");
    }

    // ================= VALIDATION =================

    [MenuItem("Tilemaps/Clear Selected Tilemap &c", true)]
    private static bool ValidateClearTilemap()
    {
        return Selection.activeGameObject != null &&
               Selection.activeGameObject.GetComponent<Tilemap>() != null;
    }
    
    [MenuItem("Tilemaps/Focus on Selected %f", true)]
    private static bool ValidateFocusSelected()
    {
        return Selection.activeGameObject != null;
    }
}