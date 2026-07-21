using System;
using System.Numerics;
using PicoGK;

namespace Leap71.AerospaceGenerativeDesign
{
    public class UhtcOxidationBeamThickness : IBeamThickness
    {
        // Propiedades del compuesto SiC-B4C
        private readonly float m_fMinRadius = 0.5f;
        private readonly float m_fMaxRadius = 3.0f;
        
        // Constantes cinéticas de oxidación (Arrhenius)
        private readonly float m_fActivationEnergy_Q = 120000f; // J/mol
        private readonly float m_fGasConstant_R = 8.314f;       // J/(mol·K)
        private readonly float m_fArrheniusFactor_A = 0.05f;    // Factor pre-exponencial

        // Condiciones de misión (Sutton / ECSS)
        private readonly float m_fStagnationTemp = 2200.0f;     // K (Punto máximo)
        private readonly float m_fBaseTemp = 400.0f;            // K (Acoplamiento nave)
        private readonly float m_fShieldThickness = 50.0f;      // mm

        public void UpdateCell(IUnitCell xCell) { }

        public float fGetRadius(Vector3 vPoint)
        {
            // 1. Perfil térmico asumiendo decaimiento parabólico desde el morro (Z=0)
            float fDepthZ = Math.Abs(vPoint.Z);
            float fLocalTemp = m_fStagnationTemp - ((m_fStagnationTemp - m_fBaseTemp) * MathF.Pow(fDepthZ / m_fShieldThickness, 2));

            // 2. Cálculo de la tasa de oxidación (k_p) local
            float fOxidationRate = 0f;
            if (fLocalTemp > 900.0f) // Umbral donde el B4C se oxida a B2O3 líquido
            {
                float fExponent = -m_fActivationEnergy_Q / (m_fGasConstant_R * fLocalTemp);
                fOxidationRate = m_fArrheniusFactor_A * MathF.Exp(fExponent);
            }

            // 3. Modulación Geométrica: 
            // A mayor tasa de oxidación (mayor riesgo), necesitamos filamentos MÁS DENSOS 
            // (vigas más gruesas) y porosidad más estrecha para forzar el sellado del vidrio.
            float fRadiusRange = m_fMaxRadius - m_fMinRadius;
            
            // Normalizamos la tasa (ajuste empírico basado en el máximo térmico)
            float fMaxOxidation = m_fArrheniusFactor_A * MathF.Exp(-m_fActivationEnergy_Q / (m_fGasConstant_R * m_fStagnationTemp));
            float fSeverityIndex = fOxidationRate / fMaxOxidation;

            float fTargetRadius = m_fMinRadius + (fRadiusRange * fSeverityIndex);

            return Math.Clamp(fTargetRadius, m_fMinRadius, m_fMaxRadius);
        }
    }
}
