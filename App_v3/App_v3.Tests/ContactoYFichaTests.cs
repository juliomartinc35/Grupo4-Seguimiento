using App_v3.Models;
using App_v3.Seguimiento;

namespace App_v3.Tests;

[TestClass]
public sealed class ContactoYFichaTests
{
    private static readonly DateTime Hoy = Escenario.Hoy;

    // ---------------- H10 ----------------

    [TestMethod]
    public async Task CA10_1_RegistrarUnContactoLoGuardaEnElHistorialDelEstudiante()
    {
        var e = new Escenario();
        var est = await e.Alta("L1", "111");

        await e.Contactos.RegistrarAsync(est.Id, Hoy, "Llamada", "Respondió", "Se comprometió a asistir.");

        var contactos = await e.Contactos.ListarAsync(est.Id);
        Assert.AreEqual(1, contactos.Count);
        Assert.AreEqual("Llamada", contactos[0].Medio);
        Assert.AreEqual("Se comprometió a asistir.", contactos[0].Notas);
    }

    [TestMethod]
    public async Task CA10_2_SinFechaMedioResultadoNiNotaSeRechazaYSeIndicanLosCampos()
    {
        var e = new Escenario();
        var est = await e.Alta("L1", "111");

        var ex = await Verificar.LanzaAsync<ReglaDeNegocioException>(
            () => e.Contactos.RegistrarAsync(est.Id, null, null, null, null));

        Assert.IsTrue(ex.Message.Contains("Fecha"));
        Assert.IsTrue(ex.Message.Contains("Medio"));
        Assert.IsTrue(ex.Message.Contains("Resultado"));
        Assert.IsTrue(ex.Message.Contains("Nota del contacto"));
        Assert.AreEqual(0, (await e.Contactos.ListarAsync(est.Id)).Count);
    }

    [TestMethod]
    public async Task CA10_2_UnMedioDistintoDeLlamadaOEmailSeRechaza()
    {
        var e = new Escenario();
        var est = await e.Alta("L1", "111");

        await Verificar.LanzaAsync<ReglaDeNegocioException>(
            () => e.Contactos.RegistrarAsync(est.Id, Hoy, "Telegrama", "Respondió", "Nota."));
    }

    // ---------------- H9 ----------------

    [TestMethod]
    public async Task CA9_1_LaFichaMuestraLosDatosPersonales()
    {
        var e = new Escenario();
        var est = await e.Estudiantes.AltaAsync("L1", "40111222", "Ana", "Álvarez", "ana@mail.com", "11-5555-0101");

        var ficha = await e.Fichas.ObtenerAsync(est.Id, Hoy);

        Assert.AreEqual("L1", ficha.Estudiante.Legajo);
        Assert.AreEqual("40111222", ficha.Estudiante.Dni);
        Assert.AreEqual("Ana Álvarez", ficha.Estudiante.NombreCompleto);
        Assert.AreEqual("ana@mail.com", ficha.Estudiante.Email);
        Assert.AreEqual("11-5555-0101", ficha.Estudiante.Telefono);
    }

    [TestMethod]
    public async Task CA9_2_LaFichaMuestraNivelYMotivo()
    {
        var e = new Escenario();
        var est = await e.Alta("L1", "111");
        await e.TrabajoConEntregas(Hoy.AddDays(-20));
        await e.TrabajoConEntregas(Hoy.AddDays(-10));
        await e.TrabajoConEntregas(Hoy.AddDays(-3));

        var ficha = await e.Fichas.ObtenerAsync(est.Id, Hoy);

        Assert.AreEqual(NivelRiesgo.Alto, ficha.Nivel);
        Assert.IsTrue(ficha.Motivo.Contains("3 TPs vencidos sin entregar"));
    }

