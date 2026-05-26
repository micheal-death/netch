using Netch.Models;

namespace Netch.Servers;

public class TrojanServer : Server
{
    private string _tlsSecureType = TrojanGlobal.TLSSecure[1];

    public override string Type { get; } = "Trojan";

    public override string MaskedData()
    {
        return "";
    }

    /// <summary>
    ///     密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    ///     伪装域名
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    ///     TLS 底层传输安全
    /// </summary>
    public string TLSSecureType
    {
        get => _tlsSecureType;
        set
        {
            if (value == "")
                value = TrojanGlobal.TLSSecure[1];

            _tlsSecureType = value;
        }
    }
}

public static class TrojanGlobal
{
    public static readonly List<string> TLSSecure = new()
    {
        "none",
        "tls",
        "xtls"
    };
}
