using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Editor/ABConfig",menuName = "CreateABConfig",order = 0)]
public class ABConfig : ScriptableObject
{
    [Tooltip("保证命名唯一性")]
    public List<string> m_AllPrefabPath = new List<string>();
    public List<FileDriABName> m_AllFileDirAB= new List<FileDriABName>();

    [Serializable]
    public struct FileDriABName
    {
        public string ABName;
        public string Path;
    }
}
