
namespace Celeste64;

public class Actor
{
	protected World? world = null;
	protected Vec3 position;
	protected Vec2 facing = -Vec2.UnitY;
	protected Vec3 forward;
	protected Matrix matrix;
	protected BoundingBox localBounds;
	protected BoundingBox worldBounds;
	protected bool dirty = true;

	/// <summary>
	/// Optional GroupName, used by Strawberries to check what unlocks them. Can
	/// be used by other stuff for whatever.
	/// </summary>
	public string GroupName = string.Empty;

	/// <summary>
	/// The World we belong to - asserts if Destroyed
	/// </summary>
	public World World => world ?? throw new Exception("Actor not added to the World");

	/// <summary>
	/// If we're currently alive
	/// </summary>
	public bool Alive => world != null;

	/// <summary>
	/// If we're being destroyed
	/// </summary>
	public bool Destroying = false;

	/// <summary>
	/// If we should Update while off-screen
	/// </summary>
	public bool UpdateOffScreen = false;

	public virtual BoundingBox LocalBounds
	{
		get => localBounds;
		set
		{
			if (localBounds != value)
			{
				localBounds = value;
				dirty = true;
			}
		}
	}

	public virtual Vec3 Position
	{
		get => position;
		set
		{
			if (position != value)
			{
				position = value;
				dirty = true;
			}
		}
	}

	public Vec2 Facing
	{
		get => facing;
		set
		{
			if (facing != value)
			{
				facing = value;
				dirty = true;
			}
		}
	}

	public virtual Vec3 Forward
	{
		get
		{
			ValidateTransformations();
			return forward;
		}
	}

	public virtual Matrix Matrix
	{
		get
		{
			ValidateTransformations();
			return matrix;
		}
	}

	public virtual BoundingBox WorldBounds
	{
		get
		{
			ValidateTransformations();
			return worldBounds;
		}
	}

	public virtual void SetWorld(World? world)
	{
		if (world != null && this.world != null)
			throw new Exception("Actor is already assigned to a World");
		this.world = world;
	}

	public virtual void ValidateTransformations()
	{
		if (!dirty)
			return;
		dirty = false;

		matrix =
			Matrix.CreateRotationZ(facing.Angle() + MathF.PI / 2) *
			Matrix.CreateTranslation(position);
		worldBounds = BoundingBox.Transform(localBounds, matrix);
		forward = Vec3.TransformNormal(-Vec3.UnitY, matrix);

		Transformed();
	}

	public virtual void Created() {}
	public virtual void Added() { }
	public virtual void Update() {}
	public virtual void LateUpdate() {}
	public virtual void Destroyed() {}

	/// <summary>
	/// Called when we move
	/// </summary>
	public virtual void Transformed() {}
}
