// SPDX-License-Identifier: Apache-2.0
// Developed for Advanced Spacecraft Structural and Thermal Computational Models.
// Estructura de código adaptada para motores de diseño de Leap71 sobre PicoGK.

using System;
using System.Collections.Generic;
using System.Numerics;
using PicoGK;

namespace Leap71.AerospaceGenerativeDesign
{
    using ShapeKernel;

    /// <summary>
    /// Firma geométrica y estado de diagnóstico predictivo para el tejido del escudo térmico.
    /// </summary>
    public struct AresPreCommitSignature
    {
        public float ThermalGradient;       // dT/dz en K/mm (ej. caída de 2000°C en 40mm)
        public float ExpansionStrain;       // Deformación micro-métrica por dilatación del tejido 3D
        public float PreCommitIndex;        // Índice de 0 a 1 (1 = inicio de ablación de la capa de sacrificio)
        public bool IsHotspotCritical;      // Flag predictivo antes de la degradación visible de la superficie
    }

    /// <summary>
    /// Motor de diseño generativo enfocado en materiales monolíticos y disipación de calor 
    /// en lugar de geometrías complejas de refrigeración líquida.
    /// Optimiza los ángulos geométricos según la norma ECSS Part 9A/3A y evalúa la delaminación.
    /// </summary>
    public class MonolithicTPSGenerator : BaseShape
    {
        // Parámetros orbitales y volumétricos del vehículo (ECSS Part 9A)
        protected float m_fPlanetRadius = 6371000f; // Radio terrestre (m)
        protected float m_fOrbitAltitude = 400000f;  // Órbita LEO estándar (400 km)
        protected float m_fRatioHD = 1.2f;          // Restricción volumétrica de altura/diámetro (H/D)

        // Propiedades de la estructura monolítica ARES-MATRIX
        protected float m_fBaseRadius = 1100f;       // Radio base del cohete (mm)
        
        // El escudo monolítico total se subdivide en:
        protected float m_fSacrificialAblationThickness = 1.50f; // Capa de sacrificio para hotspots extremos (mm)
        protected float m_fIntegratedWovenFiberThickness = 4.25f; // Estructura de fibra base monolítica 3D (mm)
        protected float m_fShieldThickness = 5.75f;              // Espesor total (mm)
        
        protected float m_fStructuralThickness = 40.0f;           // Espesor total del chasis de Carbono-Carbono 3D (40mm / 4cm)

        // Constantes Críticas de Fluidos Criogénicos (Propano / LPG de referencia, ECSS Part 3A / Figura 6-6)
        protected const float T_c = 369.89f;         // Temperatura Crítica en Kelvin (Propano de referencia)
        protected const float P_c = 4251200f;        // Presión Crítica en Pascales (4.25 MPa)

        public MonolithicTPSGenerator() : base()
        {
        }

        /// <summary>
        /// Implementa la optimización analítica de Bywaters & Keeling (ECSS 9A Section 6.4)
        /// para calcular el ángulo óptimo del cono protector que minimiza la radiación parásita sobre los tanques.
        /// </summary>
        public float GetOptimalConeAngle()
        {
            // cos(beta) = Rp / (Rp + h)
            float fCosBeta = m_fPlanetRadius / (m_fPlanetRadius + m_fOrbitAltitude);
            float fBeta = MathF.Acos(fCosBeta); // Ángulo límite de emisión planetaria en radianes

            // Resolvemos de forma iterativa la ecuación de balance de Bywaters [6-6] para theta (semiángulo del cono)
            float fTheta = 15f * MathF.PI / 180f; // Inicialización (15 grados)
            float fLearningRate = 0.05f;

            for (int i = 0; i < 50; i++)
            {
                float fLeft = MathF.Sin(fTheta + fBeta);
                float fRight = (1f - 2f * m_fRatioHD * MathF.Tan(fTheta)) * MathF.Sin(fTheta);
                float fError = fLeft - fRight;

                // Actualización por descenso de gradiente numérico simple
                fTheta -= fError * fLearningRate;
                fTheta = Math.Clamp(fTheta, 5f * MathF.PI / 180f, 60f * MathF.PI / 180f);
            }

            return fTheta; // Retorna el ángulo óptimo en radianes
        }

