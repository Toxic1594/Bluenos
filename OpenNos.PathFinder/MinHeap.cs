/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using System.Collections.Generic;

namespace OpenNos.PathFinder
{
    internal class MinHeap
    {
        #region Members

        private readonly List<Node> _array = new List<Node>();

        #endregion

        #region Properties

        public int Count => _array.Count;

        #endregion

        #region Methods

        public Node Pop()
        {
            Node ret = _array[0];
            _array[0] = _array[_array.Count - 1];
            _array.RemoveAt(_array.Count - 1);

            int len = 0;
            while (len < _array.Count)
            {
                int min = len;
                if ((2 * len) + 1 < _array.Count && _array[(2 * len) + 1].CompareTo(_array[min]) == -1)
                {
                    min = (2 * len) + 1;
                }
                if ((2 * len) + 2 < _array.Count && _array[(2 * len) + 2].CompareTo(_array[min]) == -1)
                {
                    min = (2 * len) + 2;
                }

                if (min == len)
                {
                    break;
                }
                Node tmp = _array[len];
                _array[len] = _array[min];
                _array[min] = tmp;
                len = min;
            }

            return ret;
        }

        public void Push(Node element)
        {
            _array.Add(element);
            int len = _array.Count - 1;
            int parent = (len - 1) >> 1;
            while (len > 0 && _array[len].CompareTo(_array[parent]) < 0)
            {
                Node tmp = _array[len];
                _array[len] = _array[parent];
                _array[parent] = tmp;
                len = parent;
                parent = (len - 1) >> 1;
            }
        }

        #endregion
    }
}