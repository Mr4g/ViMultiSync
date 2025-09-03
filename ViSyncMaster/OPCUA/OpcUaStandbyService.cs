using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ViSyncMaster.OPCUA
{
    public class OpcUaMultiWatchService : IDisposable
    {
        private readonly string _serverUrl;

        private ApplicationConfiguration? _config;
        private Session? _session;
        private Subscription? _subscription;

        private bool _disposed;

        // reconnect (szybki) + watchdog (pełny)
        private SessionReconnectHandler? _reconnectHandler;
        private readonly object _reconnectLock = new();
        private KeepAliveEventHandler? _keepAliveHandler;
        private CancellationTokenSource? _watchdogCts;
        private Task? _watchdogTask;
        private readonly SemaphoreSlim _openLock = new(1, 1);
        private int _fullReconnectAttempt = 0;

        // Parametry startu
        private string? _username;
        private string? _password;
        private bool _useSecurity;
        private int _publishingIntervalMs;
        private int _defaultSamplingIntervalMs;

        // Watche
        private readonly object _watchLock = new();
        private readonly Dictionary<string, WatchSpec> _watches = new();                 // key -> spec
        private readonly Dictionary<string, MonitoredItem> _itemsByKey = new();          // key -> item

        public event Action<string /*key*/, DataValue>? ValueChanged;
        public event Action<string /*key*/, bool /*value*/>? BoolChanged;

        public bool IsConnected => _session?.Connected == true;

        public OpcUaMultiWatchService(string serverUrl)
        {
            _serverUrl = serverUrl ?? throw new ArgumentNullException(nameof(serverUrl));
        }

        // ========== PUBLIC API ==========

        public async Task StartAsync(
            string? username = null,
            string? password = null,
            bool useSecurity = false,
            int publishingIntervalMs = 1000,
            int defaultSamplingIntervalMs = 1000,
            CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            _username = username;
            _password = password;
            _useSecurity = useSecurity;
            _publishingIntervalMs = publishingIntervalMs;
            _defaultSamplingIntervalMs = defaultSamplingIntervalMs;

            _config = await BuildConfigurationAsync().ConfigureAwait(false);
            await OpenSessionAndRecreateWatchesAsync(cancellationToken).ConfigureAwait(false);

            if (_watchdogTask == null)
            {
                _watchdogCts = new CancellationTokenSource();
                _watchdogTask = Task.Run(() => WatchdogLoopAsync(_watchdogCts.Token));
            }
        }

        /// <summary>
        /// Dodaje/aktualizuje obserwację węzła. Key to Twoja etykieta (np. "Standby", "Pressure").
        /// Jeśli item już istnieje – aktualizuje sampling i nic nie gubi.
        /// </summary>
        public void AddWatch(string key, string nodeIdString, int? samplingIntervalMs = null, uint attributeId = Attributes.Value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrWhiteSpace(nodeIdString)) throw new ArgumentNullException(nameof(nodeIdString));
            EnsureNotDisposed();

            var spec = new WatchSpec(
                Key: key,
                NodeId: NodeId.Parse(nodeIdString),
                AttributeId: attributeId,
                SamplingIntervalMs: samplingIntervalMs ?? _defaultSamplingIntervalMs
            );

            lock (_watchLock)
            {
                _watches[key] = spec; // nadpisz/utwórz

                // jeśli subskrypcja istnieje – dołóż/zmień od razu
                if (_subscription != null && _session != null)
                {
                    if (_itemsByKey.TryGetValue(key, out var existing))
                    {
                        // zaktualizuj parametry
                        existing.SamplingInterval = spec.SamplingIntervalMs;
                        existing.AttributeId = spec.AttributeId;
                        existing.StartNodeId = spec.NodeId;
                    }
                    else
                    {
                        var mi = CreateItemForSpec(spec);
                        _subscription.AddItem(mi);
                        _itemsByKey[key] = mi;
                    }

                    _subscription.ApplyChanges();
                }
            }
        }

        /// <summary>Usuwa obserwację (przestaje monitorować).</summary>
        public void RemoveWatch(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            EnsureNotDisposed();

            lock (_watchLock)
            {
                _watches.Remove(key);

                if (_subscription != null && _itemsByKey.TryGetValue(key, out var item))
                {
                    try
                    {
                        item.Notification -= HandleDataChange;
                        _subscription.RemoveItem(item);
                        _subscription.ApplyChanges();
                    }
                    catch { /* ignore */ }
                    finally
                    {
                        _itemsByKey.Remove(key);
                    }
                }
            }
        }

        // ========== SESJA / SUBSKRYPCJE ==========

        private async Task OpenSessionAndRecreateWatchesAsync(CancellationToken ct)
        {
            await _openLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                EnsureNotDisposed();

                SafeTearDownSession();

                // cert aplikacji
                var app = new ApplicationInstance
                {
                    ApplicationName = "ViSyncMaster",
                    ApplicationType = ApplicationType.Client,
                    ApplicationConfiguration = _config
                };
                await app.CheckApplicationInstanceCertificate(false, 2048).ConfigureAwait(false);

                // endpoint
                var selectedEndpoint = CoreClientUtils.SelectEndpoint(_serverUrl, _useSecurity, 15000);
                var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, EndpointConfiguration.Create(_config));

                // tożsamość
                IUserIdentity identity = string.IsNullOrWhiteSpace(_username)
                    ? new UserIdentity(new AnonymousIdentityToken())
                    : new UserIdentity(_username, _password);

                // sesja
                _session = await Session.Create(
                    _config,
                    endpoint,
                    updateBeforeConnect: false,
                    sessionName: "ViSyncMaster",
                    sessionTimeout: 60_000,
                    identity,
                    preferredLocales: null
                ).ConfigureAwait(false);

                _keepAliveHandler = (_, e) => OnKeepAlive(e);
                _session.KeepAlive += _keepAliveHandler;

                Debug.WriteLine($"OPC UA session opened with {_serverUrl}");
                Log.Information("OPC UA session opened with {ServerUrl} | Policy={Policy} | Mode={Mode}",
                    _serverUrl, selectedEndpoint.SecurityPolicyUri, selectedEndpoint.SecurityMode);

                // subskrypcja
                _subscription = new Subscription(_session.DefaultSubscription)
                {
                    PublishingEnabled = true,
                    PublishingInterval = _publishingIntervalMs
                };
                _session.AddSubscription(_subscription);
                _subscription.Create();

                // odtwórz wszystkie watche
                lock (_watchLock)
                {
                    _itemsByKey.Clear();
                    foreach (var spec in _watches.Values)
                    {
                        var mi = CreateItemForSpec(spec);
                        _subscription.AddItem(mi);
                        _itemsByKey[spec.Key] = mi;
                    }
                }
                _subscription.ApplyChanges();

                // diagnostyka: pojedynczy odczyt z każdego węzła (nie blokuj błędami)
                foreach (var spec in SnapshotSpecs())
                {
                    try
                    {
                        var dv = _session.ReadValue(spec.NodeId);
                        Log.Information("Probe read {Key} ({Node}) = {Val}", spec.Key, spec.NodeId, dv);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Probe read failed for {Key} ({Node})", spec.Key, spec.NodeId);
                    }
                }

                _fullReconnectAttempt = 0;
            }
            finally
            {
                _openLock.Release();
            }
        }

        private MonitoredItem CreateItemForSpec(WatchSpec spec)
        {
            var mi = new MonitoredItem
            {
                StartNodeId = spec.NodeId,
                AttributeId = spec.AttributeId,
                SamplingInterval = spec.SamplingIntervalMs,
                QueueSize = 10,
                DiscardOldest = true,
                DisplayName = spec.Key,
                Handle = spec.Key // user data — użyjemy w callbacku
            };
            mi.Notification += HandleDataChange;
            return mi;
        }

        // ========== RECONNECT: KeepAlive + fast reconnect ==========

        private void OnKeepAlive(KeepAliveEventArgs e)
        {
            if (ServiceResult.IsBad(e.Status))
            {
                Log.Warning("OPC UA KeepAlive BAD: {Status}", e.Status);
                TryBeginReconnect();
            }
        }

        private void TryBeginReconnect()
        {
            lock (_reconnectLock)
            {
                if (_reconnectHandler != null || _session == null) return;

                try
                {
                    Log.Information("Starting OPC UA fast reconnect...");
                    _reconnectHandler = new SessionReconnectHandler();
                    _reconnectHandler.BeginReconnect(_session, 5000, Client_ReconnectComplete);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "BeginReconnect failed – watchdog will attempt full reconnect");
                    _reconnectHandler?.Dispose();
                    _reconnectHandler = null;
                }
            }
        }

        private void Client_ReconnectComplete(object? sender, EventArgs e)
        {
            lock (_reconnectLock)
            {
                try
                {
                    Log.Information("OPC UA fast reconnect complete.");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error completing fast reconnect");
                }
                finally
                {
                    try { _reconnectHandler?.Dispose(); } catch { /* ignore */ }
                    _reconnectHandler = null;
                }
            }
        }

        // ========== Watchdog – pełny reconnect ==========

        private async Task WatchdogLoopAsync(CancellationToken ct)
        {
            var minDelay = TimeSpan.FromSeconds(2);
            var maxDelay = TimeSpan.FromSeconds(60);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if ((_session == null || !_session.Connected) && _reconnectHandler == null)
                    {
                        var delay = ComputeBackoff(minDelay, maxDelay, _fullReconnectAttempt);
                        Log.Warning("Session not connected – full reconnect attempt #{Attempt} in {Delay}s",
                            _fullReconnectAttempt + 1, (int)delay.TotalSeconds);

                        await Task.Delay(delay, ct).ConfigureAwait(false);
                        if (ct.IsCancellationRequested) break;

                        _fullReconnectAttempt++;
                        try
                        {
                            await OpenSessionAndRecreateWatchesAsync(ct).ConfigureAwait(false);
                            Log.Information("Full reconnect successful.");
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Full reconnect failed (attempt #{Attempt})", _fullReconnectAttempt);
                        }
                    }

                    await Task.Delay(1000, ct).ConfigureAwait(false);
                }
                catch (TaskCanceledException) { break; }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Log.Error(ex, "Watchdog loop error");
                    await Task.Delay(2000, ct).ConfigureAwait(false);
                }
            }
        }

        private static TimeSpan ComputeBackoff(TimeSpan min, TimeSpan max, int attempt)
        {
            double seconds = Math.Min(max.TotalSeconds, min.TotalSeconds * Math.Pow(2, Math.Max(0, attempt)));
            return TimeSpan.FromSeconds(seconds);
        }

        // ========== Data change ==========

        private void HandleDataChange(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            try
            {
                var key = (item.Handle as string) ?? item.DisplayName ?? item.StartNodeId?.ToString() ?? "?";

                if (e.NotificationValue is MonitoredItemNotification mon)
                {
                    Emit(key, mon.Value);
                    return;
                }

                foreach (var dv in item.DequeueValues())
                    Emit(key, dv);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in HandleDataChange");
            }
        }

        private void Emit(string key, DataValue dv)
        {
            try
            {
                ValueChanged?.Invoke(key, dv);

                var val = dv?.Value;
                if (val is bool b)
                {
                    BoolChanged?.Invoke(key, b);
                }
                else if (val != null)
                {
                    try
                    {
                        // 0/1, "true"/"false", itp.
                        bool converted = Convert.ToBoolean(val);
                        BoolChanged?.Invoke(key, converted);
                    }
                    catch
                    {
                        // ignoruj — nie-boolean
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Emit error for key {Key}", key);
            }
        }

        // ========== Config / cleanup / guards ==========

        private async Task<ApplicationConfiguration> BuildConfigurationAsync()
        {
            string common = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string storeRoot = Path.Combine(common, "OPC Foundation", "CertificateStores");

            var config = new ApplicationConfiguration
            {
                ApplicationName = "ViSyncMaster",
                ApplicationUri = $"urn:{Utils.GetHostName()}:ViSyncMaster",
                ApplicationType = ApplicationType.Client,

                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = Path.Combine(storeRoot, "MachineDefault"),
                        SubjectName = "CN=ViSyncMaster, O=ViSync, DC=local"
                    },
                    AutoAcceptUntrustedCertificates = true,
                    RejectSHA1SignedCertificates = false,
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

            await config.Validate(ApplicationType.Client).ConfigureAwait(false);

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

        private void SafeTearDownSession()
        {
            try
            {
                foreach (var item in _itemsByKey.Values.ToList())
                {
                    try { item.Notification -= HandleDataChange; } catch { /* ignore */ }
                }
            }
            catch { /* ignore */ }

            try
            {
                if (_subscription != null && _session != null)
                {
                    try { _subscription.Delete(true); } catch { /* ignore */ }
                }
            }
            catch { /* ignore */ }

            try
            {
                if (_session != null)
                {
                    if (_keepAliveHandler != null)
                    {
                        try { _session.KeepAlive -= _keepAliveHandler; } catch { /* ignore */ }
                        _keepAliveHandler = null;
                    }

                    try { _session.Close(); } catch { /* ignore */ }
                    _session.Dispose();
                }
            }
            catch { /* ignore */ }

            _subscription = null;
            _session = null;
            _itemsByKey.Clear();
        }

        private void EnsureNotDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OpcUaMultiWatchService));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                try { _watchdogCts?.Cancel(); } catch { /* ignore */ }
                try { _watchdogTask?.Wait(2000); } catch { /* ignore */ }

                try { _reconnectHandler?.Dispose(); } catch { /* ignore */ }
                _reconnectHandler = null;

                SafeTearDownSession();
            }
            catch { /* ignore */ }
            finally
            {
                _watchdogTask = null;
                _watchdogCts?.Dispose();
                _watchdogCts = null;
            }
        }

        // Pomocnicze
        private List<WatchSpec> SnapshotSpecs()
        {
            lock (_watchLock) return _watches.Values.ToList();
        }

        private record WatchSpec(string Key, NodeId NodeId, uint AttributeId, int SamplingIntervalMs);
    }
}
