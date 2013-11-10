// OsmSharp - OpenStreetMap tools & library.
// Copyright (C) 2012 Abelshausen Ben
// 
// This file is part of OsmSharp.
// 
// OsmSharp is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// OsmSharp is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with OsmSharp. If not, see <http://www.gnu.org/licenses/>.

using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
#if WINDOWS_PHONE
using Ionic.Zlib;
#else
using System.IO.Compression;
#endif
using OsmSharp.Osm.Simple;
using OsmSharp.Osm.Data.Core.Processor;

namespace OsmSharp.Osm.Data.XML.Processor
{
    /// <summary>
    /// A data processor source that read from OSM XML.
    /// </summary>
    public class XmlDataProcessorSource : DataProcessorSource
    {
        private XmlReader _reader;

        private XmlSerializer _serNode;

        private XmlSerializer _serWay;

        private XmlSerializer _serRelation;

        private SimpleOsmGeo _next;

        private readonly string _fileName;

        private readonly Stream _stream;

        private readonly bool _gzip;

        readonly XmlReaderSettings _settings = new XmlReaderSettings { CloseInput = true, CheckCharacters = false, IgnoreComments = true, IgnoreProcessingInstructions = true };

        /// <summary>
        /// Creates a new XML data processor source.
        /// </summary>
        /// <param name="file_name"></param>
        public XmlDataProcessorSource(string file_name) :
            this(file_name,false)
        {

        }

        /// <summary>
        /// Creates a new OSM XML processor source.
        /// </summary>
        /// <param name="stream"></param>
        public XmlDataProcessorSource(Stream stream) :
            this(stream, false)
        {

        }

        /// <summary>
        /// Creates a new OSM XML processor source.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="gzip"></param>
        public XmlDataProcessorSource(Stream stream, bool gzip)
        {
            _stream = stream;
            _gzip = gzip;
        }

        /// <summary>
        /// Creates a new OSM XML processor source.
        /// </summary>
        /// <param name="file_name"></param>
        /// <param name="gzip"></param>
        public XmlDataProcessorSource(string file_name, bool gzip)
        {
            _fileName = file_name;
            _gzip = gzip;
        }

        /// <summary>
        /// Initializes this source.
        /// </summary>
        public override void Initialize()
        {
            _next = null;
            _serNode = new XmlSerializer(typeof(Xml.v0_6.node));
            _serWay = new XmlSerializer(typeof(Xml.v0_6.way));
            _serRelation = new XmlSerializer(typeof(Xml.v0_6.relation));

            Reset();
        }
        
        /// <summary>
        /// Resets this source.
        /// </summary>
        public override void Reset()
        {            
            // create the stream.
            Stream fileStream;
            if (_stream != null)
            { // take the preset stream.
                fileStream = _stream;

                // seek to the beginning of the stream.
                if (fileStream.CanSeek)
                { // if a non-seekable stream is given resetting is disabled.
                    fileStream.Seek(0, SeekOrigin.Begin);
                }
            }
            else
            { // create a file stream.
                fileStream = new FileInfo(_fileName).OpenRead();
            }

            // decompress if needed.
            if (_gzip)
            {
                fileStream = new GZipStream(fileStream, CompressionMode.Decompress);
            }

            TextReader textReader = new StreamReader(fileStream, Encoding.UTF8);
            _reader = XmlReader.Create(textReader, _settings);     
        }

        /// <summary>
        /// Returns true if this source can be reset.
        /// </summary>
        public override bool CanReset
        {
            get
            {
                return _fileName != null || _stream.CanSeek;
            }
        }

        /// <summary>
        /// Moves this source to the next object.
        /// </summary>
        /// <returns></returns>
        public override bool MoveNext()
        {
            while (_reader.Read())
            {
                if (_reader.NodeType != XmlNodeType.Element)
                    continue;

                string name = _reader.Name.ToLower();
                if (name != "node" && name != "way" && name != "relation")
                    continue;

                // create a stream for only this element.
                string nextElement = _reader.ReadOuterXml();
                XmlReader reader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(nextElement)), _settings);
                object osmObj;

                // select type of element.
                switch (name)
                {
                    case "node":
                        osmObj = _serNode.Deserialize(reader);
                        if (osmObj is Xml.v0_6.node)
                        {
                            _next = XmlSimpleConverter.ConvertToSimple(osmObj as Xml.v0_6.node);
                            return true;
                        }
                        break;

                    case "way":
                        osmObj = _serWay.Deserialize(reader);
                        if (osmObj is Xml.v0_6.way)
                        {
                            _next = XmlSimpleConverter.ConvertToSimple(osmObj as Xml.v0_6.way);
                            return true;
                        }
                        break;

                    case "relation":
                        osmObj = _serRelation.Deserialize(reader);
                        if (osmObj is Xml.v0_6.relation)
                        {
                            _next = XmlSimpleConverter.ConvertToSimple(osmObj as Xml.v0_6.relation);
                            return true;
                        }
                        break;
                }
            }
            _next = null;
            return false;
        }

        /// <summary>
        /// Returns the current object.
        /// </summary>
        /// <returns></returns>
        public override SimpleOsmGeo Current()
        {
            return _next;
        }
    }
}
