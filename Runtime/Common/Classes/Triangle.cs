using System;
using UnityEngine;

namespace Metamatic
{
    public enum TrianglePivotAnchor {
        Centroid,
        X,
        Y,
        Z
    }
    public class TrianglePivot {
        public TrianglePivotAnchor anchor = TrianglePivotAnchor.Centroid;
        public Vector3 offset = Vector3.zero;
        public Quaternion rotation = Quaternion.Euler(0f, 0f, 0f);
    }
    public class Triangle {
        public const double Radian90 = 90 * Mathf.Deg2Rad;
        public const double Radian180 = 180 * Mathf.Deg2Rad;
        public double X;
        public double Y;
        public double Z;
        public double x;
        public double y;
        public double z;
        public TrianglePivot pivot = new TrianglePivot();
        public Vector3 edges;
        public Triplet<Vector3> vertexCoords;
        /// <summary>
        /// Angle X in degree
        /// </summary>
        /// <value></value>
        public float degX {
            get {
                return (float)this.X * Mathf.Rad2Deg;
            }
        }
        /// <summary>
        /// Angle Y in degree
        /// </summary>
        /// <value></value>
        public float degY {
            get {
                return (float)this.Y * Mathf.Rad2Deg;
            }
        }
        /// <summary>
        /// Angle Z in degree
        /// </summary>
        /// <value></value>
        public float degZ {
            get {
                return (float)this.Z * Mathf.Rad2Deg;
            }
        }

        public Triplet<Vector3> GetVertexCoords() {
            Triplet<Vector3> coords = default;
            switch (this.pivot.anchor) {
                case TrianglePivotAnchor.Centroid:
                    coords = GetVertexCoordsRelativeCentroid();
                break;
                case TrianglePivotAnchor.X:
                    coords = GetVertexCoordsRelativeX();
                break;
                case TrianglePivotAnchor.Y:
                    coords = GetVertexCoordsRelativeY();
                break;
                case TrianglePivotAnchor.Z:
                    coords = GetVertexCoordsRelativeZ();
                break;
            }

            return coords.Map(ApplyPivotToCoord);
        }

        private Vector3 ApplyPivotToCoord(Vector3 coord) {
            return Trigf.RotatePointAroundPivot(coord + this.pivot.offset, Vector3.zero, this.pivot.rotation.eulerAngles);
        }

        public Triplet<Vector3> GetVertexCoordsRelativeX() {
            /* Get angles of the center of mass */
            if (this.x == default(double) || this.y == default(double) || this.Z == default(double)) {
                if (!this.TryComplete()) {
                    throw new Exception("Not enough triangle information to calculate coords. You should specify longtitude of all edges and angle Z");
                }
            }

            double Xx = 0;
            double Xy = 0;

            double Zx = Xx - Trigf.SolveTriangleEdgeBySinusTheorem(this.y, Radian90, Radian90 - this.Z);
            double Zy = Xy - Trigf.SolveTriangleEdgeBySinusTheorem(this.y, Radian90, this.Z);

            double Yx = Zx + this.x;
            double Yy = Zy;

            return new Triplet<Vector3>(
                new Vector3((float)Xx, (float)Xy, 0f),
                new Vector3((float)Yx, (float)Yy, 0f),
                new Vector3((float)Zx, (float)Zy, 0f)
            );
        }

        public Triplet<Vector3> GetVertexCoordsRelativeY() {
            /* Get angles of the center of mass */
            if (this.x == default(double) || this.y == default(double) || this.Z == default(double)) {
                if (!this.TryComplete()) {
                    throw new Exception("Not enough triangle information to calculate coords. You should specify longtitude of all edges and angle Z");
                }
            }

            double Yx = 0;
            double Yy = 0;

            double Zx = Yx - this.x;
            double Zy = Yy;

            double Xx = Trigf.SolveTriangleEdgeBySinusTheorem(this.z, Radian90, Radian90 - (Radian180 - this.Y));
            double Xy = Trigf.SolveTriangleEdgeBySinusTheorem(this.z, Radian90, Radian180 - this.Y);

            return new Triplet<Vector3>(
                new Vector3((float)Xx, (float)Xy, 0f),
                new Vector3((float)Yx, (float)Yy, 0f),
                new Vector3((float)Zx, (float)Zy, 0f)
            );
        }