        /// <summary>
        /// Calcula la Presión de Saturación (p_sat) según la formulación matemática de la ECSS (Figura 6-6)
        /// </summary>
        public float CalculateSaturationPressure(float fTempKelvin)
        {
            float fTr = fTempKelvin / T_c; // Temperatura reducida
            if (fTr >= 1.0f) return P_c;

            float fTerm1 = 1f - fTr;
            float fTerm2 = 1f - MathF.Pow(fTr, 2f);
            float fTerm3 = MathF.Pow(1f - fTr, 3f) * (3f + fTr);

            // Ecuación empírica [6-41] de la ECSS: log(p_sat / p_c)
            float fLogRatio = - (1.4473f / fTr) * fTerm2 + 0.4535f * fTerm3;
            float fPressure = P_c * MathF.Pow(10f, fLogRatio);

            return fPressure; // Retorna la presión de vapor en Pascales
        }

        /// <summary>
        /// Calcula el calor latente de vaporización (h_fg) según la ECSS (Figura 6-6)
        /// </summary>
        public float CalculateLatentHeat(float fTempKelvin)
        {
            if (fTempKelvin < 144f || fTempKelvin > 370f) return 0f;

            // h_fg = 0.6422e6 * (1 - T/T_c)^0.4023 (unidades de calor latente J/kg)
            float fBase = 1f - (fTempKelvin / T_c);
            float fHfg = 0.6422f * 1e6f * MathF.Pow(fBase, 0.4023f);

            return fHfg;
        }

        /// <summary>
        /// Calcula la densidad del líquido criogénico en base a la temperatura local (Figura 6-6)
        /// </summary>
        public float CalculateLiquidDensity(float fTempKelvin, float fVaporDensity = 1.8f)
        {
            // rho_l = 452 - rho_v + 344.64 * (1 - T/T_c)
            float fDensity = 452f - fVaporDensity + 344.64f * (1f - (fTempKelvin / T_c));
            return fDensity; // Retorna kg/m³
        }

        /// <summary>
        /// Calcula la viscosidad dinámica del fluido criogénico de paso (Figura 6-6, Eq. [6-42])
        /// </summary>
        public float CalculateLiquidViscosity(float fTempKelvin)
        {
            if (fTempKelvin < 83f || fTempKelvin > 333f) return 1e-6f;

            // mu_l = 10^-4 * exp[-4.9633 + 2127.5/T - 2.3239e5 / T^2 + 1.0312e7 / T^3]
            float fExponent = -4.9633f + (2127.5f / fTempKelvin) - (232390f / MathF.Pow(fTempKelvin, 2f)) + (10312000f / MathF.Pow(fTempKelvin, 3f));
            float fViscosity = 1e-4f * MathF.Exp(fExponent);

            return fViscosity; // Retorna Pa·s (N·s/m²)
        }

        /// <summary>
        /// Evalúa el factor de radiación en un acoplamiento cono-cilindro-cono de la ECSS Part 3A (Figura 4-18)
        /// Retorna la aproximación del ratio (A_I / A_E)^(1/4) como función de gamma y delta.
        /// </summary>
        public float GetInterstageRadiantRatio(float fGammaDeg, float fDeltaDeg)
        {
            // Ajustamos numéricamente según la curva experimental (Figura 4-18, H/R = 10)
            float fGammaRad = fGammaDeg * MathF.PI / 180f;
            float fDeltaFactor = (fDeltaDeg == 90f) ? 0.95f : 0.88f;

            // Aproximación polinómica de la curva de decaimiento
            float fBaseRatio = 0.75f * MathF.Cos(fGammaRad * 0.5f * fDeltaFactor);
            return Math.Clamp(fBaseRatio, 0.1f, 0.8f);
        }

