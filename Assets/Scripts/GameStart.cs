using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using 君莫笑;

public class GameStart : MonoBehaviour
{
    private AudioClip clip;
    public AudioSource source;
    void Awake()
    {
        bool iscuccess=AssetBundleManager.Instance.LoadAssetBundleConfig();
        Debug.Log($"ab资源依赖配置表是否加载成功:{iscuccess}");
    }

    void Start()
    {
        clip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/GameData/Sounds/senlin.mp3");
        source.clip = clip;
        source.Play();

    }

    void Update()
    {
        //return;
        if (Input.GetKeyDown(KeyCode.A))
        {
            source.Stop();
            source.clip = null;
            ResourceManager.Instance.ReleaseResource(clip, true);
        }
    }
}
