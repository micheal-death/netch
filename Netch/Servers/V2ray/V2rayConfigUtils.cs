using Netch.Models;
using Netch.Utils;

#pragma warning disable VSTHRD200

namespace Netch.Servers;

public static class V2rayConfigUtils
{
    public static async Task<V2rayConfig> GenerateClientConfigAsync(Server server)
    {
        var v2rayConfig = new V2rayConfig
        {
            inbounds = new object[]
            {
                new
                {
                    port = Global.Settings.Socks5LocalPort,
                    protocol = "socks",
                    listen = Global.Settings.LocalAddress,
                    settings = new
                    {
                        auth = "noauth",
                        udp = true
                    }
                }
            }
        };

        v2rayConfig.outbounds = new[] { await outbound(server) };

        return v2rayConfig;
    }

    private static async Task<Outbound> outbound(Server server)
    {
        var outbound = new Outbound
        {
            settings = new OutboundConfiguration(),
            mux = new Mux()
        };

        switch (server)
        {
            case Socks5Server socks:
            {
                outbound.protocol = "socks";
                outbound.settings.servers = new object[]
                {
                    new
                    {
                        address = await server.AutoResolveHostnameAsync(),
                        port = server.Port,
                        users = socks.Auth()
                            ? new[]
                            {
                                new
                                {
                                    user = socks.Username,
                                    pass = socks.Password,
                                    level = 1
                                }
                            }
                            : null
                    }
                };
                outbound.settings.version = socks.Version;

                outbound.mux.enabled = false;
                outbound.mux.concurrency = -1;
                break;
            }
            case VLESSServer vless:
            {
                outbound.protocol = "vless";
                outbound.settings.vnext = new[]
                {
                    new VnextItem
                    {
                        address = await server.AutoResolveHostnameAsync(),
                        port = server.Port,
                        users = new[]
                        {
                            new User
                            {
                                id = getUUID(vless.UserID),
                                flow = vless.TLSSecureType == "reality" ? "xtls-rprx-vision" :
                                       vless.TLSSecureType == "xtls" ? "xtls-rprx-direct" : null,
                                encryption = vless.EncryptMethod
                            }
                        }
                    }
                };

                outbound.settings.packetEncoding = Global.Settings.V2RayConfig.XrayCone ? vless.PacketEncoding : "none";
                outbound.mux.packetEncoding = Global.Settings.V2RayConfig.XrayCone ? vless.PacketEncoding : "none";

                outbound.streamSettings = boundStreamSettings(vless);

                if (vless.TLSSecureType is "xtls" or "reality")
                {
                    outbound.mux.enabled = false;
                    outbound.mux.concurrency = -1;
                }
                else
                {
                    outbound.mux.enabled = vless.UseMux ?? Global.Settings.V2RayConfig.UseMux;
                    outbound.mux.concurrency = vless.UseMux ?? Global.Settings.V2RayConfig.UseMux ? 8 : -1;
                }

                break;
            }
            case VMessServer vmess:
            {
                outbound.protocol = "vmess";
                if (vmess.EncryptMethod == "auto" && vmess.TLSSecureType != "none" && !Global.Settings.V2RayConfig.AllowInsecure)
                {
                    vmess.EncryptMethod = "zero";
                }
                outbound.settings.vnext = new[]
                {
                    new VnextItem
                    {
                        address = await server.AutoResolveHostnameAsync(),
                        port = server.Port,
                        users = new[]
                        {
                            new User
                            {
                                id = getUUID(vmess.UserID),
                                alterId = vmess.AlterID,
                                security = vmess.EncryptMethod
                            }
                        }
                    }
                };

                outbound.settings.packetEncoding = Global.Settings.V2RayConfig.XrayCone ? vmess.PacketEncoding : "none";
                outbound.mux.packetEncoding = Global.Settings.V2RayConfig.XrayCone ? vmess.PacketEncoding : "none";

                outbound.streamSettings = boundStreamSettings(vmess);

                outbound.mux.enabled = vmess.UseMux ?? Global.Settings.V2RayConfig.UseMux;
                outbound.mux.concurrency = vmess.UseMux ?? Global.Settings.V2RayConfig.UseMux ? 8 : -1;
                break;
            }
            case ShadowsocksServer ss:
                outbound.protocol = "shadowsocks";
                outbound.settings.servers = new[]
                {
                    new ShadowsocksServerItem
                    {
                        address = await server.AutoResolveHostnameAsync(),
                        port = server.Port,
                        method = ss.EncryptMethod,
                        password = ss.Password
                    }
                };
                outbound.settings.plugin = ss.Plugin ?? "";
                outbound.settings.pluginOpts = ss.PluginOption ?? "";
                
                if (Global.Settings.V2RayConfig.TCPFastOpen)
                {
                    outbound.streamSettings = new StreamSettings
                    {
                        sockopt = new Sockopt
                        {
                            tcpFastOpen = true
                        }
                    };
                }
                break;
             case ShadowsocksRServer ssr:
                outbound.protocol = "shadowsocks";
                outbound.settings.servers = new[]
                {
                    new ShadowsocksServerItem
                    {
                        address = await server.AutoResolveHostnameAsync(),
                        port = server.Port,
                        method = ssr.EncryptMethod,
                        password = ssr.Password,
                    }
                };
                outbound.settings.plugin = "shadowsocksr";
                outbound.settings.pluginArgs = new string[]
                {
                    "--obfs=" + ssr.OBFS,
                    "--obfs-param=" + ssr.OBFSParam ?? "",
                    "--protocol=" + ssr.Protocol,
                    "--protocol-param=" + ssr.ProtocolParam ?? ""
                };

                if (Global.Settings.V2RayConfig.TCPFastOpen)
                {
                    outbound.streamSettings = new StreamSettings
                    {
                        sockopt = new Sockopt
                        {
                            tcpFastOpen = true
                        }
                    };
                }
                break;
             case TrojanServer trojan:
                outbound.protocol = "trojan";
                outbound.settings.servers = new[]
                {
                    new ShadowsocksServerItem // I'm not serious
                    {
                        address = await server.AutoResolveHostnameAsync(),
                        port = server.Port,
                        method = "",
                        password = trojan.Password,
                        flow = trojan.TLSSecureType == "xtls" ? "xtls-rprx-direct" : ""
                    }
                };

                outbound.streamSettings = new StreamSettings
                {
                    network = "tcp",
                    security = trojan.TLSSecureType
                };
                if (trojan.TLSSecureType != "none")
                {
                    var tlsSettings = new TlsSettings
                    {
                        allowInsecure = Global.Settings.V2RayConfig.AllowInsecure,
                        serverName = trojan.Host ?? ""
                    };

                    switch (trojan.TLSSecureType)
                    {
                        case "tls":
                            outbound.streamSettings.tlsSettings = tlsSettings;
                            break;
                        case "xtls":
                            outbound.streamSettings.xtlsSettings = tlsSettings;
                            break;
                    }
                }

                if (Global.Settings.V2RayConfig.TCPFastOpen)
                {
                    outbound.streamSettings.sockopt = new Sockopt
                    {
                        tcpFastOpen = true
                    };
                }
                break;
            case WireGuardServer wg:
                outbound.protocol = "wireguard";
                outbound.settings.address = await server.AutoResolveHostnameAsync();
                outbound.settings.port = server.Port;
                outbound.settings.localAddresses = wg.LocalAddresses.SplitOrDefault();
                outbound.settings.peerPublicKey = wg.PeerPublicKey;
                outbound.settings.privateKey = wg.PrivateKey;
                outbound.settings.preSharedKey = wg.PreSharedKey;
                outbound.settings.mtu = wg.MTU;

                if (Global.Settings.V2RayConfig.TCPFastOpen)
                {
                    outbound.streamSettings = new StreamSettings
                    {
                        sockopt = new Sockopt
                        {
                            tcpFastOpen = true
                        }
                    };
                }
                break;

            case SSHServer ssh:
                outbound.protocol = "ssh";
                outbound.settings.address = await server.AutoResolveHostnameAsync();
                outbound.settings.port = server.Port;
                outbound.settings.user = ssh.User;
                outbound.settings.password = ssh.Password;
                outbound.settings.privateKey = ssh.PrivateKey;
                outbound.settings.publicKey = ssh.PublicKey;
                
                if (Global.Settings.V2RayConfig.TCPFastOpen)
                {
                    outbound.streamSettings = new StreamSettings
                    {
                        sockopt = new Sockopt
                        {
                            tcpFastOpen = true
                        }
                    };
                }
                break;
        }

        return outbound;
    }

