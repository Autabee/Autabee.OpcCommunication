using Opc.Ua.Client;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Text;
using Autabee.Communication.ManagedOpcClient.ManagedNode;

namespace Autabee.Communication.ManagedOpcClient.Utilities
{
    public static class MonitoredItemCreation
    {
        public static MonitoredItem CreateMonitoredValueItem(ValueNodeEntry nodeEntry,
            int samplingInterval,
            uint queueSize,
            bool discardOldest,
            MonitoringMode moneteringMode = MonitoringMode.Reporting)
        {
            MonitoredItem monitoredItem = CreateMoniroredValueItemWithoutName(samplingInterval,
                queueSize, discardOldest, moneteringMode);
            monitoredItem.DisplayName = nodeEntry.NodeString;
            monitoredItem.StartNodeId = nodeEntry.UnregisteredNodeId;
            return monitoredItem;
        }

        static public MonitoredItem CreateMonitoredValueItem(NodeId nodeId, int samplingInterval, uint queueSize, bool discardOldest, MonitoringMode moneteringMode = MonitoringMode.Reporting)
        {
            MonitoredItem monitoredItem = CreateMoniroredValueItemWithoutName(samplingInterval,
                queueSize, discardOldest, moneteringMode);
            monitoredItem.DisplayName = nodeId.ToString();
            monitoredItem.StartNodeId = nodeId;
            return monitoredItem;
        }

        private static MonitoredItem CreateMoniroredValueItemWithoutName(int samplingInterval, uint queueSize, bool discardOldest, MonitoringMode mode)
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
