using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using 君莫笑;
public class GameStart : MonoBehaviour
{
    private AudioClip clip;
    public AudioSource source;

    private GameObject obj;
    void Awake()
    {
        GameObject.DontDestroyOnLoad(gameObject);

        bool iscuccess = AssetBundleManager.Instance.LoadAssetBundleConfig();
        Debug.Log($"ab资源依赖配置表是否加载成功:{iscuccess}");
        ResourceManager.Instance.Init(this);

        ObjectManager.Instance.Init(transform.Find("RecyclePoolTrs"),transform.Find("SceneTrs"));
    }

    void Start()
    {
        //SynLoad();
        //Invoke("AsycnLoadTest", 1f);
       // ResourceManager.Instance.PreLoadRes("Assets/GameData/Sounds/senlin.mp3");

        ObjectManager.Instance.InstantiateObjectAsync("Assets/GameData/Prefabs/Attack.prefab",OnLoadFinishObj,LoadResPriority.RES_HIGHT,true);

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

    void OnLoadFinishObj(string path, Object obj, object param1 = null, object param2 = null, object param3 = null)
    {
        this.obj = obj as GameObject;
    }

    void Update()
    {
        //return;
        if (Input.GetKeyDown(KeyCode.A))
        {
            ObjectManager.Instance.ReleaseObject(obj);
            obj = null;
        }
        else if(Input.GetKeyDown(KeyCode.D))
        {
            ObjectManager.Instance.InstantiateObjectAsync("Assets/GameData/Prefabs/Attack.prefab",OnLoadFinishObj,LoadResPriority.RES_HIGHT,true);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            ObjectManager.Instance.ReleaseObject(obj,0,true);
            obj = null;
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
