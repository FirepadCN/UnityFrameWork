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
        bool iscuccess = AssetBundleManager.Instance.LoadAssetBundleConfig();
        Debug.Log($"ab资源依赖配置表是否加载成功:{iscuccess}");
        ResourceManager.Instance.Init(this);
    }

    void Start()
    {
        //SynLoad();
        //Invoke("AsycnLoadTest", 1f);
       // ResourceManager.Instance.PreLoadRes("Assets/GameData/Sounds/senlin.mp3");
    }

    private void SynLoad()
    {
        clip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/GameData/Sounds/senlin.mp3");
        source.clip = clip;
        source.Play();
    }

    public void AsycnLoadTest()
    {
        ResourceManager.Instance.AsynLoadResource("Assets/GameData/Sounds/menusound.mp3", OnLoadFinish,LoadResPriority.RES_MIDDLE);
    }

    void OnLoadFinish(string path, Object obj, object p1, object p2, object p3)
    {
        clip = obj as AudioClip;
        source.clip = clip;
        source.Play();
    }

    void Update()
    {
        //return;
        if (Input.GetKeyDown(KeyCode.A))
        {
            source.Stop();
            ResourceManager.Instance.ReleaseResource(clip,true);
            source.clip = null;
            clip = null;
        }

        //TEST:预加载
        if (Input.GetKeyDown(KeyCode.C))
        {
            source.Stop();
            clip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/GameData/Sounds/senlin.mp3");
            source.clip = clip;
            source.Play();
        }
    }

    private void OnApplicaitonQuit()
    {
#if UNITY_EDITOR
        ResourceManager.Instance.ClearCache();
        Resources.UnloadUnusedAssets();
        Debug.Log("END Game");
#endif
    }
}