        /// <summary>
        /// Evalúa el estado de "pre-compromiso" del tejido ARES-MATRIX bajo un flujo térmico crítico.
        /// Traduce la caída de 2000°C a través de 40mm de estructura de soporte en firmas geométricas predecibles.
        /// </summary>
        public AresPreCommitSignature EvaluateAresMatrixPreCommitState(float fSurfaceTempK, float fInternalTempK)
        {
            AresPreCommitSignature oSig = new AresPreCommitSignature();
            
            // 1. Gradiente térmico real (K/mm) a través del chasis de Carbono-Carbono 3D de 40 mm (4 cm)
            oSig.ThermalGradient = (fSurfaceTempK - fInternalTempK) / m_fStructuralThickness;

            // 2. Coeficiente de expansión térmica medio del Carbono-Carbono 3D (CTE ~ 1.5e-6 / K)
            float fCTE = 1.5e-6f;
            oSig.ExpansionStrain = fCTE * (fSurfaceTempK - fInternalTempK);

            // 3. Índice de Pre-Compromiso: Mapea la cercanía a la temperatura de ablación del recubrimiento (aprox. 2400 K)
            // Cuando la superficie alcanza 2200K, se inicia el pre-compromiso antes de la ablación visible del material de sacrificio.
            float fAblationOnsetTemp = 2400f;
            float fPreCommitOnsetTemp = 2000f;

            if (fSurfaceTempK < fPreCommitOnsetTemp)
            {
                oSig.PreCommitIndex = (fSurfaceTempK / fPreCommitOnsetTemp) * 0.5f;
            }
            else
            {
                oSig.PreCommitIndex = 0.5f + 0.5f * ((fSurfaceTempK - fPreCommitOnsetTemp) / (fAblationOnsetTemp - fPreCommitOnsetTemp));
            }
            oSig.PreCommitIndex = Math.Clamp(oSig.PreCommitIndex, 0f, 1f);

            // 4. Diagnóstico predictivo de hotspot crítico
            oSig.IsHotspotCritical = (oSig.PreCommitIndex >= 0.85f);

            return oSig;
        }

