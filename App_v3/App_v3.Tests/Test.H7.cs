using App_v3.Models;

namespace App_v3.Tests;

[TestClass]
public class ConfiguracionRiesgoTests
{
    [TestMethod]
    public void CA7_2_PocasInasistenciasYSinTpsPendientesDaRiesgoBajo()
    {
        // Arrange
        var config = new ConfiguracionRiesgo();

        // Act
        var nivel = config.CalcularNivel(inasistencias: 1, tpsNoEntregados: 0);

        // Assert
        Assert.AreEqual(NivelRiesgo.Bajo, nivel);
    }

    [TestMethod]
    public void CA7_2_MuchasInasistenciasDaRiesgoAlto()
    {
        // Arrange
        var config = new ConfiguracionRiesgo();

        // Act
        var nivel = config.CalcularNivel(inasistencias: 5, tpsNoEntregados: 0);

        // Assert
        Assert.AreEqual(NivelRiesgo.Alto, nivel);
    }

    [TestMethod]
    public void CA7_2_ElNivelGeneralEsElMasAltoDeLasDosSenales()
    {
        // Arrange
        var config = new ConfiguracionRiesgo();

        // Act: 2 inasistencias es Bajo, pero 1 TP sin entregar es Medio
        var nivel = config.CalcularNivel(inasistencias: 2, tpsNoEntregados: 1);

        // Assert
        Assert.AreEqual(NivelRiesgo.Medio, nivel);
    }

    [TestMethod]
    public void CA7_2_AlBajarElUmbralAltoCambiaElNivelDelEstudiante()
    {
        // Arrange
        var config = new ConfiguracionRiesgo();
        var antes = config.CalcularNivel(inasistencias: 4, tpsNoEntregados: 0);

        // Act: el docente baja el umbral Alto de inasistencias a 4
        config.InasistenciasAlto = 4;
        var despues = config.CalcularNivel(inasistencias: 4, tpsNoEntregados: 0);

        // Assert
        Assert.AreEqual(NivelRiesgo.Medio, antes);
        Assert.AreEqual(NivelRiesgo.Alto, despues);
    }
}
