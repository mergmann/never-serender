using System;
using System.Text;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using VRageMath;

namespace NeverSerender
{
    public static class Util
    {
        public static string VectorToString(Vector4 vector)
        {
            return $"[{vector.X}, {vector.Y}, {vector.Z}, {vector.W}]";
        }

        public static string MatrixToString(Matrix m)
        {
            var builder = new StringBuilder();
            builder.Append("[\n");

            for (var i = 0; i < 4; i++)
            {
                var row = m.GetRow(i);
                builder.Append("    " + VectorToString(row) + ",\n");
            }

            builder.Append("]");
            return builder.ToString();
        }

        public static string GetActiveBlockModel(MySlimBlock block, out MatrixI orientation)
        {
            var buildLevelRatio = block.BuildLevelRatio;
            orientation = new MatrixI(block.Orientation);

            if (!(buildLevelRatio < 1.0)
                || block.BlockDefinition.BuildProgressModels == null
                || block.BlockDefinition.BuildProgressModels.Length == 0)
                return block.FatBlock == null ? block.BlockDefinition.Model : block.FatBlock.Model.AssetName;

            foreach (var model in block.BlockDefinition.BuildProgressModels)
            {
                if (!(model.BuildRatioUpperBound >= buildLevelRatio)) continue;

                if (model.RandomOrientation)
                    orientation = MyCubeGridDefinitions.AllPossible90rotations[
                        Math.Abs(block.Position.GetHashCode()) %
                        MyCubeGridDefinitions.AllPossible90rotations.Length
                    ];
                return model.File;
            }

            return block.FatBlock == null ? block.BlockDefinition.Model : block.FatBlock.Model.AssetName;
        }
    }
}