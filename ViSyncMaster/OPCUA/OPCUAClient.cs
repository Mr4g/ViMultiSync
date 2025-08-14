using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ViSyncMaster.OPCUA
{
    public class OpcUaStandbyService : IDisposable
    {
        private readonly string _serverUrl;
        private readonly string _nodeIdString;
        private Session? _session;
        private Subscription? _subscription;
        private MonitoredItem? _monitoredItem;
        private bool _disposed;

        public event Action<bool>? StandbyChanged;
        public bool IsConnected => _session?.Connected == true;

        public OpcUaStandbyService(string serverUrl, string nodeId)
        {
            _serverUrl = serverUrl ?? throw new ArgumentNullException(nameof(serverUrl));
            _nodeIdString = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
        }

        public async Task StartAsync(
            string? username = null,
            string? password = null,
            bool useSecurity = false,          // domyślnie NONE
            int publishingIntervalMs = 1000,
            int samplingIntervalMs = 1000)
        {
            EnsureNotDisposed();

            var config = await BuildConfigurationAsync();

            // Wygeneruj certyfikat aplikacji (wymagane przez Validate/Session nawet dla None)
            var app = new ApplicationInstance
            {
                ApplicationName = "ViSyncMaster",
                ApplicationType = ApplicationType.Client,
                ApplicationConfiguration = config
            };
            await app.CheckApplicationInstanceCertificate(false, 2048);

            // Wybierz endpoint (dla None -> useSecurity:false)
            var selectedEndpoint = CoreClientUtils.SelectEndpoint(_serverUrl, useSecurity, 15000);
            var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, EndpointConfiguration.Create(config));

            // Tożsamość (Anonymous lub Username/Password)
            IUserIdentity identity = string.IsNullOrWhiteSpace(username)
                ? new UserIdentity(new AnonymousIdentityToken())
                : new UserIdentity(username, password);

            _session = await Session.Create(
                config,
                endpoint,
                updateBeforeConnect: false,
                sessionName: "ViSyncMaster",
                sessionTimeout: 60_000,
                identity,
                preferredLocales: null);

            _session.KeepAlive += (_, e) =>
            {
                if (ServiceResult.IsBad(e.Status))
                    Log.Warning("OPC UA KeepAlive status: {Status}", e.Status);
            };

            Debug.WriteLine($"OPC UA session opened with {_serverUrl}");
            Log.Information("OPC UA session opened with {ServerUrl} | Policy={Policy} | Mode={Mode}",
                _serverUrl, selectedEndpoint.SecurityPolicyUri, selectedEndpoint.SecurityMode);

            // Subskrypcja
            _subscription = new Subscription(_session.DefaultSubscription)
            {
                PublishingEnabled = true,
                PublishingInterval = publishingIntervalMs
            };
            _session.AddSubscription(_subscription);
            _subscription.Create();

            // Monitor wybranego NodeId
            var nodeId = NodeId.Parse(_nodeIdString);
            _monitoredItem = new MonitoredItem(_subscription.DefaultItem)
            {
                StartNodeId = nodeId,
                AttributeId = Attributes.Value,
                SamplingInterval = samplingIntervalMs,
                QueueSize = 10,
                DiscardOldest = true
            };
            _monitoredItem.Notification += HandleDataChange;

            _subscription.AddItem(_monitoredItem);
            _subscription.ApplyChanges();

            Log.Information("Subscribed to {NodeId}", _nodeIdString);

            // Odczyt testowy (diag)
            try
            {
                var probe = _session.ReadValue(nodeId);
                Log.Information("Probe read {NodeId} = {Val}", _nodeIdString, probe);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Probe read failed for {NodeId}", _nodeIdString);
            }
        }

        private async Task<ApplicationConfiguration> BuildConfigurationAsync()
        {
            // Katalogi OPC Foundation dla store’ów (cross-platform)
            string common = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string storeRoot = Path.Combine(common, "OPC Foundation", "CertificateStores");

            var config = new ApplicationConfiguration
            {
                ApplicationName = "ViSyncMaster",
                ApplicationUri = $"urn:{Utils.GetHostName()}:ViSyncMaster",
                ApplicationType = ApplicationType.Client,

                SecurityConfiguration = new SecurityConfiguration
                {
                    // Nawet dla None biblioteka chce mieć wskazany ApplicationCertificate
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = Path.Combine(storeRoot, "MachineDefault"),
                        SubjectName = "CN=ViSyncMaster, O=ViSync, DC=local"
                    },
                    AutoAcceptUntrustedCertificates = true,  // DEV: akceptuj serwery bez pytania
                    RejectSHA1SignedCertificates = false, // jeśli masz starsze certy
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = Path.Combine(storeRoot, "UA Applications")
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = Path.Combine(storeRoot, "UA Certificate Authorities")
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = Path.Combine(storeRoot, "RejectedCertificates")
                    }
                },

                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 }
            };

            await config.Validate(ApplicationType.Client);

            // Validator – w DEV auto-accept
            config.CertificateValidator = new CertificateValidator();
            config.CertificateValidator.CertificateValidation += (s, e) =>
            {
                if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
                {
                    e.Accept = true;
                    Log.Information("Accepted server certificate: {Subject}", e.Certificate?.Subject);
                }
            };

            return config;
        }

        private void HandleDataChange(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            try
            {
                // Typowy przypadek: pojedyncze powiadomienie
                if (e.NotificationValue is MonitoredItemNotification mon)
                {
                    EmitIfBool(mon.Value);
                    return;
                }

                // Fallback – zawsze działa
                foreach (var dv in item.DequeueValues())
                    EmitIfBool(dv);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in HandleDataChange");
            }
        }

        private void EmitIfBool(DataValue dv)
        {
            var val = dv?.Value;
            if (val is bool b)
            {
                StandbyChanged?.Invoke(b);
            }
            else if (val != null)
            {
                try
                {
                    bool converted = Convert.ToBoolean(val);
                    StandbyChanged?.Invoke(converted);
                }
                catch
                {
                    Log.Debug("Standby value not boolean-convertible: {Type}", val.GetType().Name);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                if (_monitoredItem != null)
                    _monitoredItem.Notification -= HandleDataChange;

                if (_subscription != null && _session != null)
                {
                    try { _subscription.Delete(true); } catch { /* ignore */ }
                }

                if (_session != null)
                {
                    try { _session.Close(); } catch { /* ignore */ }
                    _session.Dispose();
                }
            }
            catch { /* ignore */ }
        }

        private void EnsureNotDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OpcUaStandbyService));
        }
    }
}