        public Triplet<Vector3> GetVertexCoordsRelativeZ() {
            /* Get angles of the center of mass */
            if (this.x == default(double) || this.y == default(double) || this.Z == default(double)) {
                if (!this.TryComplete()) {
                    throw new Exception("Not enough triangle information to calculate coords. You should specify longtitude of all edges and angle Z");
                }
            }

            double Zx = 0;
            double Zy = 0;

            double Yx = Zx + this.x;
            double Yy = Zy;

            double Xx = Trigf.SolveTriangleEdgeBySinusTheorem(this.y, Radian90, Radian90 - this.Z);
            double Xy = Trigf.SolveTriangleEdgeBySinusTheorem(this.y, Radian90, this.Z);

            return new Triplet<Vector3>(
                new Vector3((float)Xx, (float)Xy, 0f),
                new Vector3((float)Yx, (float)Yy, 0f),
                new Vector3((float)Zx, (float)Zy, 0f)
            );
        }

        public Triplet<Vector3> GetVertexCoordsRelativeCentroid() {
            /* Get angles of the center of mass */
            if (this.x == default(double) || this.y == default(double) || this.z == default(double) || this.Z == default(double)) {
                if (!this.TryComplete()) {
                    throw new Exception("Not enough triangle information to calculate coords. You should specify longtitude of all edges and angle Z");
                }
            }
            
            double xMeridianLongtitude = GetXMedianLongtitude();
            /* Split triganle to the two triagnles by its x median, so we will get left triangle with verticles Z, Xym, Yxm
            Where:
            Z - original angle Z
            Xym - angle X splitted by x-median of triangle, formed from splitted main triangle with x-median, that contains y edge
            Yxm - angle of splitted main triangle with x-median placed at x-median base
            */
            double Yxm = Trigf.SolveTriangleAngleByEdges(this.y, this.x / 2, GetXMedianLongtitude());
            /* Split triangle to thw two by its z-median. Form two new triangles: Zxm, Xxm
            Where:
            Zxm - new variant of angle Z of triangle formed by z-median, that contains edge x
            Xxm - triganle of z-median base closed to x edge
            */
            double Zxm = Trigf.SolveTriangleAngleByEdges(this.z / 2, this.x, GetZMedianLongtitude());

            /* Search for longtitude of Z-centroid edge */
            double z_centroid = Trigf.SolveTriangleEdgeBySinusTheorem(this.x / 2, Radian180 - Yxm - Zxm, Yxm);

            /* Search for projection of centroid to x-edge */
            double Zy = Trigf.SolveTriangleEdgeBySinusTheorem(z_centroid, Radian90, Zxm) * -1;

            /* Search for second cathet of centroid projection to x-edge */
            double Zx = Trigf.SolveTriangleEdgeBySinusTheorem(z_centroid, Radian90, Radian90 - Zxm) * -1;

            /* Search for projection longtitude of angle X to ray x */
            double Xy = Zy + Trigf.SolveTriangleEdgeBySinusTheorem(this.y, Radian90, this.Z);

            /* Search for second cathet of angle X projection of x */
            double xProjectionXCathet = Trigf.SolveTriangleEdgeBySinusTheorem(xMeridianLongtitude, Radian90, Radian90 - Yxm);
            double Xx = Zx + (this.x / 2) + (Yxm > 90 ? xProjectionXCathet : xProjectionXCathet * -1);

            return new Triplet<Vector3>(
                new Vector3((float)Xx, (float)Xy, 0),
                new Vector3((float)Zx + (float)this.x, (float)Zy, 0),
                new Vector3((float)Zx, (float)Zy, 0)
            );
        }

