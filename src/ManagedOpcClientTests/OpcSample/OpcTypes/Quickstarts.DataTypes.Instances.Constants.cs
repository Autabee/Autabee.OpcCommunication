/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Xml;
using System.Runtime.Serialization;
using Opc.Ua;

namespace Quickstarts.DataTypes.Instances
{
    #region DataType Identifiers
    /// <summary>
    /// A class that declares constants for all DataTypes in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class DataTypes
    {
        /// <summary>
        /// The identifier for the ParkingLotType DataType.
        /// </summary>
        public const uint ParkingLotType = 378;

        /// <summary>
        /// The identifier for the TwoWheelerType DataType.
        /// </summary>
        public const uint TwoWheelerType = 15014;

        /// <summary>
        /// The identifier for the BicycleType DataType.
        /// </summary>
        public const uint BicycleType = 15004;

        /// <summary>
        /// The identifier for the ScooterType DataType.
        /// </summary>
        public const uint ScooterType = 15015;
    }
    #endregion

    #region Object Identifiers
    /// <summary>
    /// A class that declares constants for all Objects in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Objects
    {
        /// <summary>
        /// The identifier for the ParkingLot Object.
        /// </summary>
        public const uint ParkingLot = 281;

        /// <summary>
        /// The identifier for the ParkingLot_DriverOfTheMonth Object.
        /// </summary>
        public const uint ParkingLot_DriverOfTheMonth = 375;

        /// <summary>
        /// The identifier for the TwoWheelerType_Encoding_DefaultBinary Object.
        /// </summary>
        public const uint TwoWheelerType_Encoding_DefaultBinary = 15016;

        /// <summary>
        /// The identifier for the BicycleType_Encoding_DefaultBinary Object.
        /// </summary>
        public const uint BicycleType_Encoding_DefaultBinary = 15005;

        /// <summary>
        /// The identifier for the ScooterType_Encoding_DefaultBinary Object.
        /// </summary>
        public const uint ScooterType_Encoding_DefaultBinary = 15017;

        /// <summary>
        /// The identifier for the TwoWheelerType_Encoding_DefaultXml Object.
        /// </summary>
        public const uint TwoWheelerType_Encoding_DefaultXml = 15024;

        /// <summary>
        /// The identifier for the BicycleType_Encoding_DefaultXml Object.
        /// </summary>
        public const uint BicycleType_Encoding_DefaultXml = 15009;

        /// <summary>
        /// The identifier for the ScooterType_Encoding_DefaultXml Object.
        /// </summary>
        public const uint ScooterType_Encoding_DefaultXml = 15025;

        /// <summary>
        /// The identifier for the TwoWheelerType_Encoding_DefaultJson Object.
        /// </summary>
        public const uint TwoWheelerType_Encoding_DefaultJson = 15032;

        /// <summary>
        /// The identifier for the BicycleType_Encoding_DefaultJson Object.
        /// </summary>
        public const uint BicycleType_Encoding_DefaultJson = 15013;

        /// <summary>
        /// The identifier for the ScooterType_Encoding_DefaultJson Object.
        /// </summary>
        public const uint ScooterType_Encoding_DefaultJson = 15033;
    }
    #endregion

