using App_v3.Seguimiento;

namespace App_v3.Tests;

[TestClass]
public sealed class EntregaServiceTests
{
    private static async Task<(Escenario e, TrabajoCursada trabajo)> ConDosActivosAsync()
    {
        var e = new Escenario();
        await e.Alta("L1", "111", "Ana", "Álvarez");
        await e.Alta("L2", "222", "Beto", "Benítez");
        var trabajo = await e.Calendario.AgregarTrabajoAsync("TP1", new DateTime(2026, 9, 30));
        return (e, trabajo);
    }

    [TestMethod]
    public async Task CA5_3_ListaATodosLosEstudiantesActivos()
    {
        var (e, trabajo) = await ConDosActivosAsync();
        var baja = (await e.Estudiantes.ListarAsync()).First(x => x.Legajo == "L2");
        await e.Estudiantes.InactivarAsync(baja.Id);

        var planilla = await e.Entregas.ObtenerPlanillaAsync(trabajo.Id);

        Assert.IsFalse(planilla.YaRegistrado);
        Assert.AreEqual(1, planilla.Filas.Count);
        Assert.AreEqual("L1", planilla.Filas[0].Legajo);
    }

    [TestMethod]
    public async Task CA5_1_RegistraLaEntregaDeUnEstudiante()
    {
        var (e, trabajo) = await ConDosActivosAsync();
        var planilla = await e.Entregas.ObtenerPlanillaAsync(trabajo.Id);
        planilla.Filas.First(f => f.Legajo == "L1").Entregado = true;

        await e.Entregas.GuardarAsync(trabajo.Id, planilla.Filas);

        var guardada = await e.Entregas.ObtenerPlanillaAsync(trabajo.Id);
        Assert.IsTrue(guardada.Filas.First(f => f.Legajo == "L1").Entregado);
    }

    [TestMethod]
    public async Task CA5_2_RegistraLaFaltaDeEntrega()
    {
        var (e, trabajo) = await ConDosActivosAsync();
        var planilla = await e.Entregas.ObtenerPlanillaAsync(trabajo.Id);
        planilla.Filas.First(f => f.Legajo == "L1").Entregado = true;

        await e.Entregas.GuardarAsync(trabajo.Id, planilla.Filas);

        var guardada = await e.Entregas.ObtenerPlanillaAsync(trabajo.Id);
        Assert.IsFalse(guardada.Filas.First(f => f.Legajo == "L2").Entregado);
    }

    [TestMethod]
    public async Task CA5_4_UnTrabajoYaRegistradoMuestraElEstadoGuardado()
    {
        var (e, trabajo) = await ConDosActivosAsync();
        var planilla = await e.Entregas.ObtenerPlanillaAsync(trabajo.Id);
        planilla.Filas.First(f => f.Legajo == "L2").Entregado = true;
        await e.Entregas.GuardarAsync(trabajo.Id, planilla.Filas);

        var otra = await e.Entregas.ObtenerPlanillaAsync(trabajo.Id);

        Assert.IsTrue(otra.YaRegistrado);
        Assert.IsTrue(otra.Filas.First(f => f.Legajo == "L2").Entregado);
        Assert.IsFalse(otra.Filas.First(f => f.Legajo == "L1").Entregado);
    }

    [TestMethod]
    public async Task CA5_4_VolverAGuardarActualizaElEstadoSinDuplicarRegistros()
    {
        var (e, trabajo) = await ConDosActivosAsync();
        var planilla = await e.Entregas.ObtenerPlanillaAsync(trabajo.Id);
        await e.Entregas.GuardarAsync(trabajo.Id, planilla.Filas);

        var segunda = await e.Entregas.ObtenerPlanillaAsync(trabajo.Id);
        segunda.Filas.First(f => f.Legajo == "L1").Entregado = true;
        await e.Entregas.GuardarAsync(trabajo.Id, segunda.Filas);

        var final = await e.Entregas.ObtenerPlanillaAsync(trabajo.Id);
        Assert.AreEqual(2, final.Filas.Count);
        Assert.IsTrue(final.Filas.First(f => f.Legajo == "L1").Entregado);
    }
}
