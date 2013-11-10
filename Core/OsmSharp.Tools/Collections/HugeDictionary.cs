﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OsmSharp.Tools.Collections.Huge
{
    /// <summary>
    /// A dictionary working around the pre .NET 4.5 memory limitations for one object.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class HugeDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        /// <summary>
        /// Holds the list of internal dictionaries.
        /// </summary>
        private List<IDictionary<TKey, TValue>> _dictionary;

        /// <summary>
        /// Holds the maximum size of one individual dictionary.
        /// </summary>
        private const int _MAX_DIC_SIZE = 1000000;

        /// <summary>
        /// Creates a new huge dictionary.
        /// </summary>
        public HugeDictionary()
        {
            _dictionary = new List<IDictionary<TKey, TValue>>();
            _dictionary.Add(new Dictionary<TKey, TValue>(_MAX_DIC_SIZE));
        }

        /// <summary>
        /// Adds a new element.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(TKey key, TValue value)
        { // adds a new key-value pair.
            bool added = false;
            for (int idx = 0; idx < _dictionary.Count; idx++)
            {
                if (_dictionary[idx].ContainsKey(key))
                {
                    throw new System.ArgumentException("An element with the same key already exists in the System.Collections.Generic.IDictionary<TKey,TValue>.");
                }
                if (!added && _dictionary[idx].Count < _MAX_DIC_SIZE)
                { // add the key-values.
                    _dictionary[idx].Add(key, value);
                    added = true;
                }
            }
            if (!added)
            { // add the key-values.
                _dictionary.Add(new Dictionary<TKey, TValue>(_MAX_DIC_SIZE));
                _dictionary[_dictionary.Count - 1].Add(key, value);
            }
        }

        /// <summary>
        /// Returns true if contains the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(TKey key)
        {
            for (int idx = _dictionary.Count; idx < _dictionary.Count; idx++)
            {
                if (_dictionary[idx].ContainsKey(key))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the collection of all the keys.
        /// </summary>
        public ICollection<TKey> Keys
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Removes an item from this dictionary.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(TKey key)
        {
            for (int idx = 0; idx < _dictionary.Count; idx++)
            {
                if (_dictionary[idx].Remove(key))
                {
                    if (_dictionary[idx].Count == 0 && _dictionary.Count > 1)
                    {
                        _dictionary.RemoveAt(idx);
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Tries getting a value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default(TValue);
            for (int idx = 0; idx < _dictionary.Count; idx++)
            {
                if (_dictionary[idx].TryGetValue(key, out value))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns a collection of all values.
        /// </summary>
        public ICollection<TValue> Values
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets/sets the value corresponding to the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (this.TryGetValue(key, out value))
                {
                    return value;
                }
                throw new System.Collections.Generic.KeyNotFoundException();
            }
            set
            {
                for (int idx = 0; idx < _dictionary.Count; idx++)
                {
                    if (_dictionary[idx].ContainsKey(key))
                    { // replace the original value.
                        _dictionary[idx][key] = value;
                        return;
                    }
                }

                // the original does not exist yet.
                this.Add(key, value);
            }
        }

        /// <summary>
        /// Adds the given item.
        /// </summary>
        /// <param name="item"></param>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            this.Add(item.Key, item.Value);
        }

        /// <summary>
        /// Clears the entire dictionary.
        /// </summary>
        public void Clear()
        {
            _dictionary = new List<IDictionary<TKey, TValue>>();
            _dictionary.Add(new Dictionary<TKey, TValue>(_MAX_DIC_SIZE));
        }

        /// <summary>
        /// Returns true if the given item is contained in this dictionairy.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            for (int idx = 0; idx < _dictionary.Count; idx++)
            {
                if (_dictionary[idx].Contains(item))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Copies the content of an array to this dictionary.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            for (int idx = arrayIndex; idx < array.Length; idx++)
            {
                this.Add(array[idx]);
            }
        }

        /// <summary>
        /// Returns the total element count in this dictionary.
        /// </summary>
        public int Count
        {
            get
            {
                int total = 0;
                for (int idx = 0; idx < _dictionary.Count; idx++)
                {
                    total = total + _dictionary[idx].Count;
                }
                return total;
            }
        }

        /// <summary>
        /// Returns the count of the internal dictionaries.
        /// </summary>
        public int CountDictionaries
        {
            get
            {
                return _dictionary.Count;
            }
        }

        /// <summary>
        /// Returns false.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes the given item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return this.Remove(item.Key);
        }

        /// <summary>
        /// Enumerates all key-value pairs.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Enumerates all key-value pairs.
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}