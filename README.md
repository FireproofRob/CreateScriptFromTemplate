# CreateScriptFromTemplate

A Unity Editor utility providing a more flexible system for creating new scripts (or any other sort of text file) than Unity's built-in "Create->C# Script" option.

##Usage
Copy into an Editor folder somewhere in your Unity project. Use it by right-clicking somewhere in your project view and selecting "Create->Script From Template...". That'll open a dialogue box which allows you which template you want to use.

There's a few standard templates defined (which you should modify to suit your needs).

You can easily create new templates by duplicating the existing ones. Templates can live in any folder called "ScriptTemplates", and must have an extension .fpst. At Fireproof, we have a bunch of them inside our Common folder (which is shared between projects), and some specific to each project.

###Example Template
Here's an example template:

```C#
&&MenuName=C#/Editor/CustomInspector&&
&&Priority=2&&
//
// ##ClassName##.cs
// Copyright (c) ##Year## Fireproof Studios, All Rights Reserved
//

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(##EditorTargetClass##))]
public class ##ClassName## : Editor 
{
    public override void OnInspectorGUI()
    {
        ##EditorTargetClass## myTarget = (##EditorTargetClass##)target;
        
        DrawDefaultInspector();
    }
}
```

There's two sorts of special tokens:
    "&&" Is a special token to tell the system how the template should be presented. They're stripped out of the final script.
    "##" Is something that should be replaced in the script by something that the user will specify in the UI

Let's see how those are used in the example:
* `&&MenuName=C#/Editor/CustomInspector&&` Tells the system where to put the template in the dialogue box's dropdown of templates
* `&&Priority=1&&` Priority can be used to force often-used options to the top of the list (We use this for Monobehaviours)
* `##ClassName##` Tells the system to show a "Classname" text-entry box in the dialogue. When the script is created, all instances of this will be replaced by whatever was entered. Note that Classname is a special case - the system uses this as the filename (combined with the &&EXTENSION&& property)
* `##Year##` This is a special replacement that the system knows about. It'll insert the current year without presenting it in the dialogue
* `##EditorTargetClass##` A "normal" replacement. The system knows nothing about what "EditorTargetClass" means, and will just blindly replace whatever the user enters into the dialogue.

You can define as many extra ##Token## things as you like. They'll all be presented in the UI.

The only other special token is "Extension". By default, the created script will use the ##ClassName## token with a ".cs" extension. If you want something else, put a like like `&&EXTENSION=.shader&&` at the top of the file.