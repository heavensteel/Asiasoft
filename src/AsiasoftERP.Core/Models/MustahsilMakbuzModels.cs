using System;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// DTO representing an official Farmer/Producer Purchase Receipt (Müstahsil Makbuzu)
/// according to V.U.K. Madde 235 and stored in MariaDB 'mustahsilmakbuzu'.
/// </summary>
public class MustahsilMakbuzDto
{
    public double IdNo { get; set; }
    public string SirKod { get; set; } = "1";
    public double GirisKodu { get; set; }
    public double CariIdNo { get; set; }
    public string BagkurNo { get; set; } = string.Empty;
    public string TcNo { get; set; } = string.Empty;
    public string AdiSoyadi { get; set; } = string.Empty;
    public string BabaAdi { get; set; } = string.Empty;
    public string DogumTarihiYeri { get; set; } = string.Empty;
    public string Adresi { get; set; } = string.Empty;
    public double TgKodu { get; set; }
    public string TgAdi { get; set; } = string.Empty;
    public string KKullanici { get; set; } = string.Empty;
    public string DKullanici { get; set; } = string.Empty;
    public DateTime Tarih { get; set; } = DateTime.Today;
    public DateTime? IslemTarihi { get; set; } = DateTime.Now;
    public string Bolgesi { get; set; } = string.Empty;
    public double MakNo { get; set; }
    public string MakbuzNo { get; set; } = string.Empty;
    public string MalKodu { get; set; } = string.Empty;
    public string MalAdi { get; set; } = string.Empty;
    public double Miktar { get; set; } // Net KG
    public double Fiyati { get; set; } // Birim Fiyat TL/KG
    public double Tutari { get; set; } // Brüt Tutar = Miktar * Fiyat
    public double StopajOrani { get; set; } = 2.0; // Standart Borsa Tescilli %2 Stopaj
    public double StopajTutari { get; set; }
    public double BagkurOrani { get; set; } = 2.0; // Standart %2 Bağkur
    public double BagkurTutari { get; set; }
    public double TicBorOrani { get; set; } = 0.2; // Ticaret Borsası Tescil Payı %0.2
    public double TicBorTutari { get; set; }
    public double NetKesinti { get; set; } // Stopaj + Bagkur + TicBor
    public double Odenen { get; set; } // Net Ödenen = Tutari - NetKesinti
    public string Yaziile { get; set; } = string.Empty;
    public string KunyeNo { get; set; } = string.Empty; // HKS Künye No
    public string Sehir { get; set; } = string.Empty;
    public string Ilece { get; set; } = string.Empty;
    public double IsyeriNo { get; set; } = 1;
    public string YetkiKodu { get; set; } = "MERKEZ";

    // GİB 22.05.2026 Duyurusu & V1.1 Kılavuzu (05.11.2026 Zorunlu SMS Doğrulama)
    public string SmsDogrulamaKodu { get; set; } = string.Empty;
    public string SmsTelefon { get; set; } = string.Empty;
    public string SmsOperator { get; set; } = "NETGSM İLETİŞİM BİLGİ TEKNOLOJİLERİ A.Ş.";
    public string SmsOperatorVkn { get; set; } = "6310507301";
    public DateTime? SmsDogrulamaTarihi { get; set; }
    public string SmsDurumu { get; set; } = "BEKLİYOR";

    public bool IsSmsDogrulandi => SmsDurumu == "DOĞRULANDI" && !string.IsNullOrWhiteSpace(SmsDogrulamaKodu);
    public string SmsDurumuBadge => IsSmsDogrulandi ? $"🛡️ DOĞRULANDI ({SmsDogrulamaKodu})" : "⏳ BEKLİYOR";

    // Formatted Display Helpers
    public string TarihFormatted => Tarih.ToString("dd.MM.yyyy");
    public string MiktarFormatted => Miktar.ToString("N0") + " KG";
    public string FiyatFormatted => Fiyati.ToString("N2") + " TL";
    public string BrutTutarFormatted => Tutari.ToString("N2") + " TL";
    public string StopajFormatted => StopajTutari.ToString("N2") + " TL";
    public string BagkurFormatted => BagkurTutari.ToString("N2") + " TL";
    public string BorsaFormatted => TicBorTutari.ToString("N2") + " TL";
    public string NetKesintiFormatted => NetKesinti.ToString("N2") + " TL";
    public string OdenenFormatted => Odenen.ToString("N2") + " TL";

