using UnityEngine;

namespace 君莫笑
{
    public class MenuUI : Window
    {
        private MenuPanel m_MenuPanel;
        private AudioClip m_Clip;
        public override void Awake(params object[] paralist)
        {
            m_MenuPanel = GameObject.GetComponent<MenuPanel>();
            AddButtonClickListener(m_MenuPanel.m_StartBtn, OnClickStart);
            AddButtonClickListener(m_MenuPanel.m_LoadBtn, OnClickLoad);
            AddButtonClickListener(m_MenuPanel.m_ExitBtn, OnClickExit);
            m_Clip = ResourceManager.Instance.LoadResource<AudioClip>(ConStr.MENUSOUND);
            m_MenuPanel.m_Audio.clip = m_Clip;
            m_MenuPanel.m_Audio.Play();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (Input.GetKeyDown(KeyCode.A))
            {
                ResourceManager.Instance.ReleaseResource(m_Clip, true);
                m_MenuPanel.m_Audio.clip = null;
                m_Clip = null;
            }
            
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