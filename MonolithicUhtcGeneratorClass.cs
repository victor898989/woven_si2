public class MonolithicUhtcGenerator : BaseShape
{
    private readonly float m_fOuterRadius = 150.0f;
    private readonly float m_fInnerRadius = 110.0f;
    private readonly float m_fHeight = 300.0f;

    public override Voxels voxConstruct()
    {
        // 1. Inicialización de los contornos volumétricos base del motor/cuerpo
        Voxels voxOuter = Sh.voxCylinder(Vector3.Zero, Vector3.UnitZ * m_fHeight, m_fOuterRadius);
        Voxels voxInner = Sh.voxCylinder(Vector3.Zero, Vector3.UnitZ * m_fHeight, m_fInnerRadius);
        Voxels voxWallDomain = Sh.voxSubtract(voxOuter, voxInner);

        // 2. Instanciación del modulador físico térmico y termoquímico
        QuasiCrystalUhtcThickness xPhysicsModulator = new QuasiCrystalUhtcThickness(0.3f, 2.5f, 40.0f);

        // 3. Generación del campo de cuasicristales tridimensionales (Icosahedral Symmetry 6D -> 3D)
        // Usamos una proyección armónica para definir los ejes no periódicos
        Voxels voxQuasiCrystalFibers = GenerateQuasiCrystalFibers(voxWallDomain, xPhysicsModulator);

        // 4. Integración monolítica (Fusión de la matriz base con el lattice cuasicristalino de microfibras)
        Voxels voxFinalMonolith = Sh.voxUnion(voxWallDomain, voxQuasiCrystalFibers);

        // Post-procesamiento de suavizado (OverOffset) para cerrar micro-poros superficiales (simulación del borosilicato)
        // Redondea las uniones de las vigas evitando concentradores de tensión
        voxFinalMonolith = Sh.voxOverOffset(voxFinalMonolith, 0.4f, 0.0f);

        return voxFinalMonolith;
    }

    private Voxels GenerateQuasiCrystalFibers(Voxels voxDomain, QuasiCrystalUhtcThickness xModulator)
    {
        // Inicializar contenedor de vóxeles vacío
        Voxels voxFibers = new Voxels();

        // 6 Vectores de dirección proyectados del icosaedro regular (Simetría de cuasicristal de 5 ejes)
        float fGoldenRatio = (1.0f + MathF.Sqrt(5.0f)) / 2.0f;
        Vector3[] aDirections = new Vector3[6]
        {
            Vector3.Normalize(new Vector3(1.0f, 0.0f, fGoldenRatio)),
            Vector3.Normalize(new Vector3(1.0f, 0.0f, -fGoldenRatio)),
            Vector3.Normalize(new Vector3(0.0f, fGoldenRatio, 1.0f)),
            Vector3.Normalize(new Vector3(0.0f, -fGoldenRatio, 1.0f)),
            Vector3.Normalize(new Vector3(fGoldenRatio, 1.0f, 0.0f)),
            Vector3.Normalize(new Vector3(-fGoldenRatio, 1.0f, 0.0f))
        };

        // Escala espacial del cuasicristal (ajusta la densidad microestructural)
        float fFrequency = 0.85f; 
        float fThreshold = 0.25f; // Umbral de corte para definir filamento sólido

        // NOTA: Para producción real, en lugar de un escaneo de vóxeles celda por celda,
        // PicoGK mapea el campo implícito de forma procedural usando la GPU o el procesador de vóxeles nativo.
        // Aquí simulamos el muestreo adaptativo del campo para generar los ejes de las fibras:
        
        for (float fZ = 0; fZ < m_fHeight; fZ += 2.0f)
        {
            for (float fAngle = 0; fAngle < 2.0f * MathF.PI; fAngle += 0.05f)
            {
                float fR = (m_fOuterRadius + m_fInnerRadius) / 2.0f;
                Vector3 vPos = new Vector3(fR * MathF.Cos(fAngle), fR * MathF.Sin(fAngle), fZ);

                // Ecuación de onda de interferencia cuasi-periódica
                float fQuasiValue = 0f;
                for (int i = 0; i < 6; i++)
                {
                    fQuasiValue += MathF.Cos(Vector3.Dot(vPos, aDirections[i]) * fFrequency);
                }

                // Si supera el umbral, instanciamos un nodo de fibra microestructural
                if (fQuasiValue > fThreshold)
                {
                    float fRadius = xModulator.fGetRadius(vPos);
                    Voxels voxFiberNode = Sh.voxSphere(vPos, fRadius);
                    voxFibers = Sh.voxUnion(voxFibers, voxFiberNode);
                }
            }
        }

        return Sh.voxIntersect(voxFibers, voxDomain);
    }
}
