using Opc.Ua;
using System.Collections.Generic;
using System.Linq;

namespace Autabee.Communication.ManagedOpcClient.Utilities
{
    static public class BrowseHelperFunctions
    {
        public static (List<T>, ByteStringCollection) GetContinuationPoints<T>(
            List<T> nodesToBrowse,
            BrowseResultCollection results)
        {
            var unprocessedOperations = new List<T>();
            var continuationPoints = new ByteStringCollection();
            for (int i = 0; i < continuationPoints.Count; i++)
            {
                // check for error.
                if (StatusCode.IsBad(results[i].StatusCode))
                {
                    // this error indicates that the server does not have enough simultaneously active 
                    // continuation points. This request will need to be resent after the other operations
                    // have been completed and their continuation points released.
                    if (results[i].StatusCode == StatusCodes.BadNoContinuationPoints)
                    {
                        unprocessedOperations.Add(nodesToBrowse[i]);
                    }

                    continue;
                }
                if (results[i].References.Count == 0)
                {
                    continue;
                }
                if (results[i].ContinuationPoint != null) { 
                    continuationPoints.Add(results[i].ContinuationPoint); 
                }
            }
            return (unprocessedOperations, continuationPoints);
        }

        public static ByteStringCollection GetNewContinuationPoints(
            ByteStringCollection continuationPoints,
            BrowseResultCollection results)
        {
            var (unprocessedOperations, revisedContinuationPoints) = GetContinuationPoints(continuationPoints, results);
            revisedContinuationPoints.AddRange(unprocessedOperations);
            return revisedContinuationPoints;
        }

        public static ReferenceDescriptionCollection GetDescriptions(BrowseResponse results) => GetDescriptions(
            results.Results);

        public static ReferenceDescriptionCollection GetDescriptions(BrowseNextResponse results) => GetDescriptions(
            results.Results);

        public static ReferenceDescriptionCollection GetDescriptions(BrowseResultCollection results)
        {
            ReferenceDescriptionCollection temp = new ReferenceDescriptionCollection();
            foreach (ReferenceDescriptionCollection item in
                results.Where(o => StatusCode.IsNotBad(o.StatusCode))
                       .Select(o => o.References))
                temp.AddRange(item);

            return temp;
        }


        

        public static BrowseDescription GetChildrenBrowseDescription(NodeId node) => new BrowseDescription()
        {
            NodeId = node,
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
            IncludeSubtypes = true,
            NodeClassMask = 255u,
            ResultMask = (uint)BrowseResultMask.All
        };

        public static BrowseDescription GetParentBrowseDescription(NodeId node) => new BrowseDescription()
        {
            NodeId = node,
            BrowseDirection = BrowseDirection.Inverse,
            ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
            IncludeSubtypes = true,
            NodeClassMask = 255u,
            ResultMask = (uint)BrowseResultMask.All
        };

        public static BrowseDescription GetMethodArgumentsBrowseDescription(NodeId node) => new BrowseDescription()
        {
            NodeId = node,
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.HasProperty,
            IncludeSubtypes = true,
            NodeClassMask = (uint)NodeClass.Variable,
            ResultMask = (uint)BrowseResultMask.All
        };

        public static BrowseDescription GetEncodingBrowseDescription(NodeId node) => new BrowseDescription()
        {
            NodeId = node,
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.HasProperty,
            IncludeSubtypes = true,
            NodeClassMask = (uint)NodeClass.Variable,
            ResultMask = (uint)BrowseResultMask.All
        };
    }
}
