using Opc.Ua;

namespace TypeServerNodes
{
	public static class AddressSpace
	{
		public static readonly NodeId ParkingLot_VehiclesInLot = new NodeId(283);
		public static readonly NodeId ParkingLot_DriverOfTheMonth = new NodeId(375);
		public static readonly NodeId ParkingLot_DriverOfTheMonth_PrimaryVehicle = new NodeId(376);
		public static readonly NodeId ParkingLot_DriverOfTheMonth_OwnedVehicles = new NodeId(377);
		public static readonly NodeId ParkingLot_LotType = new NodeId(380);
	}
}