using Autabee.Communication.ManagedOpcClient;
using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Autabee.Communication.ManagedOpcClient.Utilities;
using AutabeeTestFixtures;
using Opc.Ua;
using TypeServerNodes;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Xunit;
using Xunit.Sdk;

namespace Autabee.Communication.OpcCommunicatorTests.OpcSample
{
    public class OpcTypeSampleTests : IClassFixture<OpcUaTypeSampleFixture>
    {
        private readonly AutabeeManagedOpcClient communicator;
        private readonly bool skipServerNotFound;

        public OpcTypeSampleTests(OpcUaTypeSampleFixture testPlcTestsFixture, ITestOutputHelper outputHelper)
        {
            communicator = testPlcTestsFixture.Communicator;
            skipServerNotFound = testPlcTestsFixture.SkipServerNotFound;
        }

        [Fact]
        public void ConnectWithTestServer()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");

            var vehicleInLot = NodeId.Parse("ns=4;i=283");

            //communicator.Session.Factory.AddEncodeableTypes(typeof(ParkingLotType).Assembly);

            var data = communicator.ReadValue(vehicleInLot);



            if (data is object[] nData)
            {
                //check if it correctly maps sub types
                if (nData[0] is Dictionary<string, object> obj)
                {
                    Assert.Contains("CargoCapacity", obj);
                }
            }
        }



        [Fact]
        public void ConnectWithTestServerDefinedTypes()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");

            var vehicleInLot = NodeId.Parse("ns=4;i=283");

            communicator.AddTypeAssembly(typeof(ParkingLotType).Assembly);

            var data = communicator.ReadValue(vehicleInLot) as object[];


            if (data.Length == 4)
            {
                // write value test had run.
                Assert.IsType<TruckType>(data[0]);
                Assert.IsType<CarType>(data[1]);
                Assert.IsType<BicycleType>(data[2]);
                Assert.IsType<ScooterType>(data[3]);
            }
            else
            {
                // truck car car bic Sc
                Assert.IsType<TruckType>(data[0]);
                Assert.IsType<CarType>(data[1]);
                Assert.IsType<CarType>(data[2]);
                Assert.IsType<BicycleType>(data[3]);
                Assert.IsType<ScooterType>(data[4]);
            }
        }

        [Fact]
        public void WriteValue()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");

            var LotType = NodeId.Parse("ns=4;i=380");

            communicator.AddTypeAssembly(typeof(ParkingLotType).Assembly);

            communicator.WriteValue(LotType, (int)2 );


            var vehicleInLot = NodeId.Parse("ns=4;i=283");
            communicator.WriteValue(vehicleInLot, new VehicleType[]
            {
                new TruckType
                {
                    Engine = EngineType.Diesel,
                    Make = "DAFF",
                    Model = "DAFF Transit",
                    CargoCapacity = 4000

                },
                  new CarType
                {
                    Engine = EngineType.Petrol,
                    Make = "Chevrolet",
                    Model = "Kalos",
                    NoOfPassengers = 2

                },
                  new BicycleType
                  {
                      NoOfGears = 5,
                      ManufacturerName = "Monark",
                      Engine = EngineType.Manual,
                      Make = "Monark",
                      Model = "Bicycle"
                  },
                  new ScooterType
                    {
                         
                         ManufacturerName = "Monark",
                         Engine = EngineType.Petrol,
                         Make = "Monark s1",
                         NoOfSeats = 2,
                         Model = "Scooter"
                    }
            });
        }
    }
}