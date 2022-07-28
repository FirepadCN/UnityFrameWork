using UnityEngine;

namespace 君莫笑
{
    public class EffectOfflineData : OfflineData
    {
        public ParticleSystem[] m_Particle;
        public TrailRenderer[] m_TrailRe;

        public override void ResetProp()
        {
            base.ResetProp();
            int particleCount = m_Particle.Length;
            for (int i = 0; i < particleCount; i++)
            {
                m_Particle[i].Clear(true);
                m_Particle[i].Play();
            }

            foreach (var trailRenderer in m_TrailRe)
            {
                trailRenderer.Clear();
            }
        }

        public override void BindData()
        {
            base.BindData();
            m_Particle = gameObject.GetComponentsInChildren<ParticleSystem>(true);
            m_TrailRe = gameObject.GetComponentsInChildren<TrailRenderer>(true);
        }
    }
}