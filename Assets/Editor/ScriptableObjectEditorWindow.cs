using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.WSA;


public class ScriptableObjectEditorWindow : EditorWindow
{
    private Rect headerSection;
    private Rect bodySection;
    private Rect footerSection;

    private Vector2 scrollPos;

    private int itemToRemove = -1;

    enum VariableEnum
    {
        Integer,
        Float,
        Double,
        Boolean,
        Character,
        String 
    };

    enum AccessModifierEnum
    {
        Public,
        Private,
        Protected,
        Internal
    };

    private class Variable
    {
        public AccessModifierEnum accessModifier;
        public bool inspectorEditable;
        public string name = "Enter name...";
        public VariableEnum type;
        public int intVal;
        public float floatVal;
        public double doubleVal;
        public bool boolVal;
        public char charVal = '0';
        public string stringVal = "Enter text...";
    }

    private readonly List<Variable> variables = new List<Variable>();


    /// <summary>
    /// Opens the Scriptable Object Editor Window. 
    /// </summary>
    [MenuItem("Window/Scriptable Object Editor")]
    static void OpenWindow()
    {
        ScriptableObjectEditorWindow window = (ScriptableObjectEditorWindow) GetWindow(typeof(ScriptableObjectEditorWindow));
        window.minSize = new Vector2(600,300);
        window.titleContent = new GUIContent("Scriptable Object Editor");
        window.Show();
    }

    private void OnGUI()
    {
        DrawLayouts();
        DrawHeader();
        DrawBody();
        DrawFooter();
    }

    /// <summary>
    /// Sets the region boundaries.
    /// </summary>
    private void DrawLayouts()
    {
        headerSection = new Rect(10, 10, position.width - 20, 40);

        bodySection = new Rect(10, 50, position.width - 20, position.height - 100);

        footerSection = new Rect(10, position.height - 50, position.width - 20, 40);
    }

