namespace App_v3.Core;

public enum NivelRiesgo
{
    Bajo = 0,
    Medio = 1,
    Alto = 2
}

public record Umbrales(
    int InasistenciasMedio = 3,
    int InasistenciasAlto = 5,
    int TpsNoEntregadosMedio = 1,
    int TpsNoEntregadosAlto = 3);

public class UmbralesInvalidosException(string mensaje) : InvalidOperationException(mensaje);

public class ConfiguracionRiesgo
{
    public Umbrales Actual { get; private set; } = new();

    // CA 7.1: registra la nueva configuración. CA 7.3: rechaza valores contradictorios.
    public void Configurar(Umbrales nuevos)
    {
        ArgumentNullException.ThrowIfNull(nuevos);

        if (nuevos.InasistenciasMedio < 1 || nuevos.InasistenciasAlto < 1 ||
            nuevos.TpsNoEntregadosMedio < 1 || nuevos.TpsNoEntregadosAlto < 1)
            throw new UmbralesInvalidosException("Los umbrales deben ser mayores o iguales a 1.");

        if (nuevos.InasistenciasMedio >= nuevos.InasistenciasAlto)
            throw new UmbralesInvalidosException("El umbral Medio de inasistencias debe ser menor que el Alto.");

        if (nuevos.TpsNoEntregadosMedio >= nuevos.TpsNoEntregadosAlto)
            throw new UmbralesInvalidosException("El umbral Medio de TPs no entregados debe ser menor que el Alto.");

        Actual = nuevos;
    }

    // CA 7.2: aplica los umbrales actuales. El nivel general es el más alto de las dos señales.
    public NivelRiesgo CalcularNivel(int inasistencias, int tpsNoEntregados)
    {
        var porInasistencias = NivelPorSenal(inasistencias, Actual.InasistenciasMedio, Actual.InasistenciasAlto);
        var porTps = NivelPorSenal(tpsNoEntregados, Actual.TpsNoEntregadosMedio, Actual.TpsNoEntregadosAlto);

        return (NivelRiesgo)Math.Max((int)porInasistencias, (int)porTps);
    }

    private static NivelRiesgo NivelPorSenal(int cantidad, int umbralMedio, int umbralAlto)
    {
        if (cantidad >= umbralAlto) return NivelRiesgo.Alto;
        if (cantidad >= umbralMedio) return NivelRiesgo.Medio;
        return NivelRiesgo.Bajo;
    }
}
