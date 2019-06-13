using OpenNos.GameObject.Battle;
using System;

namespace OpenNos.GameObject.Helpers
{
    public class SkillHelper
    {
        public static bool IsNoDamage(short skillVNum)
        {
            switch (skillVNum)
            {
                case 815: // Provoke
                case 817: // Intimidate
                case 847: // Thick Smog
                case 848: // Chain Hook Throw
                case 870: // Heaven Song
                case 916: // Poison Gas Shell
                    return true;
            }

            return false;
        }

        public static bool CalculateNewPosition(MapInstance mapInstance, short x, short y, short cells, ref short mapX, ref short mapY)
        {
            short deltaX = (short)(mapX - x);
            short deltaY = (short)(mapY - y);

            if (cells == 0 || (deltaX == 0 && deltaY == 0))
            {
                return false;
            }

            if (cells > 0)
            {
                double distance = Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));

                while (cells > 0)
                {
                    double scalar = (distance + cells--) / distance;

                    mapX = (short)(x + (deltaX * scalar));
                    mapY = (short)(y + (deltaY * scalar));

                    if (!mapInstance.Map.IsBlockedZone(mapX, mapY))
                    {
                        return true;
                    }
                }
            }
            else
            {
                cells *= -1;

                short velocityX = 0;
                short velocityY = 0;

                if (deltaX != 0)
                {
                    velocityX = deltaX > 0 ? (short)1 : (short)-1;
                }

                if (deltaY != 0)
                {
                    velocityY = deltaY > 0 ? (short)1 : (short)-1;
                }

                velocityX *= cells;
                velocityY *= cells;

                mapX = (short)(x + velocityX);
                mapY = (short)(y + velocityY);

                if (!mapInstance.Map.IsBlockedZone(mapX, mapY))
                {
                    return true;
                }
            }

            return false;
        }
    }
}