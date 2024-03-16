namespace Celeste64.Mod.Editor;

public class PositionGizmo(PositionGizmo.GetPositionDelegate getPosition, PositionGizmo.SetPositionDelegate setPosition) : Gizmo
{
	public delegate Vec3 GetPositionDelegate();
	public delegate void SetPositionDelegate(Vec3 value);

	private GizmoTarget target;
	private Vec3 beforeDragPosition = Vec3.Zero;

	private const float CubeSize = 0.15f;
	private const float PlaneSize = 0.6f;
	private const float Padding = 0.15f;
	private const float AxisLen = 1.5f;
	private const float AxisRadius = AxisLen / 35.0f;
	private const float ConeLen = AxisLen / 2.5f;
	private const float ConeRadius = ConeLen / 3.0f;

	private const float BoundsPadding = 0.1f;

	// Axis
	private const float AxisBoundsLengthMin = CubeSize + Padding;
	private const float AxisBoundsLengthMax = AxisLen + ConeLen * 0.9f;
	private const float AxisBoundsRadiusMin = -AxisRadius - BoundsPadding;
	private const float AxisBoundsRadiusMax = AxisRadius + BoundsPadding;

	private static readonly BoundingBox XAxisBounds = new(
		new Vec3(AxisBoundsLengthMin, AxisBoundsRadiusMin, AxisBoundsRadiusMin),
		new Vec3(AxisBoundsLengthMax, AxisBoundsRadiusMax, AxisBoundsRadiusMax));

	private static readonly BoundingBox YAxisBounds = new(
		new Vec3(AxisBoundsRadiusMin, AxisBoundsLengthMin, AxisBoundsRadiusMin),
		new Vec3(AxisBoundsRadiusMax, AxisBoundsLengthMax, AxisBoundsRadiusMax));

	private static readonly BoundingBox ZAxisBounds = new(
		new Vec3(AxisBoundsRadiusMin, AxisBoundsRadiusMin, AxisBoundsLengthMin),
		new Vec3(AxisBoundsRadiusMax, AxisBoundsRadiusMax, AxisBoundsLengthMax));

	// Planes
	private const float PlaneBoundsMin = CubeSize + AxisLen / 2.0f - PlaneSize / 2.0f - BoundsPadding;
	private const float PlaneBoundsMax = CubeSize + AxisLen / 2.0f + PlaneSize / 2.0f + BoundsPadding;

	private static readonly BoundingBox XZPlaneBounds = new(
		new Vec3(PlaneBoundsMin, 0.0f, PlaneBoundsMin),
		new Vec3(PlaneBoundsMax, 0.0f, PlaneBoundsMax));

	private static readonly BoundingBox YZPlaneBounds = new(
		new Vec3(0.0f, PlaneBoundsMin, PlaneBoundsMin),
		new Vec3(0.0f, PlaneBoundsMax, PlaneBoundsMax));

	private static readonly BoundingBox XYPlaneBounds = new(
		new Vec3(PlaneBoundsMin, PlaneBoundsMin, 0.0f),
		new Vec3(PlaneBoundsMax, PlaneBoundsMax, 0.0f));

	// Cube
	private static readonly BoundingBox XYZCubeBounds = new(
		-new Vec3(CubeSize + BoundsPadding),
		 new Vec3(CubeSize + BoundsPadding));

	public override Matrix Transform
	{
		get
		{
			var position = getPosition();

			const float minScale = 10.0f;
			float scale = Math.Max(minScale, Vec3.Distance(EditorWorld.Current.Camera.Position, position) / 20.0f);

			return Matrix.CreateScale(scale) *
				   Matrix.CreateTranslation(position);
		}
	}

