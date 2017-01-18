using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Linq.Expressions;

public class CreateScriptFromTemplate : EditorWindow
{
    [MenuItem("Assets/Create/Script From Template...", false, 50)]
    public static void CreateEditorScript()
    {
        CreateScriptFromTemplate window = GetWindow<CreateScriptFromTemplate>();
        window.CreatePath = window.GetProjectBrowserPath();
        window.GatherTemplates();
    }

    private class TemplateInfo
    {
        public string MenuName;

        //Things where the position is specified in the file, and the
        //value is exposed to the UI (e.g. classname).
        //Formatted as ##key##
        public Dictionary<string, string> Replacements;

        //Things where the key and value are specified in the template.
        //e.g. the extension. 
        //Formatted as &&key=value&&
        public Dictionary<string, string> SpecialKeys;

        //The complete contents of the file
        public string WholeTemplate;

        public int Priority;
    }

    private Dictionary<string, TemplateInfo> Templates;

    private string[] MenuItems;

    private string CreatePath;


    private int Selected = 0;

    private string GetProjectBrowserPath()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path == "")
        {
            path = "Assets";
        }
        else if (Path.GetExtension(path) != "")
        {
            path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }

        return path;
    }

    void OnGUI()
    {
        if (Templates == null || MenuItems == null)
        {
            GatherTemplates();
        }

        TemplateInfo info = Templates[MenuItems[Selected]];

        EditorGUILayout.BeginVertical();
        Selected = EditorGUILayout.Popup("Template", Selected, MenuItems, GUILayout.Width(350));

        List<string> keys = new List<string>(info.Replacements.Keys);    //Dictionaries don't like it when you make changes in a loop, so cache the key names
        foreach (string key in keys)
        {
            if (key == "Year")
            {
                continue;
            }

            info.Replacements[key] = EditorGUILayout.TextField(key, info.Replacements[key], GUILayout.Width(350));
        }

        GUILayout.Label("Creating file " + CreatePath + "/" + info.Replacements["ClassName"] + info.SpecialKeys["EXTENSION"]);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Close"))
        {
            Close();
        }

        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(info.Replacements["ClassName"]));
        if (GUILayout.Button("OK"))
        {
            CreateScript(info);
            Close();
            Templates = null;
            MenuItems = null;
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

    }

    private void GatherTemplates()
    {
        List<string> paths = new List<string>();

        string[] dirs = Directory.GetDirectories(Application.dataPath, "ScriptTemplates", SearchOption.AllDirectories);

        foreach (string dir in dirs)
        {
            paths.AddRange(Directory.GetFiles(dir, "*.fpst"));
        }

        Templates = new Dictionary<string, TemplateInfo>();

        foreach (string template in paths)
        {
            TemplateInfo info = ParseTemplate(template);

            if (info != null)
            {
                Templates.Add(info.MenuName, info);
            }
        }

        //Collect the names to use in the GUI
        MenuItems = Templates.Keys.ToArray();

        //Sort by priority
        MenuItems = MenuItems.OrderByDescending(s => Templates[s].Priority).ToArray();
    }

    private TemplateInfo ParseTemplate(string path)
    {
        TemplateInfo temp = new TemplateInfo();

        string template = System.IO.File.ReadAllText(path);

        if (string.IsNullOrEmpty(template))
        {
            return null;
        }

        //Extract the SpecialKeys
        //https://www.debuggex.com/ is your friend
        string pattern = @"&&(\w+) *= *(.?[\w/# ]+)&&\n?";

        Regex reg = new Regex(pattern, RegexOptions.Multiline);
        Match match = reg.Match(template);

        temp.SpecialKeys = new Dictionary<string, string>();

        while (match.Success)
        {
            string key = match.Groups[1].Value.ToUpper();
            string value = match.Groups[2].Value;

            if (!temp.SpecialKeys.ContainsKey(key))
            {
                temp.SpecialKeys.Add(key, value);
            }

            //Remove from the file
            template = template.Replace(match.Groups[0].Value, "");

            match = match.NextMatch();
        }

        //Assume .cs if no extension is specified
        if (!temp.SpecialKeys.ContainsKey("EXTENSION"))
        {
            temp.SpecialKeys.Add("EXTENSION", ".cs");
        }

        //Extract the Replacements
        pattern = @"##(\w+)##";

        reg = new Regex(pattern, RegexOptions.Multiline);
        match = reg.Match(template);

        temp.Replacements = new Dictionary<string, string>();

        //Classname is used to name the file, so make sure it's present
        temp.Replacements.Add("ClassName", "");
        while (match.Success)
        {
            string key = match.Groups[1].Value;

            //Ignore the "Year". That's a special one that'll get filled in automatically
            if (!temp.Replacements.ContainsKey(key))
            {
                temp.Replacements.Add(key, "");

                //Fill in the year now (it's skipped in the GUI)
                if (key == "Year")
                {
                    temp.Replacements[key] = System.DateTime.Now.Year.ToString();
                }
            }

            match = match.NextMatch();
        }

        //If we didn't get a "MenuName" from the special keys, 
        //Automatically create it from the filename
        if (!temp.SpecialKeys.TryGetValue("MENUNAME", out temp.MenuName))
        {
            temp.MenuName = Path.GetFileNameWithoutExtension(path);
        }

        temp.Priority = 0;
        string priorityString;
        if (temp.SpecialKeys.TryGetValue("PRIORITY", out priorityString))
        {
            int priority;
            if (int.TryParse(priorityString, out priority))
            {
                temp.Priority = priority;
            }
        }
        

        //Store the whole Text
        temp.WholeTemplate = template;

        return temp;
    }

    private void CreateScript(TemplateInfo info)
    {
        //Just in case someone's put .cs at the end of classname, delete it
        string className = System.IO.Path.GetFileNameWithoutExtension(info.Replacements["ClassName"]);

        string template = info.WholeTemplate;

        string ext = info.SpecialKeys["EXTENSION"];

        foreach (KeyValuePair<string, string> pairs in info.Replacements)
        {
            template = template.Replace("##" + pairs.Key + "##", pairs.Value);
        }

        string finalPath = System.IO.Path.Combine(CreatePath, className + ext.ToLower());

        if (System.IO.File.Exists(finalPath))
        {
            Debug.LogError("File already exists: " + finalPath);
        }
        else
        {
            System.IO.File.WriteAllText(finalPath, template);
            AssetDatabase.Refresh();

            Object asset = AssetDatabase.LoadAssetAtPath(finalPath, typeof(TextAsset));
            Selection.activeObject = asset;
            AssetDatabase.OpenAsset(asset);
        }
    }
}
