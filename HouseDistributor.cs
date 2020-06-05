using System;
using System.Collections.Generic;
using DeluMc.Utils;
using DeluMc.MCEdit;
using DeluMc.Masks;

namespace DeluMc
{
    public static class HouseDistributor
    {
        public static void Test(in float[][] deltaMap)
        {
            DeltaMap.DeltaPair[] sortedDelta = DeltaMap.SortDeltaMap(deltaMap);
        }
    }
}
