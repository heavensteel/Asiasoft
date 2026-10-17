using System;
using System.Text;

namespace AsiasoftERP.Core.Helpers;

public static class TurkishNumberToWordsHelper
{
    private static readonly string[] Birler = { "", "BİR", "İKİ", "ÜÇ", "DÖRT", "BEŞ", "ALTI", "YEDİ", "SEKİZ", "DOKUZ" };
    private static readonly string[] Onlar = { "", "ON", "YİRMİ", "OTUZ", "KIRK", "ELLİ", "ALTMIŞ", "YETMİŞ", "SEKSEN", "DOKSAN" };
    private static readonly string[] Basamaklar = { "", "BİN", "MİLYON", "MİLYAR", "TRİLYON" };

    public static string ToFinancialText(double amount)
    {
        if (double.IsNaN(amount) || double.IsInfinity(amount)) return "SIFIR TÜRK LİRASI";

        decimal decVal = (decimal)Math.Round(amount, 2);
        long tamKisim = (long)Math.Truncate(decVal);
        int kurus = (int)Math.Round((Math.Abs(decVal) - Math.Abs(tamKisim)) * 100);

        string tamYazi = ConvertIntegerToWords(Math.Abs(tamKisim));
        if (string.IsNullOrWhiteSpace(tamYazi)) tamYazi = "SIFIR";

        var sb = new StringBuilder();
        sb.Append("YALNIZ ");
        if (decVal < 0) sb.Append("EKSİ ");
        sb.Append(tamYazi);
        sb.Append(" TÜRK LİRASI");

        if (kurus > 0)
        {
            string kurusYazi = ConvertTwoDigits(kurus);
            sb.Append(" ");
            sb.Append(kurusYazi);
            sb.Append(" KURUŞ");
        }

        return sb.ToString().Trim();
    }

    private static string ConvertIntegerToWords(long n)
    {
        if (n == 0) return "";

        string sNumber = n.ToString();
        int step = 0;
        var sb = new StringBuilder();

        while (sNumber.Length > 0)
        {
            int subLen = Math.Min(3, sNumber.Length);
            string chunk = sNumber.Substring(sNumber.Length - subLen, subLen);
            sNumber = sNumber.Substring(0, sNumber.Length - subLen);

            int val = int.Parse(chunk);
            if (val > 0)
            {
                string chunkText = ConvertThreeDigits(val);
                // "BİR BİN" is not used in Turkish, only "BİN"
                if (step == 1 && val == 1) chunkText = "";

                string stepName = Basamaklar[step];
                string combined = (chunkText + " " + stepName).Trim();
                if (sb.Length > 0)
                    sb.Insert(0, combined + " ");
                else
                    sb.Insert(0, combined);
            }

            step++;
        }

        return sb.ToString().Trim();
    }

    private static string ConvertThreeDigits(int n)
    {
        var sb = new StringBuilder();
        int yuzler = n / 100;
        int kalan = n % 100;

        if (yuzler > 0)
        {
            if (yuzler > 1) sb.Append(Birler[yuzler]).Append(" ");
            sb.Append("YÜZ");
            if (kalan > 0) sb.Append(" ");
        }

        if (kalan > 0)
        {
            sb.Append(ConvertTwoDigits(kalan));
        }

        return sb.ToString().Trim();
    }

    private static string ConvertTwoDigits(int n)
    {
        var sb = new StringBuilder();
        int onlar = n / 10;
        int birler = n % 10;

        if (onlar > 0) sb.Append(Onlar[onlar]);
        if (onlar > 0 && birler > 0) sb.Append(" ");
        if (birler > 0) sb.Append(Birler[birler]);

        return sb.ToString().Trim();
    }
}
