﻿using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Autabee.Communication.ManagedOpcClient.Utilities
{
    static public class Browse
    {
        public static (List<T>, ByteStringCollection) GetContinuationPoints<T>(
            List<T> nodesToBrowse,
            BrowseResultCollection results)
        {
            var unprocessedOperations = new List<T>();
            var continuationPoints = new ByteStringCollection();
            for (int i = 0; i < results.Count; i++)
            {
                // check for error.
                if (StatusCode.IsBad(results[i].StatusCode))
                {
                    // this error indicates that the server does not have enough simultaneously active continuation points.
                    // This request will need to be resent after the other operations have been completed and their continuation points released.
                    switch (((uint)results[i].StatusCode)){
                        case StatusCodes.BadNoContinuationPoints:
                            unprocessedOperations.Add(nodesToBrowse[i]);
                            break;
                        default:
                            break;
                    }
                }
                else if (results[i].ContinuationPoint != null)
                {
                    continuationPoints.Add(results[i].ContinuationPoint);
                }
                else if (results[i].References.Count == 0)
                {
                    continue;
                }
            }
            return (unprocessedOperations, continuationPoints);
        }


        public static IEnumerable<BrowseResult> GetEnumerableDoneBrowseResults(BrowseResultCollection results)
        {
            return results.Where(o => !StatusCode.IsBad(o.StatusCode) && o.ContinuationPoint == null);
        }

        public static BrowseResultCollection GetDoneBrowseResults(
            BrowseResultCollection results)
        {
            var continuationPoints = new BrowseResultCollection();

            continuationPoints.AddRange(GetEnumerableDoneBrowseResults(results));
            return continuationPoints;
        }

        public static BrowseResultCollection GetDoneBrowseResults(
            BrowseNextResponse results)
        {
            var continuationPoints = new BrowseResultCollection();

            continuationPoints.AddRange(GetEnumerableDoneBrowseResults(results.Results));
            return continuationPoints;
        }
        public static BrowseResultCollection GetDoneBrowseResults(
            BrowseResponse results)
        {
            var continuationPoints = new BrowseResultCollection();

            continuationPoints.AddRange(GetEnumerableDoneBrowseResults(results.Results));
            return continuationPoints;
        }

        public static ByteStringCollection GetNewContinuationPoints(
            ByteStringCollection continuationPoints,
            BrowseResultCollection results)
        {
            var (unprocessedOperations, revisedContinuationPoints) = GetContinuationPoints(continuationPoints, results);
            revisedContinuationPoints.AddRange(unprocessedOperations);
            return revisedContinuationPoints;
        }

        public static ReferenceDescriptionCollection GetDescriptions(BrowseResponse results) 
            => GetDescriptions(results.Results);   

        public static ReferenceDescriptionCollection GetDescriptions(BrowseResult results) 
            => results.References;

        public static ReferenceDescriptionCollection GetDescriptions(BrowseNextResponse results) 
            => GetDescriptions(results.Results);

        public static ReferenceDescriptionCollection GetDescriptions(BrowseResultCollection results)
        {
            ReferenceDescriptionCollection temp = new ReferenceDescriptionCollection();
            foreach (ReferenceDescriptionCollection item in
                results.Where(o => StatusCode.IsNotBad(o.StatusCode))
                       .Select(o => o.References))
                temp.AddRange(item);

            return temp;
        }
        public static BrowseDescriptionCollection GetBrowseDescription(NodeIdCollection nodes, BrowseType browseType)
        {
            var temp = new BrowseDescriptionCollection();
            temp.AddRange(nodes.Select(o => GetBrowseDescription(o, browseType)));
            return temp;
        }

        public static BrowseDescription GetBrowseDescription(NodeId node, BrowseType browseType)
            => browseType switch
            {
                BrowseType.Children => GetChildrenBrowseDescription(node),
                BrowseType.Parent => GetParentBrowseDescription(node),
                BrowseType.MethodArguments => GetMethodArgumentsBrowseDescription(node),
                BrowseType.Encoding => GetEncodingBrowseDescription(node)
            };
        


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
    public enum BrowseType
    {
        Children,
        Parent,
        MethodArguments,
        Encoding,
    }
}
