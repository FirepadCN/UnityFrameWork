using UnityEngine;

namespace 君莫笑.UGUI
{
    public class MenuUI : Window
    {
        private MenuPanel m_MenuPanel;

        public override void Awake(params object[] paralist)
        {
            m_MenuPanel = GameObject.GetComponent<MenuPanel>();
            AddButtonClickListener(m_MenuPanel.m_StartBtn, OnClickStart);
            AddButtonClickListener(m_MenuPanel.m_LoadBtn, OnClickLoad);
            AddButtonClickListener(m_MenuPanel.m_ExitBtn, OnClickExit);
        }

        private void OnClickExit()
        {
            Debug.Log("StartBtn onClick");
        }

        private void OnClickLoad()
        {
            Debug.Log("LoadBtn onClick");
            
        }

        private void OnClickStart()
        {
            Debug.Log("ExitBtn onClick");
            
        }
    }
}