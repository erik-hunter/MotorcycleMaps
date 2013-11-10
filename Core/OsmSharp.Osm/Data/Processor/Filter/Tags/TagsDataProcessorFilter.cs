using System;
using OsmSharp.Osm.Data.Core.Processor;
using OsmSharp.Osm.Simple;
using System.Collections.Generic;
using OsmSharp.Osm.Data.Processor.Filter.BoundingBox.LongIndex;

namespace OsmSharp.Osm.Data.Processor.Filter.Tags
{
    /// <summary>
    ///     A data processor filter that filters objects by their tags.
    /// </summary>
    public class TagsDataProcessorFilter : DataProcessorFilter
    {
        /// <summary>
        ///     Holds the nodes filter.
        /// </summary>
        private readonly Filters.Filter _nodesFilter;

        /// <summary>
        ///     Keeps the objects in the relations also.
        /// </summary>
        private readonly bool _relationKeepObjects;

        /// <summary>
        ///     Holds the relations filter.
        /// </summary>
        private readonly Filters.Filter _relationsFilter;

        /// <summary>
        ///     Keeps the nodes in the ways also.
        /// </summary>
        private readonly bool _wayKeepNodes;

        /// <summary>
        ///     Holds the ways filter.
        /// </summary>
        private readonly Filters.Filter _waysFilter;

        /// <summary>
        ///     Holds the current object.
        /// </summary>
        private SimpleOsmGeo _current;

        /// <summary>
        ///     Filters data according to the given filters.
        /// </summary>
        /// <param name="nodesFilter"></param>
        /// <param name="waysFilter"></param>
        /// <param name="relationsFilter"></param>
        public TagsDataProcessorFilter(Filters.Filter nodesFilter, Filters.Filter waysFilter, Filters.Filter relationsFilter)
        {
            _nodesFilter = nodesFilter;
            _waysFilter = waysFilter;
            _relationsFilter = relationsFilter;

            _wayKeepNodes = false;
            _relationKeepObjects = false;
        }


        /// <summary>
        /// Filters data according to the given filters.
        /// </summary>
        /// <param name="nodesFilter"></param>
        /// <param name="waysFilter"></param>
        /// <param name="relationsFilter"></param>
        /// <param name="wayKeepNodes"></param>
        /// <param name="relationKeepObjects"></param>
        public TagsDataProcessorFilter(Filters.Filter nodesFilter, Filters.Filter waysFilter,
                                       Filters.Filter relationsFilter, bool wayKeepNodes, bool relationKeepObjects)
        {
            _nodesFilter = nodesFilter;
            _waysFilter = waysFilter;
            _relationsFilter = relationsFilter;

            _wayKeepNodes = wayKeepNodes;
            _relationKeepObjects = relationKeepObjects;
        }

        /// <summary>
        /// Returns true if this filter can be reset.
        /// </summary>
        public override bool CanReset
        {
            get { return Source.CanReset; }
        }

        /// <summary>
        ///     Initializes this filter.
        /// </summary>
        public override void Initialize()
        {
            if (Source == null)
                throw new Exception("No source registered!");
            Source.Initialize();
        }

        /// <summary>
        /// Moves to the next object.
        /// </summary>
        /// <returns></returns>
        public override bool MoveNext()
        {
            if (!_relationKeepObjects && !_wayKeepNodes)
            {
                return this.MoveNextWithoutChildren();
            }
            else
            { // we need two-passes.
                return this.MoveNextWithChildren();
            }
        }

        #region Non-Keep Child objects-code

        /// <summary>
        /// Execute the move next without considering keeping children.
        /// </summary>
        /// <returns></returns>
        private bool MoveNextWithoutChildren()
        {
            // simple here just filter!
            bool filter_ok = false;
            while (!filter_ok)
            {
                if (Source.MoveNext())
                {
                    SimpleOsmGeo current = Source.Current();

                    switch (current.Type)
                    {
                        case SimpleOsmGeoType.Node:
                            if (_nodesFilter == null || _nodesFilter.Evaluate(current))
                            {
                                _current = current;
                                return true;
                            }
                            break;
                        case SimpleOsmGeoType.Way:
                            if (_waysFilter == null || _waysFilter.Evaluate(current))
                            {
                                _current = current;
                                return true;
                            }
                            break;
                        case SimpleOsmGeoType.Relation:
                            if (_relationsFilter == null || _relationsFilter.Evaluate(current))
                            {
                                _current = current;
                                return true;
                            }
                            break;
                    }
                }
                else
                {
                    // there is no more data in the source!
                    return false;
                }
            }
            return false;
        }

        #endregion

        #region Keep Child Objects-code

        /// <summary>
        /// Holds the ids of all nodes that should end up in the output-stream as a child of a filtered way or relation.
        /// </summary>
        private LongIndex _nodesToInclude = null;

        /// <summary>
        /// Holds the ids of all relations that should end up in the output-stream as a child of a filtered relation.
        /// </summary>
        private LongIndex _relationsToInclude = null;

        /// <summary>
        /// Holds the ids of all ways that should and up in the output-stream as a child of a filtered relation.
        /// </summary>
        private LongIndex _waysToInclude = null;

