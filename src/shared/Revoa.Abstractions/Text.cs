using System.Globalization;
using System.Text;

namespace Revoa.Abstractions;

// Utilitários de texto compartilhados entre módulos (busca amigável ao pt-BR).
public static class Texto
{
    /// <summary>
    /// Remove diacríticos (FormD + descarte de non-spacing marks): "violão" → "violao",
    /// "coração" → "coracao". Case-preserving — quem decide o casing é quem compara.
    /// </summary>
    public static string SemAcento(string valor)
    {
        if (string.IsNullOrEmpty(valor))
        {
            return valor;
        }

        var sb = new StringBuilder(valor.Length);
        foreach (var ch in valor.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
