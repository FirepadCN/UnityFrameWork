namespace 君莫笑
{
    public class LoadingUi : Window
    {
        private LoadingPanel m_MainPanel;
        private string m_SceneName;

        public override void Awake(params object[] paraList)
        {
            m_MainPanel = GameObject.GetComponent<LoadingPanel>();
            m_SceneName = (string)paraList[0];
        }

        public override void OnUpdate()
        {
            if (m_MainPanel == null)
                return;

            m_MainPanel.m_Slider.value = GameMapManager.LoadingProgress/100.0f;
            m_MainPanel.m_Text.text = $"{GameMapManager.LoadingProgress}%";
            if (GameMapManager.LoadingProgress >= 100)
            {
                LoadOtherScene();
            }
        }

        /// <summary>
        /// 加载对应场景第一个UI
        /// </summary>
        private void LoadOtherScene()
        {
            //根据场景名字打开对应场景第一个界面
            if (m_SceneName == ConStr.MENUSCENE)
            {
                UIManager.Instance.PopUpWnd(ConStr.MENUPANEL);
            }
            UIManager.Instance.CloseWnd(ConStr.LOADINGPANEL);
        }
    }
    
}