        /// <summary>
        /// Explode triangle into two trangles by X median
        /// </summary>
        /// <returns></returns>
        public Triangle[] ExplodeByXMedian() {
            double xMedian = GetXMedianLongtitude();
            double leftZ = Trigf.SolveTriangleAngleByEdges(this.z, this.x / 2, xMedian);
            Triangle left = new Triangle {
                Y = this.Y,
                Z = leftZ,
                z = this.z,
                x = this.x / 2
            };
            left.TryComplete();

            Triangle right = new Triangle {
                Z = this.Z,
                Y = Radian180 - leftZ,
                y = this.Y,
                x = this.x / 2
            };
            right.TryComplete();

            return new Triangle[] { left, right };
        }

        /// <summary>
        /// Try to complete triangle by known edges and angles.true Return true on success.
        /// </summary>
        /// <returns></returns>
        public bool TryComplete() {
            /* Complete angle X */
            if (this.X == default) {
                if (this.Y != default && this.Z != default) {
                    this.X = (180 * Mathf.Deg2Rad) - this.Y - this.Z;
                }
            }

            /* Complete angle Y */
            if (this.Y == default) {
                if (this.X != default && this.Z != default) {
                    this.Y = (180 * Mathf.Deg2Rad) - this.X - this.Z;
                }
            }

            /* Complete angle Z */
            if (this.Z == default) {
                if (this.X != default && this.Y != default) {
                    this.Z = (180 * Mathf.Deg2Rad) - this.Y - this.X;
                }
            }

            /* Complete edge x */
            if (this.x == default) {
                if (!TryCompleteEdge(this.y, this.z, this.Y, this.Z, this.X, ref this.x)) {
                    return false;
                }
            }

            /* Complete edge y */
            if (this.y == default) {
                if (!TryCompleteEdge(this.x, this.z, this.X, this.Z, this.Y, ref this.y)) {
                    return false;
                }
            }

            /* Complete edge y */
            if (this.z == default) {
                if (!TryCompleteEdge(this.x, this.y, this.X, this.Y, this.Z, ref this.z)) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Complete edge a by other edges and its angles if it's possible
        /// </summary>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="B">Angle of edge b</param>
        /// <param name="C">Angle of edge c</param>
        /// <param name="A">Angle of searching edge</param>
        /// <param name="a">Out serached edge</param>
        /// <returns></returns>
        public bool TryCompleteEdge(double b, double c, double B, double C, double A, ref double a) {
            /* Try do this by sine theorem */
            if (A == default) {
                return false;
            }
            if (b != default && B != default) {
                a = Trigf.SolveTriangleEdgeBySinusTheorem(b, B, A);
            } else if (c != default && C != default) {
                a = Trigf.SolveTriangleEdgeBySinusTheorem(c, C, A);
            } else {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get median longtitude of x edge
        /// </summary>
        /// <returns></returns>
        public double GetXMedianLongtitude() {
            if (this.x != default && this.y !=default && this.Z != default) {
                return Trigf.SolveTriangleEdgeByCosineTheorem(this.x / 2, this.y, this.Z);
            }

            throw new Exception("Not enough triangle information to calculate median");
        }

        /// <summary>
        /// Get median longtitude of x edge
        /// </summary>
        /// <returns></returns>
        public double GetYMedianLongtitude() {
            if (this.y != default && this.x !=default && this.Z != default) {
                return Trigf.SolveTriangleEdgeByCosineTheorem(this.y / 2, this.x, this.Z);
            }

            throw new Exception("Not enough triangle information to calculate median");
        }

        /// <summary>
        /// Get median longtitude of x edge
        /// </summary>
        /// <returns></returns>
        public double GetZMedianLongtitude() {
            if (this.z != default && this.x !=default && this.Y != default) {
                return Trigf.SolveTriangleEdgeByCosineTheorem(this.z / 2, this.x, this.Y);
            }

            throw new Exception("Not enough triangle information to calculate median");
        }
    }
}