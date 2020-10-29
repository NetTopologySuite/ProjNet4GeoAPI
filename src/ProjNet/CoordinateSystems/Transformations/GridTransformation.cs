
namespace ProjNet.CoordinateSystems.Transformations
{
    using ProjNet.NTv2;
    using System;

    class GridTransformation : MathTransform
    {
        private bool inverse;

        private GridFile grid;

        public GridTransformation(GridFile grid, bool inverse)
        {
            this.inverse = inverse;
            this.grid = grid;
        }

        public override int DimSource => 2;

        public override int DimTarget => 2;

        public override string WKT => "";

        public override string XML => "";

        public override MathTransform Inverse()
        {
            return new GridTransformation(grid, !inverse);
        }

        public override void Invert()
        {
            inverse = !inverse;
        }

        public override void Transform(ref double x, ref double y, ref double z)
        {
            if (!grid.Transform(ref x, ref y, inverse))
            {
                throw new Exception("Grid transfomation failed: given coordinate outside of grid bounds.");
            }
        }
    }
}
