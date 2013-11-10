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

using System;

namespace OsmSharp.Osm.Data.Processor.Filter.BoundingBox.LongIndex
{
    internal class LongIndexNode : ILongIndexNode
    {
        private readonly short _base_number;

        public LongIndexNode(short base_number)
        {
            _base_number = base_number;
        }

        public ILongIndexNode has_0 { get; set; }
        public ILongIndexNode has_1 { get; set; }
        public ILongIndexNode has_2 { get; set; }
        public ILongIndexNode has_3 { get; set; }
        public ILongIndexNode has_4 { get; set; }
        public ILongIndexNode has_5 { get; set; }
        public ILongIndexNode has_6 { get; set; }
        public ILongIndexNode has_7 { get; set; }
        public ILongIndexNode has_8 { get; set; }
        public ILongIndexNode has_9 { get; set; }

        public static ILongIndexNode CreateForBase(short base_number)
        {
            if (base_number == 0)
                return new LongIndexLeaf();
            else
                return new LongIndexNode(base_number);
        }

        public static long CalculateBaseNumber(short base_number)
        {
            switch (base_number)
            {
                    // for performance reasons
                case 0:
                    return 1;
                case 1:
                    return 10;
                case 2:
                    return 100;
                case 3:
                    return 1000;
                case 4:
                    return 10000;
                case 5:
                    return 100000;
                case 6:
                    return 1000000;
                case 7:
                    return 10000000;
                case 8:
                    return 100000000;
                case 9:
                    return 1000000000;
                case 10:
                    return 10000000000;
                default:
                    return (long) Math.Pow(10, base_number);
            }
        }

        public long CalculateDigit(long number)
        {
            long base_number_plus = CalculateBaseNumber((short) (Base + 1));
            long base_number = CalculateBaseNumber(Base);
            return (number - ((number / base_number_plus) * base_number_plus)) / base_number;
        }

        #region ILongIndexNode Members

        public bool Contains(long number)
        {
            switch (CalculateDigit(number))
            {
                case 0:
                    if (has_0 == null)
                        return false;
                    return has_0.Contains(number);
                case 1:
                    if (has_1 == null)
                        return false;
                    return has_1.Contains(number);
                case 2:
                    if (has_2 == null)
                        return false;
                    return has_2.Contains(number);
                case 3:
                    if (has_3 == null)
                        return false;
                    return has_3.Contains(number);
                case 4:
                    if (has_4 == null)
                        return false;
                    return has_4.Contains(number);
                case 5:
                    if (has_5 == null)
                        return false;
                    return has_5.Contains(number);
                case 6:
                    if (has_6 == null)
                        return false;
                    return has_6.Contains(number);
                case 7:
                    if (has_7 == null)
                        return false;
                    return has_7.Contains(number);
                case 8:
                    if (has_8 == null)
                        return false;
                    return has_8.Contains(number);
                case 9:
                    if (has_9 == null)
                        return false;
                    return has_9.Contains(number);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Add(long number)
        {
            switch (CalculateDigit(number))
            {
                case 0:
                    if (has_0 == null)
                        has_0 = CreateForBase((short) (Base - 1));
                    has_0.Add(number);
                    break;
                case 1:
                    if (has_1 == null)
                        has_1 = CreateForBase((short) (Base - 1));
                    has_1.Add(number);
                    break;
                case 2:
                    if (has_2 == null)
                        has_2 = CreateForBase((short) (Base - 1));
                    has_2.Add(number);
                    break;
                case 3:
                    if (has_3 == null)
                        has_3 = CreateForBase((short) (Base - 1));
                    has_3.Add(number);
                    break;
                case 4:
                    if (has_4 == null)
                        has_4 = CreateForBase((short) (Base - 1));
                    has_4.Add(number);
                    break;
                case 5:
                    if (has_5 == null)
                        has_5 = CreateForBase((short) (Base - 1));
                    has_5.Add(number);
                    break;
                case 6:
                    if (has_6 == null)
                        has_6 = CreateForBase((short) (Base - 1));
                    has_6.Add(number);
                    break;
                case 7:
                    if (has_7 == null)
                        has_7 = CreateForBase((short) (Base - 1));
                    has_7.Add(number);
                    break;
                case 8:
                    if (has_8 == null)
                        has_8 = CreateForBase((short) (Base - 1));
                    has_8.Add(number);
                    break;
                case 9:
                    if (has_9 == null)
                        has_9 = CreateForBase((short) (Base - 1));
                    has_9.Add(number);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Remove(long number)
        {
            switch (CalculateDigit(number))
            {
                case 0:
                    if (has_0 != null)
                        has_0.Remove(number);
                    break;
                case 1:
                    if (has_1 != null)
                        has_1.Remove(number);
                    break;
                case 2:
                    if (has_2 != null)
                        has_2.Remove(number);
                    break;
                case 3:
                    if (has_3 != null)
                        has_3.Remove(number);
                    break;
                case 4:
                    if (has_4 != null)
                        has_4.Remove(number);
                    break;
                case 5:
                    if (has_5 != null)
                        has_5.Remove(number);
                    break;
                case 6:
                    if (has_6 != null)
                        has_6.Remove(number);
                    break;
                case 7:
                    if (has_7 != null)
                        has_7.Remove(number);
                    break;
                case 8:
                    if (has_8 != null)
                        has_8.Remove(number);
                    break;
                case 9:
                    if (has_9 != null)
                        has_9.Remove(number);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public short Base
        {
            get { return _base_number; }
        }

        #endregion
    }
}