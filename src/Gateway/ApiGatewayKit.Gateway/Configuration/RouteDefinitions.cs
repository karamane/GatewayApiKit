namespace ApiGatewayKit.Gateway.Configuration;

/// <summary>
/// Route tanımları - ocelot.json'dan parse edilir
/// Admin panelde gösterim için kullanılır
/// </summary>
public class RouteDefinition
{
    /// <summary>
    /// Benzersiz route anahtarı
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Modül kodu (Auth, Bill, Usage vb.)
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Legacy path (Upstream)
    /// </summary>
    public string LegacyPath { get; set; } = string.Empty;

    /// <summary>
    /// Yeni path (Downstream)
    /// </summary>
    public string NewPath { get; set; } = string.Empty;

    /// <summary>
    /// HTTP metotları
    /// </summary>
    public List<string> HttpMethods { get; set; } = new();

    /// <summary>
    /// Açıklama
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Öncelik
    /// </summary>
    public int Priority { get; set; } = 10;

    /// <summary>
    /// Endpoint bazlı yüzde override (null ise modül oranını kullanır)
    /// </summary>
    public int? OverridePercentage { get; set; }
}

/// <summary>
/// Modül özeti - Admin panelde gösterim için
/// </summary>
public class ModuleSummary
{
    /// <summary>
    /// Modül kodu
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Modül adı (Türkçe)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Açıklama
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Endpoint sayısı
    /// </summary>
    public int EndpointCount { get; set; }

    /// <summary>
    /// Yeni sisteme yönlendirme yüzdesi
    /// </summary>
    public int NewPercentage { get; set; }

    /// <summary>
    /// Bu modüle ait route'lar
    /// </summary>
    public List<RouteDefinition> Routes { get; set; } = new();
}

/// <summary>
/// Modül tanımları - Türkçe isimler ve açıklamalar
/// </summary>
public static class ModuleDefinitions
{
    public static readonly Dictionary<string, (string Name, string Description)> Modules = new()
    {
        ["Auth"] = ("Kimlik Doğrulama", "Giriş, oturum ve token işlemleri"),
        ["Usage"] = ("Kullanım", "Kullanım geçmişi ve detayları"),
        ["Bill"] = ("Fatura", "Fatura sorgulama, ödeme ve e-fatura işlemleri"),
        ["Product"] = ("Ürün", "Ürün listeleme, detay ve UNICA işlemleri"),
        ["Campaigns"] = ("Kampanyalar", "Kampanya listeleme ve katılım işlemleri"),
        ["Packages"] = ("Paketler", "Paket listeleme, değişiklik ve TİVİBU işlemleri"),
        ["PratikNet"] = ("Pratik Net", "Bağlantı sorunları, hız testi ve destek talepleri"),
        ["Guest"] = ("Misafir", "Misafir girişi ve randevu işlemleri"),
        ["Lead"] = ("Müşteri Adayı", "Müşteri adayı işlemleri"),
        ["Subscriber"] = ("Abone", "Abone bilgileri ve topluluk işlemleri"),
        ["Settings"] = ("Ayarlar", "Modem ve güvenlik ayarları"),
        ["EndToEnd"] = ("Uçtan Uca", "Sipariş ve randevu işlemleri"),
        ["Profile"] = ("Profil", "Kullanıcı profili ve geçmişi"),
        ["Address"] = ("Adres", "İl, ilçe, mahalle ve sokak sorguları"),
        ["Dashboard"] = ("Gösterge Paneli", "Ana sayfa ve özet bilgiler"),
        ["Thk"] = ("THK", "Taksitli hız kampanyaları"),
        ["Document"] = ("Doküman", "Belge gönderme ve alma işlemleri"),
        ["Otp"] = ("OTP", "Tek kullanımlık şifre işlemleri"),
        ["LineSuspension"] = ("Hat Askıya Alma", "Hat askıya alma ve iptal işlemleri"),
        ["AutoPayment"] = ("Otomatik Ödeme", "Otomatik ödeme talimatı işlemleri"),
        ["Flow"] = ("Akış", "İş akışı işlemleri"),
        ["Features"] = ("Özellikler", "OVIT ve özel teklifler"),
        ["BanaOzel"] = ("Bana Özel", "Promosyon kodları ve özel kampanyalar"),
        ["Digitt"] = ("Dijital Teklifler", "Dijital teklifler"),
        ["Image"] = ("Görsel", "Captcha ve görsel işlemleri"),
        ["GenericMessages"] = ("Genel Mesajlar", "Sistem mesajları"),
        ["Adid"] = ("ADID", "Reklam kimliği işlemleri")
    };

    /// <summary>
    /// Modül kodundan Türkçe isim döndürür
    /// </summary>
    public static string GetModuleName(string moduleCode)
    {
        return Modules.TryGetValue(moduleCode, out var info) ? info.Name : moduleCode;
    }

    /// <summary>
    /// Modül kodundan açıklama döndürür
    /// </summary>
    public static string GetModuleDescription(string moduleCode)
    {
        return Modules.TryGetValue(moduleCode, out var info) ? info.Description : string.Empty;
    }
}


