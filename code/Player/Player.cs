using Sandbox;
using System;

partial class SandboxPlayer : Player
{
	private TimeSince timeSinceJumpReleased;
	private Prop katamari;
	private int rotateLeft = 0;
	private int rotateForward = 0;

	[Net] public PawnController VehicleController { get; set; }
	[Net] public PawnAnimator VehicleAnimator { get; set; }
	[Net, Predicted] public ICamera VehicleCamera { get; set; }
	[Net, Predicted] public Entity Vehicle { get; set; }
	[Net, Predicted] public ICamera MainCamera { get; set; }

	protected virtual Vector3 LightOffset => Vector3.Forward * 10;
	public ICamera LastCamera { get; set; }

	public SandboxPlayer()
	{
		Inventory = new Inventory( this );
	}

	public override void Spawn()
	{
		MainCamera = new ThirdPersonCamera();
		LastCamera = MainCamera;

		base.Spawn();
	}

	public override void Respawn()
	{
		//SetModel( "models/ball/ball.vmdl" );
		//RenderAlpha = 0;

		katamari = new Prop // probably need to have the prop as a root prop, not parented to anything. Maybe do this by creating the prop and changing its position based of player controls?
		{
			Position = 0
		};
		katamari.SetModel( "models/ball/ball.vmdl" );
		//katamari.SetModel( "models/citizen_props/balloonears01.vmdl" ); // balloon is easier to check if rotation is correct
		katamari.SetParent( this, true );
		katamari.EnableSolidCollisions = true;

		Controller = new WalkController();
		Animator = new StandardPlayerAnimator();

		MainCamera = LastCamera;
		Camera = MainCamera;

		EnableAllCollisions = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;

		base.Respawn();
	}

	public bool TouchGround()
	{
		var p = Position;
		var vd = Vector3.Down;
		return Trace.Ray( p, p + vd * 20 ).Radius( 1 ).Ignore( this ).Run().Hit;
	}

	public override PawnController GetActiveController()
	{
		if ( VehicleController != null ) return VehicleController;
		if ( DevController != null ) return DevController;

		return base.GetActiveController();
	}

	public override PawnAnimator GetActiveAnimator()
	{
		if ( VehicleAnimator != null ) return VehicleAnimator;

		return base.GetActiveAnimator();
	}

	public ICamera GetActiveCamera()
	{
		if ( VehicleCamera != null ) return VehicleCamera;

		return MainCamera;
	}

	protected override void OnPhysicsCollision( CollisionEventData eventData )
	{
		// idk, try to get the entity and weld it to the katamari?
	}

	public override void Simulate( Client cl )
	{
		base.Simulate( cl );

		if ( Input.ActiveChild != null )
		{
			ActiveChild = Input.ActiveChild;
		}

		if ( LifeState != LifeState.Alive )
			return;

		if ( VehicleController != null && DevController is NoclipController )
		{
			DevController = null;
		}

		var controller = GetActiveController();
		if ( controller != null )
			EnableSolidCollisions = !controller.HasTag( "noclip" );

		TickPlayerUse();
		SimulateActiveChild( cl, ActiveChild );

		Camera = GetActiveCamera();

		if ( Input.Released( InputButton.Jump ) )
		{
			if ( timeSinceJumpReleased < 0.3f )
			{
				Game.Current?.DoPlayerNoclip( cl );
			}

			timeSinceJumpReleased = 0;
		}

		if ( Input.Left != 0 || Input.Forward != 0 )
		{
			timeSinceJumpReleased = 1;
			this.Rotation = Rotation.From( new Angles( rotateForward, 0, rotateLeft ) );
			//this.LocalRotation = Rotation.From( new Angles( rotateForward, 0, rotateLeft ) );
		}

		if ( Input.Left == 1 )
		{
			rotateLeft -= 2;
		}
		if ( Input.Left == -1 )
		{
			rotateLeft += 2;
		}
		if ( Input.Forward == 1 )
		{
			rotateForward += 2;
		}
		if ( Input.Forward == -1 )
		{
			rotateForward -= 2;
		}
	}

	public override void StartTouch( Entity other )
	{
		createPropfromEntity( other );
		other.Delete();
		base.StartTouch( other );
	}

	public void createPropfromEntity( Entity ent )
	{
		var position = ent.Position;
		var rotation = ent.Rotation;
		var scale = ent.Scale;
		var model = "models/ball/ball.vmdl";

		var entProp = new Prop
		{
			Position = position,
			Rotation = rotation,
			Scale = scale,
			//Parent = this
		};
		entProp.SetModel( model );

		var thisBody = katamari.PhysicsBody;
		var otherBody = entProp.PhysicsBody;

		// We want traces to now think the other body is now this body
		otherBody.Parent = thisBody;

		// Disable solid collisions on other prop so things attached with constraints wont collide with it
		entProp.EnableSolidCollisions = true;

		// Merge all other shapes from other body into this body
		for ( int shapeIndex = 0; shapeIndex < otherBody.ShapeCount; ++shapeIndex )
		{
			var clonedShape = thisBody.AddCloneShape( otherBody.GetShape( shapeIndex ) );

			// We don't want to be able to trace this cloned shape (but we want it to generate contacts)
			clonedShape.DisableTraceQuery();
		}

		// Visually parent other prop to this prop
		entProp.Parent = katamari;
		//entProp.weldParent = this;

		thisBody.RebuildMass();

		//WeldProp( entProp );
	}

	public void WeldProp( Prop other )
	{
		other.Weld( katamari );
	}
}
