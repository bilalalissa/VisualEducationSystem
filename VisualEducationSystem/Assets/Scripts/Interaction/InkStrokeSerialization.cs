using System;
using System.Collections.Generic;
using UnityEngine;

namespace VisualEducationSystem.Interaction
{
    public static class InkStrokeSerialization
    {
        [Serializable]
        private sealed class InkDrawingData
        {
            public List<InkStrokeData> strokes = new();
        }

        [Serializable]
        private sealed class InkStrokeData
        {
            public List<InkPointData> points = new();
        }

        [Serializable]
        private sealed class InkPointData
        {
            public float x;
            public float y;
        }

        public static List<List<Vector2>> Deserialize(string serialized)
        {
            if (string.IsNullOrWhiteSpace(serialized))
            {
                return new List<List<Vector2>>();
            }

            var data = JsonUtility.FromJson<InkDrawingData>(serialized);
            if (data == null || data.strokes == null)
            {
                return new List<List<Vector2>>();
            }

            var strokes = new List<List<Vector2>>(data.strokes.Count);
            foreach (var stroke in data.strokes)
            {
                var points = new List<Vector2>(stroke.points.Count);
                foreach (var point in stroke.points)
                {
                    points.Add(new Vector2(point.x, point.y));
                }

                if (points.Count > 0)
                {
                    strokes.Add(points);
                }
            }

            return strokes;
        }

        public static string Serialize(List<List<Vector2>> strokes)
        {
            var data = new InkDrawingData();
            foreach (var stroke in strokes)
            {
                if (stroke == null || stroke.Count == 0)
                {
                    continue;
                }

                var strokeData = new InkStrokeData();
                foreach (var point in stroke)
                {
                    strokeData.points.Add(new InkPointData
                    {
                        x = point.x,
                        y = point.y
                    });
                }

                data.strokes.Add(strokeData);
            }

            return JsonUtility.ToJson(data);
        }
    }
}
