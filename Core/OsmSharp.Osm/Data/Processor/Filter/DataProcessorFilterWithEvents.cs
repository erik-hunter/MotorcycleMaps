using System;
using OsmSharp.Osm.Data.Core.Processor;
using OsmSharp.Osm.Simple;

namespace OsmSharp.Osm.Data.Processor.Filter
{
    /// <summary>
    ///     A filter that raizes events for each object.
    /// </summary>
    public class DataProcessorFilterWithEvents : DataProcessorFilter
    {
        /// <summary>
        ///     An empty delegate.
        /// </summary>
        public delegate void EmptyDelegate();

        /// <summary>
        ///     A delegate with a simple osm geo object as parameter.
        /// </summary>
        /// <param name="simple_osm_geo"></param>
        /// <param name="param"></param>
        public delegate void SimpleOsmGeoDelegate(SimpleOsmGeo simple_osm_geo, object param);

        /// <summary>
        ///     Holds the parameters object sent with the events.
        /// </summary>
        private readonly object _param;

        /// <summary>
        ///     Creates a new filter with events.
        /// </summary>
        public DataProcessorFilterWithEvents()
        {
            _param = null;
        }

        /// <summary>
        ///     Creates a new filter with events.
        /// </summary>
        /// <param name="param"></param>
        public DataProcessorFilterWithEvents(object param)
        {
            _param = param;
        }

        /// <summary>
        ///     Returns true if this filter can be reset.
        /// </summary>
        public override bool CanReset
        {
            get { return Source.CanReset; }
        }

        /// <summary>
        ///     Event raised when filter is initialized.
        /// </summary>
        public event EmptyDelegate InitializeEvent = delegate { };

        /// <summary>
        ///     Initializes this filter.
        /// </summary>
        public override void Initialize()
        {
            InitializeEvent();

            if (Source == null)
                throw new Exception("No source registered!");
            Source.Initialize();
        }

        /// <summary>
        ///     Event raised when the move is made to the next object.
        /// </summary>
        public event SimpleOsmGeoDelegate MovedToNextEvent = delegate { };

        /// <summary>
        ///     Moves this filter to the next object.
        /// </summary>
        /// <returns></returns>
        public override bool MoveNext()
        {
            if (Source.MoveNext())
            {
                MovedToNextEvent(Source.Current(), _param);
                return true;
            }
            return false;
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
        ///     Resets this filter.
        /// </summary>
        public override void Reset()
        {
            Source.Reset();
        }
    }
}