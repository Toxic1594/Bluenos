﻿namespace OpenNos.PathFinder
{
    public class GridPos
    {
        #region Properties    

        public byte Value { get; set; }

        public short X { get; set; }

        public short Y { get; set; }

        #endregion

        #region Methods
        public GridPos(short x, short y)
        {
            X = x;
            Y = y;
        }
        public GridPos()
        {

        }

        public bool IsWalkable()
        {
            return Value == 0 || Value == 2 || Value >= 16 && Value <= 19;
        }

        public bool IsArenaStairs()
        {
            return Value > 0;
        }

        #endregion
    }
}