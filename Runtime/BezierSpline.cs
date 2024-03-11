using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FrameworksXD.SplinesXD
{
    public class BezierSpline : MonoBehaviour
    {
        [SerializeField] private Vector3[] points;
        [SerializeField] private BezierControlPointMode[] modes;
        [SerializeField] private bool loop;

        private bool IsCached;
        private Bounds CachedBoundingBox;

        public bool Loop
        {
            get
            {
                return loop;
            }
            set
            {
                loop = value;
                if (value == true)
                {
                    modes[modes.Length - 1] = modes[0];
                    SetControlPoint(0, points[0]);
                }
            }
        }

        public int ControlPointCount
        {
            get
            {
                return points.Length;
            }
        }

        public Vector3 GetControlPoint(int index)
        {
            return points[index];
        }

        public void SetControlPoint(int index, Vector3 point)
        {
            if (index % 3 == 0)
            {
                Vector3 delta = point - points[index];
                if (loop)
                {
                    if (index == 0)
                    {
                        points[1] += delta;
                        points[points.Length - 2] += delta;
                        points[points.Length - 1] = point;
                    }
                    else if (index == points.Length - 1)
                    {
                        points[0] = point;
                        points[1] += delta;
                        points[index - 1] += delta;
                    }
                    else
                    {
                        points[index - 1] += delta;
                        points[index + 1] += delta;
                    }
                }
                else
                {
                    if (index > 0)
                    {
                        points[index - 1] += delta;
                    }
                    if (index + 1 < points.Length)
                    {
                        points[index + 1] += delta;
                    }
                }
            }
            points[index] = point;
            EnforceMode(index);
        }

        public int GetPointsOnSplineCount()
        {
            return modes.Length;
        }

        public Vector3 GetSplinePoint(int index)
        {
            if (index == 0)
                return points[0];
            if (index == GetPointsOnSplineCount() - 1)
                return points[points.Length - 1];

            int pIndex = 3 * index;
            return points[pIndex];
        }

        public void SetSplinePoint(int index, Vector3 point)
        {
            if (index == 0)
            {
                SetControlPoint(0, point);
                return;
            }
            if (index == GetPointsOnSplineCount() - 1)
            {
                SetControlPoint(points.Length - 1, point);
                return;
            }

            int pIndex = 3 * index;
            SetControlPoint(pIndex, point);
        }

        public void Reset()
        {
            points = new Vector3[] {
                new Vector3(1f, 0f, 0f),
                new Vector3(2f, 0f, 0f),
                new Vector3(3f, 0f, 0f),
                new Vector3(4f, 0f, 0f)
            };

            modes = new BezierControlPointMode[] {
            BezierControlPointMode.Free,
            BezierControlPointMode.Free
        };
        }

        public Vector3 GetPoint(float t)
        {
            int i;
            if (t >= 1f)
            {
                t = 1f;
                i = points.Length - 4;
            }
            else
            {
                t = Mathf.Clamp01(t) * CurveCount;
                i = (int)t;
                t -= i;
                i *= 3;
            }
            return transform.TransformPoint(Bezier.GetPoint(
                points[i], points[i + 1], points[i + 2], points[i + 3], t));
        }

        public Vector3 GetVelocity(float t)
        {
            int i;
            if (t >= 1f)
            {
                t = 1f;
                i = points.Length - 4;
            }
            else
            {
                t = Mathf.Clamp01(t) * CurveCount;
                i = (int)t;
                t -= i;
                i *= 3;
            }
            return transform.TransformPoint(Bezier.GetFirstDerivative(
                points[i], points[i + 1], points[i + 2], points[i + 3], t)) - transform.position;
        }

        public Vector3 GetDirection(float t)
        {
            return GetVelocity(t).normalized;
        }

        public void AddCurve()
        {
            Vector3 point = points[points.Length - 1];
            Array.Resize(ref points, points.Length + 3);
            point.x += 1f;
            points[points.Length - 3] = point;
            point.x += 1f;
            points[points.Length - 2] = point;
            point.x += 1f;
            points[points.Length - 1] = point;

            Array.Resize(ref modes, modes.Length + 1);
            modes[modes.Length - 1] = modes[modes.Length - 2]; 
            EnforceMode(points.Length - 4);
            
            if (loop)
            {
                points[points.Length - 1] = points[0];
                modes[modes.Length - 1] = modes[0];
                EnforceMode(0);
            }
        }

        public void RemoveCurve(int selectedPointIndex)
        {
            Vector3[] newPoints = new Vector3[points.Length - 3];
            BezierControlPointMode[] newModes = new BezierControlPointMode[modes.Length - 1];

            if (selectedPointIndex <= 1)
            {
                Array.Copy(points, 3, newPoints, 0, newPoints.Length);
                Array.Copy(modes, 1, newModes, 0, newModes.Length);

            }
            else if (selectedPointIndex >= points.Length - 2)
            {
                Array.Copy(points, 0, newPoints, 0, points.Length - 3);
                Array.Copy(modes, 0, newModes, 0, modes.Length - 1);
            }
            else
            {
                for (int i = 0, j = 0; i < points.Length; i++)
                {
                    if ((i + 1) / 3 == (selectedPointIndex + 1) / 3)
                        continue;
                    newPoints[j++] = points[i];
                }

                int modeIndex = (selectedPointIndex + 1) / 3;
                for (int i = 0, j = 0; i < modes.Length; i++)
                {
                    if (i == modeIndex)
                        continue;
                    newModes[j++] = modes[i];
                }
            }

            points = newPoints;
            modes = newModes;
        }

        public int CurveCount
        {
            get
            {
                return (points.Length - 1) / 3;
            }
        }

        public BezierControlPointMode GetControlPointMode(int index)
        {
            return modes[(index + 1) / 3];
        }

        public void SetControlPointMode(int index, BezierControlPointMode mode)
        {
            int modeIndex = (index + 1) / 3;
            modes[modeIndex] = mode;
            if (loop)
            {
                if (modeIndex == 0)
                {
                    modes[modes.Length - 1] = mode;
                }
                else if (modeIndex == modes.Length - 1)
                {
                    modes[0] = mode;
                }
            }
            EnforceMode(index);
        }

        private void EnforceMode(int index)
        {
            int modeIndex = (index + 1) / 3;
            BezierControlPointMode mode = modes[modeIndex];
            if (mode == BezierControlPointMode.Free || !loop && (modeIndex == 0 || modeIndex == modes.Length - 1))
            {
                return;
            }

            int middleIndex = modeIndex * 3;
            int fixedIndex, enforcedIndex;
            if (index <= middleIndex)
            {
                fixedIndex = middleIndex - 1;
                if (fixedIndex < 0)
                {
                    fixedIndex = points.Length - 2;
                }
                enforcedIndex = middleIndex + 1;
                if (enforcedIndex >= points.Length)
                {
                    enforcedIndex = 1;
                }
            }
            else
            {
                fixedIndex = middleIndex + 1;
                if (fixedIndex >= points.Length)
                {
                    fixedIndex = 1;
                }
                enforcedIndex = middleIndex - 1;
                if (enforcedIndex < 0)
                {
                    enforcedIndex = points.Length - 2;
                }
            }

            Vector3 middle = points[middleIndex];
            Vector3 enforcedTangent = middle - points[fixedIndex];
            if (mode == BezierControlPointMode.Aligned)
            {
                enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, points[enforcedIndex]);
            }
            points[enforcedIndex] = middle + enforcedTangent;
        }

        private void TryCacheRuntimeVariables()
        {
#if UNITY_EDITOR
            if(!Application.isPlaying || !IsCached)
#else
            if (!IsCached)
#endif
            CacheRuntimeVariables();
        }
        private void CacheRuntimeVariables()
        {
            CachedBoundingBox = CalculateBoundingBox();
            IsCached = true;
        }

        private Bounds CalculateBoundingBox()
        {
            var bounds = new Bounds(transform.position + points[0], Vector3.one * 0.1f);
            int steps = 10 * CurveCount;
            for (int i = 1; i <= steps; i++)
            {
                var point = GetPoint(i / (float)steps);
                bounds.Encapsulate(point);
            }
            return bounds;
        }

        public Bounds GetBoundingBox()
        {
            TryCacheRuntimeVariables();
            return CachedBoundingBox;
        }

        public Vector3 GetNearestPointOnSpline(Vector3 point)
        {
            float nearestT = 0;
            float distToNearestT = Vector3.Distance(point, GetPoint(nearestT));
            float minT = 0f;
            float maxT = 1f;
            float newMinT = minT;
            float newMaxT = maxT;
            for (float stepSize = 0.1f; stepSize > 0.001f; stepSize /= 8f) 
            {
                float halfStepSize = stepSize / 2f;
                for (float t = minT; t <= maxT; t += stepSize)
                {
                    var dist = Vector3.Distance(point, GetPoint(t));
                    if (dist < distToNearestT)
                    {
                        distToNearestT = dist;
                        nearestT = t;
                        newMinT = t - halfStepSize;
                        newMaxT = t + halfStepSize;
                    }
                }
                minT = newMinT;
                maxT = newMaxT;
            }
            return GetPoint(nearestT);
        }

        /// <summary>
        /// Only works for convex splines
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool IsPointInsideSpline(Vector3 point)
        {
            var bounds = GetBoundingBox();
            var nearestOnSpline = GetNearestPointOnSpline(point);

            Vector3 boundsCenter = bounds.center;
            return Vector3.Distance(point, boundsCenter) < Vector3.Distance(nearestOnSpline, boundsCenter);
        }
    }
}