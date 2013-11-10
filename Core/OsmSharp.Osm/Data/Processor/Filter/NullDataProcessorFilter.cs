using System;
using OsmSharp.Osm.Data.Core.Processor;
using OsmSharp.Osm.Simple;

namespace OsmSharp.Osm.Data.Processor.Filter
{
    /// <summary>
    ///     Does not perform any filtering
    /// </summary>
    public class NullDataProcessorFilter : DataProcessorFilter
    {
        /// <summary>
        ///     Returns true if this source can be reset.
        /// </summary>
        /// <returns></returns>
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
        ///     Moves to the next object.
        /// </summary>
        /// <returns></returns>
        public override bool MoveNext()
        {
            return Source.MoveNext();
        }

        /// <summary>
        ///     Returns the current object.
        /// </summary>
        /// <returns></returns>
        public override SimpleOsmGeo Current()
        {
            return Source.Current();
        }

        /// <summary>
        ///     Resets the source to the beginning.
        /// </summary>
        public override void Reset()
        {
            Source.Reset();
        }
    }
}