    #region Variable Identifiers
    /// <summary>
    /// A class that declares constants for all Variables in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Variables
    {
        /// <summary>
        /// The identifier for the ParkingLotType_EnumValues Variable.
        /// </summary>
        public const uint ParkingLotType_EnumValues = 15001;

        /// <summary>
        /// The identifier for the ParkingLot_LotType Variable.
        /// </summary>
        public const uint ParkingLot_LotType = 380;

        /// <summary>
        /// The identifier for the ParkingLot_DriverOfTheMonth_PrimaryVehicle Variable.
        /// </summary>
        public const uint ParkingLot_DriverOfTheMonth_PrimaryVehicle = 376;

        /// <summary>
        /// The identifier for the ParkingLot_DriverOfTheMonth_OwnedVehicles Variable.
        /// </summary>
        public const uint ParkingLot_DriverOfTheMonth_OwnedVehicles = 377;

        /// <summary>
        /// The identifier for the ParkingLot_VehiclesInLot Variable.
        /// </summary>
        public const uint ParkingLot_VehiclesInLot = 283;

        /// <summary>
        /// The identifier for the DataTypeInstances_BinarySchema Variable.
        /// </summary>
        public const uint DataTypeInstances_BinarySchema = 353;

        /// <summary>
        /// The identifier for the DataTypeInstances_BinarySchema_NamespaceUri Variable.
        /// </summary>
        public const uint DataTypeInstances_BinarySchema_NamespaceUri = 355;

        /// <summary>
        /// The identifier for the DataTypeInstances_BinarySchema_Deprecated Variable.
        /// </summary>
        public const uint DataTypeInstances_BinarySchema_Deprecated = 15002;

        /// <summary>
        /// The identifier for the DataTypeInstances_BinarySchema_TwoWheelerType Variable.
        /// </summary>
        public const uint DataTypeInstances_BinarySchema_TwoWheelerType = 15018;

        /// <summary>
        /// The identifier for the DataTypeInstances_BinarySchema_BicycleType Variable.
        /// </summary>
        public const uint DataTypeInstances_BinarySchema_BicycleType = 15006;

        /// <summary>
        /// The identifier for the DataTypeInstances_BinarySchema_ScooterType Variable.
        /// </summary>
        public const uint DataTypeInstances_BinarySchema_ScooterType = 15021;

        /// <summary>
        /// The identifier for the DataTypeInstances_XmlSchema Variable.
        /// </summary>
        public const uint DataTypeInstances_XmlSchema = 341;

        /// <summary>
        /// The identifier for the DataTypeInstances_XmlSchema_NamespaceUri Variable.
        /// </summary>
        public const uint DataTypeInstances_XmlSchema_NamespaceUri = 343;

        /// <summary>
        /// The identifier for the DataTypeInstances_XmlSchema_Deprecated Variable.
        /// </summary>
        public const uint DataTypeInstances_XmlSchema_Deprecated = 15003;

        /// <summary>
        /// The identifier for the DataTypeInstances_XmlSchema_TwoWheelerType Variable.
        /// </summary>
        public const uint DataTypeInstances_XmlSchema_TwoWheelerType = 15026;

        /// <summary>
        /// The identifier for the DataTypeInstances_XmlSchema_BicycleType Variable.
        /// </summary>
        public const uint DataTypeInstances_XmlSchema_BicycleType = 15010;

        /// <summary>
        /// The identifier for the DataTypeInstances_XmlSchema_ScooterType Variable.
        /// </summary>
        public const uint DataTypeInstances_XmlSchema_ScooterType = 15029;
    }
    #endregion

