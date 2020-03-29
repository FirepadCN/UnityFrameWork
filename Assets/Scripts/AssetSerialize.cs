using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "AssetSerialize", menuName = "CreateAsset")]
public class AssetSerialize : ScriptableObject
{

    public int Id;
    public string Name;
    public List<int> List;
}