    [TestMethod]
    public async Task CA9_3_LaFichaIndicaSiElEstudianteEstaActivoOInactivo()
    {
        var e = new Escenario();
        var est = await e.Alta("L1", "111");
        Assert.IsTrue((await e.Fichas.ObtenerAsync(est.Id, Hoy)).Estudiante.Activo);

        await e.Estudiantes.InactivarAsync(est.Id);

        Assert.IsFalse((await e.Fichas.ObtenerAsync(est.Id, Hoy)).Estudiante.Activo);
    }

    [TestMethod]
    public async Task LaFichaDeUnEstudianteInexistenteSeRechaza()
    {
        var e = new Escenario();

        await Verificar.LanzaAsync<ReglaDeNegocioException>(() => e.Fichas.ObtenerAsync(99, Hoy));
    }

    // ---------------- H12 ----------------

    private static async Task<(Escenario e, Cursante est)> ConHistorialAsync()
    {
        var e = new Escenario();
        var est = await e.Alta("L1", "111");
        await e.ClaseConAsistencia(Hoy.AddDays(-14), "L1"); // ausente
        await e.ClaseConAsistencia(Hoy.AddDays(-7));        // presente
        await e.TrabajoConEntregas(Hoy.AddDays(-10), "L1"); // entregó
        await e.TrabajoConEntregas(Hoy.AddDays(-3));        // no entregó
        return (e, est);
    }

    [TestMethod]
    public async Task CA12_1_ElHistorialMuestraFechaYEstadoDeCadaClase()
    {
        var (e, est) = await ConHistorialAsync();

        var ficha = await e.Fichas.ObtenerAsync(est.Id, Hoy);

        Assert.AreEqual(2, ficha.Asistencias.Count);
        Assert.AreEqual(Hoy.AddDays(-14), ficha.Asistencias[0].Fecha);
        Assert.AreEqual("Ausente", ficha.Asistencias[0].Estado);
        Assert.AreEqual("Presente", ficha.Asistencias[1].Estado);
    }

    [TestMethod]
    public async Task CA12_2_ElHistorialMuestraCadaTrabajoYSuEstado()
    {
        var (e, est) = await ConHistorialAsync();

        var ficha = await e.Fichas.ObtenerAsync(est.Id, Hoy);

        Assert.AreEqual(2, ficha.Entregas.Count);
        Assert.AreEqual("Entregado", ficha.Entregas[0].Estado);
        Assert.AreEqual("No entregado", ficha.Entregas[1].Estado);
    }

    [TestMethod]
    public async Task CA12_3_ElResumenTotalizaInasistenciasYTrabajosNoEntregados()
    {
        var (e, est) = await ConHistorialAsync();

        var ficha = await e.Fichas.ObtenerAsync(est.Id, Hoy);

        Assert.AreEqual(1, ficha.TotalInasistencias);
        Assert.AreEqual(1, ficha.TotalTpsNoEntregados);
    }

    // ---------------- H13 ----------------

    [TestMethod]
    public async Task CA12_4_CA13_2_UnInactivoConservaSuHistorialYNoTieneNivel()
    {
        var (e, est) = await ConHistorialAsync();
        await e.Estudiantes.InactivarAsync(est.Id);

        var ficha = await e.Fichas.ObtenerAsync(est.Id, Hoy);

        Assert.IsFalse(ficha.Estudiante.Activo);
        Assert.AreEqual(2, ficha.Asistencias.Count);
        Assert.AreEqual(2, ficha.Entregas.Count);
        Assert.IsNull(ficha.Nivel);
        Assert.IsTrue(ficha.Motivo.Contains("inactivo"));
    }

    [TestMethod]
    public async Task CA13_3_DarDeBajaConservaLosRegistrosDeAsistenciaYEntrega()
    {
        var (e, est) = await ConHistorialAsync();

        await e.Estudiantes.InactivarAsync(est.Id);

        Assert.AreEqual(2, (await e.Db.GetAllAsync<RegistroAsistencia>()).Count);
        Assert.AreEqual(2, (await e.Db.GetAllAsync<RegistroEntrega>()).Count);
        Assert.AreEqual(1, (await e.Estudiantes.ListarAsync()).Count);
    }
}