    /// <summary>
    /// Generates GİB UBL-TR 1.2.1 / UBL 2.1 compliant e-Müstahsil Makbuzu XML
    /// implementing GİB Technical Guide V1.1 (22.05.2026) mandatory SMS verification tags.
    /// </summary>
    public string GenerateGibUblTrXml()
    {
        var docGuid = Guid.NewGuid().ToString("D");
        var issueDateStr = Tarih.ToString("yyyy-MM-dd");
        var issueTimeStr = (IslemTarihi ?? DateTime.Now).ToString("HH:mm:ss");

        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!--
  Gelir İdaresi Başkanlığı (GİB) e-Müstahsil Makbuzu UBL-TR 1.2.1
  22.05.2026 Tarihli Duyuru & e-Müstahsil Makbuzu Teknik Kılavuzu V1.1
  Zorunluluk Başlangıç Tarihi: 05.11.2026 (Islak İmza Yerine SMS Doğrulama)
-->
<CreditNote xmlns=""urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2""
            xmlns:cac=""urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2""
            xmlns:cbc=""urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2""
            xmlns:ccts=""urn:un:unece:uncefact:documentation:2""
            xmlns:ds=""http://www.w3.org/2000/09/xmldsig#""
            xmlns:ext=""urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2""
            xmlns:ubltr=""urn:oasis:names:specification:ubl:schema:xsd:TurkishCustomizationExtensionComponents"">
  <cbc:UBLVersionID>2.1</cbc:UBLVersionID>
  <cbc:CustomizationID>TR1.2.1</cbc:CustomizationID>
  <cbc:ProfileID>EARSIVBELGE</cbc:ProfileID>
  <cbc:ID>{MakbuzNo}</cbc:ID>
  <cbc:CopyIndicator>false</cbc:CopyIndicator>
  <cbc:UUID>{docGuid}</cbc:UUID>
  <cbc:IssueDate>{issueDateStr}</cbc:IssueDate>
  <cbc:IssueTime>{issueTimeStr}</cbc:IssueTime>
  <cbc:CreditNoteTypeCode>MUSTAHSILMAKBUZ</cbc:CreditNoteTypeCode>
  <cbc:Note>GİB 22.05.2026 Duyurusu &amp; V1.1 Kılavuzu Uyarınca SMS Onaylıdır.</cbc:Note>
  <cbc:Note>Yazı ile: {Yaziile}</cbc:Note>
  <cbc:DocumentCurrencyCode>TRY</cbc:DocumentCurrencyCode>
  <cac:AccountingSupplierParty>
    <cac:Party>
      <cac:PartyIdentification>
        <cbc:ID schemeID=""VKN"">8330051553</cbc:ID>
      </cac:PartyIdentification>
      <cac:PartyName>
        <cbc:Name>TEK ASYA TARIM ÜRÜNLERİ İNŞ. TUR. VE TİC. LTD. ŞTİ.</cbc:Name>
      </cac:PartyName>
      <cac:PostalAddress>
        <cbc:CitySubdivisionName>Antakya</cbc:CitySubdivisionName>
        <cbc:CityName>Hatay</cbc:CityName>
        <cac:Country>
          <cbc:Name>Türkiye</cbc:Name>
        </cac:Country>
      </cac:PostalAddress>
      <cac:PartyTaxScheme>
        <cac:TaxScheme>
          <cbc:Name>23 TEMMUZ V.D.</cbc:Name>
        </cac:TaxScheme>
      </cac:PartyTaxScheme>
      <cac:Contact>
        <!-- GİB V1.1 Bölüm 2.4: SMS Gönderen Operatör Bilgisi -->
        <cac:OtherCommunication>
          <cbc:ChannelCode name=""SMS_PROVIDER"">{SmsOperator}</cbc:ChannelCode>
          <cbc:Value>{SmsOperatorVkn}</cbc:Value>
        </cac:OtherCommunication>
      </cac:Contact>
    </cac:Party>
  </cac:AccountingSupplierParty>
  <cac:AccountingCustomerParty>
    <cac:Party>
      <cac:PartyIdentification>
        <cbc:ID schemeID=""TCKN"">{TcNo}</cbc:ID>
      </cac:PartyIdentification>
      <cac:PartyName>
        <cbc:Name>{AdiSoyadi}</cbc:Name>
      </cac:PartyName>
      <cac:PostalAddress>
        <cbc:StreetName>{Adresi}</cbc:StreetName>
        <cbc:CitySubdivisionName>{Ilece}</cbc:CitySubdivisionName>
        <cbc:CityName>{Sehir}</cbc:CityName>
        <cac:Country>
          <cbc:Name>Türkiye</cbc:Name>
        </cac:Country>
      </cac:PostalAddress>
      <cac:Contact>
        <!-- GİB V1.1 Bölüm 2.4: Müstahsil Telefon ve SMS Doğrulama Kodu -->
        <cbc:ID>{SmsDogrulamaKodu}</cbc:ID>
        <cbc:Name>SMS</cbc:Name>
        <cbc:Telephone>{SmsTelefon}</cbc:Telephone>
      </cac:Contact>
    </cac:Party>
  </cac:AccountingCustomerParty>
  <cac:TaxTotal>
    <cbc:TaxAmount currencyID=""TRY"">{StopajTutari:0.00}</cbc:TaxAmount>
    <cac:TaxSubtotal>
      <cbc:TaxableAmount currencyID=""TRY"">{Tutari:0.00}</cbc:TaxableAmount>
      <cbc:TaxAmount currencyID=""TRY"">{StopajTutari:0.00}</cbc:TaxAmount>
      <cbc:Percent>{StopajOrani:0.0}</cbc:Percent>
      <cac:TaxCategory>
        <cac:TaxScheme>
          <cbc:Name>GV STOPAJ</cbc:Name>
          <cbc:TaxTypeCode>0003</cbc:TaxTypeCode>
        </cac:TaxScheme>
      </cac:TaxCategory>
    </cac:TaxSubtotal>
  </cac:TaxTotal>
  <cac:LegalMonetaryTotal>
    <cbc:LineExtensionAmount currencyID=""TRY"">{Tutari:0.00}</cbc:LineExtensionAmount>
    <cbc:TaxExclusiveAmount currencyID=""TRY"">{Tutari:0.00}</cbc:TaxExclusiveAmount>
    <cbc:TaxInclusiveAmount currencyID=""TRY"">{Tutari:0.00}</cbc:TaxInclusiveAmount>
    <cbc:AllowanceTotalAmount currencyID=""TRY"">{NetKesinti:0.00}</cbc:AllowanceTotalAmount>
    <cbc:PayableAmount currencyID=""TRY"">{Odenen:0.00}</cbc:PayableAmount>
  </cac:LegalMonetaryTotal>
  <cac:CreditNoteLine>
    <cbc:ID>1</cbc:ID>
    <cbc:CreditedQuantity unitCode=""KGM"">{Miktar:0.00}</cbc:CreditedQuantity>
    <cbc:LineExtensionAmount currencyID=""TRY"">{Tutari:0.00}</cbc:LineExtensionAmount>
    <cac:Item>
      <cbc:Name>{MalAdi}</cbc:Name>
      <cac:SellersItemIdentification>
        <cbc:ID>{MalKodu}</cbc:ID>
      </cac:SellersItemIdentification>
      <cac:AdditionalItemProperty>
        <cbc:Name>HKS_KUNYE_NO</cbc:Name>
        <cbc:Value>{KunyeNo}</cbc:Value>
      </cac:AdditionalItemProperty>
    </cac:Item>
    <cac:Price>
      <cbc:PriceAmount currencyID=""TRY"">{Fiyati:0.00}</cbc:PriceAmount>
    </cac:Price>
  </cac:CreditNoteLine>
</CreditNote>";
    }
}

