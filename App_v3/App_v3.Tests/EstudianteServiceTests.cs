using App_v3.Seguimiento;

namespace App_v3.Tests;

[TestClass]
public sealed class EstudianteServiceTests
{
    [TestMethod]
    public async Task CA1_1_AltaDeEstudianteApareceEnLaLista()
    {
        var e = new Escenario();

        await e.Alta("L1", "111");

        var lista = await e.Estudiantes.ListarAsync();
        Assert.AreEqual(1, lista.Count);
        Assert.AreEqual("L1", lista[0].Legajo);
        Assert.IsTrue(lista[0].Activo);
    }

    [TestMethod]
    public async Task CA1_1_TelefonoEsOpcional()
    {
        var e = new Escenario();

        var nuevo = await e.Estudiantes.AltaAsync("L1", "111", "Juan", "Pérez", "juan@mail.com");

        Assert.AreEqual(string.Empty, nuevo.Telefono);
    }

    [TestMethod]
    [DataRow("", "111", "Juan", "Pérez", "a@b.com", "Legajo")]
    [DataRow("L1", " ", "Juan", "Pérez", "a@b.com", "DNI")]
    [DataRow("L1", "111", "", "Pérez", "a@b.com", "Nombre")]
    [DataRow("L1", "111", "Juan", "", "a@b.com", "Apellido")]
    [DataRow("L1", "111", "Juan", "Pérez", "", "Email")]
    public async Task CA1_1_AltaSinCamposObligatoriosSeRechazaYNoGuarda(
        string legajo, string dni, string nombre, string apellido, string email, string campoFaltante)
    {
        var e = new Escenario();

        var ex = await Verificar.LanzaAsync<ReglaDeNegocioException>(
            () => e.Estudiantes.AltaAsync(legajo, dni, nombre, apellido, email));

        Assert.IsTrue(ex.Message.Contains(campoFaltante));
        Assert.AreEqual(0, (await e.Estudiantes.ListarAsync()).Count);
    }

    [TestMethod]
    public async Task CA1_2_RechazaLegajoDuplicado()
    {
        var e = new Escenario();
        await e.Alta("L1", "111");

        var ex = await Verificar.LanzaAsync<ReglaDeNegocioException>(() => e.Alta("L1", "222"));

        Assert.IsTrue(ex.Message.Contains("legajo"));
        Assert.AreEqual(1, (await e.Estudiantes.ListarAsync()).Count);
    }

    [TestMethod]
    public async Task CA1_2_RechazaDniDuplicado()
    {
        var e = new Escenario();
        await e.Alta("L1", "111");

        var ex = await Verificar.LanzaAsync<ReglaDeNegocioException>(() => e.Alta("L2", "111"));

        Assert.IsTrue(ex.Message.Contains("DNI"));
        Assert.AreEqual(1, (await e.Estudiantes.ListarAsync()).Count);
    }

    [TestMethod]
    public async Task CA1_3_ListaTodosLosEstudiantesRegistrados()
    {
        var e = new Escenario();
        await e.Alta("L1", "111");
        await e.Alta("L2", "222");

        Assert.AreEqual(2, (await e.Estudiantes.ListarAsync()).Count);
    }

    [TestMethod]
    public async Task CA1_4_InactivarNoBorraAlEstudiante()
    {
        var e = new Escenario();
        var uno = await e.Alta("L1", "111");
        await e.Alta("L2", "222");

        await e.Estudiantes.InactivarAsync(uno.Id);

        Assert.AreEqual(2, (await e.Estudiantes.ListarAsync()).Count);
        var activos = await e.Estudiantes.ListarActivosAsync();
        Assert.AreEqual(1, activos.Count);
        Assert.AreEqual("L2", activos[0].Legajo);
    }

    [TestMethod]
    public async Task CA13_1_LosInactivosSeListanAparte()
    {
        var e = new Escenario();
        var uno = await e.Alta("L1", "111");
        await e.Alta("L2", "222");
        await e.Estudiantes.InactivarAsync(uno.Id);

        var inactivos = await e.Estudiantes.ListarInactivosAsync();

        Assert.AreEqual(1, inactivos.Count);
        Assert.AreEqual("L1", inactivos[0].Legajo);
        Assert.IsFalse(inactivos[0].Activo);
        Assert.IsNotNull(inactivos[0].FechaBaja);
    }

    [TestMethod]
    public async Task CA1_2_UnLegajoDeUnInactivoTambienCuentaComoDuplicado()
    {
        var e = new Escenario();
        var uno = await e.Alta("L1", "111");
        await e.Estudiantes.InactivarAsync(uno.Id);

        await Verificar.LanzaAsync<ReglaDeNegocioException>(() => e.Alta("L1", "999"));
    }

    [TestMethod]
    public async Task ReactivarVuelveAPonerAlEstudianteEnActivos()
    {
        var e = new Escenario();
        var uno = await e.Alta("L1", "111");
        await e.Estudiantes.InactivarAsync(uno.Id);

        await e.Estudiantes.ReactivarAsync(uno.Id);

        var activos = await e.Estudiantes.ListarActivosAsync();
        Assert.AreEqual(1, activos.Count);
        Assert.IsNull(activos[0].FechaBaja);
    }

    [TestMethod]
    public async Task InactivarUnEstudianteInexistenteLanzaExcepcion()
    {
        var e = new Escenario();

        await Verificar.LanzaAsync<ReglaDeNegocioException>(() => e.Estudiantes.InactivarAsync(99));
    }
}
