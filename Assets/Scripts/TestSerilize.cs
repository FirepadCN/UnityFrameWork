using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
[SerializeField]
[Serializable]//使用二进制序列化时需添加该标签，表示该类可被序列化为二进制
public class TestSerilize 
{
    [XmlAttribute("Id")]
    public int Id { get; set; }

    [XmlAttribute("Name")]
    public string Name { get; set; }

    [XmlElement("List")]
    public List<int> List { get; set; }

    public override string ToString()
    {
        StringBuilder list=new StringBuilder();
        for (int i = 0; i < List.Count; i++)
        {
            list.Append(List[i]+",") ;
        }

        return $"id:{Id},name:{Name},list:{list}";
    }
}
