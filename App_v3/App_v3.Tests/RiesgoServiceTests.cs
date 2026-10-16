using App_v3.Models;
using App_v3.Seguimiento;

namespace App_v3.Tests;

[TestClass]
public sealed class RiesgoServiceTests
{
    private static readonly DateTime Hoy = Escenario.Hoy;

    private static async Task<Escenario> ConUnEstudianteAsync()
    {
        var e = new Escenario();
        await e.Alta("L1", "111");
        return e;
    }

    [TestMethod]
    public async Task CA6_1_CuentaLasInasistenciasDelEstudiante()
    {
        var e = await ConUnEstudianteAsync();
        await e.ClaseConAsistencia(Hoy.AddDays(-28), "L1");
        await e.ClaseConAsistencia(Hoy.AddDays(-21), "L1");
        await e.ClaseConAsistencia(Hoy.AddDays(-14), "L1");
        await e.ClaseConAsistencia(Hoy.AddDays(-7));

        var filas = await e.Riesgo.CalcularAsync(Hoy);

        Assert.AreEqual(3, filas[0].Inasistencias);
        Assert.AreEqual(NivelRiesgo.Medio, filas[0].Nivel); // umbral Medio por defecto: 3
    }

    [TestMethod]
    public async Task CA6_1_CuentaSoloLosTrabajosVencidosSinEntregar()
    {
        var e = await ConUnEstudianteAsync();
        await e.TrabajoConEntregas(Hoy.AddDays(-20));
        await e.TrabajoConEntregas(Hoy.AddDays(-10));
        await e.TrabajoConEntregas(Hoy.AddDays(-2));
        await e.TrabajoConEntregas(Hoy.AddDays(5)); // todavía no venció: no cuenta

        var filas = await e.Riesgo.CalcularAsync(Hoy);

        Assert.AreEqual(3, filas[0].TpsVencidosNoEntregados);
        Assert.AreEqual(NivelRiesgo.Alto, filas[0].Nivel); // umbral Alto por defecto: 3
    }

    [TestMethod]
    public async Task CA6_1_UnTrabajoEntregadoNoCuentaComoPendiente()
    {
        var e = await ConUnEstudianteAsync();
        await e.TrabajoConEntregas(Hoy.AddDays(-5), "L1");

        var filas = await e.Riesgo.CalcularAsync(Hoy);

        Assert.AreEqual(0, filas[0].TpsVencidosNoEntregados);
        Assert.AreEqual(NivelRiesgo.Bajo, filas[0].Nivel);
    }

    [TestMethod]
    public async Task CA6_2_ElNivelGeneralEsElMasAltoDeLasDosSenales()
    {
        var e = await ConUnEstudianteAsync();
        await e.ClaseConAsistencia(Hoy.AddDays(-7)); // sin inasistencias: Bajo
        await e.TrabajoConEntregas(Hoy.AddDays(-3)); // 1 TP sin entregar: Medio

        var filas = await e.Riesgo.CalcularAsync(Hoy);

        Assert.AreEqual(NivelRiesgo.Medio, filas[0].Nivel);
    }

    [TestMethod]
    public async Task CA6_4_LosInactivosNoEntranEnElCalculo()
    {
        var e = new Escenario();
        var uno = await e.Alta("L1", "111");
        await e.Alta("L2", "222");
        await e.ClaseConAsistencia(Hoy.AddDays(-7), "L1");
        await e.Estudiantes.InactivarAsync(uno.Id);

        var filas = await e.Riesgo.CalcularAsync(Hoy);

        Assert.AreEqual(1, filas.Count);
        Assert.AreEqual("L2", filas[0].Legajo);
    }

    [TestMethod]
    public async Task CA6_3_CorregirUnaAsistenciaActualizaElRiesgo()
    {
        var e = await ConUnEstudianteAsync();
        var primera = await e.ClaseConAsistencia(Hoy.AddDays(-21), "L1");
        await e.ClaseConAsistencia(Hoy.AddDays(-14), "L1");
        await e.ClaseConAsistencia(Hoy.AddDays(-7), "L1");
        Assert.AreEqual(NivelRiesgo.Medio, (await e.Riesgo.CalcularAsync(Hoy))[0].Nivel); // 3 inasistencias

        var planilla = await e.Asistencia.ObtenerPlanillaAsync(primera.Id);
        planilla.Filas[0].Presente = true;
        await e.Asistencia.CorregirAsync(primera.Id, planilla.Filas);

        var filas = await e.Riesgo.CalcularAsync(Hoy);
        Assert.AreEqual(2, filas[0].Inasistencias);
        Assert.AreEqual(NivelRiesgo.Bajo, filas[0].Nivel);
    }

