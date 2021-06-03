using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

namespace ProjNet.CoordinateSystems.Projections
{
    /// <summary>
    /// 
    /// </summary>
    public class LambertAzimuthalEqualAreaProjection : MapProjection
    {
        /// <summary>
        /// An enumeration of modes
        /// </summary>
        private enum Mode
        {
            /// <summary>
            /// North pole
            /// </summary> 
            N_POLE,
            /// <summary>
            /// South pole
            /// </summary>
            S_POLE,
            /// <summary>
            /// Equitorial
            /// </summary>
            EQUIT,
            /// <summary>
            /// Oblique
            /// </summary>
            OBLIQ
        }

        /// <summary>
        /// A function to perform the actual transformation
        /// </summary>
        /// <param name="o1">The horizontal ordinate</param>
        /// <param name="o2">The vertical ordinate</param>
        delegate void Transformer(ref double o1, ref double o2);

        /// <summary>
        /// The delegate to perform forward transformation
        /// </summary>
        private readonly Transformer _radiansToMeters;
        /// <summary>
        /// The delegate to perform reverse transformation
        /// </summary>
        private readonly Transformer _metersToRadians;

        private readonly Mode _mode;
        private readonly double _qp;
        private readonly double _one_es;
        private readonly double[] _apa;
        //private readonly double _mmf;
        private readonly double _dd;
        private readonly double _sinb1;
        private readonly double _cosb1;
        private readonly double _rq;
        private readonly double _xmf;
        private readonly double _ymf;

        private readonly double _reciprocSemiMajorTimesScaleFactor;

        /// <summary>
        /// Creates an instance of this class
        /// </summary>
        /// <param name="parameters">An enumeration of Projection parameters</param>
        public LambertAzimuthalEqualAreaProjection(IEnumerable<ProjectionParameter> parameters) : this(parameters, null)
        {
        }

        /// <summary>
        /// Creates an instance of this class
        /// </summary>
        /// <param name="parameters">An enumeration of Projection parameters</param>
        /// <param name="inverse">The inverse projection</param>
        public LambertAzimuthalEqualAreaProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
            : base(parameters, inverse)
        {
            Name = "Lambert_Azimuthal_Equal_Area";

            double phi0 = lat_origin;

            double t = Math.Abs(phi0);
            if (t > HALF_PI + EPS10)
                throw new ArgumentException(nameof(parameters));

            if (Math.Abs(t - HALF_PI) < EPS10) {
                _mode = phi0 < 0.0 ? Mode.S_POLE : Mode.N_POLE;
            }
            else if (Math.Abs(t) < EPS10) {
                _mode = Mode.EQUIT; }
            else {
                _mode = Mode.OBLIQ;
            }

            if (_es != 0d)
            {
                _one_es = 1.0 - _es;
                _qp = qsfn(1, _e, _one_es);
                //_mmf = 0.5 / (1.0 - _es);
                _apa = authset(_es);
                if (_apa == null)
                    throw new ArgumentException(nameof(parameters));

                switch (_mode)
                {
                    case Mode.N_POLE:
                    case Mode.S_POLE:
                        _dd = 1.0;
                        break;
                    case Mode.EQUIT:
                        _dd = 1.0 / (_rq = Math.Sqrt(0.5 * _qp));
                        _xmf = 1.0;
                        _ymf = 0.5 * _qp;
                        break;
                    case Mode.OBLIQ:
                        _rq = Math.Sqrt(0.5 * _qp);
                        double sinphi = Math.Sin(phi0);
                        _sinb1 = qsfn(sinphi, _e, _one_es) / _qp;
                        _cosb1 = Math.Sqrt(1.0 - _sinb1 * _sinb1);
                        _dd = Math.Cos(phi0) / (Math.Sqrt(1.0 - _es * sinphi * sinphi) * _rq * _cosb1);
                        _ymf = (_xmf = _rq) / _dd;
                        _xmf *= _dd;
                        break;
                }
                _radiansToMeters = EllipsoidalRadiansToMeters;
                _metersToRadians = EllipsoidalMetersToRadians;
            }
            else
            {
                if (_mode == Mode.OBLIQ)
                {
                    _sinb1 = Math.Sin(phi0);
                    _cosb1 = Math.Cos(phi0);
                }

                _radiansToMeters = SphericalRadiansToMeters;
                _metersToRadians = SphericalMetersToRadians;
            }

            _reciprocSemiMajorTimesScaleFactor = 1d / (scale_factor * _semiMajor);
        }


