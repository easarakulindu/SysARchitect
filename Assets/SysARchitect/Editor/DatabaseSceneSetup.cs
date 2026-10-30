using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SysARchitect.Simulation;
using SysARchitect.UI;
using static SysARchitect.Editor.DesktopSceneSetup;

namespace SysARchitect.Editor
{
    public static class DatabaseSceneSetup
    {
        public static void Configure()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var canvas = GameObject.Find("Workspace UI");
            if (canvas.GetComponent<DatabaseUI>() != null)
            {
                canvas.transform.Find("Database Explorer/Record key").GetComponent<TMP_InputField>().characterLimit = 11;
                EditorSceneManager.SaveScene(scene);
                return;
            }
            var ui = canvas.AddComponent<DatabaseUI>();
            var panel = Panel(canvas.transform, "Database Explorer", 220, 410, 760, 270);
            Set(ui, "inspector", canvas.GetComponent<InspectorUI>());
            Set(ui, "simulation", UnityEngine.Object.FindFirstObjectByType<SimulationController>());
            Set(ui, "panel", panel.gameObject);
            Label(panel, "DATABASE EXPLORER", 12, 8, 736, 26, 18);
            Set(ui, "expandButton", Button(panel, "Expand / Collapse", 12, 38, 110, 32));
            Set(ui, "singleButton", Button(panel, "Single", 130, 38, 85, 32));
            Set(ui, "replicationButton", Button(panel, "Replication", 223, 38, 125, 32));
            Set(ui, "addReplicaButton", Button(panel, "Add replica", 356, 38, 110, 32));
            Set(ui, "shardingButton", Button(panel, "Sharding", 474, 38, 100, 32));
            Set(ui, "fewerShardsButton", Button(panel, "- shard", 582, 38, 78, 32));
            Set(ui, "moreShardsButton", Button(panel, "+ shard", 668, 38, 80, 32));
            Set(ui, "readButton", Button(panel, "Read", 12, 88, 80, 32));
            Set(ui, "writeButton", Button(panel, "Write", 100, 88, 80, 32));
            Label(panel, "Record key", 188, 71, 76, 16, 11);
            var inputRect = Panel(panel, "Record key", 188, 88, 76, 32);
            var input = inputRect.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = inputRect.GetComponent<Image>();
            input.textViewport = inputRect;
            input.textComponent = Label(inputRect, "1", 6, 3, 64, 26, 14);
            input.characterLimit = 11;
            Set(ui, "keyInput", input);
            Label(panel, "Choose primary / replica / shard", 280, 75, 204, 18, 11);
            Set(ui, "memberSlider", Slider(panel, 280, 98, 200, 18));
            Set(ui, "failMemberButton", Button(panel, "Fail member", 492, 88, 120, 32));
            Set(ui, "recoverMemberButton", Button(panel, "Recover member", 620, 88, 128, 32));
            Set(ui, "detailsText", Label(panel, "Database details", 12, 132, 736, 128, 12));
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
    }
}
