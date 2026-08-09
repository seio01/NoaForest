using System;
using System.Collections.Generic;
using UnityEngine;

public enum PurifyRouteSide
{
    Left,
    Right
}

public readonly struct PurifyRoutePoint
{
    public PurifyRoutePoint(Vector3 position, bool shouldToggleFacingOnDeparture)
    {
        Position = position;
        ShouldToggleFacingOnDeparture = shouldToggleFacingOnDeparture;
    }

    public Vector3 Position { get; }
    public bool ShouldToggleFacingOnDeparture { get; }
}

public class PurifyMoteRoute : MonoBehaviour
{
    private readonly struct FacingToggleTransition
    {
        public FacingToggleTransition(int startPointId, int endPointId)
        {
            StartPointId = startPointId;
            EndPointId = endPointId;
        }

        public int StartPointId { get; }
        public int EndPointId { get; }
    }

    private static readonly int[] LEFT_ROUTE_POINT_IDS =
    {
        0, 2, 1, 5, 4, 8, 9, 12, 13, 15
    };

    private static readonly int[] RIGHT_ROUTE_POINT_IDS =
    {
        0, 2, 3, 6, 7, 11, 10, 14, 13, 15
    };

    private static readonly FacingToggleTransition[] LEFT_FACING_TOGGLE_TRANSITIONS =
    {
        new(2, 1),
        new(8, 9)
    };

    private static readonly FacingToggleTransition[] RIGHT_FACING_TOGGLE_TRANSITIONS =
    {
        new(2, 3),
        new(11, 10)
    };

    private PurifyRoutePoint[] _leftRoute = Array.Empty<PurifyRoutePoint>();
    private PurifyRoutePoint[] _rightRoute = Array.Empty<PurifyRoutePoint>();
    private bool _isInitialized;

    public bool Initialize()
    {
        Dictionary<int, Transform> routePoints = CollectRoutePoints();
        _leftRoute = BuildRoute(
            routePoints,
            LEFT_ROUTE_POINT_IDS,
            LEFT_FACING_TOGGLE_TRANSITIONS);
        _rightRoute = BuildRoute(
            routePoints,
            RIGHT_ROUTE_POINT_IDS,
            RIGHT_FACING_TOGGLE_TRANSITIONS);

        if (_leftRoute == null || _rightRoute == null)
        {
            _leftRoute = Array.Empty<PurifyRoutePoint>();
            _rightRoute = Array.Empty<PurifyRoutePoint>();
            _isInitialized = false;
            return false;
        }

        _isInitialized = true;
        return true;
    }

    public PurifyRoutePoint[] GetRoute(PurifyRouteSide routeSide)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[PurifyMoteRoute] Route is not initialized.");
            return Array.Empty<PurifyRoutePoint>();
        }

        return routeSide == PurifyRouteSide.Left ? _leftRoute : _rightRoute;
    }

    private Dictionary<int, Transform> CollectRoutePoints()
    {
        Dictionary<int, Transform> routePoints = new();

        for (int index = 0; index < transform.childCount; index++)
        {
            Transform routePoint = transform.GetChild(index);
            if (!int.TryParse(routePoint.name, out int pointId))
            {
                Debug.Log($"[PurifyMoteRoute] Route point name must be numeric: {routePoint.name}");
                continue;
            }

            if (!routePoints.TryAdd(pointId, routePoint))
            {
                Debug.Log($"[PurifyMoteRoute] Duplicate route point: {pointId}");
            }
        }

        return routePoints;
    }

    private PurifyRoutePoint[] BuildRoute(
        Dictionary<int, Transform> routePoints,
        int[] pointIds,
        FacingToggleTransition[] facingToggleTransitions)
    {
        PurifyRoutePoint[] route = new PurifyRoutePoint[pointIds.Length];

        for (int index = 0; index < pointIds.Length; index++)
        {
            int pointId = pointIds[index];
            if (!routePoints.TryGetValue(pointId, out Transform routePoint))
            {
                Debug.LogError($"[PurifyMoteRoute] Route point is missing: {pointId}");
                return null;
            }

            bool shouldToggleFacing = index < pointIds.Length - 1 &&
                ContainsTransition(
                    facingToggleTransitions,
                    pointId,
                    pointIds[index + 1]);
            route[index] = new PurifyRoutePoint(
                routePoint.position,
                shouldToggleFacing);
        }

        return route;
    }

    private bool ContainsTransition(
        FacingToggleTransition[] transitions,
        int startPointId,
        int endPointId)
    {
        for (int index = 0; index < transitions.Length; index++)
        {
            FacingToggleTransition transition = transitions[index];
            if (transition.StartPointId == startPointId &&
                transition.EndPointId == endPointId)
            {
                return true;
            }
        }

        return false;
    }
}