        /// <summary>
        /// Executes a move next and return children.
        /// </summary>
        /// <returns></returns>
        private bool MoveNextWithChildren()
        {
            // index all objects to keep.
            if (_nodesToInclude == null)
            {
                _nodesToInclude = new LongIndex();
                _waysToInclude = new LongIndex();
                _relationsToInclude = new LongIndex();

                bool inconclusive = true;
                while (inconclusive)
                { // keep looping until it is sure all data is included.
                    inconclusive = false;
                    while (this.Source.MoveNext())
                    {
                        SimpleOsmGeo current = this.Source.Current();
                        switch (current.Type)
                        {
                            case SimpleOsmGeoType.Way:
                                if (_wayKeepNodes)
                                { // yes keep the nodes please!
                                    if (_waysFilter == null || _waysFilter.Evaluate(current) || _waysToInclude.Contains(current.Id.Value))
                                    { // keep children of any included way.
                                        SimpleWay way = current as SimpleWay;
                                        if (way != null && way.Nodes != null)
                                        {
                                            foreach (long nodeId in way.Nodes)
                                            {
                                                _nodesToInclude.Add(nodeId);
                                            }
                                        }
                                    }
                                }
                                break;
                            case SimpleOsmGeoType.Relation:
                                if (_relationKeepObjects)
                                { // yes keep the members please!
                                    if (_relationsFilter == null || _relationsFilter.Evaluate(current) || _relationsToInclude.Contains(current.Id.Value))
                                    { // keep members of any contained relation.
                                        SimpleRelation relation = current as SimpleRelation;
                                        if (relation != null && relation.Members != null)
                                        {
                                            foreach (SimpleRelationMember member in relation.Members)
                                            {
                                                switch (member.MemberType.Value)
                                                {
                                                    case SimpleRelationMemberType.Node:
                                                        if (_nodesFilter == null || _nodesToInclude.Contains(member.MemberId.Value))
                                                        { // node is included anyway.

                                                        }
                                                        else
                                                        { // node was not yet included.
                                                            _nodesToInclude.Add(member.MemberId.Value);
                                                        }
                                                        break;
                                                    case SimpleRelationMemberType.Way:
                                                        if (_waysFilter == null || _waysToInclude.Contains(member.MemberId.Value))
                                                        { // way is included anyway.

                                                        }
                                                        else
                                                        { // way was not included yet.
                                                            if (_wayKeepNodes)
                                                            { // oeps, also keep all nodes for all included ways!
                                                                inconclusive = true;
                                                            }
                                                            _waysToInclude.Add(member.MemberId.Value);
                                                        }
                                                        break;
                                                    case SimpleRelationMemberType.Relation:
                                                        if (_relationsFilter == null || _relationsToInclude.Contains(member.MemberId.Value))
                                                        { // relation is included anyway.

                                                        }
                                                        else
                                                        { // oeps, relation was not yet included and relation member need to be kept.
                                                            inconclusive = true;
                                                            _relationsToInclude.Add(member.MemberId.Value);
                                                        }
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                    }

                    // reset the source.
                    this.Source.Reset();
                }
            }

            // Filter or include.
            bool filter_ok = false;
            while (!filter_ok)
            {
                if (Source.MoveNext())
                {
                    SimpleOsmGeo current = Source.Current();

                    switch (current.Type)
                    {
                        case SimpleOsmGeoType.Node:
                            if (_nodesFilter == null || _nodesFilter.Evaluate(current) || _nodesToInclude.Contains(current.Id.Value))
                            {
                                _current = current;
                                return true;
                            }
                            break;
                        case SimpleOsmGeoType.Way:
                            if (_waysFilter == null || _waysFilter.Evaluate(current) || _waysToInclude.Contains(current.Id.Value))
                            {
                                _current = current;
                                return true;
                            }
                            break;
                        case SimpleOsmGeoType.Relation:
                            if (_relationsFilter == null || _relationsFilter.Evaluate(current) || _waysToInclude.Contains(current.Id.Value))
                            {
                                _current = current;
                                return true;
                            }
                            break;
                    }
                }
                else
                {
                    // there is no more data in the source!
                    return false;
                }
            }
            return false;
        }

        #endregion

        /// <summary>
        ///     Returns the current object.
        /// </summary>
        /// <returns></returns>
        public override SimpleOsmGeo Current()
        {
            return _current;
        }

        /// <summary>
        ///     Resets this filter.
        /// </summary>
        public override void Reset()
        {
            _current = null;

            _nodesToInclude = null;
            _waysToInclude = null;
            _relationsToInclude = null;

            _nodesToInclude.Clear();
            _waysToInclude.Clear();
            _relationsToInclude.Clear();

            Source.Reset();
        }

        /// <summary>
        ///     Registers the source of this filter.
        /// </summary>
        /// <param name="source"></param>
        public override void RegisterSource(DataProcessorSource source)
        {
            if (_wayKeepNodes || _relationKeepObjects)
            {
                if (!source.CanReset)
                    throw new ArgumentException("The tags data processor source cannot be reset!", "source");
            }

            base.RegisterSource(source);
        }
    }
}