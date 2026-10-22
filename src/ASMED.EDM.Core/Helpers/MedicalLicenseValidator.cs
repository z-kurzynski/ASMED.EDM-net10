namespace ASMED.EDM.Core.Helpers;

/// <summary>
/// Walidacja numeru prawa wykonywania zawodu lekarza (NPWZ).
/// NPWZ lekarza ma 7 cyfr; pierwsza cyfra jest kontrolna i równa się
/// (suma cyfr 2..7 pomnożonych przez wagi 1..6) mod 11, przy czym nie może wynosić 0.
/// </summary>
public static class MedicalLicenseValidator
{
    public static bool IsValidDoctorPwz(string? pwz)
    {
        if (string.IsNullOrWhiteSpace(pwz))
        {
            return false;
        }

        var value = pwz.Trim();
        if (value.Length != 7 || !value.All(char.IsAsciiDigit))
        {
            return false;
        }

        var sum = 0;
        for (var i = 1; i < 7; i++)
        {
            sum += (value[i] - '0') * i;
        }

        var checkDigit = sum % 11;
        return checkDigit != 0 && checkDigit != 10 && checkDigit == value[0] - '0';
    }
}