    #region DataType Node Identifiers
    /// <summary>
    /// A class that declares constants for all DataTypes in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class DataTypeIds
    {
        /// <summary>
        /// The identifier for the ParkingLotType DataType.
        /// </summary>
        public static readonly ExpandedNodeId ParkingLotType = new ExpandedNodeId(Quickstarts.DataTypes.Instances.DataTypes.ParkingLotType, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the TwoWheelerType DataType.
        /// </summary>
        public static readonly ExpandedNodeId TwoWheelerType = new ExpandedNodeId(Quickstarts.DataTypes.Instances.DataTypes.TwoWheelerType, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the BicycleType DataType.
        /// </summary>
        public static readonly ExpandedNodeId BicycleType = new ExpandedNodeId(Quickstarts.DataTypes.Instances.DataTypes.BicycleType, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the ScooterType DataType.
        /// </summary>
        public static readonly ExpandedNodeId ScooterType = new ExpandedNodeId(Quickstarts.DataTypes.Instances.DataTypes.ScooterType, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);
    }
    #endregion

    #region Object Node Identifiers
    /// <summary>
    /// A class that declares constants for all Objects in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectIds
    {
        /// <summary>
        /// The identifier for the ParkingLot Object.
        /// </summary>
        public static readonly ExpandedNodeId ParkingLot = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Objects.ParkingLot, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the ParkingLot_DriverOfTheMonth Object.
        /// </summary>
        public static readonly ExpandedNodeId ParkingLot_DriverOfTheMonth = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Objects.ParkingLot_DriverOfTheMonth, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the TwoWheelerType_Encoding_DefaultBinary Object.
        /// </summary>
        public static readonly ExpandedNodeId TwoWheelerType_Encoding_DefaultBinary = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Objects.TwoWheelerType_Encoding_DefaultBinary, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the BicycleType_Encoding_DefaultBinary Object.
        /// </summary>
        public static readonly ExpandedNodeId BicycleType_Encoding_DefaultBinary = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Objects.BicycleType_Encoding_DefaultBinary, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the ScooterType_Encoding_DefaultBinary Object.
        /// </summary>
        public static readonly ExpandedNodeId ScooterType_Encoding_DefaultBinary = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Objects.ScooterType_Encoding_DefaultBinary, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the TwoWheelerType_Encoding_DefaultXml Object.
        /// </summary>
        public static readonly ExpandedNodeId TwoWheelerType_Encoding_DefaultXml = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Objects.TwoWheelerType_Encoding_DefaultXml, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the BicycleType_Encoding_DefaultXml Object.
        /// </summary>
        public static readonly ExpandedNodeId BicycleType_Encoding_DefaultXml = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Objects.BicycleType_Encoding_DefaultXml, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the ScooterType_Encoding_DefaultXml Object.
        /// </summary>
        public static readonly ExpandedNodeId ScooterType_Encoding_DefaultXml = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Objects.ScooterType_Encoding_DefaultXml, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the TwoWheelerType_Encoding_DefaultJson Object.
        /// </summary>
        public static readonly ExpandedNodeId TwoWheelerType_Encoding_DefaultJson = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Objects.TwoWheelerType_Encoding_DefaultJson, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the BicycleType_Encoding_DefaultJson Object.
        /// </summary>
        public static readonly ExpandedNodeId BicycleType_Encoding_DefaultJson = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Objects.BicycleType_Encoding_DefaultJson, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the ScooterType_Encoding_DefaultJson Object.
        /// </summary>
        public static readonly ExpandedNodeId ScooterType_Encoding_DefaultJson = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Objects.ScooterType_Encoding_DefaultJson, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);
    }
    #endregion

    #region Variable Node Identifiers
    /// <summary>
    /// A class that declares constants for all Variables in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class VariableIds
    {
        /// <summary>
        /// The identifier for the ParkingLotType_EnumValues Variable.
        /// </summary>
        public static readonly ExpandedNodeId ParkingLotType_EnumValues = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Variables.ParkingLotType_EnumValues, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the ParkingLot_LotType Variable.
        /// </summary>
        public static readonly ExpandedNodeId ParkingLot_LotType = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Variables.ParkingLot_LotType, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the ParkingLot_DriverOfTheMonth_PrimaryVehicle Variable.
        /// </summary>
        public static readonly ExpandedNodeId ParkingLot_DriverOfTheMonth_PrimaryVehicle = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Variables.ParkingLot_DriverOfTheMonth_PrimaryVehicle, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the ParkingLot_DriverOfTheMonth_OwnedVehicles Variable.
        /// </summary>
        public static readonly ExpandedNodeId ParkingLot_DriverOfTheMonth_OwnedVehicles = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Variables.ParkingLot_DriverOfTheMonth_OwnedVehicles, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the ParkingLot_VehiclesInLot Variable.
        /// </summary>
        public static readonly ExpandedNodeId ParkingLot_VehiclesInLot = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Variables.ParkingLot_VehiclesInLot, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the DataTypeInstances_BinarySchema Variable.
        /// </summary>
        public static readonly ExpandedNodeId DataTypeInstances_BinarySchema = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Variables.DataTypeInstances_BinarySchema, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the DataTypeInstances_BinarySchema_NamespaceUri Variable.
        /// </summary>
        public static readonly ExpandedNodeId DataTypeInstances_BinarySchema_NamespaceUri = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Variables.DataTypeInstances_BinarySchema_NamespaceUri, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the DataTypeInstances_BinarySchema_Deprecated Variable.
        /// </summary>
        public static readonly ExpandedNodeId DataTypeInstances_BinarySchema_Deprecated = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Variables.DataTypeInstances_BinarySchema_Deprecated, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the DataTypeInstances_BinarySchema_TwoWheelerType Variable.
        /// </summary>
        public static readonly ExpandedNodeId DataTypeInstances_BinarySchema_TwoWheelerType = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Variables.DataTypeInstances_BinarySchema_TwoWheelerType, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the DataTypeInstances_BinarySchema_BicycleType Variable.
        /// </summary>
        public static readonly ExpandedNodeId DataTypeInstances_BinarySchema_BicycleType = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Variables.DataTypeInstances_BinarySchema_BicycleType, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the DataTypeInstances_BinarySchema_ScooterType Variable.
        /// </summary>
        public static readonly ExpandedNodeId DataTypeInstances_BinarySchema_ScooterType = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Variables.DataTypeInstances_BinarySchema_ScooterType, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the DataTypeInstances_XmlSchema Variable.
        /// </summary>
        public static readonly ExpandedNodeId DataTypeInstances_XmlSchema = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Variables.DataTypeInstances_XmlSchema, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the DataTypeInstances_XmlSchema_NamespaceUri Variable.
        /// </summary>
        public static readonly ExpandedNodeId DataTypeInstances_XmlSchema_NamespaceUri = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Variables.DataTypeInstances_XmlSchema_NamespaceUri, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the DataTypeInstances_XmlSchema_Deprecated Variable.
        /// </summary>
        public static readonly ExpandedNodeId DataTypeInstances_XmlSchema_Deprecated = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Variables.DataTypeInstances_XmlSchema_Deprecated, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the DataTypeInstances_XmlSchema_TwoWheelerType Variable.
        /// </summary>
        public static readonly ExpandedNodeId DataTypeInstances_XmlSchema_TwoWheelerType = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Variables.DataTypeInstances_XmlSchema_TwoWheelerType, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the DataTypeInstances_XmlSchema_BicycleType Variable.
        /// </summary>
        public static readonly ExpandedNodeId DataTypeInstances_XmlSchema_BicycleType = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Variables.DataTypeInstances_XmlSchema_BicycleType, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);

        /// <summary>
        /// The identifier for the DataTypeInstances_XmlSchema_ScooterType Variable.
        /// </summary>
        public static readonly ExpandedNodeId DataTypeInstances_XmlSchema_ScooterType = new ExpandedNodeId(Quickstarts.DataTypes.Instances.Variables.DataTypeInstances_XmlSchema_ScooterType, Quickstarts.DataTypes.Instances.Namespaces.DataTypeInstances);
    }
    #endregion

    #region BrowseName Declarations
    /// <summary>
    /// Declares all of the BrowseNames used in the Model Design.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class BrowseNames
    {
        /// <summary>
        /// The BrowseName for the BicycleType component.
        /// </summary>
        public const string BicycleType = "BicycleType";

        /// <summary>
        /// The BrowseName for the DataTypeInstances_BinarySchema component.
        /// </summary>
        public const string DataTypeInstances_BinarySchema = "Quickstarts.DataTypes.Instances";

        /// <summary>
        /// The BrowseName for the DataTypeInstances_XmlSchema component.
        /// </summary>
        public const string DataTypeInstances_XmlSchema = "Quickstarts.DataTypes.Instances";

        /// <summary>
        /// The BrowseName for the DriverOfTheMonth component.
        /// </summary>
        public const string DriverOfTheMonth = "DriverOfTheMonth";

        /// <summary>
        /// The BrowseName for the LotType component.
        /// </summary>
        public const string LotType = "LotType";

        /// <summary>
        /// The BrowseName for the ParkingLot component.
        /// </summary>
        public const string ParkingLot = "ParkingLot";

        /// <summary>
        /// The BrowseName for the ParkingLotType component.
        /// </summary>
        public const string ParkingLotType = "ParkingLotType";

        /// <summary>
        /// The BrowseName for the ScooterType component.
        /// </summary>
        public const string ScooterType = "ScooterType";

        /// <summary>
        /// The BrowseName for the TwoWheelerType component.
        /// </summary>
        public const string TwoWheelerType = "TwoWheelerType";

        /// <summary>
        /// The BrowseName for the VehiclesInLot component.
        /// </summary>
        public const string VehiclesInLot = "VehiclesInLot";
    }
    #endregion

    #region Namespace Declarations
    /// <summary>
    /// Defines constants for all namespaces referenced by the model design.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Namespaces
    {
        /// <summary>
        /// The URI for the OpcUa namespace (.NET code namespace is 'Opc.Ua').
        /// </summary>
        public const string OpcUa = "http://opcfoundation.org/UA/";

        /// <summary>
        /// The URI for the OpcUaXsd namespace (.NET code namespace is 'Opc.Ua').
        /// </summary>
        public const string OpcUaXsd = "http://opcfoundation.org/UA/2008/02/Types.xsd";

        /// <summary>
        /// The URI for the DataTypes namespace (.NET code namespace is 'Quickstarts.DataTypes.Types').
        /// </summary>
        public const string DataTypes = "http://opcfoundation.org/UA/Quickstarts/DataTypes/Types";

        /// <summary>
        /// The URI for the DataTypeInstances namespace (.NET code namespace is 'Quickstarts.DataTypes.Instances').
        /// </summary>
        public const string DataTypeInstances = "http://opcfoundation.org/UA/Quickstarts/DataTypes/Instances";
    }
    #endregion
}