        /// <summary>
        /// Creates the inverse transform of this object.
        /// </summary>
        /// <remarks>This method may fail if the transform is not one to one. However, all cartographic projections should succeed.</remarks>
        /// <returns></returns>
        public override MathTransform Inverse()
        {
            if (_inverse == null)
                _inverse = new LambertAzimuthalEqualAreaProjection(_Parameters.ToProjectionParameter(), this);
            return _inverse;
        }

        #region forward

        /// <summary>
        /// Method to convert a point (lon, lat) in radians to (x, y) in meters
        /// </summary>
        /// <param name="lon">The longitude of the point in radians when entering, its x-ordinate in meters after exit.</param>
        /// <param name="lat">The latitude of the point in radians when entering, its y-ordinate in meters after exit.</param>
        protected override void RadiansToMeters(ref double lon, ref double lat)
        {
            _radiansToMeters(ref lon, ref lat);
            lon *= scale_factor * _semiMajor;
            lat *= scale_factor * _semiMajor;
        }

        private void EllipsoidalRadiansToMeters(ref double lon, ref double lat)
        {
            double sinb = 0.0, cosb = 0.0, b = 0.0;

            double lam = adjust_lon(lon-central_meridian);
            double phi = lat;

            double coslam = Math.Cos(lam);
            double sinlam = Math.Sin(lam);
            double sinphi = Math.Sin(phi);
            double q = qsfn(sinphi, _e, _one_es);

            if (_mode == Mode.OBLIQ || _mode == Mode.EQUIT)
            {
                sinb = q / _qp;
                cosb = Math.Sqrt(1.0 - sinb * sinb);
            }

            switch (_mode)
            {
                case Mode.OBLIQ:
                    b = 1.0 + _sinb1 * sinb + _cosb1 * cosb * coslam;
                    break;
                case Mode.EQUIT:
                    b = 1.0 + cosb * coslam;
                    break;
                case Mode.N_POLE:
                    b = HALF_PI + phi;
                    q = _qp - q;
                    break;
                case Mode.S_POLE:
                    b = phi - HALF_PI;
                    q = _qp + q;
                    break;
            }

            double x = HUGE_VAL;
            double y = HUGE_VAL;
            if (Math.Abs(b) < EPS10)
            {
                //proj_errno_set(P, PJD_ERR_TOLERANCE_CONDITION);
                return;
            }

            switch (_mode)
            {
                case Mode.OBLIQ:
                    b = Math.Sqrt(2.0 / b);
                    y = _ymf * b * (_cosb1 * sinb - _sinb1 * cosb * coslam);
                    goto eqcon;
                case Mode.EQUIT:
                    b = Math.Sqrt(2.0 / (1.0 + cosb * coslam));
                    y = b * sinb * _ymf;
                    eqcon:
                    x = _xmf * b * cosb * sinlam;
                    break;
                case Mode.N_POLE:
                case Mode.S_POLE:
                    if (q >= 1e-15)
                    {
                        b = Math.Sqrt(q);
                        x = b * sinlam;
                        y = coslam * (_mode == Mode.S_POLE ? b : -b);
                    }
                    else
                        x = y = 0.0;
                    break;
            }

            lon = x;
            lat = y;

        }

        private void SphericalRadiansToMeters(ref double lon, ref double lat)
        {

            double lam = adjust_lon(lon - central_meridian);
            double phi = lat;

            double sinphi = Math.Sin(phi);
            double cosphi = Math.Cos(phi);
            double coslam = Math.Sin(lam);

            double x = HUGE_VAL;
            double y = HUGE_VAL;

            switch (_mode)
            {
                case Mode.EQUIT:
                    y = 1.0 + cosphi * coslam;
                    goto oblcon;
                case Mode.OBLIQ:
                    y = 1.0 + _sinb1 * sinphi + _cosb1 * cosphi * coslam;
                    oblcon:
                    if (y <= EPS10)
                    {
                        //proj_errno_set(P, PJD_ERR_TOLERANCE_CONDITION);
                        return;
                    }
                    y = Math.Sqrt(2.0 / y);
                    x = y * cosphi * Math.Sin(lam);
                    y *= _mode == Mode.EQUIT ? sinphi :
                        _cosb1 * sinphi - _sinb1 * cosphi * coslam;
                    break;
                case Mode.N_POLE:
                    coslam = -coslam;
                    goto continue_S_POLE;
                /*-fallthrough*/
                case Mode.S_POLE:
                    continue_S_POLE:
                    if (Math.Abs(phi + lat_origin) < EPS10)
                    {
                        //proj_errno_set(P, PJD_ERR_TOLERANCE_CONDITION);
                        return;
                    }
                    y = FORT_PI - phi * 0.5;
                    y = 2.0 * (_mode == Mode.S_POLE ? Math.Cos(y) : Math.Sin(y));
                    x = y * Math.Sin(lam);
                    y *= coslam;
                    break;
            }

            lon = x;
            lat = y;
        }
        #endregion

