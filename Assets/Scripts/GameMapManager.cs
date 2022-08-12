using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

namespace 君莫笑
{
    public class GameMapManager : Singleton<GameMapManager>
    {
        protected GameMapManager(){}
        //加载场景完成回调
        public Action LoadSceneOverCallBack;

        //加载场景开始回调
        public Action LoadSceneEnterCallBack;

        //当前场景名
        public string CurrentMapName { get; set; }

        //场景加载是否完成
        public bool AlreadyLoadScene { get; set; }

        //切换场景进度条
        public static int LoadingProgress = 0;

        private MonoBehaviour m_Mono;

        /// <summary>
        /// 场景管理初始化
        /// </summary>
        /// <param name="mono"></param>
        public void Init(MonoBehaviour mono)
        {
            m_Mono = mono;
        }

        /// <summary>
        /// 加载场景
        /// </summary>
        /// <param name="name">场景名</param>
        public void LoadScene(string name)
        {
            LoadingProgress = 0;
            m_Mono.StartCoroutine(LoadSceneAsync(name));
            UIManager.Instance.PopUpWnd(ConStr.LOADINGPANEL, true, name);
        }

        /// <summary>
        /// 设置场景环境
        /// </summary>
        /// <param name="name"></param>
        void SetSceneSetting(string name)
        {
            //设置各种场景环境，可以根据配表来,TODO:
        }

        /// <summary>
        /// 异步加载场景，平滑进度条
        /// </summary>
        IEnumerator LoadSceneAsync(string sceneName)
        {
            LoadSceneEnterCallBack?.Invoke();

            ClearCache();
            AlreadyLoadScene = false;
            AsyncOperation unLoadScene = SceneManager.LoadSceneAsync(ConStr.EMPTYSCENE, LoadSceneMode.Single);
            while (unLoadScene != null && !unLoadScene.isDone)
            {
                yield return new WaitForEndOfFrame();
            }

            LoadingProgress = 0;
            int targetProgress = 0;
            AsyncOperation asyncScene = SceneManager.LoadSceneAsync(sceneName);
            if (asyncScene != null && !asyncScene.isDone)
            {
                asyncScene.allowSceneActivation = false;
                //把allowSceneActivation设置为false后，Unity就只会加载场景到90%，剩下的10%要等到allowSceneActivation设置为true后才加载
                while (asyncScene.progress < 0.9f)
                {
                    targetProgress = (int) asyncScene.progress * 100;
                    yield return new WaitForEndOfFrame();
                    //平滑过渡
                    while (LoadingProgress < targetProgress)
                    {
                        ++LoadingProgress;
                        yield return new WaitForEndOfFrame();
                    }
                }

                CurrentMapName = sceneName;
                SetSceneSetting(sceneName);
                //自行加载剩余的10%
                targetProgress = 100;
                while (LoadingProgress < targetProgress - 2)
                {
                    ++LoadingProgress;
                    yield return new WaitForEndOfFrame();
                }

                LoadingProgress = 100;
                asyncScene.allowSceneActivation = true;
                AlreadyLoadScene = true;

                LoadSceneOverCallBack?.Invoke();
            }
        }

        /// <summary>
        /// 跳场景需要清除的东西
        /// </summary>
        private void ClearCache()
        {
            ObjectManager.Instance.ClearCache();
            ResourceManager.Instance.ClearCache();
        }
    }
}