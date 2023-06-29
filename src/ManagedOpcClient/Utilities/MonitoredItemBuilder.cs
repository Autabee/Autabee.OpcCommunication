using Opc.Ua.Client;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Text;
using Autabee.Communication.ManagedOpcClient.ManagedNode;

namespace Autabee.Communication.ManagedOpcClient.Utilities
{
    public static class MonitoredItemBuilder
    {
        public static MonitoredItem CreateValueItem(ValueNodeEntry nodeEntry,
            int samplingInterval,
            uint queueSize,
            bool discardOldest,
            MonitoringMode monitoringMode = MonitoringMode.Reporting)
        {
            MonitoredItem monitoredItem = CreateMonitoredValueItemWithoutName(samplingInterval,
                queueSize, discardOldest, monitoringMode);
            monitoredItem.DisplayName = nodeEntry.NodeString;
            monitoredItem.StartNodeId = nodeEntry.UnregisteredNodeId;
            return monitoredItem;
        }

        static public MonitoredItem CreateMonitoredValueItem(NodeId nodeId, int samplingInterval, uint queueSize, bool discardOldest, MonitoringMode monitoringMode = MonitoringMode.Reporting)
        {
            MonitoredItem monitoredItem = CreateMonitoredValueItemWithoutName(samplingInterval,
                queueSize, discardOldest, monitoringMode);
            monitoredItem.DisplayName = nodeId.ToString();
            monitoredItem.StartNodeId = nodeId;
            return monitoredItem;
        }

        static public MonitoredItem CreateMonitoredValueItem(string nodeId, int samplingInterval, uint queueSize, bool discardOldest, MonitoringMode monitoringMode = MonitoringMode.Reporting)
        {
            MonitoredItem monitoredItem = CreateMonitoredValueItemWithoutName(samplingInterval,
                queueSize, discardOldest, monitoringMode);
            monitoredItem.DisplayName = nodeId;
            monitoredItem.StartNodeId = nodeId;
            return monitoredItem;
        }

        private static MonitoredItem CreateMonitoredValueItemWithoutName(int samplingInterval, uint queueSize, bool discardOldest, MonitoringMode mode)
        {
            MonitoredItem monitoredItem = new MonitoredItem();
            monitoredItem.AttributeId = Attributes.Value;
            monitoredItem.MonitoringMode = mode;
            monitoredItem.SamplingInterval = samplingInterval;
            monitoredItem.QueueSize = queueSize;
            monitoredItem.DiscardOldest = discardOldest;
            return monitoredItem;
        }
    }
}