	public override void Render(Batcher3D batch3D)
	{
		const byte normalAlpha = 0xff;
		const byte hoverAlpha = 0xff;
		const byte dragAlpha = 0xff;

		var xColorNormal = new Color(0xde1100, normalAlpha);
		var xColorHover = new Color(0xff6450, hoverAlpha);
		var xColorDrag = new Color(0xff9989, dragAlpha);

		var yColorNormal = new Color(0x4aed00, normalAlpha);
		var yColorHover = new Color(0x83ff66, hoverAlpha);
		var yColorDrag = new Color(0xccffbe, dragAlpha);

		var zColorNormal = new Color(0x0d00f3, normalAlpha);
		var zColorHover = new Color(0x3064ff, hoverAlpha);
		var zColorDrag = new Color(0x6693ff, dragAlpha);

		var xyzColorNormal = new Color(0xc7c7c7, normalAlpha);
		var xyzColorHover = new Color(0xe2e2e2, hoverAlpha);
		var xyzColorDrag = new Color(0xffffff, dragAlpha);

		var xAxisColor = target == GizmoTarget.AxisX
			? Input.Mouse.LeftDown ? xColorDrag : xColorHover
			: xColorNormal;
		var yAxisColor = target == GizmoTarget.AxisY
			? Input.Mouse.LeftDown ? yColorDrag : yColorHover
			: yColorNormal;
		var zAxisColor = target == GizmoTarget.AxisZ
			? Input.Mouse.LeftDown ? zColorDrag : zColorHover
			: zColorNormal;

		var xzPlaneColor = target == GizmoTarget.PlaneXZ
			? Input.Mouse.LeftDown ? yColorDrag : yColorHover
			: yColorNormal;
		var yzPlaneColor = target == GizmoTarget.PlaneYZ
			? Input.Mouse.LeftDown ? xColorDrag : xColorHover
			: xColorNormal;
		var xyPlaneColor = target == GizmoTarget.PlaneXY
			? Input.Mouse.LeftDown ? zColorDrag : zColorHover
			: zColorNormal;

		var xyzCubeColor = target == GizmoTarget.CubeXYZ
			? Input.Mouse.LeftDown ? xyzColorDrag : xyzColorHover
			: xyzColorNormal;

		// X
		batch3D.Line(Vec3.UnitX * (CubeSize + Padding), Vec3.UnitX * AxisLen, xAxisColor, Transform, AxisRadius);
		batch3D.Cone(Vec3.UnitX * AxisLen, Batcher3D.Direction.X, ConeLen, ConeRadius, 12, xAxisColor, Transform);
		// Y
		batch3D.Line(Vec3.UnitY * (CubeSize + Padding), Vec3.UnitY * AxisLen, yAxisColor, Transform, AxisRadius);
		batch3D.Cone(Vec3.UnitY * AxisLen, Batcher3D.Direction.Y, ConeLen, ConeRadius, 12, yAxisColor, Transform);
		// Z
		batch3D.Line(Vec3.UnitZ * (CubeSize + Padding), Vec3.UnitZ * AxisLen, zAxisColor, Transform, AxisRadius);
		batch3D.Cone(Vec3.UnitZ * AxisLen, Batcher3D.Direction.Z, ConeLen, ConeRadius, 12, zAxisColor, Transform);

		// XZ
		batch3D.Square(Vec3.UnitX * (CubeSize + AxisLen / 2.0f) + Vec3.UnitZ * (CubeSize + AxisLen / 2.0f),
					   Vec3.UnitY, xzPlaneColor, Transform, PlaneSize / 2.0f);
		// YZ
		batch3D.Square(Vec3.UnitY * (CubeSize + AxisLen / 2.0f) + Vec3.UnitZ * (CubeSize + AxisLen / 2.0f),
					   Vec3.UnitX, yzPlaneColor, Transform, PlaneSize / 2.0f);
		// XY
		batch3D.Square(Vec3.UnitX * (CubeSize + AxisLen / 2.0f) + Vec3.UnitY * (CubeSize + AxisLen / 2.0f),
					   Vec3.UnitZ, xyPlaneColor, Transform, PlaneSize / 2.0f);

		// XYZ
		batch3D.Cube(Vec3.Zero, xyzCubeColor, Transform, CubeSize);
	}