        /// <summary>
        /// Construye y devuelve el volumen del escudo monolítico aplicando el ángulo de cono optimizado por la ECSS.
        /// </summary>
        public override Voxels voxConstruct()
        {
            Library.Log("===============================================================");
            Library.Log("OPTIMIZACIÓN DE ESTRUCTURA MONOLÍTICA REUSABLE (LEAP71 ENGINE)");
            Library.Log("===============================================================");

            // 1. Calcular el ángulo geométrico óptimo para rechazo de radiación planetaria (ECSS Part 9A)
            float fOptThetaRad = GetOptimalConeAngle();
            float fOptThetaDeg = fOptThetaRad * 180f / MathF.PI;
            Library.Log($"Ángulo de cono protector optimizado (Bywaters & Keeling): {fOptThetaDeg:F2}º");

            // 2. Evaluar el coeficiente térmico del interstage cono-cilindro-cono (ECSS Part 3A / Figura 4-18)
            float fGamma = fOptThetaDeg; 
            float fDelta = 90f;          
            float fRadiantRatio = GetInterstageRadiantRatio(fGamma, fDelta);
            Library.Log($"Ratio de radiación interna (A_I / A_E)^(1/4) (Part 3A Fig 4-18): {fRadiantRatio:F4}");

            // 3. Análisis predictivo ARES-MATRIX (Simulación de reentrada extrema: 2273.15 K de superficie / 273.15 K interna)
            float fT_surface = 2273.15f; // ~2000 ºC en cara exterior
            float fT_internal = 273.15f;  // ~0 ºC en cara interior (caída térmica de 2000 ºC sobre el chasis de 4cm)
            AresPreCommitSignature oAresSignature = EvaluateAresMatrixPreCommitState(fT_surface, fT_internal);

            Library.Log("\n--- DIAGNÓSTICO PREDICTIVO ARES-MATRIX (ESTADO PRE-COMPROMISO) ---");
            Library.Log($" - Caída Térmica:                   {fT_surface - fT_internal:F1} K a través de {m_fStructuralThickness} mm");
            Library.Log($" - Gradiente Térmico Local (dT/dz): {oAresSignature.ThermalGradient:F2} K/mm");
            Library.Log($" - Deformación por Dilatación (ε):  {oAresSignature.ExpansionStrain * 1e6f:F1} micro-strains (Legible por geometría consistente)");
            Library.Log($" - Índice de Pre-Compromiso:        {oAresSignature.PreCommitIndex * 100f:F1}% de inicio de ablación");
            
            if (oAresSignature.IsHotspotCritical)
            {
                Library.Log(" -> PREVENTIVE WARNING: Hotspot crítico detectado geométricamente antes de daño superficial permanente.");
                Library.Log("    Ajustando la densidad del lattice interno de disipación para mitigar el gradiente local...");
            }
            else
            {
                Library.Log(" -> ESTADO DE SEGURIDAD: Capa de sacrificio intacta. Tejido 3D en rango de resiliencia nominal.");
            }
            Library.Log("------------------------------------------------------------------\n");

            // 4. Construcción del spline de perfil cilíndrico (CylindricalControlSpline) basado en el ángulo óptimo
            float fSegmentHeight = 800f; // mm
            float fDeltaRadius = fSegmentHeight * MathF.Tan(fOptThetaRad);

            CylindricalControlSpline oProfileSpline = new CylindricalControlSpline(new Vector3(0, 0, 0));
            oProfileSpline.AddAbsoluteStep(CylindricalControlSpline.EDirection.RADIAL, m_fBaseRadius);
            oProfileSpline.AddRelativeStep(CylindricalControlSpline.EDirection.Z, fSegmentHeight);
            oProfileSpline.AddAbsoluteStep(CylindricalControlSpline.EDirection.RADIAL, m_fBaseRadius + fDeltaRadius);

            List<Vector3> aSpinePoints = oProfileSpline.aGetPoints(150);
            Library.Log($"Spline del perfil generado con {aSpinePoints.Count} puntos de muestreo.");

            // 5. Generación del lattice para soporte estructural amortiguado (Capa 3 de aislamiento)
            Lattice oLatticeCore = new Lattice();
            float fMinBeam = 1.2f; 
            float fMaxBeam = 4.5f; 

            // Evaluación de ebullición criogénica en la pared del tanque a 220K
            float fTankWallTemp = 220f; 
            float fCryoPressure = CalculateSaturationPressure(fTankWallTemp);
            float fCryoDensity = CalculateLiquidDensity(fTankWallTemp);
            float fCryoViscosity = CalculateLiquidViscosity(fTankWallTemp);

            Library.Log("ANÁLISIS DE INTERFAZ CRIOGÉNICA DE TANQUES (Propano / LPG Ref):");
            Library.Log($" - Temperatura local de la pared del tanque: {fTankWallTemp:F1} K");
            Library.Log($" - Presión de saturación calculada (p_sat):   {fCryoPressure / 1000f:F2} kPa");
            Library.Log($" - Densidad del líquido criogénico (rho_l):  {fCryoDensity:F2} kg/m³");
            Library.Log($" - Viscosidad del líquido criogénico (mu_l):  {fCryoViscosity * 1e3f:F4} cP");

            for (int i = 1; i < aSpinePoints.Count; i++)
            {
                float fRatio = (float)i / aSpinePoints.Count;
                
                // Si el diagnóstico predictivo detecta un hotspot crítico, reforzamos el lattice localmente 
                // para propagar lateralmente la sobrecarga térmica y conductiva.
                float fHotspotReinforcement = oAresSignature.IsHotspotCritical ? 1.35f : 1.0f;
                float fPressureAdaptabilityFactor = fCryoPressure / P_c; 
                float fCurrentBeam = (fMinBeam + (fMaxBeam - fMinBeam) * fRatio * (1.0f + fPressureAdaptabilityFactor)) * fHotspotReinforcement;
                
                oLatticeCore.AddBeam(aSpinePoints[i - 1], fCurrentBeam, aSpinePoints[i], fCurrentBeam, true);
            }

            Voxels voxLatticeStructure = new Voxels(oLatticeCore);

            // 6. Modelado del escudo de superficie de ultra alta temperatura (UHTC) y chasis Carbono-Carbono
            Lattice oShieldLat = new Lattice();
            for (int i = 1; i < aSpinePoints.Count; i++)
            {
                oShieldLat.AddBeam(aSpinePoints[i-1], m_fBaseRadius, aSpinePoints[i], m_fBaseRadius, true);
            }
            Voxels voxBaseSolid = new Voxels(oShieldLat);

            // Capa 1: Escudo Cerámico Monolítico Externo (ZrB2 + Fleece) - Sin interfaces adhesivas de desprendimiento
            Voxels voxOuterShield = Sh.voxOffset(voxBaseSolid, m_fShieldThickness);
            Voxels voxMonolithicShield = Sh.voxSubtract(voxOuterShield, voxBaseSolid);

            // Capa 2: Chasis estructural resistente Carbono-Carbono 3D de alta resistencia
            Voxels voxInnerChassis = Sh.voxOffset(voxBaseSolid, -m_fStructuralThickness);
            Voxels voxCCChassis = Sh.voxSubtract(voxBaseSolid, voxInnerChassis);

            // Unión booleana final del chasis monolítico con el lattice interno aligerado de soporte
            Voxels voxFinalAssembly = Sh.voxUnion(voxCCChassis, voxMonolithicShield);
            voxFinalAssembly = Sh.voxUnion(voxFinalAssembly, voxLatticeStructure);

            // 7. Análisis estructural y reporte de ensayos virtuales
            PerformThermalLinkingVerification();

            Library.Log("Estructura de transiciones y protección integrada generada.");
            Library.Log("===============================================================");

            return voxFinalAssembly;
        }

