using App_v3.Seguimiento;

namespace App_v3.Tests;

[TestClass]
public sealed class AsistenciaServiceTests
{
    private static async Task<(Escenario e, ClaseDictada clase)> ConDosActivosAsync()
    {
        var e = new Escenario();
        await e.Alta("L1", "111", "Ana", "Álvarez");
        await e.Alta("L2", "222", "Beto", "Benítez");
        var clase = await e.Calendario.AgregarClaseAsync(new DateTime(2026, 9, 7), "Clase 1");
        return (e, clase);
    }

    [TestMethod]
    public async Task CA3_1_TodosLosActivosQuedanPresentesPorDefecto()
    {
        var (e, clase) = await ConDosActivosAsync();

        var planilla = await e.Asistencia.ObtenerPlanillaAsync(clase.Id);

        Assert.IsFalse(planilla.YaRegistrada);
        Assert.AreEqual(2, planilla.Filas.Count);
        Assert.IsTrue(planilla.Filas.All(f => f.Presente));
    }

    [TestMethod]
    public async Task CA3_1_LosInactivosNoAparecenEnLaPlanilla()
    {
        var (e, clase) = await ConDosActivosAsync();
        var inactivo = (await e.Estudiantes.ListarAsync()).First(x => x.Legajo == "L2");
        await e.Estudiantes.InactivarAsync(inactivo.Id);

        var planilla = await e.Asistencia.ObtenerPlanillaAsync(clase.Id);

        Assert.AreEqual(1, planilla.Filas.Count);
        Assert.AreEqual("L1", planilla.Filas[0].Legajo);
    }

    [TestMethod]
    public async Task CA3_1_SinEstudiantesActivosNoSePuedeTomarAsistencia()
    {
        var e = new Escenario();
        var clase = await e.Calendario.AgregarClaseAsync(new DateTime(2026, 9, 7), "Clase 1");

        await Verificar.LanzaAsync<ReglaDeNegocioException>(() => e.Asistencia.ObtenerPlanillaAsync(clase.Id));
    }

    [TestMethod]
    public async Task CA3_3_GuardarAlmacenaElEstadoDeCadaEstudiante()
    {
        var (e, clase) = await ConDosActivosAsync();
        var planilla = await e.Asistencia.ObtenerPlanillaAsync(clase.Id);
        planilla.Filas.First(f => f.Legajo == "L2").Presente = false; // CA 3.2

        await e.Asistencia.GuardarAsync(clase.Id, planilla.Filas);

        var guardada = await e.Asistencia.ObtenerPlanillaAsync(clase.Id);
        Assert.IsTrue(guardada.YaRegistrada);
        Assert.IsTrue(guardada.Filas.First(f => f.Legajo == "L1").Presente);
        Assert.IsFalse(guardada.Filas.First(f => f.Legajo == "L2").Presente);
    }

    [TestMethod]
    public async Task CA3_4_UnaClaseConRegistroSeInformaComoYaRegistrada()
    {
        var (e, clase) = await ConDosActivosAsync();
        var planilla = await e.Asistencia.ObtenerPlanillaAsync(clase.Id);
        await e.Asistencia.GuardarAsync(clase.Id, planilla.Filas);

        var otra = await e.Asistencia.ObtenerPlanillaAsync(clase.Id);

        Assert.IsTrue(otra.YaRegistrada);
    }

    [TestMethod]
    public async Task CA3_4_GuardarDosVecesLaMismaClaseSeRechaza()
    {
        var (e, clase) = await ConDosActivosAsync();
        var planilla = await e.Asistencia.ObtenerPlanillaAsync(clase.Id);
        await e.Asistencia.GuardarAsync(clase.Id, planilla.Filas);

        await Verificar.LanzaAsync<ReglaDeNegocioException>(
            () => e.Asistencia.GuardarAsync(clase.Id, planilla.Filas));
    }

    [TestMethod]
    public async Task CA4_3_CorregirActualizaElRegistroHistorico()
    {
        var (e, clase) = await ConDosActivosAsync();
        var planilla = await e.Asistencia.ObtenerPlanillaAsync(clase.Id);
        planilla.Filas.First(f => f.Legajo == "L1").Presente = false;
        await e.Asistencia.GuardarAsync(clase.Id, planilla.Filas);

        var guardada = await e.Asistencia.ObtenerPlanillaAsync(clase.Id);
        guardada.Filas.First(f => f.Legajo == "L1").Presente = true; // era una ausencia justificada
        int cambios = await e.Asistencia.CorregirAsync(clase.Id, guardada.Filas);

        Assert.AreEqual(1, cambios);
        var final = await e.Asistencia.ObtenerPlanillaAsync(clase.Id);
        Assert.IsTrue(final.Filas.First(f => f.Legajo == "L1").Presente);
    }

    [TestMethod]
    public async Task CA4_3_CorregirSinCambiosDevuelveCero()
    {
        var (e, clase) = await ConDosActivosAsync();
        var planilla = await e.Asistencia.ObtenerPlanillaAsync(clase.Id);
        await e.Asistencia.GuardarAsync(clase.Id, planilla.Filas);

        int cambios = await e.Asistencia.CorregirAsync(clase.Id, planilla.Filas);

        Assert.AreEqual(0, cambios);
    }

    [TestMethod]
    public async Task CA4_1_NoSePuedeCorregirUnaClaseSinAsistenciaRegistrada()
    {
        var (e, clase) = await ConDosActivosAsync();
        var planilla = await e.Asistencia.ObtenerPlanillaAsync(clase.Id);

        await Verificar.LanzaAsync<ReglaDeNegocioException>(
            () => e.Asistencia.CorregirAsync(clase.Id, planilla.Filas));
    }
}
