/*
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.Utils;

namespace ProjectEntities
{
    //------\\
    // Type \\
    //------\\
    public class CollidableType : RoVEntityType
    {
        public class ConvexShape
        {
            [FieldSerialize]
            List<Vec2> shapePoints = new List<Vec2>();
            public List<Vec2> ShapePoints { get { return shapePoints; } }
        }

        [FieldSerialize]
        List<ConvexShape> convexShapes = new List<ConvexShape>();
        public List<ConvexShape> ConvexShapes { get { return convexShapes; } }

        [FieldSerialize]
        List<Vec2> cornerPoints = new List<Vec2>();
        public List<Vec2> CornerPoints { get { return cornerPoints; } }

        public class CollisionVariation
        {
            public class ConvexShape
            {
                [FieldSerialize]
                List<Vec2> shapePoints = new List<Vec2>();
                public List<Vec2> ShapePoints { get { return shapePoints; } }
            }

            [FieldSerialize]
            List<ConvexShape> convexShapes = new List<ConvexShape>();
            public List<ConvexShape> ConvexShapes { get { return convexShapes; } }

            [FieldSerialize]
            List<Vec2> cornerPoints = new List<Vec2>();
            public List<Vec2> CornerPoints { get { return cornerPoints; } }
        }

        [FieldSerialize]
        private List<CollisionVariation> collisionVariations = new List<CollisionVariation>();
        public List<CollisionVariation> CollisionVariations { get { return collisionVariations; } }
    }
    //----\\


    //----------\\
    // Instance \\
    //----------\\
    public class Collidable : RoVEntity
    {
        public class Simplex
        {
            public struct Edge
            {
                public Line2 EdgeLine;
                public Vec2 Normal;
                public Vec2 Center;
            }

            public Vec2 ShapeCenter;
            public List<Edge> Edges = new List<Edge>();

            public bool IsPointInShape(Vec2 point)
            {
                foreach (Edge edge in Edges)
                    if (Vec2.Dot((point - edge.Center).GetNormalize(), edge.Normal) > 0)
                        return false;

                return true;
            }
        }

        public List<Vec2> CornerPoints()
        {
            List<Vec2> points = new List<Vec2>();
            foreach (Corner corner in corners) points.Add(corner.Position);
            return points;
        }

        private class Corner
        {
            public Vec2 Position;
            public Vec2 RightPosition;
            public Vec2 LeftPosition;
            public Vec2 RightNormal;
            public Vec2 LeftNormal;
            public float ATan;

            public bool CanSeePoint(Vec2 point)
            {
                Vec2 pointDirection = (point - Position).GetNormalize();
                if (Vec2.Dot(RightNormal, pointDirection) > 0 || Vec2.Dot(LeftNormal, pointDirection) > 0) return true;
                else return false;
            }
        }

        private float collisionNearDistance = 0;
        public float CollisionNearDistance { get { return collisionNearDistance; } }

        private List<Corner> corners = new List<Corner>();

        private List<Simplex> shapes = new List<Simplex>();
        public List<Simplex> Shapes { get { return shapes; } }
        //----\\


        //----\\
        CollidableType _type = null; public new CollidableType Type { get { return _type; } }
        //----\\


        //-----------\\
        // Overrides \\
        //-----------\\
        protected override void UpdateVisualObject()
        {
            base.UpdateVisualObject();

            if (IsRendered)
                InitializeShapes();
        }

        //gets the averaged center of shapes
        public override Vec2 GetCenter
        {
            get
            {
                if (shapes.Count == 0)
                    return base.GetCenter;
                else if (shapes.Count == 1)
                    return shapes[0].ShapeCenter;
                else
                {
                    Vec2 center = new Vec2();
                    foreach (Simplex shape in shapes) center += shape.ShapeCenter;
                    center /= shapes.Count;
                    return center;
                }
            }
        }
        //----\\


        //---------\\
        // Methods \\
        //---------\\
        private void InitializeShapes()
        {
            //clear previous
            shapes.Clear();
            corners.Clear();

            foreach (CollidableType.ConvexShape shape in Type.ConvexShapes)
            {
                //can have a shape with only two points, this will result into two edges: the front and the back
                if (shape.ShapePoints.Count < 2) continue;

                //initialize shape
                Simplex simplex = new Simplex();
                Vec2 center = new Vec2();
                Vec2 previousPoint = Pos2D + shape.ShapePoints[shape.ShapePoints.Count - 1] * Scale.Z;

                foreach (Vec2 shapePoint in shape.ShapePoints)
                {
                    //calculate main properties
                    Simplex.Edge edge = new Simplex.Edge();
                    edge.EdgeLine = new Line2(previousPoint, Pos2D + shapePoint * Scale.Z);
                    edge.Center = new Vec2((edge.EdgeLine.Start + edge.EdgeLine.End) / 2);

                    //rotate edge 90 degrees to get normal
                    Vec2 edgeAsNormal = (edge.EdgeLine.End - edge.EdgeLine.Start).GetNormalize();
                    edge.Normal = new Vec2(-edgeAsNormal.Y, edgeAsNormal.X);

                    //add to center collection
                    center += shapePoint * Scale.Z;

                    //next edge
                    previousPoint = edge.EdgeLine.End;
                    simplex.Edges.Add(edge);
                }

                //average center
                simplex.ShapeCenter = Pos2D + (center / shape.ShapePoints.Count);
                shapes.Add(simplex);
            }

            //create corners
            if (Type.CornerPoints.Count > 1)
            {
                List<Corner> tempCorners = new List<Corner>();

                Vec2 previousCorner = Pos2D + Type.CornerPoints[Type.CornerPoints.Count - 1] * Scale.Z;
                for (int i = 0; i < Type.CornerPoints.Count; i++)
                {
                    //form two walls
                    Corner corner = new Corner();
                    corner.LeftPosition = previousCorner;
                    corner.Position = Pos2D + Type.CornerPoints[i] * Scale.Z;
                    corner.RightPosition = Pos2D + Type.CornerPoints[i == Type.CornerPoints.Count - 1 ? 0 : i + 1] * Scale.Z;

                    //rotate edges 90 degrees counter-clockwise to get normals
                    Vec2 leftAsNormal = (corner.Position - corner.LeftPosition).GetNormalize();
                    Vec2 rightAsNormal = (corner.RightPosition - corner.Position).GetNormalize();
                    corner.LeftNormal = new Vec2(-leftAsNormal.Y, leftAsNormal.X);
                    corner.RightNormal = new Vec2(-rightAsNormal.Y, rightAsNormal.X);

                    //calculate ATan
                    Vec2 positionAsNormal = (corner.Position - GetCenter).GetNormalize();
                    corner.ATan = MathFunctions.ATan(positionAsNormal.X, positionAsNormal.Y);

                    //check if bigger than current bound
                    float range = (corner.Position - GetCenter).LengthSqr() + DataDriver.boundsOffset;
                    if (range > collisionNearDistance)
                        collisionNearDistance = range;

                    //add to collection
                    previousCorner = corner.Position;
                    tempCorners.Add(corner);
                }

                //reorder corners to start from lowest ATan value
                float lowestATan = (float)Math.PI;
                int startPos = 0;
                for (int i = 0; i < tempCorners.Count; i++)
                    if (tempCorners[i].ATan < lowestATan)
                    {
                        lowestATan = tempCorners[i].ATan;
                        startPos = i;
                    }
                while (tempCorners.Count > 0)
                {
                    corners.Add(tempCorners[startPos]);
                    tempCorners.RemoveAt(startPos);
                    if (startPos >= tempCorners.Count)
                        startPos = 0;
                }
            }
        }

        //used to prevent objects overlapping
        public bool IsPointInShapes(Vec2 point)
        {
            foreach (Simplex shape in Shapes)
                if (shape.IsPointInShape(point))
                    return true;

            return false;
        }

        //main method for workers
        public bool IsPointInsideCorners(Vec2 point)
        {
            foreach (Corner corner in corners)
                if (Vec2.Dot(corner.RightNormal, (point - corner.Position).GetNormalize()) > 0)
                    return false;

            return corners.Count > 0;
        }
        //tests if two collidables have intersecting shapes
        public bool ShapesCollide(Collidable targetEntity)
        {
            foreach (Simplex shape in shapes)
                foreach (Simplex otherShape in targetEntity.Shapes)
                {
                    //fast early exit, also needed to check if either shape is fully inside the other
                    if (shape.IsPointInShape(otherShape.ShapeCenter)) return true;
                    if (otherShape.IsPointInShape(shape.ShapeCenter)) return true;

                    foreach (Simplex.Edge edge in shape.Edges)
                        foreach (Simplex.Edge otherEdge in otherShape.Edges)
                            if (LinesIntercect(edge.EdgeLine, otherEdge.EdgeLine))
                                return true;
                }

            return false;
        }
        //----\\

        //----------------\\
        // Corner Walking \\
        //----------------\\
        public bool GetATanCorners(Vec2 point, out Vec2 leftCorner, out Vec2 rightCorner)
        {
            leftCorner = GetCenter;
            rightCorner = GetCenter;

            //avoid NaN and out of array
            if (point == GetCenter) return false;
            if (corners.Count < 2) return false;

            Vec2 pointDirection = (point - GetCenter);
            float pointATan = MathFunctions.ATan(pointDirection.X, pointDirection.Y);
            foreach (Corner corner in corners)
                if (pointATan < corner.ATan)
                {
                    rightCorner = corner.Position;
                    leftCorner = corner.LeftPosition;
                    return true;
                }

            //loop over
            leftCorner = corners[0].LeftPosition;
            rightCorner = corners[0].Position;
            return true;
        }

        public Vec2 GetBestNextCorner(Vec2 from, Vec2 target)
        {
            //non-collidable, keep moving
            if (corners.Count == 0) return target;

            //find closest corner
            Corner closestCorner = corners[0];
            float bestDistance = 100f;
            foreach (Corner corner in corners)
            {
                Vec2 selfDifference = corner.Position - from;
                float distanceToSelf = (selfDifference).Length();

                if (distanceToSelf < bestDistance)
                {
                    closestCorner = corner;
                    bestDistance = distanceToSelf;
                }
            }

            //can detach directly
            if (closestCorner.CanSeePoint(target))
                return target;

            //are we on same sector
            Vec2 leftCornerTo, rightCornerTo;
            if (GetATanCorners(target, out leftCornerTo, out rightCornerTo))
                if (closestCorner.Position == leftCornerTo || closestCorner.Position == rightCornerTo)
                    return target;

            //evaluate neighbors
            float closeDistance = (target - closestCorner.Position).LengthSqr();
            float leftDistance = (target - closestCorner.LeftPosition).LengthSqr();
            float rightDistance = (target - closestCorner.RightPosition).LengthSqr();

            //check if we stand on best corner
            if (closeDistance <= leftDistance && closeDistance <= rightDistance)
                return target;

            //move to better neighbor
            if (leftDistance < rightDistance)
                return closestCorner.LeftPosition;
            else
                return closestCorner.RightPosition;
        }

        public Vec2 GetBestInitialCorner(Vec2 from, Vec2 target)
        {
            //non-collidable, keep moving
            if (corners.Count == 0) return target;

            //select corners by sector
            Vec2 leftCornerFrom, rightCornerFrom, leftCornerTarget, rightCornerTarget;
            if (GetATanCorners(from, out leftCornerFrom, out rightCornerFrom))
            {
                //get difference to corners
                Vec2 fromLeftToTarget = target - leftCornerFrom;
                Vec2 fromRightToTarget = target - rightCornerFrom;

                if (GetATanCorners(target, out leftCornerTarget, out rightCornerTarget))
                {
                    //test to see if on same sector 
                    if (leftCornerFrom == leftCornerTarget && rightCornerFrom == rightCornerTarget)
                        return target;

                    //to avoid NaN
                    if (leftCornerFrom != from && rightCornerFrom != from && leftCornerTarget != target && rightCornerTarget != target)
                        if (IsPointInsideCorners(from))
                        {
                            //turn to normals and rotate counter-clockwise to get frustum
                            Vec2 leftCorner = (leftCornerFrom - from).GetNormalize();
                            Vec2 leftFrustum = new Vec2(-leftCorner.Y, leftCorner.X);
                            Vec2 targetLeft = fromLeftToTarget.GetNormalize();
                            Vec2 rightCorner = (rightCornerFrom - from).GetNormalize();
                            Vec2 rightFrustum = new Vec2(-rightCorner.Y, rightCorner.X);
                            Vec2 targetRight = fromRightToTarget.GetNormalize();

                            //can we exit through frustum, guaranteed handedness since we are inside shape
                            if (Vec2.Dot(targetLeft, leftFrustum) <= 0 && 0 <= Vec2.Dot(targetRight, rightFrustum))
                                return target;
                        }

                    //select shared corner
                    if (leftCornerFrom == rightCornerTarget) return leftCornerFrom;
                    if (rightCornerFrom == leftCornerTarget) return rightCornerFrom;
                }

                //return better corner
                return (fromLeftToTarget.LengthSqr() < fromRightToTarget.LengthSqr()) ? leftCornerFrom : rightCornerFrom;
            }

            //we are on center or not enough corners
            return target;
        }
        //----\\


    }//class
}//namespace
*/