/// <summary>
/// Producer lookup DTO for farmers / müstahsiller with 11-digit TC Kimlik No
/// </summary>
public class MustahsilLookupDto
{
    public double FirKod { get; set; }
    public string FirmaAdi { get; set; } = string.Empty;
    public string TcNo { get; set; } = string.Empty;
    public string VergiDairesi { get; set; } = string.Empty;
    public string Adres { get; set; } = string.Empty;
    public string Sehir { get; set; } = string.Empty;
    public string Ilce { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string BagkurNo { get; set; } = string.Empty;
    public string BabaAdi { get; set; } = string.Empty;

    public string DisplayText => $"{TcNo} - {FirmaAdi} ({Ilce}/{Sehir})";
}

/// <summary>
/// Dashboard KPI Summary for Müstahsil Makbuzları
/// </summary>
public class MustahsilKpiDto
{
    public int ToplamMakbuzSayisi { get; set; }
    public double ToplamNetKg { get; set; }
    public double ToplamBrutTutar { get; set; }
    public double ToplamStopaj { get; set; }
    public double ToplamBagkur { get; set; }
    public double ToplamBorsa { get; set; }
    public double ToplamNetKesinti { get; set; }
    public double ToplamNetOdenen { get; set; }

    public string ToplamTonFormatted => (ToplamNetKg / 1000.0).ToString("N1") + " Ton";
    public string BrutHacimFormatted => ToplamBrutTutar.ToString("N0") + " TL";
    public string NetOdenenFormatted => ToplamNetOdenen.ToString("N0") + " TL";
    public string ToplamKesintiFormatted => ToplamNetKesinti.ToString("N0") + " TL";
    public string StopajFormatted => ToplamStopaj.ToString("N0") + " TL";
    public string BagkurFormatted => ToplamBagkur.ToString("N0") + " TL";
}
