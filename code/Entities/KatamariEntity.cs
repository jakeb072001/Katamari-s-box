using Sandbox;

[Library( "katamari_ent", Title = "Katamari", Spawnable = true )]
public partial class KatamariEntity : Prop
{
	private float physicsScale;
	public override void Spawn()
	{
		base.Spawn();
		physicsScale = 0;
		SetModel( "models/katamari/katamari.vmdl" );
		Tags.Add( "dontkatamari" );
		EnableSelfCollisions = false;
	}

	protected override void UpdatePropData( Model model )
	{
		base.UpdatePropData( model );
	}

	public override void StartTouch( Entity other )
	{
		if ( !other.Tags.Has( "dontkatamari" ) && other != null && other is not WorldEntity )
		{
			createPropfromEntity( other );
			other.Delete();
			base.StartTouch( other );
		}
	}

	public void createPropfromEntity( Entity ent )
	{
		Prop modelRead = ent as Prop;

		var position = ent.Position;
		var rotation = ent.Rotation;
		var scale = ent.Scale;
		var model = "";
		var color = new Color();
		if ( modelRead != null )
		{
			model = modelRead.GetModelName();
			color = modelRead.RenderColor;
		}

		var entProp = new Prop
		{
			Position = position,
			Rotation = rotation,
			Scale = scale,
			RenderColor = color,
			Health = -1,
			//Parent = this
		};
		entProp.SetModel( model );

		entProp.Tags.Add( "dontkatamari" );

		var thisBody = PhysicsBody;
		var otherBody = entProp.PhysicsBody;
		var otherCollisionBounds = entProp.CollisionBounds;
		var thisCollisionBounds = CollisionBounds;

		if ( otherBody == null )
			return;

		if ( thisBody == null )
			return;

		physicsScale += 3f;

		var test = otherBody.AddCapsuleShape( otherCollisionBounds.Mins / 2f, otherCollisionBounds.Maxs / 2f, entProp.Scale / 2f, true );
		test.DisableTraceQuery();
		otherBody.RemoveShape( otherBody.GetShape( 0 ) );

		thisBody.AddSphereShape( new Vector3( thisCollisionBounds.Center.x, thisCollisionBounds.Center.y, thisCollisionBounds.Center.z - 17.5f ), Scale + 17f + physicsScale, true );
		thisBody.RemoveShape( thisBody.GetShape( 0 ) );

		// We want traces to now think the other body is now this body
		otherBody.Parent = thisBody;

		// Disable solid collisions on other prop so things attached with constraints wont collide with it
		entProp.EnableSolidCollisions = false;

		// Merge all other shapes from other body into this body
		for ( int shapeIndex = 0; shapeIndex < otherBody.ShapeCount; ++shapeIndex )
		{
			var clonedShape = thisBody.AddCloneShape( otherBody.GetShape( shapeIndex ) );

			// We don't want to be able to trace this cloned shape (but we want it to generate contacts)
			clonedShape.DisableTraceQuery();
		}

		// Visually parent other prop to this prop
		entProp.Parent = this;

		thisBody.RebuildMass();

		//WeldProp( entProp );
	}
}
