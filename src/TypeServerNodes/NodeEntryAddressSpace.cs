using Opc.Ua;
using Autabee.Communication.ManagedOpcClient.ManagedNode;

namespace TypeServerNodes
{
	public static class NodeEntryAddressSpace
	{
		public static ValueNodeEntry<VehicleType[]> ParkingLot_VehiclesInLot() => new ValueNodeEntry<VehicleType[]>(NodeId.Parse($"ns=4;i=283"));
		public static ValueNodeEntry<VehicleType> ParkingLot_DriverOfTheMonth_PrimaryVehicle() => new ValueNodeEntry<VehicleType>(NodeId.Parse($"ns=4;i=376"));
		public static ValueNodeEntry<VehicleType[]> ParkingLot_DriverOfTheMonth_OwnedVehicles() => new ValueNodeEntry<VehicleType[]>(NodeId.Parse($"ns=4;i=377"));
		public static ValueNodeEntry<ParkingLotType> ParkingLot_LotType() => new ValueNodeEntry<ParkingLotType>(NodeId.Parse($"ns=4;i=380"));
	}
}