    [TestMethod]
    public async Task CA6_3_CambiarLosUmbralesActualizaElNivel()
    {
        var e = await ConUnEstudianteAsync();
        await e.ClaseConAsistencia(Hoy.AddDays(-7), "L1");
        Assert.AreEqual(NivelRiesgo.Bajo, (await e.Riesgo.CalcularAsync(Hoy))[0].Nivel);

        await e.Db.SaveAsync(new ConfiguracionRiesgo
        {
            InasistenciasMedio = 1,
            InasistenciasAlto = 2,
            TpsNoEntregadosMedio = 1,
            TpsNoEntregadosAlto = 3
        });

        Assert.AreEqual(NivelRiesgo.Medio, (await e.Riesgo.CalcularAsync(Hoy))[0].Nivel);
    }

    // Tres estudiantes: L1 (Alto por 3 TPs), L2 (Medio por 1 TP) y L3 (Bajo). Se cargan al revés a propósito.
    private static async Task<Escenario> ConTresNivelesAsync()
    {
        var e = new Escenario();
        await e.Alta("L3", "333", "Carla", "Castro");
        await e.Alta("L2", "222", "Beto", "Benítez");
        await e.Alta("L1", "111", "Ana", "Álvarez");

        await e.TrabajoConEntregas(Hoy.AddDays(-20), "L2", "L3");
        await e.TrabajoConEntregas(Hoy.AddDays(-10), "L2", "L3");
        await e.TrabajoConEntregas(Hoy.AddDays(-3), "L3");
        return e;
    }

    [TestMethod]
    public async Task CA8_1_ElPanelSeOrdenaDeMayorAMenorRiesgo()
    {
        var e = await ConTresNivelesAsync();

        var panel = await e.Riesgo.ObtenerPanelAsync(Hoy);

        CollectionAssert.AreEqual(new[] { "L1", "L2", "L3" }, panel.Select(f => f.Legajo).ToArray());
        CollectionAssert.AreEqual(
            new[] { NivelRiesgo.Alto, NivelRiesgo.Medio, NivelRiesgo.Bajo },
            panel.Select(f => f.Nivel).ToArray());
    }

    [TestMethod]
    public async Task CA8_2_ElPanelJustificaElNivelConLasCantidades()
    {
        var e = await ConTresNivelesAsync();

        var panel = await e.Riesgo.ObtenerPanelAsync(Hoy);

        Assert.AreEqual(3, panel[0].TpsVencidosNoEntregados);
        Assert.IsTrue(panel[0].Motivo.Contains("3 TPs vencidos sin entregar"));
    }

    [TestMethod]
    public async Task CA11_2_ElFiltroMuestraSoloEnRiesgoSinContactoEnLosUltimos7Dias()
    {
        var e = await ConTresNivelesAsync();
        var l1 = (await e.Estudiantes.ListarAsync()).First(x => x.Legajo == "L1");
        await e.Contactos.RegistrarAsync(l1.Id, Hoy.AddDays(-2), "Llamada", "Respondió", "Habló con el alumno.");

        var filtrado = await e.Riesgo.ObtenerPanelAsync(Hoy, soloNoContactadosEstaSemana: true);

        // L1 ya fue contactado, L3 no está en riesgo: queda solo L2
        Assert.AreEqual(1, filtrado.Count);
        Assert.AreEqual("L2", filtrado[0].Legajo);
    }

    [TestMethod]
    public async Task CA11_2_UnContactoDeHaceMasDeUnaSemanaNoAlcanza()
    {
        var e = await ConTresNivelesAsync();
        var l1 = (await e.Estudiantes.ListarAsync()).First(x => x.Legajo == "L1");
        await e.Contactos.RegistrarAsync(l1.Id, Hoy.AddDays(-10), "Email", "No respondió", "Se envió un recordatorio.");

        var filtrado = await e.Riesgo.ObtenerPanelAsync(Hoy, soloNoContactadosEstaSemana: true);

        CollectionAssert.AreEqual(new[] { "L1", "L2" }, filtrado.Select(f => f.Legajo).ToArray());
    }

    [TestMethod]
    public async Task CA11_1_ElPanelIndicaLaFechaDelUltimoContacto()
    {
        var e = await ConTresNivelesAsync();
        var l1 = (await e.Estudiantes.ListarAsync()).First(x => x.Legajo == "L1");
        await e.Contactos.RegistrarAsync(l1.Id, Hoy.AddDays(-9), "Llamada", "Respondió", "Primer llamado.");
        await e.Contactos.RegistrarAsync(l1.Id, Hoy.AddDays(-2), "Email", "Respondió", "Segundo contacto.");

        var panel = await e.Riesgo.ObtenerPanelAsync(Hoy);

        Assert.IsTrue(panel[0].Contactado);
        Assert.AreEqual(Hoy.AddDays(-2), panel[0].UltimoContacto!.Value);
        Assert.IsFalse(panel[1].Contactado);
    }
}
