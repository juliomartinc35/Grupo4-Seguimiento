using Microsoft.Maui.Graphics;
using App_v3.Models;

namespace App_v3.ViewModels;

/// <summary>Colores compartidos por las pantallas (CA 8.3: cada nivel de riesgo se distingue por color).</summary>
public static class Apariencia
{
    public static readonly Color Verde = Color.FromArgb("#2E7D32");
    public static readonly Color Naranja = Color.FromArgb("#E65100");
    public static readonly Color Rojo = Color.FromArgb("#C62828");
    public static readonly Color Gris = Color.FromArgb("#6B7280");

    public static Color Nivel(NivelRiesgo nivel) => nivel switch
    {
        NivelRiesgo.Alto => Rojo,
        NivelRiesgo.Medio => Naranja,
        _ => Verde
    };

    public static Color Activo(bool activo) => activo ? Verde : Gris;
}
