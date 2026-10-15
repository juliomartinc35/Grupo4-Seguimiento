using App_v3.Models;

namespace App_v3.ViewModels
{
    public class EstudianteItemViewModel : BindableObject
    {
        // Se mantiene el nombre "Estudiante" para no tocar el XAML
        // (Estudiante.NombreCompleto, Estudiante.Legajo, Estudiante.Email)
        public Persona Estudiante { get; }
        public double Promedio { get; }
        public int PorcentajeAsistencia { get; }

        public bool EnRiesgo => RiesgoAcademico.EnRiesgo(Promedio, PorcentajeAsistencia);

        public EstudianteItemViewModel(Persona estudiante, double promedio, int porcentajeAsistencia)
        {
            Estudiante = estudiante;
            Promedio = promedio;
            PorcentajeAsistencia = porcentajeAsistencia;
        }
    }
}