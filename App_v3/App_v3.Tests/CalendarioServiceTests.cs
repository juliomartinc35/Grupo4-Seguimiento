using App_v3.Seguimiento;

namespace App_v3.Tests;

[TestClass]
public sealed class CalendarioServiceTests
{
    [TestMethod]
    public async Task CA2_1_AgregarClaseLaDejaRegistradaEnElCalendario()
    {
        var e = new Escenario();

        await e.Calendario.AgregarClaseAsync(new DateTime(2026, 9, 7), "Clase 1");

        var clases = await e.Calendario.ListarClasesAsync();
        Assert.AreEqual(1, clases.Count);
        Assert.AreEqual("Clase 1", clases[0].Tema);
        Assert.AreEqual(new DateTime(2026, 9, 7), clases[0].Fecha);
    }

    [TestMethod]
    public async Task CA2_2_AgregarTrabajoLoAnadeAlCalendario()
    {
        var e = new Escenario();

        await e.Calendario.AgregarTrabajoAsync("TP1", new DateTime(2026, 9, 30));

        var trabajos = await e.Calendario.ListarTrabajosAsync();
        Assert.AreEqual(1, trabajos.Count);
        Assert.AreEqual("TP1", trabajos[0].Nombre);
        Assert.AreEqual(new DateTime(2026, 9, 30), trabajos[0].FechaLimite);
    }

    [TestMethod]
    public async Task CA2_3_ElCalendarioListaClasesYTrabajosOrdenadosPorFecha()
    {
        var e = new Escenario();
        await e.Calendario.AgregarClaseAsync(new DateTime(2026, 9, 14), "Clase 2");
        await e.Calendario.AgregarClaseAsync(new DateTime(2026, 9, 7), "Clase 1");
        await e.Calendario.AgregarTrabajoAsync("TP2", new DateTime(2026, 10, 20));
        await e.Calendario.AgregarTrabajoAsync("TP1", new DateTime(2026, 9, 30));

        var clases = await e.Calendario.ListarClasesAsync();
        var trabajos = await e.Calendario.ListarTrabajosAsync();

        Assert.AreEqual("Clase 1", clases[0].Tema);
        Assert.AreEqual("Clase 2", clases[1].Tema);
        Assert.AreEqual("TP1", trabajos[0].Nombre);
        Assert.AreEqual("TP2", trabajos[1].Nombre);
    }

    [TestMethod]
    [DataRow(true, "")]
    [DataRow(true, "   ")]
    [DataRow(false, "Clase 1")]
    public async Task CA2_4_ClaseSinDatosObligatoriosNoSeGuarda(bool conFecha, string tema)
    {
        var e = new Escenario();
        DateTime? fecha = conFecha ? new DateTime(2026, 9, 7) : null;

        await Verificar.LanzaAsync<ReglaDeNegocioException>(() => e.Calendario.AgregarClaseAsync(fecha, tema));

        Assert.AreEqual(0, (await e.Calendario.ListarClasesAsync()).Count);
    }

    [TestMethod]
    [DataRow("", true)]
    [DataRow("TP1", false)]
    public async Task CA2_4_TrabajoSinDatosObligatoriosNoSeGuarda(string nombre, bool conFecha)
    {
        var e = new Escenario();
        DateTime? limite = conFecha ? new DateTime(2026, 9, 30) : null;

        await Verificar.LanzaAsync<ReglaDeNegocioException>(() => e.Calendario.AgregarTrabajoAsync(nombre, limite));

        Assert.AreEqual(0, (await e.Calendario.ListarTrabajosAsync()).Count);
    }

    [TestMethod]
    public async Task NoSePermiteCargarDosClasesElMismoDia()
    {
        var e = new Escenario();
        await e.Calendario.AgregarClaseAsync(new DateTime(2026, 9, 7), "Clase 1");

        await Verificar.LanzaAsync<ReglaDeNegocioException>(
            () => e.Calendario.AgregarClaseAsync(new DateTime(2026, 9, 7), "Otra"));
    }
}