	private static readonly (BoundingBox Bounds, GizmoTarget Target)[] GizmoTargets = [
		(XAxisBounds, GizmoTarget.AxisX),
		(YAxisBounds, GizmoTarget.AxisY),
		(ZAxisBounds, GizmoTarget.AxisZ),

		(XZPlaneBounds, GizmoTarget.PlaneXZ),
		(YZPlaneBounds, GizmoTarget.PlaneYZ),
		(XYPlaneBounds, GizmoTarget.PlaneXY),

		(XYZCubeBounds, GizmoTarget.CubeXYZ),
	];

	public override bool RaycastCheck(Vec3 origin, Vec3 direction)
	{
		float closestGizmo = float.PositiveInfinity;

		target = GizmoTarget.None;
		foreach (var (checkBounds, checkTarget) in GizmoTargets)
		{
			if (!ModUtils.RayIntersectOBB(origin, direction, checkBounds, Transform, out float dist) || dist >= closestGizmo)
				continue;

			target = checkTarget;
			closestGizmo = dist;
		}

		return target != GizmoTarget.None;
	}
	
	public override void DragStart()
	{
		beforeDragPosition = getPosition();
	}

	public override void Drag(EditorWorld editor, Vec2 mouseDelta, Vec3 mouseRay)
	{
		var axisMatrix = Transform * editor.Camera.ViewProjection;
		var screenXAxis = Vec3.TransformNormal(Vec3.UnitX, axisMatrix).XY();
		var screenYAxis = Vec3.TransformNormal(Vec3.UnitY, axisMatrix).XY();
		var screenZAxis = Vec3.TransformNormal(Vec3.UnitZ, axisMatrix).XY();
		// Flip Y, since down is positive in screen coords
		screenXAxis.Y *= -1.0f;
		screenYAxis.Y *= -1.0f;
		screenZAxis.Y *= -1.0f;

		// Linear scalar for the movement. Chosen on what felt best.
		const float dotScale = 1.0f / 50.0f;
		float dotX = Vec2.Dot(mouseDelta, screenXAxis) * dotScale;
		float dotY = Vec2.Dot(mouseDelta, screenYAxis) * dotScale;
		float dotZ = Vec2.Dot(mouseDelta, screenZAxis) * dotScale;

		Vec3 newPosition = getPosition();

		var xzPlaneDelta = Vec3.Transform(XZPlaneBounds.Center, Transform) - newPosition;
		var yzPlaneDelta = Vec3.Transform(YZPlaneBounds.Center, Transform) - newPosition;
		var xyPlaneDelta = Vec3.Transform(XYPlaneBounds.Center, Transform) - newPosition;

		var cameraPlaneNormal = (editor.Camera.Position - beforeDragPosition).Normalized();
		var cameraPlane = new Plane(cameraPlaneNormal, Vec3.Dot(cameraPlaneNormal, beforeDragPosition));

		switch (target)
		{
			case GizmoTarget.AxisX:
				newPosition = beforeDragPosition + Vec3.UnitX * dotX;
				break;
			case GizmoTarget.AxisY:
				newPosition = beforeDragPosition + Vec3.UnitY * dotY;
				break;
			case GizmoTarget.AxisZ:
				newPosition = beforeDragPosition + Vec3.UnitZ * dotZ;
				break;

			case GizmoTarget.PlaneXZ:
				float tY = (beforeDragPosition.Y - editor.Camera.Position.Y) / mouseRay.Y;
				newPosition = editor.Camera.Position + mouseRay * tY - xzPlaneDelta;
				break;
			case GizmoTarget.PlaneYZ:
				float tX = (beforeDragPosition.X - editor.Camera.Position.X) / mouseRay.X;
				newPosition = editor.Camera.Position + mouseRay * tX - yzPlaneDelta;
				break;
			case GizmoTarget.PlaneXY:
				float tZ = (beforeDragPosition.Z - editor.Camera.Position.Z) / mouseRay.Z;
				newPosition = editor.Camera.Position + mouseRay * tZ - xyPlaneDelta;
				break;

			case GizmoTarget.CubeXYZ:
				if (ModUtils.RayIntersectsPlane(editor.Camera.Position, mouseRay, cameraPlane, out var hit))
				{
					newPosition = hit;
				}
				break;

			case GizmoTarget.None:
			default:
				break;
		}

		setPosition(newPosition);
	}
}