    private static StreamSettings boundStreamSettings(VMessServer server)
    {
        // https://xtls.github.io/config/transports

        var streamSettings = new StreamSettings
        {
            network = server.TransferProtocol,
            security = server.TLSSecureType
        };

        if (server is VLESSServer vlessServer && vlessServer.TLSSecureType == "reality")
        {
            if (string.IsNullOrWhiteSpace(vlessServer.REALITYPublicKey))
            {
                throw new MessageException("REALITY public key is required.");
            }

            if (!IsValidRealityShortId(vlessServer.REALITYShortId))
            {
                throw new MessageException("REALITY short ID must be an even-length hex string.");
            }

            streamSettings.security = "reality";
            streamSettings.realitySettings = new RealitySettings
            {
                serverName = GetRealityServerName(vlessServer),
                fingerprint = vlessServer.REALITYFingerprint.ValueOrDefault("chrome") ?? "chrome",
                publicKey = vlessServer.REALITYPublicKey,
                shortId = vlessServer.REALITYShortId.ValueOrDefault(),
                spiderX = vlessServer.REALITYSpiderX.ValueOrDefault()
            };
        }
        else if (server.TLSSecureType == "tls" || server.TLSSecureType == "xtls")
        {
            var tlsSettings = new TlsSettings
            {
                allowInsecure = Global.Settings.V2RayConfig.AllowInsecure,
                serverName = server.ServerName.ValueOrDefault() ?? server.Host.SplitOrDefault()?[0]
            };

            if (server.TLSSecureType == "tls")
            {
                streamSettings.tlsSettings = tlsSettings;
            }
            else // Must be "xtls"
            {
                streamSettings.xtlsSettings = tlsSettings;
            }
        }

        switch (server.TransferProtocol)
        {
            case "tcp":

                streamSettings.tcpSettings = new TcpSettings
                {
                    header = new
                    {
                        type = server.FakeType,
                        request = server.FakeType switch
                        {
                            "none" => null,
                            "http" => new
                            {
                                path = server.Path.SplitOrDefault(),
                                headers = new
                                {
                                    Host = server.Host.SplitOrDefault()
                                }
                            },
                            _ => throw new MessageException($"Invalid tcp type {server.FakeType}")
                        }
                    }
                };

                break;
            case "ws":

                streamSettings.wsSettings = new WsSettings
                {
                    path = server.Path.ValueOrDefault(),
                    headers = new
                    {
                        Host = server.Host.ValueOrDefault()
                    }
                };

                break;
            case "kcp":

                streamSettings.kcpSettings = new KcpSettings
                {
                    mtu = Global.Settings.V2RayConfig.KcpConfig.mtu,
                    tti = Global.Settings.V2RayConfig.KcpConfig.tti,
                    uplinkCapacity = Global.Settings.V2RayConfig.KcpConfig.uplinkCapacity,
                    downlinkCapacity = Global.Settings.V2RayConfig.KcpConfig.downlinkCapacity,
                    congestion = Global.Settings.V2RayConfig.KcpConfig.congestion,
                    readBufferSize = Global.Settings.V2RayConfig.KcpConfig.readBufferSize,
                    writeBufferSize = Global.Settings.V2RayConfig.KcpConfig.writeBufferSize,
                    header = new
                    {
                        type = server.FakeType
                    },
                    seed = server.Path.ValueOrDefault()
                };

                break;
            case "h2":

                streamSettings.httpSettings = new HttpSettings
                {
                    host = server.Host.SplitOrDefault(),
                    path = server.Path.ValueOrDefault()
                };

                break;
            case "quic":

                streamSettings.quicSettings = new QuicSettings
                {
                    security = server.QUICSecure,
                    key = server.QUICSecret,
                    header = new
                    {
                        type = server.FakeType
                    }
                };

                break;
            case "grpc":

                streamSettings.grpcSettings = new GrpcSettings
                {
                    serviceName = server.Path,
                    multiMode = server.FakeType == "multi"
                };

                break;
            default:
                throw new MessageException($"transfer protocol \"{server.TransferProtocol}\" not implemented yet");
        }

        if (Global.Settings.V2RayConfig.TCPFastOpen)
        {
            streamSettings.sockopt = new Sockopt
            {
                tcpFastOpen = true
            };
        }

        return streamSettings;
    }

    public static string getUUID(string uuid)
    {
        if (uuid.Length == 36 || uuid.Length == 32)
        {
            return uuid;
        }
        return uuid.GenerateUUIDv5();
    }

    private static string GetRealityServerName(VLESSServer server)
    {
        return server.ServerName.ValueOrDefault()
            ?? server.Host.SplitOrDefault()?[0]
            ?? server.Hostname.ValueOrDefault()
            ?? throw new MessageException("REALITY server name is required.");
    }

    private static bool IsValidRealityShortId(string? shortId)
    {
        return string.IsNullOrWhiteSpace(shortId) || shortId.Length % 2 == 0 && shortId.All(Uri.IsHexDigit);
    }
}