        #region Reverse

        /// <summary>
        /// Method to convert a point from meters to radians
        /// </summary>
        /// <param name="x">The x-ordinate when entering, the longitude value upon exit.</param>
        /// <param name="y">The y-ordinate when entering, the latitude value upon exit.</param>
        protected override void MetersToRadians(ref double x, ref double y)
        {
            x *= _reciprocSemiMajorTimesScaleFactor;
            y *= _reciprocSemiMajorTimesScaleFactor;

            _metersToRadians(ref x, ref y);
        }

        private void EllipsoidalMetersToRadians(ref double x, ref double y)
        {
            double cCe, sCe, q, rho, ab = 0.0;

            switch (_mode)
            {
                case Mode.EQUIT:
                case Mode.OBLIQ:
                    x /= _dd;
                    y *= _dd;
                    rho = hypot(x, y);
                    if (rho < EPS10)
                    {
                        x = central_meridian; // lam
                        y = lat_origin; // phi
                        return;
                    }
                    sCe = 2.0 * Math.Asin(0.5 * rho / _rq);
                    cCe = Math.Cos(sCe);
                    sCe = Math.Sin(sCe);
                    x *= sCe;
                    if (_mode == Mode.OBLIQ)
                    {
                        ab = cCe * _sinb1 + y * sCe * _cosb1 / rho;
                        y = rho * _cosb1 * cCe - y * _sinb1 * sCe;
                    }
                    else
                    {
                        ab = y * sCe / rho;
                        y = rho * cCe;
                    }
                    break;
                case Mode.N_POLE:
                    y = -y;
                    goto continue_S_POLE;
                /*-fallthrough*/
                case Mode.S_POLE:
                    continue_S_POLE:
                    q = (x * x + y * y);
                    if (q == 0.0)
                    {
                        x = central_meridian;          // lam
                        y = lat_origin;   // phi
                        return ;
                    }
                    ab = 1.0 - q / _qp;
                    if (_mode == Mode.S_POLE)
                        ab = -ab;
                    break;
            }

            x = x = adjust_lon(Math.Atan2(x, y) + central_meridian); // lam
            y = authlat(Math.Asin(ab), _apa);                      // phi 
        }

        private void SphericalMetersToRadians(ref double x, ref double y)
        {
            double cosz = 0.0, rh, sinz = 0.0;

            rh = hypot(x, y);
            double phi = rh * .5;
            if (phi > 1.0)
            {
                x = 0; // lam
                y = 0; // phi
                return ;
            }
            phi = 2.0 * Math.Asin(phi);
            if (_mode == Mode.OBLIQ || _mode == Mode.EQUIT)
            {
                sinz = Math.Sin(phi);
                cosz = Math.Cos(phi);
            }
            switch (_mode)
            {
                case Mode.EQUIT:
                    phi = Math.Abs(rh) <= EPS10 ? 0.0 : Math.Asin(y * sinz / rh);
                    x *= sinz;
                    y = cosz * rh;
                    break;
                case Mode.OBLIQ:
                    phi = Math.Abs(rh) <= EPS10 ? lat_origin :
                        Math.Asin(cosz * _sinb1 + y * sinz * _cosb1 / rh);
                    x *= sinz * _cosb1;
                    y = (cosz - Math.Sin(phi) * _sinb1) * rh;
                    break;
                case Mode.N_POLE:
                    y = -y;
                    phi = HALF_PI - phi;
                    break;
                case Mode.S_POLE:
                    phi -= HALF_PI;
                    break;
            }

            double lam = (y == 0.0 && (_mode == Mode.EQUIT || _mode == Mode.OBLIQ)) ?
                0.0 : Math.Atan2(x, y);

            x = adjust_lon(lam + central_meridian);
            y = phi;

        }
        #endregion
    }
}
