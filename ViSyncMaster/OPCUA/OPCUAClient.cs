using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViSyncMaster.OPCUA
{
    public class OpcUaStandbyService : IDisposable
    {
        private readonly string _serverUrl;
        private readonly string _nodeId;
        private Session? _session;
        private Subscription? _subscription;
        private MonitoredItem? _monitoredItem;

        public event Action<bool>? StandbyChanged;

        public OpcUaStandbyService(string serverUrl, string nodeId)
        {
            _serverUrl = serverUrl;
            _nodeId = nodeId;
        }

        public async Task StartAsync()
        {
            var config = new ApplicationConfiguration
            {
                ApplicationName = "ViSyncMaster",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    AutoAcceptUntrustedCertificates = true,
                    ApplicationCertificate = new CertificateIdentifier()
                },
                ClientConfiguration = new ClientConfiguration()
            };

            await config.Validate(ApplicationType.Client);

            var endpointDescription = CoreClientUtils.SelectEndpoint(_serverUrl, false, 15000);
            var endpointConfig = EndpointConfiguration.Create(config);
            var endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfig);
            _session = await Session.Create(config, endpoint, false, "", 60000, null, null);

            _subscription = new Subscription(_session.DefaultSubscription)
            {
                PublishingInterval = 1000
            };

            _monitoredItem = new MonitoredItem(_subscription.DefaultItem)
            {
                StartNodeId = new NodeId(_nodeId),
                AttributeId = Attributes.Value,
                SamplingInterval = 1000,
                QueueSize = 1,
                DiscardOldest = true
            };
            _monitoredItem.Notification += HandleDataChange;
            _subscription.AddItem(_monitoredItem);
            _session.AddSubscription(_subscription);
            _subscription.Create();
        }

        private void HandleDataChange(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            foreach (var value in item.DequeueValues())
            {
                if (value.Value is bool b)
                {
                    StandbyChanged?.Invoke(b);
                }
            }
        }

        public void Dispose()
        {
            try
            {
                _subscription?.Delete(true);
                _session?.Close();
                _session?.Dispose();
            }
            catch
            {
                // ignore
            }
        }
    }
}