    /// <summary>
    /// Draws the header region, including the Scriptable Object naming TextField.
    /// </summary>
    private void DrawHeader()
    {
        GUILayout.BeginArea(headerSection);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add Variable", GUILayout.MaxWidth(150)))
        {
            variables.Add(new Variable());
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    /// <summary>
    /// Draws the body region, including the variable naming, access modifier, variable type and base value fields.
    /// </summary>
    private void DrawBody()
    {
        GUILayout.BeginArea(bodySection);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var item in variables)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();

            GUILayout.Label("Variable Name");
            item.name = EditorGUILayout.TextField(item.name);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();
            
            GUILayout.Label("Access Modifier");
            item.accessModifier = (AccessModifierEnum)EditorGUILayout.EnumPopup(item.accessModifier, GUILayout.MaxWidth(200));

            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();

            if (item.accessModifier != AccessModifierEnum.Public)
            {
                EditorGUILayout.BeginVertical();

                GUILayout.Label("Inspector Editable");
                item.inspectorEditable = EditorGUILayout.Toggle(item.inspectorEditable, GUILayout.MaxWidth(200));

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();

                
            }

            EditorGUILayout.BeginVertical();

            GUILayout.Label("Variable type");
            item.type = (VariableEnum) EditorGUILayout.EnumPopup(item.type, GUILayout.MaxWidth(200));

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();
            GUILayout.Label("Base value");
            switch (item.type)
            {
                case VariableEnum.Integer:
                    item.intVal = EditorGUILayout.IntField(item.intVal, GUILayout.MaxWidth(200));
                    break;
                case VariableEnum.Float:
                    item.floatVal = EditorGUILayout.FloatField(item.floatVal, GUILayout.MaxWidth(200));
                    break;
                case VariableEnum.Double:
                    item.doubleVal = EditorGUILayout.DoubleField(item.doubleVal, GUILayout.MaxWidth(200));
                    break;
                case VariableEnum.Boolean:
                    item.boolVal = EditorGUILayout.Toggle(item.boolVal, GUILayout.MaxWidth(200));
                    break;
                case VariableEnum.Character:
                    item.charVal = EditorGUILayout.TextField(item.charVal.ToString(), GUILayout.MaxWidth(200)).ToCharArray()[0];
                    break;
                case VariableEnum.String:
                    item.stringVal = EditorGUILayout.TextArea(item.stringVal, GUILayout.MaxWidth(200));
                    break;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();

            GUILayout.Label("");

            if (GUILayout.Button("Remove"))
            {
                itemToRemove = variables.IndexOf(item);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        if (itemToRemove >= 0)
        {
            variables.RemoveAt(itemToRemove);
            itemToRemove = -1;
        }

        

        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }
    /// <summary>
    /// Draws the footer, including the Save- and Clear-buttons.
    /// </summary>
    void DrawFooter()
    {
        GUILayout.BeginArea(footerSection);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Save", GUILayout.MaxWidth(150)))
        {
            CreateScriptableObject(variables);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Clear", GUILayout.MaxWidth(150)))
        {
            ResetVariables();
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    /// <summary>
    /// Clears the variable list in the Editor Window.
    /// </summary>
    private void ResetVariables()
    {
        if (EditorUtility.DisplayDialog("Clear Variables?",
            "Are you sure you want to clear all the variables from your Scriptable Object?", "Ok", "Cancel"))
        {
            variables.Clear();
        }
    }

    /// <summary>
    /// Creates a C#-file from the list of variables and saves it in a ScriptableObjects-folder that is created in the Assets-folder.
    /// </summary>
    /// <param name="vars">List of variables from the Editor Window</param>
    /// <param name="objectName">The name to be given to the file</param>
    private static void CreateScriptableObject(List<Variable> vars)
    {

        //Remove whitespace and dash
        string name = "NewScriptableObject.cs";
        var copyPath = EditorUtility.SaveFilePanel(
        "Save Scriptable Object C# base file",
        UnityEngine.Application.dataPath,
        name,
        "cs");

        name = Path.GetFileNameWithoutExtension(copyPath);

        if (copyPath.Length != 0 && File.Exists(copyPath) == false ){ //Do not overwrite
                using (StreamWriter outfile = 
                    new StreamWriter(copyPath))
                {
                    outfile.WriteLine("using System.Collections;");
                    outfile.WriteLine("using System.Collections.Generic;");
                    outfile.WriteLine("using UnityEngine;");
                    outfile.WriteLine("");
                    outfile.WriteLine("[CreateAssetMenu(fileName = \"New " + name + "\", menuName = \"" + name + "\")]");
                    outfile.WriteLine("public class "+name+" : ScriptableObject {");
                    outfile.WriteLine(" ");

                    //Check the variable type and whether it should be editable in the inspector (if not public)
                    foreach (var item in vars)
                    {
                        switch (item.type)
                        {
                            case VariableEnum.Integer:
                                if (item.inspectorEditable)
                                {
                                    outfile.WriteLine("\t[SerializeField]");
                                }
                                outfile.WriteLine("\t" + item.accessModifier.ToString().ToLower() + " int " + item.name + " = " + item.intVal + ";");
                                outfile.WriteLine("");
                                break;

                            case VariableEnum.Float:
                                if (item.inspectorEditable)
                                {
                                    outfile.WriteLine("[SerializeField]");
                                }
                                outfile.WriteLine("\t" + item.accessModifier.ToString().ToLower() + " float " + item.name + " = " + item.floatVal + "f;");
                                outfile.WriteLine("");
                                break;

                            case VariableEnum.Double:
                                if (item.inspectorEditable)
                                {
                                    outfile.WriteLine("\t[SerializeField]");
                                }
                                outfile.WriteLine("\t" + item.accessModifier.ToString().ToLower() + " double " + item.name + " = " + item.doubleVal + ";");
                                outfile.WriteLine("");
                                break;

                            case VariableEnum.Boolean:
                                if (item.inspectorEditable)
                                {
                                    outfile.WriteLine("\t[SerializeField]");
                                }
                                outfile.WriteLine("\t" + item.accessModifier.ToString().ToLower() + " bool " + item.name + " = " + item.boolVal.ToString().ToLower() + ";");
                                outfile.WriteLine("");
                                break;

                            case VariableEnum.Character:
                                if (item.inspectorEditable)
                                {
                                    outfile.WriteLine("\t[SerializeField]");
                                }
                                outfile.WriteLine("\t" + item.accessModifier.ToString().ToLower() + " char " + item.name + " = \'" + item.charVal + "\';");
                                outfile.WriteLine("");
                                break;

                            case VariableEnum.String:
                                if (item.inspectorEditable)
                                {
                                    outfile.WriteLine("\t[SerializeField]");
                                }
                                outfile.WriteLine("\t" + item.accessModifier.ToString().ToLower() + " string " + item.name + " = \"" + item.stringVal + "\";");
                                outfile.WriteLine("");
                                break;
                        }
                    }
                    outfile.WriteLine("}");
                }
            }
            AssetDatabase.Refresh();
    }
}