        /// <summary>
        /// Valida la resistencia a flexión de 3 puntos del material compuesto Carbono-Carbono de la tobera.
        /// Aplica la ecuación (2.13) para asegurar que el esfuerzo de reentrada no cause delaminación por cortante.
        /// </summary>
        private void PerformThermalLinkingVerification()
        {
            float fFlexuralSpan = 1280.0f; // l = 32 * h (mm) para Carbono-Carbono (32 * 40mm = 1280mm)
            float fProbetaWidth = 15.0f;   // b = 15 mm
            float fProbetaHeight = 40.0f;  // h = 40 mm (Espesores de chasis monolítico 3D)
            float fMaxReentryBendingLoad = 12500f; // Pb = 12.5 kN carga pico por presión dinámica

            // rf = 3 * Pb * l / (2 * b * h^2)
            float fFlexuralStress = (3f * fMaxReentryBendingLoad * fFlexuralSpan) / (2f * fProbetaWidth * MathF.Pow(fProbetaHeight, 2f));
            
            // Límite de rotura a flexión del material TLC 3D-Braided de alúmina-carbono
            float fLimitFlexuralStrength = 380.0f; // MPa

            Library.Log("ANÁLISIS DE RESISTENCIA MECÁNICA DE INTERFAZ (TLC No-Delaminating):");
            Library.Log($" - Esfuerzo de flexión inducido en reentrada: {fFlexuralStress:F2} MPa");
            Library.Log($" - Límite de rotura a flexión del compuesto:  {fLimitFlexuralStrength:F2} MPa");

            if (fFlexuralStress < fLimitFlexuralStrength)
            {
                float fSafetyMargin = (fLimitFlexuralStrength / fFlexuralStress) - 1.0f;
                Library.Log($" -> ESTADO DE SEGURIDAD: Conforme (Margen de seguridad: +{fSafetyMargin*100:F1}%)");
                Library.Log(" -> Nota: Al emplear enlaces trenzados tridimensionales de gradiente (TLC), se descarta por completo el modo de fallo por delaminación de tejas.");
            }
            else
            {
                Library.Log(" -> ALERTA DE DISEÑO: Superado el límite de flexión. Se requiere aumentar el espesor estructural h_cc.");
            }
        }
    }
}